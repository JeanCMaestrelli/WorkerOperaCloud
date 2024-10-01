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
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Membership_Types : IServiceJobs
    {
        private readonly ILogger<Membership_Types> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Membership_Types(ILogger<Membership_Types> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Membership_Types>(filePath, LoggBuilder);

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
                            var _where = $"MEMBERSHIP_TYPE = '{trial.MEMBERSHIP_TYPE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Membership_Types>(query);

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
                            LoggBuilder.AppendLine($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {e.Message}.\n{trial.Serialize()}");
                            _logger.LogInformation($"      - {DateTime.Now} Erro ao inserir registro {TIPO_AGENDAMENTO}, Exception: {e.Message}.\n{trial.Serialize()}");
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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Membership_Types Trxs_Codes)
        {
            parametros.Parameters.Add("MEMBERSHIP_TYPE", Trxs_Codes.MEMBERSHIP_TYPE);
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("MEMBERSHIP_CLASS", Trxs_Codes.MEMBERSHIP_CLASS);
            parametros.Parameters.Add("CARD_PREFIX", Trxs_Codes.CARD_PREFIX);
            parametros.Parameters.Add("CARD_LENGTH", Trxs_Codes.CARD_LENGTH);
            parametros.Parameters.Add("CALCULATION_METHOD", Trxs_Codes.CALCULATION_METHOD);
            parametros.Parameters.Add("CALCULATION_MONTHS", Trxs_Codes.CALCULATION_MONTHS);
            parametros.Parameters.Add("EXPIRATION_MONTH", Trxs_Codes.EXPIRATION_MONTH);
            parametros.Parameters.Add("NUMERIC_VALIDATION", Trxs_Codes.NUMERIC_VALIDATION);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("POINTS_LABEL", Trxs_Codes.POINTS_LABEL);
            parametros.Parameters.Add("FOLIO_MESSAGE", Trxs_Codes.FOLIO_MESSAGE);
            parametros.Parameters.Add("COST_PER_POINT", Trxs_Codes.COST_PER_POINT);
            parametros.Parameters.Add("CENTRAL_SETUP_YN", Trxs_Codes.CENTRAL_SETUP_YN);
            parametros.Parameters.Add("TRANSACTION_MAX_POINTS", Trxs_Codes.TRANSACTION_MAX_POINTS);
            parametros.Parameters.Add("POINTS_ISSUED_CENTRALLY_YN", Trxs_Codes.POINTS_ISSUED_CENTRALLY_YN);
            parametros.Parameters.Add("MEMBERSHIP_ACTION", Trxs_Codes.MEMBERSHIP_ACTION);
            parametros.Parameters.Add("ALLOW_SHARES_YN", Trxs_Codes.ALLOW_SHARES_YN);
            parametros.Parameters.Add("ALLOW_ADHOC_MULTIPLIER_YN", Trxs_Codes.ALLOW_ADHOC_MULTIPLIER_YN);
            parametros.Parameters.Add("UDF_CARD_VALIDATION_YN", Trxs_Codes.UDF_CARD_VALIDATION_YN);
            parametros.Parameters.Add("AWARD_GENERATION_METHOD", Trxs_Codes.AWARD_GENERATION_METHOD);
            parametros.Parameters.Add("BATCH_DELAY_PERIOD", Trxs_Codes.BATCH_DELAY_PERIOD);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("GRACE_EXPIRATION_MONTH", Trxs_Codes.GRACE_EXPIRATION_MONTH);
            parametros.Parameters.Add("CAN_DELETE_YN", Trxs_Codes.CAN_DELETE_YN);
            parametros.Parameters.Add("EXCHANGE_RATE_TYPE", Trxs_Codes.EXCHANGE_RATE_TYPE);
            parametros.Parameters.Add("YEARS_TO_EXPIRE", Trxs_Codes.YEARS_TO_EXPIRE);
            parametros.Parameters.Add("FULFILMENT_YN", Trxs_Codes.FULFILMENT_YN);
            parametros.Parameters.Add("EXPIRATION_DATE_REQUIRED", Trxs_Codes.EXPIRATION_DATE_REQUIRED);
            parametros.Parameters.Add("LEVEL_REQUIRED", Trxs_Codes.LEVEL_REQUIRED);
            parametros.Parameters.Add("PRIMARY_MEMBERSHIP_YN", Trxs_Codes.PRIMARY_MEMBERSHIP_YN);
            parametros.Parameters.Add("FOLIO_MESSAGE_NONMEMBERS", Trxs_Codes.FOLIO_MESSAGE_NONMEMBERS);
            parametros.Parameters.Add("UDF_FORMULA", Trxs_Codes.UDF_FORMULA);
            parametros.Parameters.Add("CARD_VALID_YEARS", Trxs_Codes.CARD_VALID_YEARS);
            parametros.Parameters.Add("UPGRADE_PERIOD", Trxs_Codes.UPGRADE_PERIOD);
            parametros.Parameters.Add("DOWNGRADE_PERIOD", Trxs_Codes.DOWNGRADE_PERIOD);
            parametros.Parameters.Add("ALLOW_DUP_CARD_YN", Trxs_Codes.ALLOW_DUP_CARD_YN);
            parametros.Parameters.Add("EXCEPTION_TYPE", Trxs_Codes.EXCEPTION_TYPE);
            parametros.Parameters.Add("MULTIPLE_ROOMS_LIMIT", Trxs_Codes.MULTIPLE_ROOMS_LIMIT);
            parametros.Parameters.Add("BOOKER_PROGRAM_YN", Trxs_Codes.BOOKER_PROGRAM_YN);
            parametros.Parameters.Add("AUTO_CARD_NO_BASED_ON", Trxs_Codes.AUTO_CARD_NO_BASED_ON);
            parametros.Parameters.Add("MEMBER_INFO_DISP_SET", Trxs_Codes.MEMBER_INFO_DISP_SET);
            parametros.Parameters.Add("CHAIN_CODE", Trxs_Codes.CHAIN_CODE);
            parametros.Parameters.Add("DEFAULT_MEM_STATUS", Trxs_Codes.DEFAULT_MEM_STATUS);
            parametros.Parameters.Add("ENROLLMENT_CODE_REQ_YN", Trxs_Codes.ENROLLMENT_CODE_REQ_YN);
            parametros.Parameters.Add("TSC_DATE_FLAG", Trxs_Codes.TSC_DATE_FLAG);
            parametros.Parameters.Add("FOLIO_MESSAGE_NQ", Trxs_Codes.FOLIO_MESSAGE_NQ);
            parametros.Parameters.Add("FOLIO_MESSAGE_NONMEMBERS_NQ", Trxs_Codes.FOLIO_MESSAGE_NONMEMBERS_NQ);
            parametros.Parameters.Add("SEND_CHKOUT_TO_IFC", Trxs_Codes.SEND_CHKOUT_TO_IFC);
            parametros.Parameters.Add("PROMPT_AT_CHECKIN", Trxs_Codes.PROMPT_AT_CHECKIN);
            parametros.Parameters.Add("VALIDATION_BY_IFC", Trxs_Codes.VALIDATION_BY_IFC);
            parametros.Parameters.Add("EXTERNAL_PROCESS_DAYS", Trxs_Codes.EXTERNAL_PROCESS_DAYS);
            parametros.Parameters.Add("PROMPT_AT_NEW_RESERVATION", Trxs_Codes.PROMPT_AT_NEW_RESERVATION);
            parametros.Parameters.Add("PROMPT_AT_UPDATE_RESERVATION", Trxs_Codes.PROMPT_AT_UPDATE_RESERVATION);
            parametros.Parameters.Add("PROMPT_AT_CHECK_OUT", Trxs_Codes.PROMPT_AT_CHECK_OUT);
            parametros.Parameters.Add("FOLIO_MESSAGE_CREDITS", Trxs_Codes.FOLIO_MESSAGE_CREDITS);
            parametros.Parameters.Add("EXTERNALLY_CONTROLLED_YN", Trxs_Codes.EXTERNALLY_CONTROLLED_YN);
            parametros.Parameters.Add("CHIP_AND_PIN_YN", Trxs_Codes.CHIP_AND_PIN_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
