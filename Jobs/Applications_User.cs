using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Xml.Linq;
using WorkerOperaCloud.Models;
using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Services;
using WorkerOperaCloud.Services.Interfaces;
using System.Data;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Reflection.PortableExecutable;
using Newtonsoft.Json;
using ConvertCsvToJson;
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Applications_User : IServiceJobs
    {
        private readonly ILogger<Applications_User> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";
        private readonly IMain _Conversor;

        public Applications_User(ILogger<Applications_User> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec, IMain Conversor)
        {
            _logger = logger;
            _context = context;
            _Job_Exec = Job_Exec;
            _Conversor = Conversor;
        }

        public void IntegrarJobAsync(string filePath, string id_job, string TIPO_AGENDAMENTO)
        {
            LoggBuilder.Clear();
            _stopwatch.Reset();
            _stopwatch.Start();
            DataExecucao = DateTime.Now.ToString();
            erros = "0";
            string IDEXEC = "";
            string queryInsert = "";
            string queryUpdate = "";
            long registros = 0;
            long regIntegrados = 0;
            long regAtualizados = 0;
            _TIPO_AGENDAMENTO = TIPO_AGENDAMENTO;
            OracleCommand command = new();

            LoggBuilder.AppendLine($"\n   ################## {DateTime.Now} INICIANDO INTEGRAÇÃO {TIPO_AGENDAMENTO} ##################\n");

            var List = _Job_Exec.Get_List<Mdl_Applications_User>(filePath, LoggBuilder);

            registros = List.Count;
            if (List.Count > 0)
            {
                try
                {
                    Mdl_Job_Exec jobex = new()
                    {
                        ID_JOB = id_job,
                        JOB = TIPO_AGENDAMENTO,
                        RESORT = "",
                        INICIO = DataExecucao
                    };

                    IDEXEC = _Job_Exec.InsertJobExec(jobex);

                    LoggBuilder.AppendLine($"      {DateTime.Now} Criando conexão com banco de dados...");

                    command.Connection = _context.CreateConnection();

                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Abrindo conexão...");
                    command.Connection.Open();

                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Integrando...\n");

                    foreach (var trial in List)
                    {
                        try
                        {
                            var _where = $"APP_USER_ID = '{trial.APP_USER_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Applications_User>(query);

                            if (result.ToList().Count > 0)
                            {
                                if (result.ToList().Count > 1)
                                {
                                    erros = "1";
                                    LoggBuilder.AppendLine($"  ******  {DateTime.Now}  Registro duplicado, QTD: {result.ToList().Count}. \n{trial.Serialize()} ****** ");
                                }
                                else
                                {
                                    var bdUpdate = Convert.ToDateTime(result.FirstOrDefault().RNA_UPDATEDATE);//data vinda do banco
                                    var xmlUpdate = Convert.ToDateTime(trial.RNA_UPDATEDATE);//data vinda do xml
                                    var update = DateTime.Compare(xmlUpdate, bdUpdate);

                                    if (update == 1 || trial.DELETED_FLAG == "Y")//se data xml maior entao update
                                    {
                                        if (regAtualizados == 0)
                                            queryUpdate = _Job_Exec.GetQuery(TIPO_AGENDAMENTO, "U", LoggBuilder);

                                        try
                                        {
                                            command.CommandText = (queryUpdate + _where);
                                            MountParametros(command, trial);
                                            command.ExecuteNonQuery();
                                            regAtualizados++;
                                        }
                                        catch (Exception ex)
                                        {
                                            erros = "1";
                                            LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                            _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                        }

                                        command.Parameters.Clear();
                                    }
                                }
                            }
                            else //insert registros
                            {
                                if (regIntegrados == 0)
                                    queryInsert = _Job_Exec.GetQuery(TIPO_AGENDAMENTO, "I", LoggBuilder);

                                try
                                {
                                    command.CommandText = queryInsert;
                                    MountParametros(command, trial);
                                    command.ExecuteNonQuery();
                                    regIntegrados++;
                                }
                                catch (Exception ex)
                                {
                                    erros = "1";
                                    LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                    _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {ex.Message}.\n{trial.Serialize()}");
                                }

                                command.Parameters.Clear();
                            }
                        }
                        catch (Exception e)
                        {
                            erros = "1";
                            LoggBuilder.AppendLine($"\n      {DateTime.Now}  Erro Exception: {e.Message}");
                        }
                    }

                    command.Connection.Close();
                }
                catch (Exception e)
                {
                    erros = "1";
                    command.Parameters.Clear();
                    if (command.Connection.State == System.Data.ConnectionState.Open)
                        command.Connection.Close();
                    LoggBuilder.AppendLine($"\n      {DateTime.Now} Erro Exception: {e.Message}");
                }
            }
            else
            {
                if (erros == "0") { LoggBuilder.AppendLine($"\n      {DateTime.Now} Arquivo vazio, Sem registros para integrar."); }
            }
            LoggBuilder.AppendLine($"      {DateTime.Now} Integração Finalizada...");
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Fechando conexão...");
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Total de registros no arquivo {filePath.Split("\\")[filePath.Split("\\").Length - 1]}: {registros} registros.");
            LoggBuilder.AppendLine($"      {DateTime.Now} Total de registros integrados: {regIntegrados} registros.");
            LoggBuilder.AppendLine($"      {DateTime.Now} Total de registros atualizados: {regAtualizados} registros.");

            _stopwatch.Stop();
            LoggBuilder.AppendLine($"\n      {DateTime.Now} Tempo de integração: {_stopwatch.Elapsed.ToString().Split(".")[0]}.");

            LoggBuilder.AppendLine($"\n   ################## {DateTime.Now} FIM INTEGRAÇÂO {TIPO_AGENDAMENTO} ##################");
            _logger.LogInformation($"\n   ################## {DateTime.Now} FIM INTEGRAÇÂO {TIPO_AGENDAMENTO} ##################");

            _Job_Exec.InsertLog(id_job, TIPO_AGENDAMENTO, erros, _stopwatch, LoggBuilder, DataExecucao);

            _Job_Exec.DeletJobExec(IDEXEC);

        }

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Applications_User Trxs_Codes)
        {
            parametros.Parameters.Add("APP_USER_ID", Trxs_Codes.APP_USER_ID);
            parametros.Parameters.Add("APP_USER", Trxs_Codes.APP_USER);
            parametros.Parameters.Add("APP_PASSWORD", Trxs_Codes.APP_PASSWORD);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("ORACLE_UID", Trxs_Codes.ORACLE_UID);
            parametros.Parameters.Add("ORACLE_USER", Trxs_Codes.ORACLE_USER);
            parametros.Parameters.Add("ORACLE_PASSWORD", Trxs_Codes.ORACLE_PASSWORD);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("TITLE", Trxs_Codes.TITLE);
            parametros.Parameters.Add("DEFAULT_FORM", Trxs_Codes.DEFAULT_FORM);
            parametros.Parameters.Add("NAME", Trxs_Codes.NAME);
            parametros.Parameters.Add("APP_USER_TYPE", Trxs_Codes.APP_USER_TYPE);
            parametros.Parameters.Add("LAST_LOGGED_RESORT", Trxs_Codes.LAST_LOGGED_RESORT);
            parametros.Parameters.Add("DEF_CASHIER_ID", Trxs_Codes.DEF_CASHIER_ID);
            parametros.Parameters.Add("APP_USER_DESCRIPTION", Trxs_Codes.APP_USER_DESCRIPTION);
            parametros.Parameters.Add("PERSON_NAME_ID", Trxs_Codes.PERSON_NAME_ID);
            parametros.Parameters.Add("DISABLED_UNTIL", Trxs_Codes.DISABLED_UNTIL.ToString());
            parametros.Parameters.Add("EXPIRES_ON", Trxs_Codes.EXPIRES_ON.ToString());
            parametros.Parameters.Add("LAST_LOGGED_TIMESTAMP", Trxs_Codes.LAST_LOGGED_TIMESTAMP.ToString());
            parametros.Parameters.Add("IS_SUPERUSER", Trxs_Codes.IS_SUPERUSER);
            parametros.Parameters.Add("EMPLOYEE_NUMBER", Trxs_Codes.EMPLOYEE_NUMBER);
            parametros.Parameters.Add("GENERAL_FILEPATH", Trxs_Codes.GENERAL_FILEPATH);
            parametros.Parameters.Add("USER_FILEPATH", Trxs_Codes.USER_FILEPATH);
            parametros.Parameters.Add("DEFAULT_RESORT", Trxs_Codes.DEFAULT_RESORT);
            parametros.Parameters.Add("MAX_USER_SESSIONS", Trxs_Codes.MAX_USER_SESSIONS);
            parametros.Parameters.Add("INTERNAL_YN", Trxs_Codes.INTERNAL_YN);
            parametros.Parameters.Add("MAX_CHECKOUT_DAYS", Trxs_Codes.MAX_CHECKOUT_DAYS);
            parametros.Parameters.Add("DEFAULT_TERMINAL", Trxs_Codes.DEFAULT_TERMINAL);
            parametros.Parameters.Add("DEFAULT_LANGUAGE", Trxs_Codes.DEFAULT_LANGUAGE);
            parametros.Parameters.Add("DEPT_ID", Trxs_Codes.DEPT_ID);
            parametros.Parameters.Add("MALE_FEMALE", Trxs_Codes.MALE_FEMALE);
            parametros.Parameters.Add("USER_PBX_ID", Trxs_Codes.USER_PBX_ID);
            parametros.Parameters.Add("DATE_HIRED", Trxs_Codes.DATE_HIRED.ToString());
            parametros.Parameters.Add("WORK_PERMIT_NO", Trxs_Codes.WORK_PERMIT_NO);
            parametros.Parameters.Add("WORK_PERMIT_EXPDATE", Trxs_Codes.WORK_PERMIT_EXPDATE.ToString());
            parametros.Parameters.Add("RATE_TYPE", Trxs_Codes.RATE_TYPE);
            parametros.Parameters.Add("SALARY_INTERVAL", Trxs_Codes.SALARY_INTERVAL);
            parametros.Parameters.Add("HOURLY_RATE", Trxs_Codes.HOURLY_RATE);
            parametros.Parameters.Add("WEEKLY_SALARY", Trxs_Codes.WEEKLY_SALARY);
            parametros.Parameters.Add("OT_MULTIPLIER", Trxs_Codes.OT_MULTIPLIER);
            parametros.Parameters.Add("HIRE_TYPE", Trxs_Codes.HIRE_TYPE);
            parametros.Parameters.Add("REHIRE_YN", Trxs_Codes.REHIRE_YN);
            parametros.Parameters.Add("EMP_EXTENSION", Trxs_Codes.EMP_EXTENSION);
            parametros.Parameters.Add("EMP_PAGER", Trxs_Codes.EMP_PAGER);
            parametros.Parameters.Add("TERM_REASON", Trxs_Codes.TERM_REASON);
            parametros.Parameters.Add("TERMINATED_DATE", Trxs_Codes.TERMINATED_DATE.ToString());
            parametros.Parameters.Add("INACTIVE_REASON_CODE", Trxs_Codes.INACTIVE_REASON_CODE);
            parametros.Parameters.Add("INACTIVE_FROM", Trxs_Codes.INACTIVE_FROM.ToString());
            parametros.Parameters.Add("INACTIVE_TO", Trxs_Codes.INACTIVE_TO.ToString());
            parametros.Parameters.Add("WEEK_MIN", Trxs_Codes.WEEK_MIN);
            parametros.Parameters.Add("WEEK_MAX", Trxs_Codes.WEEK_MAX);
            parametros.Parameters.Add("MONDAY_MIN", Trxs_Codes.MONDAY_MIN);
            parametros.Parameters.Add("MONDAY_MAX", Trxs_Codes.MONDAY_MAX);
            parametros.Parameters.Add("TUESDAY_MIN", Trxs_Codes.TUESDAY_MIN);
            parametros.Parameters.Add("TUESDAY_MAX", Trxs_Codes.TUESDAY_MAX);
            parametros.Parameters.Add("WEDNESDAY_MIN", Trxs_Codes.WEDNESDAY_MIN);
            parametros.Parameters.Add("WEDNESDAY_MAX", Trxs_Codes.WEDNESDAY_MAX);
            parametros.Parameters.Add("THURSDAY_MIN", Trxs_Codes.THURSDAY_MIN);
            parametros.Parameters.Add("THURSDAY_MAX", Trxs_Codes.THURSDAY_MAX);
            parametros.Parameters.Add("FRIDAY_MIN", Trxs_Codes.FRIDAY_MIN);
            parametros.Parameters.Add("FRIDAY_MAX", Trxs_Codes.FRIDAY_MAX);
            parametros.Parameters.Add("SATURDAY_MIN", Trxs_Codes.SATURDAY_MIN);
            parametros.Parameters.Add("SATURDAY_MAX", Trxs_Codes.SATURDAY_MAX);
            parametros.Parameters.Add("SUNDAY_MIN", Trxs_Codes.SUNDAY_MIN);
            parametros.Parameters.Add("SUNDAY_MAX", Trxs_Codes.SUNDAY_MAX);
            parametros.Parameters.Add("COMMENTS", Trxs_Codes.COMMENTS);
            parametros.Parameters.Add("LEAD_ADDRESS", Trxs_Codes.LEAD_ADDRESS);
            parametros.Parameters.Add("LEAD_COMM", Trxs_Codes.LEAD_COMM);
            parametros.Parameters.Add("LEAD_ADDRESS_DET", Trxs_Codes.LEAD_ADDRESS_DET);
            parametros.Parameters.Add("LAPTOP_ID", Trxs_Codes.LAPTOP_ID);
            parametros.Parameters.Add("HOURS_PER_WEEK", Trxs_Codes.HOURS_PER_WEEK);
            parametros.Parameters.Add("EMP_STATUS", Trxs_Codes.EMP_STATUS);
            parametros.Parameters.Add("PASSWORD_LAST_CHANGE", Trxs_Codes.PASSWORD_LAST_CHANGE.ToString());
            parametros.Parameters.Add("PASSWORD_CHANGE_DAYS", Trxs_Codes.PASSWORD_CHANGE_DAYS);
            parametros.Parameters.Add("GRACE_LOGIN", Trxs_Codes.GRACE_LOGIN);
            parametros.Parameters.Add("SREP_GROUP", Trxs_Codes.SREP_GROUP);
            parametros.Parameters.Add("DEFAULT_REPORTGROUP", Trxs_Codes.DEFAULT_REPORTGROUP);
            parametros.Parameters.Add("AUTHORIZER_YN", Trxs_Codes.AUTHORIZER_YN);
            parametros.Parameters.Add("AUTHORIZER_INACTIVE_DATE", Trxs_Codes.AUTHORIZER_INACTIVE_DATE.ToString());
            parametros.Parameters.Add("SFA_NAME", Trxs_Codes.SFA_NAME);
            parametros.Parameters.Add("LOGIN_CRO", Trxs_Codes.LOGIN_CRO);
            parametros.Parameters.Add("AUTHORIZER_RATE_CODE", Trxs_Codes.AUTHORIZER_RATE_CODE);
            parametros.Parameters.Add("LOGIN_DOMAIN", Trxs_Codes.LOGIN_DOMAIN);
            parametros.Parameters.Add("RECEIVE_BROADCAST_MSG", Trxs_Codes.RECEIVE_BROADCAST_MSG);
            parametros.Parameters.Add("DEFAULT_MFN_RESORT", Trxs_Codes.DEFAULT_MFN_RESORT);
            parametros.Parameters.Add("MFN_USER_TYPE", Trxs_Codes.MFN_USER_TYPE);
            parametros.Parameters.Add("FORCE_PASSWORD_CHANGE_YN", Trxs_Codes.FORCE_PASSWORD_CHANGE_YN);
            parametros.Parameters.Add("ACCOUNT_LOCKED_OUT_YN", Trxs_Codes.ACCOUNT_LOCKED_OUT_YN);
            parametros.Parameters.Add("PREVENT_ACCOUNT_LOCKOUT", Trxs_Codes.PREVENT_ACCOUNT_LOCKOUT);
            parametros.Parameters.Add("LOCKOUT_DATE", Trxs_Codes.LOCKOUT_DATE.ToString());
            parametros.Parameters.Add("ACCESS_PMS", Trxs_Codes.ACCESS_PMS);
            parametros.Parameters.Add("ACCESS_SC", Trxs_Codes.ACCESS_SC);
            parametros.Parameters.Add("ACCESS_CONFIG", Trxs_Codes.ACCESS_CONFIG);
            parametros.Parameters.Add("ACCESS_EOD", Trxs_Codes.ACCESS_EOD);
            parametros.Parameters.Add("ACCESS_UTIL", Trxs_Codes.ACCESS_UTIL);
            parametros.Parameters.Add("ACCESS_ORS", Trxs_Codes.ACCESS_ORS);
            parametros.Parameters.Add("ACCESS_SFA", Trxs_Codes.ACCESS_SFA);
            parametros.Parameters.Add("ACCESS_OCIS", Trxs_Codes.ACCESS_OCIS);
            parametros.Parameters.Add("ACCESS_OCM", Trxs_Codes.ACCESS_OCM);
            parametros.Parameters.Add("ACCESS_OXI", Trxs_Codes.ACCESS_OXI);
            parametros.Parameters.Add("ACCESS_OXIHUB", Trxs_Codes.ACCESS_OXIHUB);
            parametros.Parameters.Add("CHAIN_CODE", Trxs_Codes.CHAIN_CODE);
            parametros.Parameters.Add("APP_USER_UNIQ", Trxs_Codes.APP_USER_UNIQ);
            parametros.Parameters.Add("MAX_DAYS_AFTER_CO", Trxs_Codes.MAX_DAYS_AFTER_CO);
            parametros.Parameters.Add("USER_GROUP_ADMIN", Trxs_Codes.USER_GROUP_ADMIN);
            parametros.Parameters.Add("ACCESS_ORMS", Trxs_Codes.ACCESS_ORMS);
            parametros.Parameters.Add("ACCESS_OBI", Trxs_Codes.ACCESS_OBI);
            parametros.Parameters.Add("SREP_CODE", Trxs_Codes.SREP_CODE);
            parametros.Parameters.Add("LOGIN_ATTEMPTS", Trxs_Codes.LOGIN_ATTEMPTS);
            parametros.Parameters.Add("PROPERTY_ACCESS_YN", Trxs_Codes.PROPERTY_ACCESS_YN);
            parametros.Parameters.Add("ACCESS_SCBI", Trxs_Codes.ACCESS_SCBI);
            parametros.Parameters.Add("TIMEZONE_REGION", Trxs_Codes.TIMEZONE_REGION);
            parametros.Parameters.Add("ACCESS_OCRM", Trxs_Codes.ACCESS_OCRM);
            parametros.Parameters.Add("EMPLOYEE_INCENTIVE_NUMBER", Trxs_Codes.EMPLOYEE_INCENTIVE_NUMBER);
            parametros.Parameters.Add("SERVICE_REQUEST_ALERTS_YN", Trxs_Codes.SERVICE_REQUEST_ALERTS_YN);
            parametros.Parameters.Add("MOBILE_ALERTS_YN", Trxs_Codes.MOBILE_ALERTS_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
