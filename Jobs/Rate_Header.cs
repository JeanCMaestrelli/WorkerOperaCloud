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
    public class Rate_Header : IServiceJobs
    {
        private readonly ILogger<Rate_Header> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Rate_Header(ILogger<Rate_Header> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Rate_Header>(filePath, LoggBuilder);

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
                                        $"RATE_CODE = '{trial.RATE_CODE}' and " +
                                        $"RATE_CLASS = '{trial.RATE_CLASS}' and " +
                                        $"RATE_CATEGORY = '{trial.RATE_CATEGORY}' and " +
                                        $"SELL_SEQUENCE = '{trial.SELL_SEQUENCE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Rate_Header>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Rate_Header Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("RATE_CLASS", Trxs_Codes.RATE_CLASS);
            parametros.Parameters.Add("RATE_CATEGORY", Trxs_Codes.RATE_CATEGORY);
            parametros.Parameters.Add("LOS_UNIT", Trxs_Codes.LOS_UNIT);
            parametros.Parameters.Add("SELL_SEQUENCE", Trxs_Codes.SELL_SEQUENCE);
            parametros.Parameters.Add("PACKAGE_YN", Trxs_Codes.PACKAGE_YN);
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("FLAT_OR_PERCENTAGE", Trxs_Codes.FLAT_OR_PERCENTAGE);
            parametros.Parameters.Add("OPERATOR_TYPE", Trxs_Codes.OPERATOR_TYPE);
            parametros.Parameters.Add("BASE_RATE_CODE", Trxs_Codes.BASE_RATE_CODE);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("ALTERNATE_RATE_CODE", Trxs_Codes.ALTERNATE_RATE_CODE);
            parametros.Parameters.Add("COMMISSION_CODE", Trxs_Codes.COMMISSION_CODE);
            parametros.Parameters.Add("COMMISSION_YN", Trxs_Codes.COMMISSION_YN);
            parametros.Parameters.Add("LABEL", Trxs_Codes.LABEL);
            parametros.Parameters.Add("FOLIO_TEXT", Trxs_Codes.FOLIO_TEXT);
            parametros.Parameters.Add("RATE_INCLUDES_TAX_YN", Trxs_Codes.RATE_INCLUDES_TAX_YN);
            parametros.Parameters.Add("MARKET_CODE", Trxs_Codes.MARKET_CODE);
            parametros.Parameters.Add("SOURCE_CODE", Trxs_Codes.SOURCE_CODE);
            parametros.Parameters.Add("BACK_TO_BACK_YN", Trxs_Codes.BACK_TO_BACK_YN);
            parametros.Parameters.Add("BEGIN_BOOKING_DATE", Trxs_Codes.BEGIN_BOOKING_DATE.ToString());
            parametros.Parameters.Add("END_BOOKING_DATE", Trxs_Codes.END_BOOKING_DATE.ToString());
            parametros.Parameters.Add("YIELDABLE_YN", Trxs_Codes.YIELDABLE_YN);
            parametros.Parameters.Add("HIGHLIGHT_RATE_AMOUNT_YN", Trxs_Codes.HIGHLIGHT_RATE_AMOUNT_YN);
            parametros.Parameters.Add("SHOW_RATE_AMOUNT_YN", Trxs_Codes.SHOW_RATE_AMOUNT_YN);
            parametros.Parameters.Add("DAYUSE_YN", Trxs_Codes.DAYUSE_YN);
            parametros.Parameters.Add("PRINT_RATE_YN", Trxs_Codes.PRINT_RATE_YN);
            parametros.Parameters.Add("TRX_CODE", Trxs_Codes.TRX_CODE);
            parametros.Parameters.Add("TRX_CODE_WK", Trxs_Codes.TRX_CODE_WK);
            parametros.Parameters.Add("PKG_TRX_CODE", Trxs_Codes.PKG_TRX_CODE);
            parametros.Parameters.Add("TRX_TAX_INCL_YN", Trxs_Codes.TRX_TAX_INCL_YN);
            parametros.Parameters.Add("PKG_TRX_CODE_WK", Trxs_Codes.PKG_TRX_CODE_WK);
            parametros.Parameters.Add("PROFIT_TRX_CODE", Trxs_Codes.PROFIT_TRX_CODE);
            parametros.Parameters.Add("TRX_WK_TAX_INCL_YN", Trxs_Codes.TRX_WK_TAX_INCL_YN);
            parametros.Parameters.Add("PKG_TRX_TAX_INCL_YN", Trxs_Codes.PKG_TRX_TAX_INCL_YN);
            parametros.Parameters.Add("PKG_TRX_WK_TAX_INCL_YN", Trxs_Codes.PKG_TRX_WK_TAX_INCL_YN);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("EXCHANGE_POSTING_TYPE", Trxs_Codes.EXCHANGE_POSTING_TYPE);
            parametros.Parameters.Add("NEGOTIATED", Trxs_Codes.NEGOTIATED);
            parametros.Parameters.Add("COMPLIMENTARY_YN", Trxs_Codes.COMPLIMENTARY_YN);
            parametros.Parameters.Add("HOUSE_USE_YN", Trxs_Codes.HOUSE_USE_YN);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("POSTING_RHYTHM", Trxs_Codes.POSTING_RHYTHM);
            parametros.Parameters.Add("WEEKEND_DAYS", Trxs_Codes.WEEKEND_DAYS);
            parametros.Parameters.Add("RATE_CALENDAR_YN", Trxs_Codes.RATE_CALENDAR_YN);
            parametros.Parameters.Add("ADVANCE_BOOKING", Trxs_Codes.ADVANCE_BOOKING);
            parametros.Parameters.Add("CLOSED_TO_ARRIVAL", Trxs_Codes.CLOSED_TO_ARRIVAL);
            parametros.Parameters.Add("FREQUENT_FLYER_YN", Trxs_Codes.FREQUENT_FLYER_YN);
            parametros.Parameters.Add("MAX_LOS", Trxs_Codes.MAX_LOS);
            parametros.Parameters.Add("ADDITION", Trxs_Codes.ADDITION);
            parametros.Parameters.Add("MULTIPLICATION", Trxs_Codes.MULTIPLICATION);
            parametros.Parameters.Add("SHORT_INFO", Trxs_Codes.SHORT_INFO);
            parametros.Parameters.Add("LONG_INFO", Trxs_Codes.LONG_INFO);
            parametros.Parameters.Add("RATE_CODE_LOCKED", Trxs_Codes.RATE_CODE_LOCKED);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("RATE_BUCKET", Trxs_Codes.RATE_BUCKET);
            parametros.Parameters.Add("EXTERNAL_LOCKED", Trxs_Codes.EXTERNAL_LOCKED);
            parametros.Parameters.Add("YIELD_AS", Trxs_Codes.YIELD_AS);
            parametros.Parameters.Add("GDS_ALLOWED_YN", Trxs_Codes.GDS_ALLOWED_YN);
            parametros.Parameters.Add("LOYALTY_PROGRAM_YN", Trxs_Codes.LOYALTY_PROGRAM_YN);
            parametros.Parameters.Add("REDEMPTION_RATE_YN", Trxs_Codes.REDEMPTION_RATE_YN);
            parametros.Parameters.Add("DISPLAY_SET", Trxs_Codes.DISPLAY_SET);
            parametros.Parameters.Add("BASE_FLT_PCT", Trxs_Codes.BASE_FLT_PCT);
            parametros.Parameters.Add("BASE_ROUNDING", Trxs_Codes.BASE_ROUNDING);
            parametros.Parameters.Add("BASE_AMOUNT", Trxs_Codes.BASE_AMOUNT);
            parametros.Parameters.Add("DISTRIBUTE_YN", Trxs_Codes.DISTRIBUTE_YN);
            parametros.Parameters.Add("TIERED_YN", Trxs_Codes.TIERED_YN);
            parametros.Parameters.Add("DEPT_CODE", Trxs_Codes.DEPT_CODE);
            parametros.Parameters.Add("WK_DEPT_CODE", Trxs_Codes.WK_DEPT_CODE);
            parametros.Parameters.Add("COMMISSION_PCT", Trxs_Codes.COMMISSION_PCT);
            parametros.Parameters.Add("DISCOUNT_YN", Trxs_Codes.DISCOUNT_YN);
            parametros.Parameters.Add("GROUP_CODE", Trxs_Codes.GROUP_CODE);
            parametros.Parameters.Add("RATE_LEVEL", Trxs_Codes.RATE_LEVEL);
            parametros.Parameters.Add("ROD_YN", Trxs_Codes.ROD_YN);
            parametros.Parameters.Add("ROD_BASED_YN", Trxs_Codes.ROD_BASED_YN);
            parametros.Parameters.Add("ROD_BASE_FLT_PCT", Trxs_Codes.ROD_BASE_FLT_PCT);
            parametros.Parameters.Add("ROD_BASE_ROUNDING", Trxs_Codes.ROD_BASE_ROUNDING);
            parametros.Parameters.Add("ROD_BASE_AMOUNT", Trxs_Codes.ROD_BASE_AMOUNT);
            parametros.Parameters.Add("RATEINFO_URL", Trxs_Codes.RATEINFO_URL);
            parametros.Parameters.Add("COMMISSIONABLE_YN", Trxs_Codes.COMMISSIONABLE_YN);
            parametros.Parameters.Add("FIT_DISCOUNT_PERC", Trxs_Codes.FIT_DISCOUNT_PERC);
            parametros.Parameters.Add("BFST_INCL_YN", Trxs_Codes.BFST_INCL_YN);
            parametros.Parameters.Add("BFST_PRICE", Trxs_Codes.BFST_PRICE);
            parametros.Parameters.Add("SERVICE_INCL_YN", Trxs_Codes.SERVICE_INCL_YN);
            parametros.Parameters.Add("FIT_DISCOUNT_LEVEL", Trxs_Codes.FIT_DISCOUNT_LEVEL);
            parametros.Parameters.Add("SERVICE_PERC", Trxs_Codes.SERVICE_PERC);
            parametros.Parameters.Add("COMMISSIONABLE_PERC", Trxs_Codes.COMMISSIONABLE_PERC);
            parametros.Parameters.Add("DBL_RM_SUPPLEMENT_YN", Trxs_Codes.DBL_RM_SUPPLEMENT_YN);
            parametros.Parameters.Add("DBL_RM_SUPPLEMENT_PRICE", Trxs_Codes.DBL_RM_SUPPLEMENT_PRICE);
            parametros.Parameters.Add("TAX_INCLUDED_YN", Trxs_Codes.TAX_INCLUDED_YN);
            parametros.Parameters.Add("TAX_INCLUDED_PERC", Trxs_Codes.TAX_INCLUDED_PERC);
            parametros.Parameters.Add("DAILY_RATES_YN", Trxs_Codes.DAILY_RATES_YN);
            parametros.Parameters.Add("MAX_ADVANCE_BOOKING", Trxs_Codes.MAX_ADVANCE_BOOKING);
            parametros.Parameters.Add("BBAR_YN", Trxs_Codes.BBAR_YN);
            parametros.Parameters.Add("BBAR_BASED_YN", Trxs_Codes.BBAR_BASED_YN);
            parametros.Parameters.Add("BBAR_BASE_FLT_PCT", Trxs_Codes.BBAR_BASE_FLT_PCT);
            parametros.Parameters.Add("BBAR_BASE_ROUNDING", Trxs_Codes.BBAR_BASE_ROUNDING);
            parametros.Parameters.Add("BBAR_BASE_AMOUNT", Trxs_Codes.BBAR_BASE_AMOUNT);
            parametros.Parameters.Add("YM_CODE", Trxs_Codes.YM_CODE);
            parametros.Parameters.Add("MIN_OCCUPANCY", Trxs_Codes.MIN_OCCUPANCY);
            parametros.Parameters.Add("MAX_OCCUPANCY", Trxs_Codes.MAX_OCCUPANCY);
            parametros.Parameters.Add("AVAILABILITY_UPDATE_YN", Trxs_Codes.AVAILABILITY_UPDATE_YN);
            parametros.Parameters.Add("RATES_TO_GDS_YN", Trxs_Codes.RATES_TO_GDS_YN);
            parametros.Parameters.Add("DISPLAY_REGIONAL_YN", Trxs_Codes.DISPLAY_REGIONAL_YN);
            parametros.Parameters.Add("MFN_UPLOAD_YN", Trxs_Codes.MFN_UPLOAD_YN);
            parametros.Parameters.Add("CHANGE_STATE", Trxs_Codes.CHANGE_STATE);
            parametros.Parameters.Add("SDOW_BEGIN_BOOKING_DATE", Trxs_Codes.SDOW_BEGIN_BOOKING_DATE.ToString());
            parametros.Parameters.Add("SDOW_END_BOOKING_DATE", Trxs_Codes.SDOW_END_BOOKING_DATE.ToString());
            parametros.Parameters.Add("DBASE_RATE_CODE", Trxs_Codes.DBASE_RATE_CODE);
            parametros.Parameters.Add("DBASE_FLT_PCT", Trxs_Codes.DBASE_FLT_PCT);
            parametros.Parameters.Add("DBASE_ROUNDING", Trxs_Codes.DBASE_ROUNDING);
            parametros.Parameters.Add("DBASE_AMOUNT", Trxs_Codes.DBASE_AMOUNT);
            parametros.Parameters.Add("DBASE_COMPARE_YN", Trxs_Codes.DBASE_COMPARE_YN);
            parametros.Parameters.Add("RATE_FLOOR", Trxs_Codes.RATE_FLOOR);
            parametros.Parameters.Add("ASB_RATE_CYCLE", Trxs_Codes.ASB_RATE_CYCLE);
            parametros.Parameters.Add("ADV_BASE_RATE_CODE", Trxs_Codes.ADV_BASE_RATE_CODE);
            parametros.Parameters.Add("ADV_BASE_ROUNDING", Trxs_Codes.ADV_BASE_ROUNDING);
            parametros.Parameters.Add("PENDING_APPROVAL_YN", Trxs_Codes.PENDING_APPROVAL_YN);
            parametros.Parameters.Add("UPSELL_YN", Trxs_Codes.UPSELL_YN);
            parametros.Parameters.Add("OWNER_RATE_YN", Trxs_Codes.OWNER_RATE_YN);
            parametros.Parameters.Add("MANDATE_RESV_PROFILES", Trxs_Codes.MANDATE_RESV_PROFILES);
            parametros.Parameters.Add("BBAR_COMPARE_YN", Trxs_Codes.BBAR_COMPARE_YN);
            parametros.Parameters.Add("BYPASS_HURDLE_YN", Trxs_Codes.BYPASS_HURDLE_YN);
            parametros.Parameters.Add("ORS_SELL_SEQUENCE", Trxs_Codes.ORS_SELL_SEQUENCE);
            parametros.Parameters.Add("ADV_BASE_COMPARE_YN", Trxs_Codes.ADV_BASE_COMPARE_YN);
            parametros.Parameters.Add("BYPASS_RANK_CHECK_YN", Trxs_Codes.BYPASS_RANK_CHECK_YN);
            parametros.Parameters.Add("RANK_VALUE", Trxs_Codes.RANK_VALUE);
            parametros.Parameters.Add("RANK_ADJUSTMENT_FACTOR", Trxs_Codes.RANK_ADJUSTMENT_FACTOR);
            parametros.Parameters.Add("DEFAULT_TO_HIGHEST_BAR_YN", Trxs_Codes.DEFAULT_TO_HIGHEST_BAR_YN);
            parametros.Parameters.Add("RATE_FLOOR_OVERRIDE_YN", Trxs_Codes.RATE_FLOOR_OVERRIDE_YN);
            parametros.Parameters.Add("EXTRA_PERSON_CHARGE_BEGINS", Trxs_Codes.EXTRA_PERSON_CHARGE_BEGINS);
            parametros.Parameters.Add("CURR_CODE_DECIMAL_POS", Trxs_Codes.CURR_CODE_DECIMAL_POS);
            parametros.Parameters.Add("OCCUPANCY_LEVEL", Trxs_Codes.OCCUPANCY_LEVEL);
            parametros.Parameters.Add("OVERRIDE_PACKAGE_YN", Trxs_Codes.OVERRIDE_PACKAGE_YN);
            parametros.Parameters.Add("CAT_PKG_YN", Trxs_Codes.CAT_PKG_YN);
            parametros.Parameters.Add("CAT_PKG_CODE", Trxs_Codes.CAT_PKG_CODE);
            parametros.Parameters.Add("REPEAT_POSTING_RHYTHM_YN", Trxs_Codes.REPEAT_POSTING_RHYTHM_YN);
            parametros.Parameters.Add("DISCOUNT_RATE_AMOUNT", Trxs_Codes.DISCOUNT_RATE_AMOUNT);
            parametros.Parameters.Add("DISCOUNT_RATE_PERCENTAGE_YN", Trxs_Codes.DISCOUNT_RATE_PERCENTAGE_YN);
            parametros.Parameters.Add("POSTING_RHYTHM_NIGHTS", Trxs_Codes.POSTING_RHYTHM_NIGHTS);
            parametros.Parameters.Add("ADV_DAILY_RATE_YN", Trxs_Codes.ADV_DAILY_RATE_YN);
            parametros.Parameters.Add("ADV_DAILY_BASE_YN", Trxs_Codes.ADV_DAILY_BASE_YN);
            parametros.Parameters.Add("VOUCHER_BENEFIT_RATE_YN", Trxs_Codes.VOUCHER_BENEFIT_RATE_YN);
            parametros.Parameters.Add("BASE_TYPE", Trxs_Codes.BASE_TYPE);
            parametros.Parameters.Add("PRIVILEGED_YN", Trxs_Codes.PRIVILEGED_YN);
            parametros.Parameters.Add("PRIVILEGED_RESTRICTION_YN", Trxs_Codes.PRIVILEGED_RESTRICTION_YN);
            parametros.Parameters.Add("DEPOSIT_MATURITY_PREFERENCE", Trxs_Codes.DEPOSIT_MATURITY_PREFERENCE);
            parametros.Parameters.Add("MOBILE_CHKOUT_ALLOWED", Trxs_Codes.MOBILE_CHKOUT_ALLOWED);
            parametros.Parameters.Add("ROOM_ASSIGNMENT_VALUE", Trxs_Codes.ROOM_ASSIGNMENT_VALUE);
            parametros.Parameters.Add("MARSHA_RATE_PROGRAM", Trxs_Codes.MARSHA_RATE_PROGRAM);
            parametros.Parameters.Add("MOBILE_CHECKIN_ALLOWED_YN", Trxs_Codes.MOBILE_CHECKIN_ALLOWED_YN);
            parametros.Parameters.Add("OCCUPANCY_BASED_YN", Trxs_Codes.OCCUPANCY_BASED_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
