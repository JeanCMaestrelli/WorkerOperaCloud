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
using ConvertCsvToJson;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using System.Threading.Channels;
using System.Security.Policy;
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Allotment_HeaderJob : IServiceJobs
    {
        private readonly ILogger<Allotment_HeaderJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";
        private readonly IMain _Conversor;

        public Allotment_HeaderJob(ILogger<Allotment_HeaderJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec,IMain Conversor)
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

            var List = _Job_Exec.Get_List<Mdl_Allotment_Header>(filePath, LoggBuilder);

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
                    int x = 0;
                    foreach (var trial in List)
                    {
                        x++;
                        try
                        {
                            var _where = $"RESORT = '{trial.RESORT}' and " +
                                        $"ALLOTMENT_HEADER_ID = '{trial.ALLOTMENT_HEADER_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Allotment_Header>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Allotment_Header Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("ALLOTMENT_HEADER_ID", Trxs_Codes.ALLOTMENT_HEADER_ID);
            parametros.Parameters.Add("ALLOTMENT_TYPE", Trxs_Codes.ALLOTMENT_TYPE);
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("SHOULDER_BEGIN_DATE", Trxs_Codes.SHOULDER_BEGIN_DATE.ToString());
            parametros.Parameters.Add("SHOULDER_END_DATE", Trxs_Codes.SHOULDER_END_DATE.ToString());
            parametros.Parameters.Add("ALLOTMENT_CODE", Trxs_Codes.ALLOTMENT_CODE);
            parametros.Parameters.Add("MASTER_NAME_ID", Trxs_Codes.MASTER_NAME_ID);
            parametros.Parameters.Add("COMPANY_NAME_ID", Trxs_Codes.COMPANY_NAME_ID);
            parametros.Parameters.Add("AGENT_NAME_ID", Trxs_Codes.AGENT_NAME_ID);
            parametros.Parameters.Add("SOURCE_NAME_ID", Trxs_Codes.SOURCE_NAME_ID);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("CANCEL_RULE", Trxs_Codes.CANCEL_RULE);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("BOOKING_STATUS", Trxs_Codes.BOOKING_STATUS);
            parametros.Parameters.Add("BOOKING_STATUS_ORDER", Trxs_Codes.BOOKING_STATUS_ORDER);
            parametros.Parameters.Add("STATUS", Trxs_Codes.STATUS);
            parametros.Parameters.Add("ELASTIC", Trxs_Codes.ELASTIC);
            parametros.Parameters.Add("INV_CUTOFF_DATE", Trxs_Codes.INV_CUTOFF_DATE.ToString());
            parametros.Parameters.Add("INV_CUTOFF_DAYS", Trxs_Codes.INV_CUTOFF_DAYS);
            parametros.Parameters.Add("TENTATIVE_LEVEL", Trxs_Codes.TENTATIVE_LEVEL);
            parametros.Parameters.Add("INFO", Trxs_Codes.INFO);
            parametros.Parameters.Add("MARKET_CODE", Trxs_Codes.MARKET_CODE);
            parametros.Parameters.Add("SOURCE", Trxs_Codes.SOURCE);
            parametros.Parameters.Add("CHANNEL", Trxs_Codes.CHANNEL);
            parametros.Parameters.Add("AVG_PEOPLE_PER_ROOM", Trxs_Codes.AVG_PEOPLE_PER_ROOM);
            parametros.Parameters.Add("ORIGINAL_RATE_CODE", Trxs_Codes.ORIGINAL_RATE_CODE);
            parametros.Parameters.Add("BOOKING_ID", Trxs_Codes.BOOKING_ID);
            parametros.Parameters.Add("CANCELLATION_NO", Trxs_Codes.CANCELLATION_NO);
            parametros.Parameters.Add("CANCELLATION_CODE", Trxs_Codes.CANCELLATION_CODE);
            parametros.Parameters.Add("CANCELLATION_DATE", Trxs_Codes.CANCELLATION_DATE.ToString());
            parametros.Parameters.Add("CANCELLATION_DESC", Trxs_Codes.CANCELLATION_DESC);
            parametros.Parameters.Add("GUARANTEE_CODE", Trxs_Codes.GUARANTEE_CODE);
            parametros.Parameters.Add("ROOMS_PER_DAY", Trxs_Codes.ROOMS_PER_DAY);
            parametros.Parameters.Add("AVERAGE_RATE", Trxs_Codes.AVERAGE_RATE);
            parametros.Parameters.Add("RESERVE_INVENTORY_YN", Trxs_Codes.RESERVE_INVENTORY_YN);
            parametros.Parameters.Add("ALLOTMENT_ORIGION", Trxs_Codes.ALLOTMENT_ORIGION);
            parametros.Parameters.Add("SUPER_BLOCK_ID", Trxs_Codes.SUPER_BLOCK_ID);
            parametros.Parameters.Add("SUPER_BLOCK_RESORT", Trxs_Codes.SUPER_BLOCK_RESORT);
            parametros.Parameters.Add("ACTION_ID", Trxs_Codes.ACTION_ID);
            parametros.Parameters.Add("DML_SEQ_NO", Trxs_Codes.DML_SEQ_NO);
            parametros.Parameters.Add("CONTACT_NAME_ID", Trxs_Codes.CONTACT_NAME_ID);
            parametros.Parameters.Add("ALIAS", Trxs_Codes.ALIAS);
            parametros.Parameters.Add("SALES_ID", Trxs_Codes.SALES_ID);
            parametros.Parameters.Add("PAYMENT_METHOD", Trxs_Codes.PAYMENT_METHOD);
            parametros.Parameters.Add("RIV_MARKET_SEGMENT", Trxs_Codes.RIV_MARKET_SEGMENT);
            parametros.Parameters.Add("EXCHANGE_POSTING_TYPE", Trxs_Codes.EXCHANGE_POSTING_TYPE);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("EXCHANGE_RATE", Trxs_Codes.EXCHANGE_RATE);
            parametros.Parameters.Add("DATE_OPENED_FOR_PICKUP", Trxs_Codes.DATE_OPENED_FOR_PICKUP.ToString());
            parametros.Parameters.Add("MAINMARKET", Trxs_Codes.MAINMARKET);
            parametros.Parameters.Add("TRACECODE", Trxs_Codes.TRACECODE);
            parametros.Parameters.Add("OWNER_RESORT", Trxs_Codes.OWNER_RESORT);
            parametros.Parameters.Add("OWNER", Trxs_Codes.OWNER);
            parametros.Parameters.Add("OWNER_CODE", Trxs_Codes.OWNER_CODE);
            parametros.Parameters.Add("RMS_OWNER_RESORT", Trxs_Codes.RMS_OWNER_RESORT);
            parametros.Parameters.Add("RMS_OWNER", Trxs_Codes.RMS_OWNER);
            parametros.Parameters.Add("RMS_OWNER_CODE", Trxs_Codes.RMS_OWNER_CODE);
            parametros.Parameters.Add("CAT_OWNER_RESORT", Trxs_Codes.CAT_OWNER_RESORT);
            parametros.Parameters.Add("CAT_OWNER", Trxs_Codes.CAT_OWNER);
            parametros.Parameters.Add("CAT_OWNER_CODE", Trxs_Codes.CAT_OWNER_CODE);
            parametros.Parameters.Add("BOOKINGTYPE", Trxs_Codes.BOOKINGTYPE);
            parametros.Parameters.Add("BOOKINGMETHOD", Trxs_Codes.BOOKINGMETHOD);
            parametros.Parameters.Add("METHOD_DUE", Trxs_Codes.METHOD_DUE.ToString());
            parametros.Parameters.Add("RMS_DECISION", Trxs_Codes.RMS_DECISION.ToString());
            parametros.Parameters.Add("RMS_FOLLOWUP", Trxs_Codes.RMS_FOLLOWUP.ToString());
            parametros.Parameters.Add("RMS_CURRENCY", Trxs_Codes.RMS_CURRENCY);
            parametros.Parameters.Add("RMS_QUOTE_CURR", Trxs_Codes.RMS_QUOTE_CURR);
            parametros.Parameters.Add("RMS_EXCHANGE", Trxs_Codes.RMS_EXCHANGE);
            parametros.Parameters.Add("ATTENDEES", Trxs_Codes.ATTENDEES);
            parametros.Parameters.Add("CAT_STATUS", Trxs_Codes.CAT_STATUS);
            parametros.Parameters.Add("CAT_DECISION", Trxs_Codes.CAT_DECISION.ToString());
            parametros.Parameters.Add("CAT_FOLLOWUP", Trxs_Codes.CAT_FOLLOWUP.ToString());
            parametros.Parameters.Add("CAT_CUTOFF", Trxs_Codes.CAT_CUTOFF.ToString());
            parametros.Parameters.Add("CAT_CURRENCY", Trxs_Codes.CAT_CURRENCY);
            parametros.Parameters.Add("CAT_QUOTE_CURR", Trxs_Codes.CAT_QUOTE_CURR);
            parametros.Parameters.Add("CAT_EXCHANGE", Trxs_Codes.CAT_EXCHANGE);
            parametros.Parameters.Add("CAT_CANX_NO", Trxs_Codes.CAT_CANX_NO);
            parametros.Parameters.Add("CAT_CANX_CODE", Trxs_Codes.CAT_CANX_CODE);
            parametros.Parameters.Add("CAT_CANX_DATE", Trxs_Codes.CAT_CANX_DATE.ToString());
            parametros.Parameters.Add("CAT_CANX_DESC", Trxs_Codes.CAT_CANX_DESC);
            parametros.Parameters.Add("INFOBOARD", Trxs_Codes.INFOBOARD);
            parametros.Parameters.Add("BFST_YN", Trxs_Codes.BFST_YN);
            parametros.Parameters.Add("BFST_PRICE", Trxs_Codes.BFST_PRICE);
            parametros.Parameters.Add("BFST_DESC", Trxs_Codes.BFST_DESC);
            parametros.Parameters.Add("PORTERAGE_YN", Trxs_Codes.PORTERAGE_YN);
            parametros.Parameters.Add("PORTERAGE_PRICE", Trxs_Codes.PORTERAGE_PRICE);
            parametros.Parameters.Add("COMMISSION", Trxs_Codes.COMMISSION);
            parametros.Parameters.Add("DETAILS_OK_YN", Trxs_Codes.DETAILS_OK_YN);
            parametros.Parameters.Add("DISTRIBUTED_YN", Trxs_Codes.DISTRIBUTED_YN);
            parametros.Parameters.Add("CONTRACT_NR", Trxs_Codes.CONTRACT_NR);
            parametros.Parameters.Add("FUNCTIONTYPE", Trxs_Codes.FUNCTIONTYPE);
            parametros.Parameters.Add("REPRESENTATIVE", Trxs_Codes.REPRESENTATIVE);
            parametros.Parameters.Add("DEFAULT_PM_RESV_NAME_ID", Trxs_Codes.DEFAULT_PM_RESV_NAME_ID);
            parametros.Parameters.Add("CATERINGONLY_YN", Trxs_Codes.CATERINGONLY_YN);
            parametros.Parameters.Add("EVENTS_GUARANTEED_YN", Trxs_Codes.EVENTS_GUARANTEED_YN);
            parametros.Parameters.Add("TAX_AMOUNT", Trxs_Codes.TAX_AMOUNT);
            parametros.Parameters.Add("SERVICE_CHARGE", Trxs_Codes.SERVICE_CHARGE);
            parametros.Parameters.Add("UDFC01", Trxs_Codes.UDFC01);
            parametros.Parameters.Add("UDFC02", Trxs_Codes.UDFC02);
            parametros.Parameters.Add("UDFC03", Trxs_Codes.UDFC03);
            parametros.Parameters.Add("UDFC04", Trxs_Codes.UDFC04);
            parametros.Parameters.Add("UDFC05", Trxs_Codes.UDFC05);
            parametros.Parameters.Add("UDFC06", Trxs_Codes.UDFC06);
            parametros.Parameters.Add("UDFC07", Trxs_Codes.UDFC07);
            parametros.Parameters.Add("UDFC08", Trxs_Codes.UDFC08);
            parametros.Parameters.Add("UDFC09", Trxs_Codes.UDFC09);
            parametros.Parameters.Add("UDFC10", Trxs_Codes.UDFC10);
            parametros.Parameters.Add("UDFD01", Trxs_Codes.UDFD01.ToString());
            parametros.Parameters.Add("UDFD02", Trxs_Codes.UDFD02.ToString());
            parametros.Parameters.Add("UDFD03", Trxs_Codes.UDFD03.ToString());
            parametros.Parameters.Add("UDFD04", Trxs_Codes.UDFD04.ToString());
            parametros.Parameters.Add("UDFD05", Trxs_Codes.UDFD05.ToString());
            parametros.Parameters.Add("UDFD06", Trxs_Codes.UDFD06.ToString());
            parametros.Parameters.Add("UDFD07", Trxs_Codes.UDFD07.ToString());
            parametros.Parameters.Add("UDFD08", Trxs_Codes.UDFD08.ToString());
            parametros.Parameters.Add("UDFD09", Trxs_Codes.UDFD09.ToString());
            parametros.Parameters.Add("UDFD10", Trxs_Codes.UDFD10.ToString());
            parametros.Parameters.Add("UDFN01", Trxs_Codes.UDFN01);
            parametros.Parameters.Add("UDFN02", Trxs_Codes.UDFN02);
            parametros.Parameters.Add("UDFN03", Trxs_Codes.UDFN03);
            parametros.Parameters.Add("UDFN04", Trxs_Codes.UDFN04);
            parametros.Parameters.Add("UDFN05", Trxs_Codes.UDFN05);
            parametros.Parameters.Add("UDFN06", Trxs_Codes.UDFN06);
            parametros.Parameters.Add("UDFN07", Trxs_Codes.UDFN07);
            parametros.Parameters.Add("UDFN08", Trxs_Codes.UDFN08);
            parametros.Parameters.Add("UDFN09", Trxs_Codes.UDFN09);
            parametros.Parameters.Add("UDFN10", Trxs_Codes.UDFN10);
            parametros.Parameters.Add("DOWNLOAD_RESORT", Trxs_Codes.DOWNLOAD_RESORT);
            parametros.Parameters.Add("DOWNLOAD_SREP", Trxs_Codes.DOWNLOAD_SREP);
            parametros.Parameters.Add("DOWNLOAD_DATE", Trxs_Codes.DOWNLOAD_DATE.ToString());
            parametros.Parameters.Add("UPLOAD_DATE", Trxs_Codes.UPLOAD_DATE.ToString());
            parametros.Parameters.Add("LAPTOP_CHANGE", Trxs_Codes.LAPTOP_CHANGE);
            parametros.Parameters.Add("EXTERNAL_REFERENCE", Trxs_Codes.EXTERNAL_REFERENCE);
            parametros.Parameters.Add("EXTERNAL_LOCKED", Trxs_Codes.EXTERNAL_LOCKED);
            parametros.Parameters.Add("PROFILE_ID", Trxs_Codes.PROFILE_ID);
            parametros.Parameters.Add("RESORT_BOOKED", Trxs_Codes.RESORT_BOOKED);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("MANUAL_CUTOFF", Trxs_Codes.MANUAL_CUTOFF);
            parametros.Parameters.Add("SNAPSHOT_SETUP", Trxs_Codes.SNAPSHOT_SETUP);
            parametros.Parameters.Add("TBD_RATES", Trxs_Codes.TBD_RATES);
            parametros.Parameters.Add("DESTINATION", Trxs_Codes.DESTINATION);
            parametros.Parameters.Add("LEAD_SOURCE", Trxs_Codes.LEAD_SOURCE);
            parametros.Parameters.Add("PROGRAM", Trxs_Codes.PROGRAM);
            parametros.Parameters.Add("COMPETITION", Trxs_Codes.COMPETITION);
            parametros.Parameters.Add("CONTROL_BLOCK_YN", Trxs_Codes.CONTROL_BLOCK_YN);
            parametros.Parameters.Add("CRS_GTD_YN", Trxs_Codes.CRS_GTD_YN);
            parametros.Parameters.Add("UDFC11", Trxs_Codes.UDFC11);
            parametros.Parameters.Add("UDFC12", Trxs_Codes.UDFC12);
            parametros.Parameters.Add("UDFC13", Trxs_Codes.UDFC13);
            parametros.Parameters.Add("UDFC14", Trxs_Codes.UDFC14);
            parametros.Parameters.Add("UDFC15", Trxs_Codes.UDFC15);
            parametros.Parameters.Add("UDFC16", Trxs_Codes.UDFC16);
            parametros.Parameters.Add("UDFC17", Trxs_Codes.UDFC17);
            parametros.Parameters.Add("UDFC18", Trxs_Codes.UDFC18);
            parametros.Parameters.Add("UDFC19", Trxs_Codes.UDFC19);
            parametros.Parameters.Add("UDFC20", Trxs_Codes.UDFC20);
            parametros.Parameters.Add("UDFC21", Trxs_Codes.UDFC21);
            parametros.Parameters.Add("UDFC22", Trxs_Codes.UDFC22);
            parametros.Parameters.Add("UDFC23", Trxs_Codes.UDFC23);
            parametros.Parameters.Add("UDFC24", Trxs_Codes.UDFC24);
            parametros.Parameters.Add("UDFC25", Trxs_Codes.UDFC25);
            parametros.Parameters.Add("UDFC26", Trxs_Codes.UDFC26);
            parametros.Parameters.Add("UDFC27", Trxs_Codes.UDFC27);
            parametros.Parameters.Add("UDFC28", Trxs_Codes.UDFC28);
            parametros.Parameters.Add("UDFC29", Trxs_Codes.UDFC29);
            parametros.Parameters.Add("UDFC30", Trxs_Codes.UDFC30);
            parametros.Parameters.Add("UDFC31", Trxs_Codes.UDFC31);
            parametros.Parameters.Add("UDFC32", Trxs_Codes.UDFC32);
            parametros.Parameters.Add("UDFC33", Trxs_Codes.UDFC33);
            parametros.Parameters.Add("UDFC34", Trxs_Codes.UDFC34);
            parametros.Parameters.Add("UDFC35", Trxs_Codes.UDFC35);
            parametros.Parameters.Add("UDFC36", Trxs_Codes.UDFC36);
            parametros.Parameters.Add("UDFC37", Trxs_Codes.UDFC37);
            parametros.Parameters.Add("UDFC38", Trxs_Codes.UDFC38);
            parametros.Parameters.Add("UDFC39", Trxs_Codes.UDFC39);
            parametros.Parameters.Add("UDFC40", Trxs_Codes.UDFC40);
            parametros.Parameters.Add("UDFD11", Trxs_Codes.UDFD11.ToString());
            parametros.Parameters.Add("UDFD12", Trxs_Codes.UDFD12.ToString());
            parametros.Parameters.Add("UDFD13", Trxs_Codes.UDFD13.ToString());
            parametros.Parameters.Add("UDFD14", Trxs_Codes.UDFD14.ToString());
            parametros.Parameters.Add("UDFD15", Trxs_Codes.UDFD15.ToString());
            parametros.Parameters.Add("UDFD16", Trxs_Codes.UDFD16.ToString());
            parametros.Parameters.Add("UDFD17", Trxs_Codes.UDFD17.ToString());
            parametros.Parameters.Add("UDFD18", Trxs_Codes.UDFD18.ToString());
            parametros.Parameters.Add("UDFD19", Trxs_Codes.UDFD19.ToString());
            parametros.Parameters.Add("UDFD20", Trxs_Codes.UDFD20.ToString());
            parametros.Parameters.Add("UDFN11", Trxs_Codes.UDFN11);
            parametros.Parameters.Add("UDFN12", Trxs_Codes.UDFN12);
            parametros.Parameters.Add("UDFN13", Trxs_Codes.UDFN13);
            parametros.Parameters.Add("UDFN14", Trxs_Codes.UDFN14);
            parametros.Parameters.Add("UDFN15", Trxs_Codes.UDFN15);
            parametros.Parameters.Add("UDFN16", Trxs_Codes.UDFN16);
            parametros.Parameters.Add("UDFN17", Trxs_Codes.UDFN17);
            parametros.Parameters.Add("UDFN18", Trxs_Codes.UDFN18);
            parametros.Parameters.Add("UDFN19", Trxs_Codes.UDFN19);
            parametros.Parameters.Add("UDFN20", Trxs_Codes.UDFN20);
            parametros.Parameters.Add("UDFN21", Trxs_Codes.UDFN21);
            parametros.Parameters.Add("UDFN22", Trxs_Codes.UDFN22);
            parametros.Parameters.Add("UDFN23", Trxs_Codes.UDFN23);
            parametros.Parameters.Add("UDFN24", Trxs_Codes.UDFN24);
            parametros.Parameters.Add("UDFN25", Trxs_Codes.UDFN25);
            parametros.Parameters.Add("UDFN26", Trxs_Codes.UDFN26);
            parametros.Parameters.Add("UDFN27", Trxs_Codes.UDFN27);
            parametros.Parameters.Add("UDFN28", Trxs_Codes.UDFN28);
            parametros.Parameters.Add("UDFN29", Trxs_Codes.UDFN29);
            parametros.Parameters.Add("UDFN30", Trxs_Codes.UDFN30);
            parametros.Parameters.Add("UDFN31", Trxs_Codes.UDFN31);
            parametros.Parameters.Add("UDFN32", Trxs_Codes.UDFN32);
            parametros.Parameters.Add("UDFN33", Trxs_Codes.UDFN33);
            parametros.Parameters.Add("UDFN34", Trxs_Codes.UDFN34);
            parametros.Parameters.Add("UDFN35", Trxs_Codes.UDFN35);
            parametros.Parameters.Add("UDFN36", Trxs_Codes.UDFN36);
            parametros.Parameters.Add("UDFN37", Trxs_Codes.UDFN37);
            parametros.Parameters.Add("UDFN38", Trxs_Codes.UDFN38);
            parametros.Parameters.Add("UDFN39", Trxs_Codes.UDFN39);
            parametros.Parameters.Add("UDFN40", Trxs_Codes.UDFN40);
            parametros.Parameters.Add("SYNCHRONIZE_YN", Trxs_Codes.SYNCHRONIZE_YN);
            parametros.Parameters.Add("ORIGINAL_BEGIN_DATE", Trxs_Codes.ORIGINAL_BEGIN_DATE.ToString());
            parametros.Parameters.Add("ORIGINAL_END_DATE", Trxs_Codes.ORIGINAL_END_DATE.ToString());
            parametros.Parameters.Add("MTG_REVENUE", Trxs_Codes.MTG_REVENUE);
            parametros.Parameters.Add("MTG_BUDGET", Trxs_Codes.MTG_BUDGET);
            parametros.Parameters.Add("ARRIVAL_TIME", Trxs_Codes.ARRIVAL_TIME.ToString());
            parametros.Parameters.Add("DEPARTURE_TIME", Trxs_Codes.DEPARTURE_TIME.ToString());
            parametros.Parameters.Add("COMP_ROOMS_FIXED_YN", Trxs_Codes.COMP_ROOMS_FIXED_YN);
            parametros.Parameters.Add("COMP_ROOMS", Trxs_Codes.COMP_ROOMS);
            parametros.Parameters.Add("COMP_PER_STAY_YN", Trxs_Codes.COMP_PER_STAY_YN);
            parametros.Parameters.Add("COMP_ROOM_VALUE", Trxs_Codes.COMP_ROOM_VALUE);
            parametros.Parameters.Add("UDESCRIPTION", Trxs_Codes.UDESCRIPTION);
            parametros.Parameters.Add("XDESCRIPTION", Trxs_Codes.XDESCRIPTION);
            parametros.Parameters.Add("XUDESCRIPTION", Trxs_Codes.XUDESCRIPTION);
            parametros.Parameters.Add("RM_COMMISSION_1", Trxs_Codes.RM_COMMISSION_1);
            parametros.Parameters.Add("RM_COMMISSION_2", Trxs_Codes.RM_COMMISSION_2);
            parametros.Parameters.Add("FB_COMMISSION_1", Trxs_Codes.FB_COMMISSION_1);
            parametros.Parameters.Add("FB_COMMISSION_2", Trxs_Codes.FB_COMMISSION_2);
            parametros.Parameters.Add("CATERING_PKGS_YN", Trxs_Codes.CATERING_PKGS_YN);
            parametros.Parameters.Add("AGENT_CONTACT_NAME_ID", Trxs_Codes.AGENT_CONTACT_NAME_ID);
            parametros.Parameters.Add("SHOW_RATE_AMOUNT_YN", Trxs_Codes.SHOW_RATE_AMOUNT_YN);
            parametros.Parameters.Add("PRINT_RATE_YN", Trxs_Codes.PRINT_RATE_YN);
            parametros.Parameters.Add("LEAD_TYPE", Trxs_Codes.LEAD_TYPE);
            parametros.Parameters.Add("LEAD_ORIGIN", Trxs_Codes.LEAD_ORIGIN);
            parametros.Parameters.Add("DUE_DATE", Trxs_Codes.DUE_DATE.ToString());
            parametros.Parameters.Add("LEADSTATUS", Trxs_Codes.LEADSTATUS);
            parametros.Parameters.Add("SENT_YN", Trxs_Codes.SENT_YN);
            parametros.Parameters.Add("SENT_DATE", Trxs_Codes.SENT_DATE.ToString());
            parametros.Parameters.Add("SENT_VIA", Trxs_Codes.SENT_VIA);
            parametros.Parameters.Add("SENT_BY", Trxs_Codes.SENT_BY);
            parametros.Parameters.Add("REPLY_STATUS", Trxs_Codes.REPLY_STATUS);
            parametros.Parameters.Add("REPLY_DATE", Trxs_Codes.REPLY_DATE.ToString());
            parametros.Parameters.Add("REPLY_VIA", Trxs_Codes.REPLY_VIA);
            parametros.Parameters.Add("REPLY_BY", Trxs_Codes.REPLY_BY);
            parametros.Parameters.Add("DATE_PEL", Trxs_Codes.DATE_PEL.ToString());
            parametros.Parameters.Add("DATE_ACL", Trxs_Codes.DATE_ACL.ToString());
            parametros.Parameters.Add("DATE_TDL", Trxs_Codes.DATE_TDL.ToString());
            parametros.Parameters.Add("DATE_CFL", Trxs_Codes.DATE_CFL.ToString());
            parametros.Parameters.Add("DATE_LSL", Trxs_Codes.DATE_LSL.ToString());
            parametros.Parameters.Add("TDL_REASON", Trxs_Codes.TDL_REASON);
            parametros.Parameters.Add("LEAD_NEW_YN", Trxs_Codes.LEAD_NEW_YN);
            parametros.Parameters.Add("LEAD_RECEIVED_YN", Trxs_Codes.LEAD_RECEIVED_YN);
            parametros.Parameters.Add("LEADSEND1", Trxs_Codes.LEADSEND1);
            parametros.Parameters.Add("LEADSEND2", Trxs_Codes.LEADSEND2);
            parametros.Parameters.Add("LEADSEND3", Trxs_Codes.LEADSEND3);
            parametros.Parameters.Add("COM_METHOD1", Trxs_Codes.COM_METHOD1);
            parametros.Parameters.Add("COM_METHOD2", Trxs_Codes.COM_METHOD2);
            parametros.Parameters.Add("COM_METHOD3", Trxs_Codes.COM_METHOD3);
            parametros.Parameters.Add("COM_ADDRESS1", Trxs_Codes.COM_ADDRESS1);
            parametros.Parameters.Add("COM_ADDRESS2", Trxs_Codes.COM_ADDRESS2);
            parametros.Parameters.Add("COM_ADDRESS3", Trxs_Codes.COM_ADDRESS3);
            parametros.Parameters.Add("LEAD_ERROR", Trxs_Codes.LEAD_ERROR);
            parametros.Parameters.Add("RESP_TIME", Trxs_Codes.RESP_TIME);
            parametros.Parameters.Add("RESP_TIME_CODE", Trxs_Codes.RESP_TIME_CODE);
            parametros.Parameters.Add("UPDATE_DATE_EXTERNAL", Trxs_Codes.UPDATE_DATE_EXTERNAL.ToString());
            parametros.Parameters.Add("HIDE_ACC_INFO_YN", Trxs_Codes.HIDE_ACC_INFO_YN);
            parametros.Parameters.Add("PENDING_SEND_YN", Trxs_Codes.PENDING_SEND_YN);
            parametros.Parameters.Add("SEND_TO_CENTRAL_YN", Trxs_Codes.SEND_TO_CENTRAL_YN);
            parametros.Parameters.Add("CREDIT_CARD_ID", Trxs_Codes.CREDIT_CARD_ID);
            parametros.Parameters.Add("SYNC_CONTRACT_YN", Trxs_Codes.SYNC_CONTRACT_YN);
            parametros.Parameters.Add("EXCLUSION_MESSAGE", Trxs_Codes.EXCLUSION_MESSAGE);
            parametros.Parameters.Add("POT_ROOM_NIGHTS", Trxs_Codes.POT_ROOM_NIGHTS);
            parametros.Parameters.Add("POT_ROOM_REVENUE", Trxs_Codes.POT_ROOM_REVENUE);
            parametros.Parameters.Add("POT_FB_REVENUE", Trxs_Codes.POT_FB_REVENUE);
            parametros.Parameters.Add("POT_OTHER_REVENUE", Trxs_Codes.POT_OTHER_REVENUE);
            parametros.Parameters.Add("COMMISSIONABLE_YN", Trxs_Codes.COMMISSIONABLE_YN);
            parametros.Parameters.Add("COMMISSIONABLE_PERC", Trxs_Codes.COMMISSIONABLE_PERC);
            parametros.Parameters.Add("FIT_DISCOUNT_PERC", Trxs_Codes.FIT_DISCOUNT_PERC);
            parametros.Parameters.Add("FIT_DISCOUNT_LEVEL", Trxs_Codes.FIT_DISCOUNT_LEVEL);
            parametros.Parameters.Add("BFST_INCL_YN", Trxs_Codes.BFST_INCL_YN);
            parametros.Parameters.Add("BFST_INCL_PRICE", Trxs_Codes.BFST_INCL_PRICE);
            parametros.Parameters.Add("SERVICE_INCL_YN", Trxs_Codes.SERVICE_INCL_YN);
            parametros.Parameters.Add("SERVICE_PERC", Trxs_Codes.SERVICE_PERC);
            parametros.Parameters.Add("DBL_RM_SUPPLEMENT_YN", Trxs_Codes.DBL_RM_SUPPLEMENT_YN);
            parametros.Parameters.Add("DBL_RM_SUPPLEMENT_PRICE", Trxs_Codes.DBL_RM_SUPPLEMENT_PRICE);
            parametros.Parameters.Add("TAX_INCLUDED_YN", Trxs_Codes.TAX_INCLUDED_YN);
            parametros.Parameters.Add("TAX_INCLUDED_PERC", Trxs_Codes.TAX_INCLUDED_PERC);
            parametros.Parameters.Add("CENTRAL_OWNER", Trxs_Codes.CENTRAL_OWNER);
            parametros.Parameters.Add("RATE_OVERRIDE", Trxs_Codes.RATE_OVERRIDE);
            parametros.Parameters.Add("SELL_THRU_YN", Trxs_Codes.SELL_THRU_YN);
            parametros.Parameters.Add("SERVICE_FEE_YN", Trxs_Codes.SERVICE_FEE_YN);
            parametros.Parameters.Add("SERVICE_FEE", Trxs_Codes.SERVICE_FEE);
            parametros.Parameters.Add("CAT_ITEM_DISCOUNT", Trxs_Codes.CAT_ITEM_DISCOUNT);
            parametros.Parameters.Add("BEGIN_DATE_ORIGINAL", Trxs_Codes.BEGIN_DATE_ORIGINAL.ToString());
            parametros.Parameters.Add("END_DATE_ORIGINAL", Trxs_Codes.END_DATE_ORIGINAL.ToString());
            parametros.Parameters.Add("ROOMING_LIST_RULES", Trxs_Codes.ROOMING_LIST_RULES);
            parametros.Parameters.Add("FLAT_RATE_YN", Trxs_Codes.FLAT_RATE_YN);
            parametros.Parameters.Add("TOURCODE", Trxs_Codes.TOURCODE);
            parametros.Parameters.Add("BLOCK_TYPE", Trxs_Codes.BLOCK_TYPE);
            parametros.Parameters.Add("GREEK_CONTRACT_NR", Trxs_Codes.GREEK_CONTRACT_NR);
            parametros.Parameters.Add("TA_RECORD_LOCATOR", Trxs_Codes.TA_RECORD_LOCATOR);
            parametros.Parameters.Add("UALIAS", Trxs_Codes.UALIAS);
            parametros.Parameters.Add("RATE_OVERRIDE_REASON", Trxs_Codes.RATE_OVERRIDE_REASON);
            parametros.Parameters.Add("ORIGINAL_BEGIN_DATE_HOLIDEX", Trxs_Codes.ORIGINAL_BEGIN_DATE_HOLIDEX.ToString());
            parametros.Parameters.Add("PUBLISH_RATES_YN", Trxs_Codes.PUBLISH_RATES_YN);
            parametros.Parameters.Add("TAX_TYPE", Trxs_Codes.TAX_TYPE);
            parametros.Parameters.Add("ATTACHMENT_URL", Trxs_Codes.ATTACHMENT_URL);
            parametros.Parameters.Add("LEADCHANGE_BYPROPERTY_YN", Trxs_Codes.LEADCHANGE_BYPROPERTY_YN);
            parametros.Parameters.Add("KEEP_LEAD_CONTROL_YN", Trxs_Codes.KEEP_LEAD_CONTROL_YN);
            parametros.Parameters.Add("DUE_DATE_ORD", Trxs_Codes.DUE_DATE_ORD.ToString());
            parametros.Parameters.Add("ALLOW_ALTERNATE_DATES_YN", Trxs_Codes.ALLOW_ALTERNATE_DATES_YN);
            parametros.Parameters.Add("REGENERATED_LEAD_YN", Trxs_Codes.REGENERATED_LEAD_YN);
            parametros.Parameters.Add("SUB_ALLOTMENT_YN", Trxs_Codes.SUB_ALLOTMENT_YN);
            parametros.Parameters.Add("BEO_LAST_PRINT", Trxs_Codes.BEO_LAST_PRINT.ToString());
            parametros.Parameters.Add("ORMS_BLOCK_CLASS", Trxs_Codes.ORMS_BLOCK_CLASS);
            parametros.Parameters.Add("LOST_TO_PROPERTY", Trxs_Codes.LOST_TO_PROPERTY);
            parametros.Parameters.Add("CXL_PENALTY", Trxs_Codes.CXL_PENALTY);
            parametros.Parameters.Add("ORMS_FINAL_BLOCK", Trxs_Codes.ORMS_FINAL_BLOCK);
            parametros.Parameters.Add("FIT_DISCOUNT_TYPE", Trxs_Codes.FIT_DISCOUNT_TYPE);
            parametros.Parameters.Add("ORMS_TRANSIENT_BLOCK", Trxs_Codes.ORMS_TRANSIENT_BLOCK);
            parametros.Parameters.Add("HLX_DEPOSIT_DAYS", Trxs_Codes.HLX_DEPOSIT_DAYS);
            parametros.Parameters.Add("HLX_CANX_NOTICE_DAYS", Trxs_Codes.HLX_CANX_NOTICE_DAYS);
            parametros.Parameters.Add("HLX_RETURN_EACH_DAY_YN", Trxs_Codes.HLX_RETURN_EACH_DAY_YN);
            parametros.Parameters.Add("HLX_COMMISSIONABLE_YN", Trxs_Codes.HLX_COMMISSIONABLE_YN);
            parametros.Parameters.Add("HLX_DI_SECURED_YN", Trxs_Codes.HLX_DI_SECURED_YN);
            parametros.Parameters.Add("HLX_DD_SECURED_YN", Trxs_Codes.HLX_DD_SECURED_YN);
            parametros.Parameters.Add("HLX_RATES_GNR_SECURED_YN", Trxs_Codes.HLX_RATES_GNR_SECURED_YN);
            parametros.Parameters.Add("HLX_RATE_ALL_SECURED_YN", Trxs_Codes.HLX_RATE_ALL_SECURED_YN);
            parametros.Parameters.Add("HLX_HOUSINGINFO_SECURED_YN", Trxs_Codes.HLX_HOUSINGINFO_SECURED_YN);
            parametros.Parameters.Add("ISAC_OPPTY_ID", Trxs_Codes.ISAC_OPPTY_ID);
            parametros.Parameters.Add("LINK_DATE", Trxs_Codes.LINK_DATE.ToString());
            parametros.Parameters.Add("TLP_RESPONSEID", Trxs_Codes.TLP_RESPONSEID);
            parametros.Parameters.Add("TLP_URL", Trxs_Codes.TLP_URL);
            parametros.Parameters.Add("DISTRIBUTED_DATE", Trxs_Codes.DISTRIBUTED_DATE.ToString());
            parametros.Parameters.Add("FB_AGENDA_CURR", Trxs_Codes.FB_AGENDA_CURR);
            parametros.Parameters.Add("FIT_CONTRACT_MODE", Trxs_Codes.FIT_CONTRACT_MODE);
            parametros.Parameters.Add("PROPOSAL_SHOW_SPACENAME_YN", Trxs_Codes.PROPOSAL_SHOW_SPACENAME_YN);
            parametros.Parameters.Add("PROPOSAL_SHOW_EVENTPRICE_YN", Trxs_Codes.PROPOSAL_SHOW_EVENTPRICE_YN);
            parametros.Parameters.Add("PROPOSAL_OWNER_SELECTION", Trxs_Codes.PROPOSAL_OWNER_SELECTION);
            parametros.Parameters.Add("PROPOSAL_DECISION_SELECTION", Trxs_Codes.PROPOSAL_DECISION_SELECTION);
            parametros.Parameters.Add("PROPOSAL_SENT_DATE", Trxs_Codes.PROPOSAL_SENT_DATE.ToString());
            parametros.Parameters.Add("PROPOSAL_VIEW_TOKEN", Trxs_Codes.PROPOSAL_VIEW_TOKEN);
            parametros.Parameters.Add("ALLOTMENT_CLASSIFICATION", Trxs_Codes.ALLOTMENT_CLASSIFICATION);
            parametros.Parameters.Add("SUPER_SEARCH_INDEX_TEXT", Trxs_Codes.SUPER_SEARCH_INDEX_TEXT);
            parametros.Parameters.Add("RATE_PROTECTION", Trxs_Codes.RATE_PROTECTION);
            parametros.Parameters.Add("NON_COMPETE", Trxs_Codes.NON_COMPETE);
            parametros.Parameters.Add("CONVERSION_CODE", Trxs_Codes.CONVERSION_CODE);
            parametros.Parameters.Add("RANKING_CODE", Trxs_Codes.RANKING_CODE);
            parametros.Parameters.Add("NON_COMPETE_CODE", Trxs_Codes.NON_COMPETE_CODE);
            parametros.Parameters.Add("RATE_GUARANTEED_YN", Trxs_Codes.RATE_GUARANTEED_YN);
            parametros.Parameters.Add("PROPOSAL_SHOW_PMS_ROOM_TYPE_YN", Trxs_Codes.PROPOSAL_SHOW_PMS_ROOM_TYPE_YN);
            parametros.Parameters.Add("SC_QUOTE_ID", Trxs_Codes.SC_QUOTE_ID);
            parametros.Parameters.Add("OFFSET_TYPE", Trxs_Codes.OFFSET_TYPE);
            parametros.Parameters.Add("PROPOSAL_FOLLOWUP_SELECTION", Trxs_Codes.PROPOSAL_FOLLOWUP_SELECTION);
            parametros.Parameters.Add("PROPOSAL_INCL_ALT_NAMES_YN", Trxs_Codes.PROPOSAL_INCL_ALT_NAMES_YN);
            parametros.Parameters.Add("ORIG_ALLOTMENT_HEADER_ID", Trxs_Codes.ORIG_ALLOTMENT_HEADER_ID);
            parametros.Parameters.Add("WEB_BOOKABLE_YN", Trxs_Codes.WEB_BOOKABLE_YN);
            parametros.Parameters.Add("PROPOSAL_COMBINE_EVENTS_YN", Trxs_Codes.PROPOSAL_COMBINE_EVENTS_YN);
            parametros.Parameters.Add("PROPOSAL_SPACE_MEASUREMENT", Trxs_Codes.PROPOSAL_SPACE_MEASUREMENT);
            parametros.Parameters.Add("AUTO_LOAD_FORECAST_YN", Trxs_Codes.AUTO_LOAD_FORECAST_YN);
            parametros.Parameters.Add("ORMS_FORECAST_REVIEW_REASON", Trxs_Codes.ORMS_FORECAST_REVIEW_REASON);
            parametros.Parameters.Add("MAR_HOUSE_PROTECT_YN", Trxs_Codes.MAR_HOUSE_PROTECT_YN);
            parametros.Parameters.Add("MAR_ROLL_END_DATE_YN", Trxs_Codes.MAR_ROLL_END_DATE_YN);
            parametros.Parameters.Add("MAR_EVENT_TYPE", Trxs_Codes.MAR_EVENT_TYPE);
            parametros.Parameters.Add("FS_OVERBOOKING_YN", Trxs_Codes.FS_OVERBOOKING_YN);
            parametros.Parameters.Add("BLOCK_TRX_CODE", Trxs_Codes.BLOCK_TRX_CODE);
            parametros.Parameters.Add("BWI_LEAD_ID", Trxs_Codes.BWI_LEAD_ID);
            parametros.Parameters.Add("BWI_URL", Trxs_Codes.BWI_URL);
            parametros.Parameters.Add("GIID", Trxs_Codes.GIID);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
