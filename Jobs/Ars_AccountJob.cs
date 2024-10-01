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
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Ars_AccountJob : IServiceJobs
    {
        private readonly ILogger<Ars_AccountJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Ars_AccountJob(ILogger<Ars_AccountJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
        {
            _logger = logger;
            _context = context;
            _Job_Exec = Job_Exec;
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

            var List = _Job_Exec.Get_List<Mdl_Ars_Account>(filePath,LoggBuilder);

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
                            var _where = $"RESORT = '{trial.RESORT}' and " +
                                    $"ACCOUNT_CODE = '{trial.ACCOUNT_CODE}' and " +
                                    $"ACCOUNT_NO = '{trial.ACCOUNT_NO}' and " +
                                    $"ACCOUNT_TYPE_ID = '{trial.ACCOUNT_TYPE_ID}' and " +
                                    $"ACCOUNT_NAME = '{trial.ACCOUNT_NAME}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Ars_Account>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Ars_Account Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("ACCOUNT_CODE", Trxs_Codes.ACCOUNT_CODE);
            parametros.Parameters.Add("ACCOUNT_NO", Trxs_Codes.ACCOUNT_NO);
            parametros.Parameters.Add("ACCOUNT_TYPE_ID", Trxs_Codes.ACCOUNT_TYPE_ID);
            parametros.Parameters.Add("ACCOUNT_NAME", Trxs_Codes.ACCOUNT_NAME);
            parametros.Parameters.Add("COMPANY_NAME", Trxs_Codes.COMPANY_NAME);
            parametros.Parameters.Add("ACCOUNT_SNAME", Trxs_Codes.ACCOUNT_SNAME);
            parametros.Parameters.Add("STATUS", Trxs_Codes.STATUS);
            parametros.Parameters.Add("ADDRESS1", Trxs_Codes.ADDRESS1);
            parametros.Parameters.Add("ADDRESS2", Trxs_Codes.ADDRESS2);
            parametros.Parameters.Add("ADDRESS3", Trxs_Codes.ADDRESS3);
            parametros.Parameters.Add("CITY", Trxs_Codes.CITY);
            parametros.Parameters.Add("STATE", Trxs_Codes.STATE);
            parametros.Parameters.Add("COUNTRY", Trxs_Codes.COUNTRY);
            parametros.Parameters.Add("ZIP", Trxs_Codes.ZIP);
            parametros.Parameters.Add("PHONE", Trxs_Codes.PHONE);
            parametros.Parameters.Add("FAX", Trxs_Codes.FAX);
            parametros.Parameters.Add("CONTACT", Trxs_Codes.CONTACT);
            parametros.Parameters.Add("BATCH_STMT_YN", Trxs_Codes.BATCH_STMT_YN);
            parametros.Parameters.Add("PERM_ACCT_YN", Trxs_Codes.PERM_ACCT_YN);
            parametros.Parameters.Add("SUM_CUR_CODE", Trxs_Codes.SUM_CUR_CODE);
            parametros.Parameters.Add("CREDIT_LIMIT", Trxs_Codes.CREDIT_LIMIT);
            parametros.Parameters.Add("LST_REM_SENT", Trxs_Codes.LST_REM_SENT.ToString());
            parametros.Parameters.Add("LST_STMT_SENT", Trxs_Codes.LST_STMT_SENT.ToString());
            parametros.Parameters.Add("LST_REM_TEXT", Trxs_Codes.LST_REM_TEXT);
            parametros.Parameters.Add("AGE", Trxs_Codes.AGE);
            parametros.Parameters.Add("REMARKS", Trxs_Codes.REMARKS);
            parametros.Parameters.Add("NAME_ID", Trxs_Codes.NAME_ID);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("LAST_STMT_FAX_NO", Trxs_Codes.LAST_STMT_FAX_NO);
            parametros.Parameters.Add("LAST_REM_FAX_NO", Trxs_Codes.LAST_REM_FAX_NO);
            parametros.Parameters.Add("NO_OF_PERSONS", Trxs_Codes.NO_OF_PERSONS);
            parametros.Parameters.Add("BALANCE", Trxs_Codes.BALANCE);
            parametros.Parameters.Add("LST_REM_PRT_DATE", Trxs_Codes.LST_REM_PRT_DATE.ToString());
            parametros.Parameters.Add("ACCOUNT_STATUS", Trxs_Codes.ACCOUNT_STATUS);
            parametros.Parameters.Add("EMAIL_ADDRESS", Trxs_Codes.EMAIL_ADDRESS);
            parametros.Parameters.Add("ACC_TYPE_FLAG", Trxs_Codes.ACC_TYPE_FLAG);
            parametros.Parameters.Add("ACCOUNT_STATUS_MSG", Trxs_Codes.ACCOUNT_STATUS_MSG);
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("LST_STMT_NO_SENT", Trxs_Codes.LST_STMT_NO_SENT);
            parametros.Parameters.Add("AGENT_USER_ID", Trxs_Codes.AGENT_USER_ID);
            parametros.Parameters.Add("REVENUE_POOL", Trxs_Codes.REVENUE_POOL);
            parametros.Parameters.Add("ADDRESS_ID", Trxs_Codes.ADDRESS_ID);
            parametros.Parameters.Add("PHONE_ID", Trxs_Codes.PHONE_ID);
            parametros.Parameters.Add("FAX_ID", Trxs_Codes.FAX_ID);
            parametros.Parameters.Add("EMAIL_ID", Trxs_Codes.EMAIL_ID);
            parametros.Parameters.Add("PRIMARY_YN", Trxs_Codes.PRIMARY_YN);
            parametros.Parameters.Add("PAYMENT_DUE_DAYS", Trxs_Codes.PAYMENT_DUE_DAYS);
            parametros.Parameters.Add("CREDIT_LIMIT_UPDATED_ON", Trxs_Codes.CREDIT_LIMIT_UPDATED_ON.ToString());
            parametros.Parameters.Add("FLAGGED_REASON_CODE", Trxs_Codes.FLAGGED_REASON_CODE);
            parametros.Parameters.Add("LAST_ACTIVITY_DATE", Trxs_Codes.LAST_ACTIVITY_DATE.ToString());
            parametros.Parameters.Add("MONTH_END_CALC_YN", Trxs_Codes.MONTH_END_CALC_YN);
            parametros.Parameters.Add("ACCOUNT_CREDIT_LIMIT_YN", Trxs_Codes.ACCOUNT_CREDIT_LIMIT_YN);
            parametros.Parameters.Add("EMAIL_OPT_IN_YN", Trxs_Codes.EMAIL_OPT_IN_YN);
            parametros.Parameters.Add("SUPER_SEARCH_INDEX_TEXT", Trxs_Codes.SUPER_SEARCH_INDEX_TEXT);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }
    }
}
