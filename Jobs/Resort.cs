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
using System.IO;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Resort : IServiceJobs
    {
        private readonly ILogger<Resort> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";


        public Resort(ILogger<Resort> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Resort>(filePath, LoggBuilder);

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
                            var _where = $"RESORT = '{trial.RESORT}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Resort>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Resort Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("NAME", Trxs_Codes.NAME);
            parametros.Parameters.Add("LEGAL_OWNER", Trxs_Codes.LEGAL_OWNER);
            parametros.Parameters.Add("COUNTRY_CODE", Trxs_Codes.COUNTRY_CODE);
            parametros.Parameters.Add("CRS_RESORT", Trxs_Codes.CRS_RESORT);
            parametros.Parameters.Add("STREET", Trxs_Codes.STREET);
            parametros.Parameters.Add("POST_CODE", Trxs_Codes.POST_CODE);
            parametros.Parameters.Add("CITY", Trxs_Codes.CITY);
            parametros.Parameters.Add("STATE", Trxs_Codes.STATE);
            parametros.Parameters.Add("TELEPHONE", Trxs_Codes.TELEPHONE);
            parametros.Parameters.Add("FAX", Trxs_Codes.FAX);
            parametros.Parameters.Add("WEBADDRESS", Trxs_Codes.WEBADDRESS);
            parametros.Parameters.Add("TOLLFREE", Trxs_Codes.TOLLFREE);
            parametros.Parameters.Add("EMAIL", Trxs_Codes.EMAIL);
            parametros.Parameters.Add("KEEP_AVAILABILITY", Trxs_Codes.KEEP_AVAILABILITY);
            parametros.Parameters.Add("BUDGET_MONTH", Trxs_Codes.BUDGET_MONTH);
            parametros.Parameters.Add("SAVE_PROFILES", Trxs_Codes.SAVE_PROFILES);
            parametros.Parameters.Add("DEFAULT_RESERVATION_TYPE", Trxs_Codes.DEFAULT_RESERVATION_TYPE);
            parametros.Parameters.Add("BLOCK", Trxs_Codes.BLOCK);
            parametros.Parameters.Add("CURRENCY_SYMBOL", Trxs_Codes.CURRENCY_SYMBOL);
            parametros.Parameters.Add("SOURCE_COMMISSION", Trxs_Codes.SOURCE_COMMISSION);
            parametros.Parameters.Add("TA_COMMISSION", Trxs_Codes.TA_COMMISSION);
            parametros.Parameters.Add("CHECK_IN_TIME", Trxs_Codes.CHECK_IN_TIME.ToString());
            parametros.Parameters.Add("CHECK_OUT_TIME", Trxs_Codes.CHECK_OUT_TIME.ToString());
            parametros.Parameters.Add("LONG_STAY_CONTROL", Trxs_Codes.LONG_STAY_CONTROL);
            parametros.Parameters.Add("CREDIT_LIMIT", Trxs_Codes.CREDIT_LIMIT);
            parametros.Parameters.Add("NUMBER_ROOMS", Trxs_Codes.NUMBER_ROOMS);
            parametros.Parameters.Add("NUMBER_FLOORS", Trxs_Codes.NUMBER_FLOORS);
            parametros.Parameters.Add("NUMBER_BEDS", Trxs_Codes.NUMBER_BEDS);
            parametros.Parameters.Add("RHYTHM_SHEETS", Trxs_Codes.RHYTHM_SHEETS);
            parametros.Parameters.Add("RHYTHM_TOWELS", Trxs_Codes.RHYTHM_TOWELS);
            parametros.Parameters.Add("BASE_LANGUAGE", Trxs_Codes.BASE_LANGUAGE);
            parametros.Parameters.Add("FOLIO_LANGUAGE1", Trxs_Codes.FOLIO_LANGUAGE1);
            parametros.Parameters.Add("FOLIO_LANGUAGE2", Trxs_Codes.FOLIO_LANGUAGE2);
            parametros.Parameters.Add("FOLIO_LANGUAGE3", Trxs_Codes.FOLIO_LANGUAGE3);
            parametros.Parameters.Add("FOLIO_LANGUAGE4", Trxs_Codes.FOLIO_LANGUAGE4);
            parametros.Parameters.Add("WARNING_AMOUNT", Trxs_Codes.WARNING_AMOUNT);
            parametros.Parameters.Add("THOUSAND_SEPARATOR", Trxs_Codes.THOUSAND_SEPARATOR);
            parametros.Parameters.Add("PACKAGE_PROFIT", Trxs_Codes.PACKAGE_PROFIT);
            parametros.Parameters.Add("PACKAGE_LOSS", Trxs_Codes.PACKAGE_LOSS);
            parametros.Parameters.Add("DECIMAL_PLACES", Trxs_Codes.DECIMAL_PLACES);
            parametros.Parameters.Add("DECIMAL_SEPARATOR", Trxs_Codes.DECIMAL_SEPARATOR);
            parametros.Parameters.Add("SHORT_DATE_FORMAT", Trxs_Codes.SHORT_DATE_FORMAT);
            parametros.Parameters.Add("LONG_DATE_FORMAT", Trxs_Codes.LONG_DATE_FORMAT);
            parametros.Parameters.Add("DATE_SEPARATOR", Trxs_Codes.DATE_SEPARATOR);
            parametros.Parameters.Add("FONT", Trxs_Codes.FONT);
            parametros.Parameters.Add("COPIES", Trxs_Codes.COPIES);
            parametros.Parameters.Add("DEFAULT_FOLIO_STYLE", Trxs_Codes.DEFAULT_FOLIO_STYLE);
            parametros.Parameters.Add("INDIVIDUAL_ROOM_WARNING", Trxs_Codes.INDIVIDUAL_ROOM_WARNING);
            parametros.Parameters.Add("GROUP_ROOM_WARNING", Trxs_Codes.GROUP_ROOM_WARNING);
            parametros.Parameters.Add("VIDEO_CO_START", Trxs_Codes.VIDEO_CO_START.ToString());
            parametros.Parameters.Add("VIDEO_CO_STOP", Trxs_Codes.VIDEO_CO_STOP.ToString());
            parametros.Parameters.Add("PASSERBY_SOURCE", Trxs_Codes.PASSERBY_SOURCE);
            parametros.Parameters.Add("PASSERBY_MARKET", Trxs_Codes.PASSERBY_MARKET);
            parametros.Parameters.Add("AR_COMPANY", Trxs_Codes.AR_COMPANY);
            parametros.Parameters.Add("AR_AGENT", Trxs_Codes.AR_AGENT);
            parametros.Parameters.Add("AR_GROUPS", Trxs_Codes.AR_GROUPS);
            parametros.Parameters.Add("AR_INDIVIDUALS", Trxs_Codes.AR_INDIVIDUALS);
            parametros.Parameters.Add("AR_ACCT_NO_MAND_YN", Trxs_Codes.AR_ACCT_NO_MAND_YN);
            parametros.Parameters.Add("AGING_LEVEL1", Trxs_Codes.AGING_LEVEL1);
            parametros.Parameters.Add("AGING_LEVEL2", Trxs_Codes.AGING_LEVEL2);
            parametros.Parameters.Add("AGING_LEVEL3", Trxs_Codes.AGING_LEVEL3);
            parametros.Parameters.Add("AGING_LEVEL4", Trxs_Codes.AGING_LEVEL4);
            parametros.Parameters.Add("AGING_LEVEL5", Trxs_Codes.AGING_LEVEL5);
            parametros.Parameters.Add("AR_ACCT_NO_FORMAT", Trxs_Codes.AR_ACCT_NO_FORMAT);
            parametros.Parameters.Add("DATE_FOR_AGING", Trxs_Codes.DATE_FOR_AGING);
            parametros.Parameters.Add("ZERO_INV_PUR_DAYS", Trxs_Codes.ZERO_INV_PUR_DAYS);
            parametros.Parameters.Add("MIN_DAYS_BET_2_REMINDER_LETTER", Trxs_Codes.MIN_DAYS_BET_2_REMINDER_LETTER);
            parametros.Parameters.Add("ALLOWANCE_PERIOD_ADJ", Trxs_Codes.ALLOWANCE_PERIOD_ADJ);
            parametros.Parameters.Add("HOTEL_ID", Trxs_Codes.HOTEL_ID);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("CURRENCY_DECIMALS", Trxs_Codes.CURRENCY_DECIMALS);
            parametros.Parameters.Add("EXCHANGE_POSTING_TYPE", Trxs_Codes.EXCHANGE_POSTING_TYPE);
            parametros.Parameters.Add("TURNAWAY_CODE", Trxs_Codes.TURNAWAY_CODE);
            parametros.Parameters.Add("SUMM_CURRENCY_CODE", Trxs_Codes.SUMM_CURRENCY_CODE);
            parametros.Parameters.Add("FAX_NO_FORMAT", Trxs_Codes.FAX_NO_FORMAT);
            parametros.Parameters.Add("TELEPHONE_NO_FORMAT", Trxs_Codes.TELEPHONE_NO_FORMAT);
            parametros.Parameters.Add("LOCAL_CURRENCY_FORMAT", Trxs_Codes.LOCAL_CURRENCY_FORMAT);
            parametros.Parameters.Add("DEFAULT_PROPERTY_ADDRESS", Trxs_Codes.DEFAULT_PROPERTY_ADDRESS);
            parametros.Parameters.Add("DEFAULT_GUEST_ADDRESS", Trxs_Codes.DEFAULT_GUEST_ADDRESS);
            parametros.Parameters.Add("LICENSE_CODE", Trxs_Codes.LICENSE_CODE);
            parametros.Parameters.Add("EXPIRY_DATE", Trxs_Codes.EXPIRY_DATE.ToString());
            parametros.Parameters.Add("TIME_FORMAT", Trxs_Codes.TIME_FORMAT);
            parametros.Parameters.Add("NAME_ID_LINK", Trxs_Codes.NAME_ID_LINK);
            parametros.Parameters.Add("DUTY_MANAGER_PAGER", Trxs_Codes.DUTY_MANAGER_PAGER);
            parametros.Parameters.Add("CHAIN_CODE", Trxs_Codes.CHAIN_CODE);
            parametros.Parameters.Add("RESORT_TYPE", Trxs_Codes.RESORT_TYPE);
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("GENMGR", Trxs_Codes.GENMGR);
            parametros.Parameters.Add("DIRSALES", Trxs_Codes.DIRSALES);
            parametros.Parameters.Add("LEADSEND", Trxs_Codes.LEADSEND);
            parametros.Parameters.Add("AIRPORT", Trxs_Codes.AIRPORT);
            parametros.Parameters.Add("AIRPORT_DISTANCE", Trxs_Codes.AIRPORT_DISTANCE);
            parametros.Parameters.Add("AIRPORT_TIME", Trxs_Codes.AIRPORT_TIME);
            parametros.Parameters.Add("HOTEL_TYPE", Trxs_Codes.HOTEL_TYPE);
            parametros.Parameters.Add("OWNERSHIP", Trxs_Codes.OWNERSHIP);
            parametros.Parameters.Add("QUOTED_CURRENCY", Trxs_Codes.QUOTED_CURRENCY);
            parametros.Parameters.Add("COM_METHOD", Trxs_Codes.COM_METHOD);
            parametros.Parameters.Add("COM_ADDRESS", Trxs_Codes.COM_ADDRESS);
            parametros.Parameters.Add("INVENTORY_YN", Trxs_Codes.INVENTORY_YN);
            parametros.Parameters.Add("NOTES", Trxs_Codes.NOTES);
            parametros.Parameters.Add("SGL_NUM", Trxs_Codes.SGL_NUM);
            parametros.Parameters.Add("SGL_RATE1", Trxs_Codes.SGL_RATE1);
            parametros.Parameters.Add("SGL_RATE2", Trxs_Codes.SGL_RATE2);
            parametros.Parameters.Add("DBL_NUM", Trxs_Codes.DBL_NUM);
            parametros.Parameters.Add("DBL_RATE1", Trxs_Codes.DBL_RATE1);
            parametros.Parameters.Add("DBL_RATE2", Trxs_Codes.DBL_RATE2);
            parametros.Parameters.Add("TPL_NUM", Trxs_Codes.TPL_NUM);
            parametros.Parameters.Add("TPL_RATE1", Trxs_Codes.TPL_RATE1);
            parametros.Parameters.Add("TPL_RATE2", Trxs_Codes.TPL_RATE2);
            parametros.Parameters.Add("SUI_NUM", Trxs_Codes.SUI_NUM);
            parametros.Parameters.Add("SUI_RATE1", Trxs_Codes.SUI_RATE1);
            parametros.Parameters.Add("SUI_RATE2", Trxs_Codes.SUI_RATE2);
            parametros.Parameters.Add("TOT_ROOMS", Trxs_Codes.TOT_ROOMS);
            parametros.Parameters.Add("SEASON1", Trxs_Codes.SEASON1);
            parametros.Parameters.Add("SEASON2", Trxs_Codes.SEASON2);
            parametros.Parameters.Add("SEASON3", Trxs_Codes.SEASON3);
            parametros.Parameters.Add("SEASON4", Trxs_Codes.SEASON4);
            parametros.Parameters.Add("SEASON5", Trxs_Codes.SEASON5);
            parametros.Parameters.Add("HOTEL_FC", Trxs_Codes.HOTEL_FC);
            parametros.Parameters.Add("MEETING_FC", Trxs_Codes.MEETING_FC);
            parametros.Parameters.Add("BR_AREA", Trxs_Codes.BR_AREA);
            parametros.Parameters.Add("BR_SEATS", Trxs_Codes.BR_SEATS);
            parametros.Parameters.Add("MEET_SPACE", Trxs_Codes.MEET_SPACE);
            parametros.Parameters.Add("MEET_ROOMS", Trxs_Codes.MEET_ROOMS);
            parametros.Parameters.Add("MEET_SEATS", Trxs_Codes.MEET_SEATS);
            parametros.Parameters.Add("RESTAURANT", Trxs_Codes.RESTAURANT);
            parametros.Parameters.Add("IMG_HOTEL_ID", Trxs_Codes.IMG_HOTEL_ID);
            parametros.Parameters.Add("IMG_DIRECTION_ID", Trxs_Codes.IMG_DIRECTION_ID);
            parametros.Parameters.Add("IMG_MAP_ID", Trxs_Codes.IMG_MAP_ID);
            parametros.Parameters.Add("ALLOW_LOGIN_YN", Trxs_Codes.ALLOW_LOGIN_YN);
            parametros.Parameters.Add("AR_BAL_TRX_CODE", Trxs_Codes.AR_BAL_TRX_CODE);
            parametros.Parameters.Add("AR_CREDIT_TRX_CODE", Trxs_Codes.AR_CREDIT_TRX_CODE);
            parametros.Parameters.Add("AR_TYPEWRITER", Trxs_Codes.AR_TYPEWRITER);
            parametros.Parameters.Add("AR_SETTLE_CODE", Trxs_Codes.AR_SETTLE_CODE);
            parametros.Parameters.Add("CASH_SHIFT_DROP", Trxs_Codes.CASH_SHIFT_DROP);
            parametros.Parameters.Add("CHECK_EXG_PAIDOUT", Trxs_Codes.CHECK_EXG_PAIDOUT);
            parametros.Parameters.Add("CHECK_SHIFT_DROP", Trxs_Codes.CHECK_SHIFT_DROP);
            parametros.Parameters.Add("CHECK_TRXCODE", Trxs_Codes.CHECK_TRXCODE);
            parametros.Parameters.Add("CURRENCY_EXG_PAIDOUT", Trxs_Codes.CURRENCY_EXG_PAIDOUT);
            parametros.Parameters.Add("DEPOSIT_LED_TRX_CODE", Trxs_Codes.DEPOSIT_LED_TRX_CODE);
            parametros.Parameters.Add("FISCAL_START_DATE", Trxs_Codes.FISCAL_START_DATE.ToString());
            parametros.Parameters.Add("FISCAL_END_DATE", Trxs_Codes.FISCAL_END_DATE.ToString());
            parametros.Parameters.Add("FISCAL_PERIOD_TYPE", Trxs_Codes.FISCAL_PERIOD_TYPE);
            parametros.Parameters.Add("DEFAULT_COMMISSION_PERCENTAGE", Trxs_Codes.DEFAULT_COMMISSION_PERCENTAGE);
            parametros.Parameters.Add("DEFAULT_PREPAID_COMM", Trxs_Codes.DEFAULT_PREPAID_COMM);
            parametros.Parameters.Add("DEFAULT_TRX_COMM_CODE", Trxs_Codes.DEFAULT_TRX_COMM_CODE);
            parametros.Parameters.Add("FILE_TRANSFER_FORMAT", Trxs_Codes.FILE_TRANSFER_FORMAT);
            parametros.Parameters.Add("CONFIGURATION_MODE", Trxs_Codes.CONFIGURATION_MODE);
            parametros.Parameters.Add("CONFIRM_REGCARD_PRINTER", Trxs_Codes.CONFIRM_REGCARD_PRINTER);
            parametros.Parameters.Add("DEFAULT_PRINTER", Trxs_Codes.DEFAULT_PRINTER);
            parametros.Parameters.Add("DEFAULT_REGISTRATION_CARD", Trxs_Codes.DEFAULT_REGISTRATION_CARD);
            parametros.Parameters.Add("WEEKEND_DAYS", Trxs_Codes.WEEKEND_DAYS);
            parametros.Parameters.Add("DEFAULT_GROUPS_RATE_CODE", Trxs_Codes.DEFAULT_GROUPS_RATE_CODE);
            parametros.Parameters.Add("MAX_NO_NIGHTS", Trxs_Codes.MAX_NO_NIGHTS);
            parametros.Parameters.Add("AWARDS_TIMEOUT", Trxs_Codes.AWARDS_TIMEOUT);
            parametros.Parameters.Add("DEFAULT_POSTING_ROOM", Trxs_Codes.DEFAULT_POSTING_ROOM);
            parametros.Parameters.Add("GUEST_LOOKUP_TIMEOUT", Trxs_Codes.GUEST_LOOKUP_TIMEOUT);
            parametros.Parameters.Add("VIDEOCHECKOUT_PRINTER", Trxs_Codes.VIDEOCHECKOUT_PRINTER);
            parametros.Parameters.Add("WAKE_UP_DELAY", Trxs_Codes.WAKE_UP_DELAY);
            parametros.Parameters.Add("NIGHT_AUDIT_CASHIER_ID", Trxs_Codes.NIGHT_AUDIT_CASHIER_ID);
            parametros.Parameters.Add("COMPANY_ADDRESS_TYPE", Trxs_Codes.COMPANY_ADDRESS_TYPE);
            parametros.Parameters.Add("COMPANY_PHONE_TYPE", Trxs_Codes.COMPANY_PHONE_TYPE);
            parametros.Parameters.Add("DEFAULT_FAX_TYPE", Trxs_Codes.DEFAULT_FAX_TYPE);
            parametros.Parameters.Add("DEFAULT_MEMBERSHIP_TYPE", Trxs_Codes.DEFAULT_MEMBERSHIP_TYPE);
            parametros.Parameters.Add("INDIVIDUAL_ADDRESS_TYPE", Trxs_Codes.INDIVIDUAL_ADDRESS_TYPE);
            parametros.Parameters.Add("INDIVIDUAL_PHONE_TYPE", Trxs_Codes.INDIVIDUAL_PHONE_TYPE);
            parametros.Parameters.Add("DFLT_PKG_TRAN_CODE", Trxs_Codes.DFLT_PKG_TRAN_CODE);
            parametros.Parameters.Add("DFLT_TRAN_CODE_RATE_CODE", Trxs_Codes.DFLT_TRAN_CODE_RATE_CODE);
            parametros.Parameters.Add("MAX_OCCUPANCY", Trxs_Codes.MAX_OCCUPANCY);
            parametros.Parameters.Add("INACTIVE_DAYS_FOR_GUEST_PROFIL", Trxs_Codes.INACTIVE_DAYS_FOR_GUEST_PROFIL);
            parametros.Parameters.Add("DEFAULT_RATE_CODE", Trxs_Codes.DEFAULT_RATE_CODE);
            parametros.Parameters.Add("PER_RESERVATION_ROOM_LIMIT", Trxs_Codes.PER_RESERVATION_ROOM_LIMIT);
            parametros.Parameters.Add("SCRIPT_ID", Trxs_Codes.SCRIPT_ID);
            parametros.Parameters.Add("CRO_CODE", Trxs_Codes.CRO_CODE);
            parametros.Parameters.Add("FLOW_CODE", Trxs_Codes.FLOW_CODE);
            parametros.Parameters.Add("EXT_PROPERTY_CODE", Trxs_Codes.EXT_PROPERTY_CODE);
            parametros.Parameters.Add("EXT_EXP_FILE_LOCATION", Trxs_Codes.EXT_EXP_FILE_LOCATION);
            parametros.Parameters.Add("REGION_CODE", Trxs_Codes.REGION_CODE);
            parametros.Parameters.Add("OPUS_CURRENCY_CODE", Trxs_Codes.OPUS_CURRENCY_CODE);
            parametros.Parameters.Add("COM_NAME_XREF_ID", Trxs_Codes.COM_NAME_XREF_ID);
            parametros.Parameters.Add("HOTEL_CODE", Trxs_Codes.HOTEL_CODE);
            parametros.Parameters.Add("CURTAIN_COLOR", Trxs_Codes.CURTAIN_COLOR);
            parametros.Parameters.Add("RECONCILE_DATE", Trxs_Codes.RECONCILE_DATE.ToString());
            parametros.Parameters.Add("PAYMENT_DATE", Trxs_Codes.PAYMENT_DATE.ToString());
            parametros.Parameters.Add("PATH_ID", Trxs_Codes.PATH_ID);
            parametros.Parameters.Add("XRESORT_NUMBER", Trxs_Codes.XRESORT_NUMBER);
            parametros.Parameters.Add("DIRECTIONS", Trxs_Codes.DIRECTIONS);
            parametros.Parameters.Add("DESTINATION_ID", Trxs_Codes.DESTINATION_ID);
            parametros.Parameters.Add("MAX_CREDIT_DAYS", Trxs_Codes.MAX_CREDIT_DAYS);
            parametros.Parameters.Add("PATH", Trxs_Codes.PATH);
            parametros.Parameters.Add("ACCESS_CODE", Trxs_Codes.ACCESS_CODE);
            parametros.Parameters.Add("FLAGS", Trxs_Codes.FLAGS);
            parametros.Parameters.Add("TOURIST_NUMBER", Trxs_Codes.TOURIST_NUMBER);
            parametros.Parameters.Add("DISABLE_LOGIN_YN", Trxs_Codes.DISABLE_LOGIN_YN);
            parametros.Parameters.Add("INT_TAX_INCLUDED_YN", Trxs_Codes.INT_TAX_INCLUDED_YN);
            parametros.Parameters.Add("DOWNLOAD_REST_YN", Trxs_Codes.DOWNLOAD_REST_YN);
            parametros.Parameters.Add("TIMEZONE_REGION", Trxs_Codes.TIMEZONE_REGION);
            parametros.Parameters.Add("PROPINFO_URL", Trxs_Codes.PROPINFO_URL);
            parametros.Parameters.Add("LATITUDE", Trxs_Codes.LATITUDE);
            parametros.Parameters.Add("LONGITUDE", Trxs_Codes.LONGITUDE);
            parametros.Parameters.Add("TRANSLATE_MULTICHAR_YN", Trxs_Codes.TRANSLATE_MULTICHAR_YN);
            parametros.Parameters.Add("PROP_PIC_URL", Trxs_Codes.PROP_PIC_URL);
            parametros.Parameters.Add("PROP_MAP_URL", Trxs_Codes.PROP_MAP_URL);
            parametros.Parameters.Add("CATERING_CURRENCY_CODE", Trxs_Codes.CATERING_CURRENCY_CODE);
            parametros.Parameters.Add("CATERING_CURRENCY_FORMAT", Trxs_Codes.CATERING_CURRENCY_FORMAT);
            parametros.Parameters.Add("QTY_SINGLE_ROOMS", Trxs_Codes.QTY_SINGLE_ROOMS);
            parametros.Parameters.Add("QTY_DOUBLE_ROOMS", Trxs_Codes.QTY_DOUBLE_ROOMS);
            parametros.Parameters.Add("QTY_TWIN_ROOMS", Trxs_Codes.QTY_TWIN_ROOMS);
            parametros.Parameters.Add("QTY_SUITES", Trxs_Codes.QTY_SUITES);
            parametros.Parameters.Add("QTY_GUEST_ROOM_FLOORS", Trxs_Codes.QTY_GUEST_ROOM_FLOORS);
            parametros.Parameters.Add("QTY_GUEST_ELEVATORS", Trxs_Codes.QTY_GUEST_ELEVATORS);
            parametros.Parameters.Add("QTY_NON_SMOKING_ROOMS", Trxs_Codes.QTY_NON_SMOKING_ROOMS);
            parametros.Parameters.Add("QTY_CONNECTING_ROOMS", Trxs_Codes.QTY_CONNECTING_ROOMS);
            parametros.Parameters.Add("QTY_HANDICAPPED_ROOMS", Trxs_Codes.QTY_HANDICAPPED_ROOMS);
            parametros.Parameters.Add("QTY_FAMILY_ROOMS", Trxs_Codes.QTY_FAMILY_ROOMS);
            parametros.Parameters.Add("MAX_ADULTS_FAMILY_ROOM", Trxs_Codes.MAX_ADULTS_FAMILY_ROOM);
            parametros.Parameters.Add("MAX_CHILDREN_FAMILY_ROOM", Trxs_Codes.MAX_CHILDREN_FAMILY_ROOM);
            parametros.Parameters.Add("FLOOR_NUM_EXECUTIVE_FLOOR", Trxs_Codes.FLOOR_NUM_EXECUTIVE_FLOOR);
            parametros.Parameters.Add("ROOM_AMENITY", Trxs_Codes.ROOM_AMENITY);
            parametros.Parameters.Add("SHOP_DESCRIPTION", Trxs_Codes.SHOP_DESCRIPTION);
            parametros.Parameters.Add("DEFAULT_RATECODE_RACK", Trxs_Codes.DEFAULT_RATECODE_RACK);
            parametros.Parameters.Add("DEFAULT_RATECODE_PCR", Trxs_Codes.DEFAULT_RATECODE_PCR);
            parametros.Parameters.Add("BLACKOUT_PERIOD_NOTES", Trxs_Codes.BLACKOUT_PERIOD_NOTES);
            parametros.Parameters.Add("EXTERNAL_SC_YN", Trxs_Codes.EXTERNAL_SC_YN);
            parametros.Parameters.Add("SEND_LEAD_AS_BOOKING_YN", Trxs_Codes.SEND_LEAD_AS_BOOKING_YN);
            parametros.Parameters.Add("EXP_HOTEL_CODE", Trxs_Codes.EXP_HOTEL_CODE);
            parametros.Parameters.Add("LIC_ROOM_INFO", Trxs_Codes.LIC_ROOM_INFO);
            parametros.Parameters.Add("FNS_TIER", Trxs_Codes.FNS_TIER);
            parametros.Parameters.Add("BRAND_CODE", Trxs_Codes.BRAND_CODE);
            parametros.Parameters.Add("MBS_SUPPORTED_YN", Trxs_Codes.MBS_SUPPORTED_YN);
            parametros.Parameters.Add("VAT_ID", Trxs_Codes.VAT_ID);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
