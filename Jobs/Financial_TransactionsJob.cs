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
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Financial_TransactionsJob: IServiceJobs
    {
        private readonly ILogger<Financial_TransactionsJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Financial_TransactionsJob(ILogger<Financial_TransactionsJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Financial_Trans>(filePath, LoggBuilder);

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
                                $"TRX_NO = '{trial.TRX_NO}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Financial_Trans>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Financial_Trans FinTrans)
        {
            parametros.Parameters.Add("COVERS", FinTrans.COVERS);
            parametros.Parameters.Add("INVOICE_TYPE", FinTrans.INVOICE_TYPE);
            parametros.Parameters.Add("RECPT_NO", FinTrans.RECPT_NO);
            parametros.Parameters.Add("RECPT_TYPE", FinTrans.RECPT_TYPE);
            parametros.Parameters.Add("ROOM_CLASS", FinTrans.ROOM_CLASS);
            parametros.Parameters.Add("TAX_INCLUSIVE_YN", FinTrans.TAX_INCLUSIVE_YN);
            parametros.Parameters.Add("NET_AMOUNT", FinTrans.NET_AMOUNT);
            parametros.Parameters.Add("GROSS_AMOUNT", FinTrans.GROSS_AMOUNT);
            parametros.Parameters.Add("CHEQUE_NUMBER", FinTrans.CHEQUE_NUMBER);
            parametros.Parameters.Add("CASHIER_OPENING_BALANCE", FinTrans.CASHIER_OPENING_BALANCE);
            parametros.Parameters.Add("INVOICE_CLOSE_DATE", FinTrans.INVOICE_CLOSE_DATE.ToString());
            parametros.Parameters.Add("AR_TRANSFER_DATE", FinTrans.AR_TRANSFER_DATE.ToString());
            parametros.Parameters.Add("TRX_NO", FinTrans.TRX_NO);
            parametros.Parameters.Add("RESORT", FinTrans.RESORT);
            parametros.Parameters.Add("FT_SUBTYPE", FinTrans.FT_SUBTYPE);
            parametros.Parameters.Add("TC_GROUP", FinTrans.TC_GROUP);
            parametros.Parameters.Add("TC_SUBGROUP", FinTrans.TC_SUBGROUP);
            parametros.Parameters.Add("TRX_CODE", FinTrans.TRX_CODE);
            parametros.Parameters.Add("TRX_NO_ADJUST", FinTrans.TRX_NO_ADJUST);
            parametros.Parameters.Add("TRX_NO_ADDED_BY", FinTrans.TRX_NO_ADDED_BY);
            parametros.Parameters.Add("TRX_DATE", FinTrans.TRX_DATE.ToString());
            parametros.Parameters.Add("TRX_NO_AGAINST_PACKAGE", FinTrans.TRX_NO_AGAINST_PACKAGE);
            parametros.Parameters.Add("TRX_NO_HEADER", FinTrans.TRX_NO_HEADER);
            parametros.Parameters.Add("AR_NUMBER", FinTrans.AR_NUMBER);
            parametros.Parameters.Add("BUSINESS_DATE", FinTrans.BUSINESS_DATE.ToString());
            parametros.Parameters.Add("ROOM", FinTrans.ROOM);
            parametros.Parameters.Add("TCL_CODE1", FinTrans.TCL_CODE1);
            parametros.Parameters.Add("CURRENCY", FinTrans.CURRENCY);
            parametros.Parameters.Add("FT_GENERATED_TYPE", FinTrans.FT_GENERATED_TYPE);
            parametros.Parameters.Add("TCL_CODE2", FinTrans.TCL_CODE2);
            parametros.Parameters.Add("RESV_NAME_ID", FinTrans.RESV_NAME_ID);
            parametros.Parameters.Add("CASHIER_ID", FinTrans.CASHIER_ID);
            parametros.Parameters.Add("FOLIO_VIEW", FinTrans.FOLIO_VIEW);
            parametros.Parameters.Add("QUANTITY", FinTrans.QUANTITY);
            parametros.Parameters.Add("REMARK", FinTrans.REMARK);
            parametros.Parameters.Add("REFERENCE", FinTrans.REFERENCE);
            parametros.Parameters.Add("PRICE_PER_UNIT", FinTrans.PRICE_PER_UNIT);
            parametros.Parameters.Add("CREDIT_CARD_ID", FinTrans.CREDIT_CARD_ID);
            parametros.Parameters.Add("TRX_AMOUNT", FinTrans.TRX_AMOUNT);
            parametros.Parameters.Add("NAME_ID", FinTrans.NAME_ID);
            parametros.Parameters.Add("POSTED_AMOUNT", FinTrans.POSTED_AMOUNT);
            parametros.Parameters.Add("MARKET_CODE", FinTrans.MARKET_CODE);
            parametros.Parameters.Add("GUEST_ACCOUNT_CREDIT", FinTrans.GUEST_ACCOUNT_CREDIT);
            parametros.Parameters.Add("SOURCE_CODE", FinTrans.SOURCE_CODE);
            parametros.Parameters.Add("RATE_CODE", FinTrans.RATE_CODE);
            parametros.Parameters.Add("DEFERRED_YN", FinTrans.DEFERRED_YN);
            parametros.Parameters.Add("EXCHANGE_RATE", FinTrans.EXCHANGE_RATE);
            parametros.Parameters.Add("GUEST_ACCOUNT_DEBIT", FinTrans.GUEST_ACCOUNT_DEBIT);
            parametros.Parameters.Add("CASHIER_CREDIT", FinTrans.CASHIER_CREDIT);
            parametros.Parameters.Add("CASHIER_DEBIT", FinTrans.CASHIER_DEBIT);
            parametros.Parameters.Add("PACKAGE_CREDIT", FinTrans.PACKAGE_CREDIT);
            parametros.Parameters.Add("PACKAGE_DEBIT", FinTrans.PACKAGE_DEBIT);
            parametros.Parameters.Add("DEP_LED_CREDIT", FinTrans.DEP_LED_CREDIT);
            parametros.Parameters.Add("DEP_LED_DEBIT", FinTrans.DEP_LED_DEBIT);
            parametros.Parameters.Add("HOTEL_ACCT", FinTrans.HOTEL_ACCT);
            parametros.Parameters.Add("IND_ADJUSTMENT_YN", FinTrans.IND_ADJUSTMENT_YN);
            parametros.Parameters.Add("REASON_CODE", FinTrans.REASON_CODE);
            parametros.Parameters.Add("TRAN_ACTION_ID", FinTrans.TRAN_ACTION_ID);
            parametros.Parameters.Add("FIN_DML_SEQ_NO", FinTrans.FIN_DML_SEQ_NO);
            parametros.Parameters.Add("ROUTING_INSTRN_ID", FinTrans.ROUTING_INSTRN_ID);
            parametros.Parameters.Add("FROM_RESV_ID", FinTrans.FROM_RESV_ID);
            parametros.Parameters.Add("O_TRX_DESC", FinTrans.O_TRX_DESC);
            parametros.Parameters.Add("PRODUCT", FinTrans.PRODUCT);
            parametros.Parameters.Add("NUMBER_DIALED", FinTrans.NUMBER_DIALED);
            parametros.Parameters.Add("GEN_CASHIER_ID", FinTrans.GEN_CASHIER_ID);
            parametros.Parameters.Add("REVENUE_AMT", FinTrans.REVENUE_AMT);
            parametros.Parameters.Add("PASSER_BY_NAME", FinTrans.PASSER_BY_NAME);
            parametros.Parameters.Add("AR_LED_CREDIT", FinTrans.AR_LED_CREDIT);
            parametros.Parameters.Add("AR_LED_DEBIT", FinTrans.AR_LED_DEBIT);
            parametros.Parameters.Add("AR_STATE", FinTrans.AR_STATE);
            parametros.Parameters.Add("FOLIO_NO", FinTrans.FOLIO_NO);
            parametros.Parameters.Add("INVOICE_NO", FinTrans.INVOICE_NO);
            parametros.Parameters.Add("INSERT_USER", FinTrans.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", FinTrans.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", FinTrans.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", FinTrans.UPDATE_DATE.ToString());
            parametros.Parameters.Add("FIXED_CHARGES_YN", FinTrans.FIXED_CHARGES_YN);
            parametros.Parameters.Add("TA_COMMISSIONABLE_YN", FinTrans.TA_COMMISSIONABLE_YN);
            parametros.Parameters.Add("EURO_EXCHANGE_RATE", FinTrans.EURO_EXCHANGE_RATE);
            parametros.Parameters.Add("TAX_GENERATED_YN", FinTrans.TAX_GENERATED_YN);
            parametros.Parameters.Add("ARRANGEMENT_ID", FinTrans.ARRANGEMENT_ID);
            parametros.Parameters.Add("TRNS_ACTIVITY_DATE", FinTrans.TRNS_ACTIVITY_DATE.ToString());
            parametros.Parameters.Add("TRNS_FROM_ACCT", FinTrans.TRNS_FROM_ACCT);
            parametros.Parameters.Add("TRNS_TO_ACCT", FinTrans.TRNS_TO_ACCT);
            parametros.Parameters.Add("BILL_NO", FinTrans.BILL_NO);
            parametros.Parameters.Add("REVISION_NO", FinTrans.REVISION_NO);
            parametros.Parameters.Add("RESV_DEPOSIT_ID", FinTrans.RESV_DEPOSIT_ID);
            parametros.Parameters.Add("TARGET_RESORT", FinTrans.TARGET_RESORT);
            parametros.Parameters.Add("INH_DEBIT", FinTrans.INH_DEBIT);
            parametros.Parameters.Add("INH_CREDIT", FinTrans.INH_CREDIT);
            parametros.Parameters.Add("LINK_TRX_NO", FinTrans.LINK_TRX_NO);
            parametros.Parameters.Add("FOLIO_TYPE", FinTrans.FOLIO_TYPE);
            parametros.Parameters.Add("COMPRESSED_YN", FinTrans.COMPRESSED_YN);
            parametros.Parameters.Add("NAME_TAX_TYPE", FinTrans.NAME_TAX_TYPE);
            parametros.Parameters.Add("TAX_INV_NO", FinTrans.TAX_INV_NO);
            parametros.Parameters.Add("PAYMENT_TYPE", FinTrans.PAYMENT_TYPE);
            parametros.Parameters.Add("DISPLAY_YN", FinTrans.DISPLAY_YN);
            parametros.Parameters.Add("COLL_AGENT_POSTING_YN", FinTrans.COLL_AGENT_POSTING_YN);
            parametros.Parameters.Add("FISCAL_TRX_CODE_TYPE", FinTrans.FISCAL_TRX_CODE_TYPE);
            parametros.Parameters.Add("DEFERRED_TAXES_YN", FinTrans.DEFERRED_TAXES_YN);
            parametros.Parameters.Add("COMMENTS", FinTrans.COMMENTS);
            parametros.Parameters.Add("AUTHORIZER_ID", FinTrans.AUTHORIZER_ID);
            parametros.Parameters.Add("APPROVAL_CODE", FinTrans.APPROVAL_CODE);
            parametros.Parameters.Add("APPROVAL_DATE", FinTrans.APPROVAL_DATE.ToString());
            parametros.Parameters.Add("APPROVAL_STATUS", FinTrans.APPROVAL_STATUS);
            parametros.Parameters.Add("TAX_ELEMENTS", FinTrans.TAX_ELEMENTS);
            parametros.Parameters.Add("COMP_LINK_TRX_NO", FinTrans.COMP_LINK_TRX_NO);
            parametros.Parameters.Add("COMP_LINK_TRX_CODE", FinTrans.COMP_LINK_TRX_CODE);
            parametros.Parameters.Add("POSTING_DATE", FinTrans.POSTING_DATE.ToString());
            parametros.Parameters.Add("MTRX_NO_AGAINST_PACKAGE", FinTrans.MTRX_NO_AGAINST_PACKAGE);
            parametros.Parameters.Add("ADVANCED_GENERATE_YN", FinTrans.ADVANCED_GENERATE_YN);
            parametros.Parameters.Add("FOREX_TYPE", FinTrans.FOREX_TYPE);
            parametros.Parameters.Add("FOREX_COMM_PERC", FinTrans.FOREX_COMM_PERC);
            parametros.Parameters.Add("FOREX_COMM_AMOUNT", FinTrans.FOREX_COMM_AMOUNT);
            parametros.Parameters.Add("ARTICLE_ID", FinTrans.ARTICLE_ID);
            parametros.Parameters.Add("CHECK_FILE_ID", FinTrans.CHECK_FILE_ID);
            parametros.Parameters.Add("TA_COMMISSION_NET_YN", FinTrans.TA_COMMISSION_NET_YN);
            parametros.Parameters.Add("SOURCE_COMMISSION_NET_YN", FinTrans.SOURCE_COMMISSION_NET_YN);
            parametros.Parameters.Add("TO_RESV_NAME_ID", FinTrans.TO_RESV_NAME_ID);
            parametros.Parameters.Add("ROOM_NTS", FinTrans.ROOM_NTS);
            parametros.Parameters.Add("COMP_TYPE_CODE", FinTrans.COMP_TYPE_CODE);
            parametros.Parameters.Add("FISCAL_BILL_NO", FinTrans.FISCAL_BILL_NO);
            parametros.Parameters.Add("ACC_TYPE_FLAG", FinTrans.ACC_TYPE_FLAG);
            parametros.Parameters.Add("PACKAGE_ALLOWANCE", FinTrans.PACKAGE_ALLOWANCE);
            parametros.Parameters.Add("ORIGINAL_RESV_NAME_ID", FinTrans.ORIGINAL_RESV_NAME_ID);
            parametros.Parameters.Add("ORIGINAL_ROOM", FinTrans.ORIGINAL_ROOM);
            parametros.Parameters.Add("COUPON_NO", FinTrans.COUPON_NO);
            parametros.Parameters.Add("ORG_AR_LED_DEBIT", FinTrans.ORG_AR_LED_DEBIT);
            parametros.Parameters.Add("SETTLEMENT_FLAG", FinTrans.SETTLEMENT_FLAG);
            parametros.Parameters.Add("EXT_TRX_ID", FinTrans.EXT_TRX_ID);
            parametros.Parameters.Add("PROFIT_LOSS_FLAG", FinTrans.PROFIT_LOSS_FLAG);
            parametros.Parameters.Add("CLOSURE_NO", FinTrans.CLOSURE_NO);
            parametros.Parameters.Add("PROFORMA_YN", FinTrans.PROFORMA_YN);
            parametros.Parameters.Add("ALLOWANCE_TYPE", FinTrans.ALLOWANCE_TYPE);
            parametros.Parameters.Add("ADV_GENERATE_ADJUSTMENT", FinTrans.ADV_GENERATE_ADJUSTMENT);
            parametros.Parameters.Add("ADV_GENERATE_TRX_CODE", FinTrans.ADV_GENERATE_TRX_CODE);
            parametros.Parameters.Add("HOLD_YN", FinTrans.HOLD_YN);
            parametros.Parameters.Add("TRX_SERVICE_TYPE", FinTrans.TRX_SERVICE_TYPE);
            parametros.Parameters.Add("ORG_BILL_NO", FinTrans.ORG_BILL_NO);
            parametros.Parameters.Add("ORG_FOLIO_TYPE", FinTrans.ORG_FOLIO_TYPE);
            parametros.Parameters.Add("POSTING_TYPE", FinTrans.POSTING_TYPE);
            parametros.Parameters.Add("PARALLEL_GUEST_CREDIT", FinTrans.PARALLEL_GUEST_CREDIT);
            parametros.Parameters.Add("PARALLEL_GUEST_DEBIT", FinTrans.PARALLEL_GUEST_DEBIT);
            parametros.Parameters.Add("PARALLEL_CURRENCY", FinTrans.PARALLEL_CURRENCY);
            parametros.Parameters.Add("EXCHANGE_DIFFERENCE_YN", FinTrans.EXCHANGE_DIFFERENCE_YN);
            parametros.Parameters.Add("MEMBERSHIP_ID", FinTrans.MEMBERSHIP_ID);
            parametros.Parameters.Add("PARALLEL_NET_AMOUNT", FinTrans.PARALLEL_NET_AMOUNT);
            parametros.Parameters.Add("PARALLEL_GROSS_AMOUNT", FinTrans.PARALLEL_GROSS_AMOUNT);
            parametros.Parameters.Add("EXCHANGE_TYPE", FinTrans.EXCHANGE_TYPE);
            parametros.Parameters.Add("EXCHANGE_DATE", FinTrans.EXCHANGE_DATE.ToString());
            parametros.Parameters.Add("INSTALLMENTS", FinTrans.INSTALLMENTS);
            parametros.Parameters.Add("CONTRACT_GUEST_DEBIT", FinTrans.CONTRACT_GUEST_DEBIT);
            parametros.Parameters.Add("CONTRACT_GUEST_CREDIT", FinTrans.CONTRACT_GUEST_CREDIT);
            parametros.Parameters.Add("CONTRACT_NET_AMOUNT", FinTrans.CONTRACT_NET_AMOUNT);
            parametros.Parameters.Add("CONTRACT_GROSS_AMOUNT", FinTrans.CONTRACT_GROSS_AMOUNT);
            parametros.Parameters.Add("CONTRACT_CURRENCY", FinTrans.CONTRACT_CURRENCY);
            parametros.Parameters.Add("CALC_POINTS_YN", FinTrans.CALC_POINTS_YN);
            parametros.Parameters.Add("AR_CHARGE_TRANSFER_YN", FinTrans.AR_CHARGE_TRANSFER_YN);
            parametros.Parameters.Add("PROCESSED_8300_YN", FinTrans.PROCESSED_8300_YN);
            parametros.Parameters.Add("ASB_FLAG", FinTrans.ASB_FLAG);
            parametros.Parameters.Add("POSTIT_YN", FinTrans.POSTIT_YN);
            parametros.Parameters.Add("POSTIT_NO", FinTrans.POSTIT_NO);
            parametros.Parameters.Add("ROUTING_DATE", FinTrans.ROUTING_DATE.ToString());
            parametros.Parameters.Add("PACKAGE_TRX_TYPE", FinTrans.PACKAGE_TRX_TYPE);
            parametros.Parameters.Add("EXT_SYS_RESULT_MSG", FinTrans.EXT_SYS_RESULT_MSG);
            parametros.Parameters.Add("CC_TRX_FEE_AMOUNT", FinTrans.CC_TRX_FEE_AMOUNT);
            parametros.Parameters.Add("CHANGE_DUE", FinTrans.CHANGE_DUE);
            parametros.Parameters.Add("POSTING_SOURCE_NAME_ID", FinTrans.POSTING_SOURCE_NAME_ID);
            parametros.Parameters.Add("AUTO_SETTLE_YN", FinTrans.AUTO_SETTLE_YN);
            parametros.Parameters.Add("QUEUE_NAME", FinTrans.QUEUE_NAME);
            parametros.Parameters.Add("DEP_TAX_TRANSFERED_YN", FinTrans.DEP_TAX_TRANSFERED_YN);
            parametros.Parameters.Add("ESIGNED_RECEIPT_NAME", FinTrans.ESIGNED_RECEIPT_NAME);
            parametros.Parameters.Add("BONUS_CHECK_ID", FinTrans.BONUS_CHECK_ID);
            parametros.Parameters.Add("AUTO_CREDITBILL_YN", FinTrans.AUTO_CREDITBILL_YN);
            parametros.Parameters.Add("POSTING_RHYTHM", FinTrans.POSTING_RHYTHM);
            parametros.Parameters.Add("FBA_CERTIFICATE_NUMBER", FinTrans.FBA_CERTIFICATE_NUMBER);
            parametros.Parameters.Add("EXP_ORIGINAL_INVOICE", FinTrans.EXP_ORIGINAL_INVOICE);
            parametros.Parameters.Add("EXP_INVOICE_TYPE", FinTrans.EXP_INVOICE_TYPE);
            parametros.Parameters.Add("ASB_TAX_FLAG", FinTrans.ASB_TAX_FLAG);
            parametros.Parameters.Add("ASB_ONLY_POST_TAXES_ONCE_YN", FinTrans.ASB_ONLY_POST_TAXES_ONCE_YN);
            parametros.Parameters.Add("ROUND_LINK_TRXNO", FinTrans.ROUND_LINK_TRXNO);
            parametros.Parameters.Add("ROUND_FACTOR_YN", FinTrans.ROUND_FACTOR_YN);
            parametros.Parameters.Add("DEP_POSTING_FLAG", FinTrans.DEP_POSTING_FLAG);
            parametros.Parameters.Add("EFFECTIVE_DATE", FinTrans.EFFECTIVE_DATE.ToString());
            parametros.Parameters.Add("PACKAGE_ARRANGEMENT_CODE", FinTrans.PACKAGE_ARRANGEMENT_CODE);
            parametros.Parameters.Add("CORRECTION_YN", FinTrans.CORRECTION_YN);
            parametros.Parameters.Add("ROUTED_YN", FinTrans.ROUTED_YN);
            parametros.Parameters.Add("UPSELL_CHARGE_YN", FinTrans.UPSELL_CHARGE_YN);
            parametros.Parameters.Add("REVERSE_PAYMENT_TRX_NO", FinTrans.REVERSE_PAYMENT_TRX_NO);
            parametros.Parameters.Add("ADVANCE_BILL_YN", FinTrans.ADVANCE_BILL_YN);
            parametros.Parameters.Add("ADVANCE_BILL_REVERSED_YN", FinTrans.ADVANCE_BILL_REVERSED_YN);
            parametros.Parameters.Add("GP_AWARD_CODE", FinTrans.GP_AWARD_CODE);
            parametros.Parameters.Add("ORG_POSTED_AMOUNT", FinTrans.ORG_POSTED_AMOUNT);
            parametros.Parameters.Add("INC_TAX_DEDUCTED_YN", FinTrans.INC_TAX_DEDUCTED_YN);
            parametros.Parameters.Add("GP_AWARD_CANCELLED_YN", FinTrans.GP_AWARD_CANCELLED_YN);
            parametros.Parameters.Add("GP_AWARD_CANCEL_CODE", FinTrans.GP_AWARD_CANCEL_CODE);
            parametros.Parameters.Add("CC_REFUND_POSTING", FinTrans.CC_REFUND_POSTING);
            parametros.Parameters.Add("ELECTRONIC_VOUCHER_NO", FinTrans.ELECTRONIC_VOUCHER_NO);
            parametros.Parameters.Add("ROOM_NTS_EFFECTIVE", FinTrans.ROOM_NTS_EFFECTIVE);
            parametros.Parameters.Add("SERVICE_RECOVERY_ADJUSTMENT_YN", FinTrans.SERVICE_RECOVERY_ADJUSTMENT_YN);
            parametros.Parameters.Add("SERVICE_RECOVERY_DEPT_CODE", FinTrans.SERVICE_RECOVERY_DEPT_CODE);
            parametros.Parameters.Add("THRESHOLD_DIVERSION_ID", FinTrans.THRESHOLD_DIVERSION_ID);
            parametros.Parameters.Add("THRESHOLD_ENTITY_TYPE", FinTrans.THRESHOLD_ENTITY_TYPE);
            parametros.Parameters.Add("THRESHOLD_ENTITY_QTY", FinTrans.THRESHOLD_ENTITY_QTY);
            parametros.Parameters.Add("THRESHOLD_TREATMENT_FLAG", FinTrans.THRESHOLD_TREATMENT_FLAG);
            parametros.Parameters.Add("PAYMENT_SURCHARGE_AMT", FinTrans.PAYMENT_SURCHARGE_AMT);
            parametros.Parameters.Add("PAYMENT_SURCHARGE_TYPE", FinTrans.PAYMENT_SURCHARGE_TYPE);
            parametros.Parameters.Add("PROPERTY_BILL_PREFIX", FinTrans.PROPERTY_BILL_PREFIX);
            parametros.Parameters.Add("EXCH_DIFF_TRX_NO", FinTrans.EXCH_DIFF_TRX_NO);
            parametros.Parameters.Add("DEPOSIT_TRANSACTION_ID", FinTrans.DEPOSIT_TRANSACTION_ID);
            parametros.Parameters.Add("ASSOCIATED_TRX_NO", FinTrans.ASSOCIATED_TRX_NO);
            parametros.Parameters.Add("STAMP_DUTY_YN", FinTrans.STAMP_DUTY_YN);
            parametros.Parameters.Add("ASSOCIATED_RECPT_NO", FinTrans.ASSOCIATED_RECPT_NO);
            parametros.Parameters.Add("TAX_RATE", FinTrans.TAX_RATE);
            parametros.Parameters.Add("TAX_RATE_TYPE", FinTrans.TAX_RATE_TYPE);
            parametros.Parameters.Add("VAT_OFFSET_YN", FinTrans.VAT_OFFSET_YN);
            parametros.Parameters.Add("FOREX_TAX_YN", FinTrans.FOREX_TAX_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", FinTrans.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", FinTrans.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", FinTrans.DELETED_FLAG);

            return parametros;
        }

        /*
        private List<Mdl_Financial_Trans> Get_List_Financial_Trans(string filePath)
        {
            List<Mdl_Financial_Trans> ListFinTrans = new List<Mdl_Financial_Trans>();
            Mdl_Financial_Trans FinTrans;

            LoggBuilder.AppendLine($"      {DateTime.Now} Verificando se arquivo de integração existe: {filePath}");
            if (File.Exists(filePath))
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, O Aquivo Existe.");
            }
            else
            {
                erros = "1";
                LoggBuilder.AppendLine($"      {DateTime.Now} NÃO!!!, Arquivo Inexistente.");
                LoggBuilder.AppendLine($"      {DateTime.Now} Encerrando a integração.");
                return ListFinTrans;
            }

            LoggBuilder.AppendLine($"      {DateTime.Now} Efetuando leitura de arquivo de integração: {filePath}");
            try
            {
                var reader = new StreamReader(File.OpenRead(filePath));

                int x = 1;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (x >= 2)
                    {
                        if (line != null)
                        {
                            var campos = line.Split(';');
                            FinTrans = new Mdl_Financial_Trans()
                            {
                                COVERS = campos[0],
                                INVOICE_TYPE = campos[1],
                                RECPT_NO = campos[2],
                                RECPT_TYPE = campos[3],
                                ROOM_CLASS = campos[4],
                                TAX_INCLUSIVE_YN = campos[5],
                                NET_AMOUNT = campos[6],
                                GROSS_AMOUNT = campos[7],
                                CHEQUE_NUMBER = campos[8],
                                CASHIER_OPENING_BALANCE = campos[9],
                                INVOICE_CLOSE_DATE = campos[10],
                                AR_TRANSFER_DATE = campos[11],
                                TRX_NO = campos[12],
                                RESORT = campos[13],
                                FT_SUBTYPE = campos[14],
                                TC_GROUP = campos[15],
                                TC_SUBGROUP = campos[16],
                                TRX_CODE = campos[17],
                                TRX_NO_ADJUST = campos[18],
                                TRX_NO_ADDED_BY = campos[19],
                                TRX_DATE = campos[20].Trim().Split(" ")[0].ToString(),
                                TRX_NO_AGAINST_PACKAGE = campos[21],
                                TRX_NO_HEADER = campos[22],
                                AR_NUMBER = campos[23],
                                BUSINESS_DATE = campos[24].Trim().Split(" ")[0].ToString(),
                                ROOM = campos[25],
                                TCL_CODE1 = campos[26],
                                CURRENCY = campos[27],
                                FT_GENERATED_TYPE = campos[28],
                                TCL_CODE2 = campos[29],
                                RESV_NAME_ID = campos[30],
                                CASHIER_ID = campos[31],
                                FOLIO_VIEW = campos[32],
                                QUANTITY = campos[33],
                                REMARK = campos[34],
                                REFERENCE = campos[35],
                                PRICE_PER_UNIT = campos[36],
                                CREDIT_CARD_ID = campos[37],
                                TRX_AMOUNT = campos[38],
                                NAME_ID = campos[39],
                                POSTED_AMOUNT = campos[40],
                                MARKET_CODE = campos[41],
                                GUEST_ACCOUNT_CREDIT = campos[42],
                                SOURCE_CODE = campos[43],
                                RATE_CODE = campos[44],
                                DEFERRED_YN = campos[45],
                                EXCHANGE_RATE = campos[46],
                                GUEST_ACCOUNT_DEBIT = campos[47],
                                CASHIER_CREDIT = campos[48],
                                CASHIER_DEBIT = campos[49],
                                PACKAGE_CREDIT = campos[50],
                                PACKAGE_DEBIT = campos[51],
                                DEP_LED_CREDIT = campos[52],
                                DEP_LED_DEBIT = campos[53],
                                HOTEL_ACCT = campos[54],
                                IND_ADJUSTMENT_YN = campos[55],
                                REASON_CODE = campos[56],
                                TRAN_ACTION_ID = campos[57],
                                FIN_DML_SEQ_NO = campos[58],
                                ROUTING_INSTRN_ID = campos[59],
                                FROM_RESV_ID = campos[60],
                                O_TRX_DESC = campos[61],
                                PRODUCT = campos[62],
                                NUMBER_DIALED = campos[63],
                                GEN_CASHIER_ID = campos[64],
                                REVENUE_AMT = campos[65],
                                PASSER_BY_NAME = campos[66],
                                AR_LED_CREDIT = campos[67],
                                AR_LED_DEBIT = campos[68],
                                AR_STATE = campos[69],
                                FOLIO_NO = campos[70],
                                INVOICE_NO = campos[71],
                                INSERT_USER = campos[72],
                                INSERT_DATE = campos[73].Trim().Split(" ")[0].ToString(),
                                UPDATE_USER = campos[74],
                                UPDATE_DATE = campos[75].Trim().Split(" ")[0].ToString(),
                                FIXED_CHARGES_YN = campos[76],
                                TA_COMMISSIONABLE_YN = campos[77],
                                EURO_EXCHANGE_RATE = campos[78],
                                TAX_GENERATED_YN = campos[79],
                                ARRANGEMENT_ID = campos[80],
                                TRNS_ACTIVITY_DATE = campos[81],
                                TRNS_FROM_ACCT = campos[82],
                                TRNS_TO_ACCT = campos[83],
                                BILL_NO = campos[84],
                                REVISION_NO = campos[85],
                                RESV_DEPOSIT_ID = campos[86],
                                TARGET_RESORT = campos[87],
                                INH_DEBIT = campos[88],
                                INH_CREDIT = campos[89],
                                LINK_TRX_NO = campos[90],
                                FOLIO_TYPE = campos[91],
                                COMPRESSED_YN = campos[92],
                                NAME_TAX_TYPE = campos[93],
                                TAX_INV_NO = campos[94],
                                PAYMENT_TYPE = campos[95],
                                DISPLAY_YN = campos[96],
                                COLL_AGENT_POSTING_YN = campos[97],
                                FISCAL_TRX_CODE_TYPE = campos[98],
                                DEFERRED_TAXES_YN = campos[99],
                                COMMENTS = campos[100],
                                AUTHORIZER_ID = campos[101],
                                APPROVAL_CODE = campos[102],
                                APPROVAL_DATE = campos[103],
                                APPROVAL_STATUS = campos[104],
                                TAX_ELEMENTS = campos[105],
                                COMP_LINK_TRX_NO = campos[106],
                                COMP_LINK_TRX_CODE = campos[107],
                                POSTING_DATE = campos[108].Trim().Split(" ")[0].ToString(),
                                MTRX_NO_AGAINST_PACKAGE = campos[109],
                                ADVANCED_GENERATE_YN = campos[110],
                                FOREX_TYPE = campos[111],
                                FOREX_COMM_PERC = campos[112],
                                FOREX_COMM_AMOUNT = campos[113],
                                ARTICLE_ID = campos[114],
                                CHECK_FILE_ID = campos[115],
                                TA_COMMISSION_NET_YN = campos[116],
                                SOURCE_COMMISSION_NET_YN = campos[117],
                                TO_RESV_NAME_ID = campos[118],
                                ROOM_NTS = campos[119],
                                COMP_TYPE_CODE = campos[120],
                                FISCAL_BILL_NO = campos[121],
                                ACC_TYPE_FLAG = campos[122],
                                PACKAGE_ALLOWANCE = campos[123],
                                ORIGINAL_RESV_NAME_ID = campos[124],
                                ORIGINAL_ROOM = campos[125],
                                COUPON_NO = campos[126],
                                ORG_AR_LED_DEBIT = campos[127],
                                SETTLEMENT_FLAG = campos[128],
                                EXT_TRX_ID = campos[129],
                                PROFIT_LOSS_FLAG = campos[130],
                                CLOSURE_NO = campos[131],
                                PROFORMA_YN = campos[132],
                                ALLOWANCE_TYPE = campos[133],
                                ADV_GENERATE_ADJUSTMENT = campos[134],
                                ADV_GENERATE_TRX_CODE = campos[135],
                                HOLD_YN = campos[136],
                                TRX_SERVICE_TYPE = campos[137],
                                ORG_BILL_NO = campos[138],
                                ORG_FOLIO_TYPE = campos[139],
                                POSTING_TYPE = campos[140],
                                PARALLEL_GUEST_CREDIT = campos[141],
                                PARALLEL_GUEST_DEBIT = campos[142],
                                PARALLEL_CURRENCY = campos[143],
                                EXCHANGE_DIFFERENCE_YN = campos[144],
                                MEMBERSHIP_ID = campos[145],
                                PARALLEL_NET_AMOUNT = campos[146],
                                PARALLEL_GROSS_AMOUNT = campos[147],
                                EXCHANGE_TYPE = campos[148],
                                EXCHANGE_DATE = campos[149],
                                INSTALLMENTS = campos[150],
                                CONTRACT_GUEST_DEBIT = campos[151],
                                CONTRACT_GUEST_CREDIT = campos[152],
                                CONTRACT_NET_AMOUNT = campos[153],
                                CONTRACT_GROSS_AMOUNT = campos[154],
                                CONTRACT_CURRENCY = campos[155],
                                CALC_POINTS_YN = campos[156],
                                AR_CHARGE_TRANSFER_YN = campos[157],
                                PROCESSED_8300_YN = campos[158],
                                ASB_FLAG = campos[159],
                                POSTIT_YN = campos[160],
                                POSTIT_NO = campos[161],
                                ROUTING_DATE = campos[162],
                                PACKAGE_TRX_TYPE = campos[163],
                                EXT_SYS_RESULT_MSG = campos[164],
                                CC_TRX_FEE_AMOUNT = campos[165],
                                CHANGE_DUE = campos[166],
                                POSTING_SOURCE_NAME_ID = campos[167],
                                AUTO_SETTLE_YN = campos[168],
                                QUEUE_NAME = campos[169],
                                DEP_TAX_TRANSFERED_YN = campos[170],
                                ESIGNED_RECEIPT_NAME = campos[171],
                                BONUS_CHECK_ID = campos[172],
                                AUTO_CREDITBILL_YN = campos[173],
                                POSTING_RHYTHM = campos[174],
                                FBA_CERTIFICATE_NUMBER = campos[175],
                                EXP_ORIGINAL_INVOICE = campos[176],
                                EXP_INVOICE_TYPE = campos[177],
                                ASB_TAX_FLAG = campos[178],
                                ASB_ONLY_POST_TAXES_ONCE_YN = campos[179],
                                ROUND_LINK_TRXNO = campos[180],
                                ROUND_FACTOR_YN = campos[181],
                                DEP_POSTING_FLAG = campos[182],
                                EFFECTIVE_DATE = campos[183],
                                PACKAGE_ARRANGEMENT_CODE = campos[184],
                                CORRECTION_YN = campos[185],
                                ROUTED_YN = campos[186],
                                UPSELL_CHARGE_YN = campos[187],
                                REVERSE_PAYMENT_TRX_NO = campos[188],
                                ADVANCE_BILL_YN = campos[189],
                                ADVANCE_BILL_REVERSED_YN = campos[190],
                                GP_AWARD_CODE = campos[191],
                                ORG_POSTED_AMOUNT = campos[192],
                                INC_TAX_DEDUCTED_YN = campos[193],
                                GP_AWARD_CANCELLED_YN = campos[194],
                                GP_AWARD_CANCEL_CODE = campos[195],
                                CC_REFUND_POSTING = campos[196],
                                ELECTRONIC_VOUCHER_NO = campos[197],
                                ROOM_NTS_EFFECTIVE = campos[198],
                                SERVICE_RECOVERY_ADJUSTMENT_YN = campos[199],
                                SERVICE_RECOVERY_DEPT_CODE = campos[200],
                                THRESHOLD_DIVERSION_ID = campos[201],
                                THRESHOLD_ENTITY_TYPE = campos[202],
                                THRESHOLD_ENTITY_QTY = campos[203],
                                THRESHOLD_TREATMENT_FLAG = campos[204],
                                PAYMENT_SURCHARGE_AMT = campos[205],
                                PAYMENT_SURCHARGE_TYPE = campos[206],
                                PROPERTY_BILL_PREFIX = campos[207],
                                EXCH_DIFF_TRX_NO = campos[208],
                                DEPOSIT_TRANSACTION_ID = campos[209],
                                ASSOCIATED_TRX_NO = campos[210],
                                STAMP_DUTY_YN = campos[211],
                                ASSOCIATED_RECPT_NO = campos[212],
                                TAX_RATE = campos[213],
                                TAX_RATE_TYPE = campos[214],
                                VAT_OFFSET_YN = campos[215],
                                FOREX_TAX_YN = campos[216]
                            };

                            ListFinTrans.Add(FinTrans);
                        }
                    }
                    x++;
                }
                x = 1;
                reader.Close();
                reader.Dispose();
            }
            catch (Exception e)
            {
                erros = "1";
                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas na leitura do arquivo, Execption: {e.Message}");
                return new List<Mdl_Financial_Trans>();
            }

            _Job_Exec.moverArquivoIntegrado(filePath, LoggBuilder);

            return ListFinTrans;
        }

        private string GetInsertQuery()
        {
            var query = "insert into FINANCIAL_TRANSACTIONS " +
            " values " +
            "(:COVERS,:INVOICE_TYPE,:RECPT_NO,:RECPT_TYPE,:ROOM_CLASS,:TAX_INCLUSIVE_YN,:NET_AMOUNT,:GROSS_AMOUNT,:CHEQUE_NUMBER,:CASHIER_OPENING_BALANCE," +
            ":INVOICE_CLOSE_DATE,:AR_TRANSFER_DATE,:TRX_NO,:RESORT,:FT_SUBTYPE,:TC_GROUP,:TC_SUBGROUP,:TRX_CODE,:TRX_NO_ADJUST,:TRX_NO_ADDED_BY,:TRX_DATE," +
            ":TRX_NO_AGAINST_PACKAGE,:TRX_NO_HEADER,:AR_NUMBER,:BUSINESS_DATE,:ROOM,:TCL_CODE1,:CURRENCY,:FT_GENERATED_TYPE,:TCL_CODE2,:RESV_NAME_ID,:CASHIER_ID," +
            ":FOLIO_VIEW,:QUANTITY,:REMARK,:REFERENCE,:PRICE_PER_UNIT,:CREDIT_CARD_ID,:TRX_AMOUNT,:NAME_ID,:POSTED_AMOUNT,:MARKET_CODE,:GUEST_ACCOUNT_CREDIT,:SOURCE_CODE," +
            ":RATE_CODE,:DEFERRED_YN,:EXCHANGE_RATE,:GUEST_ACCOUNT_DEBIT,:CASHIER_CREDIT,:CASHIER_DEBIT,:PACKAGE_CREDIT,:PACKAGE_DEBIT,:DEP_LED_CREDIT,:DEP_LED_DEBIT,:HOTEL_ACCT," +
            ":IND_ADJUSTMENT_YN,:REASON_CODE,:TRAN_ACTION_ID,:FIN_DML_SEQ_NO,:ROUTING_INSTRN_ID,:FROM_RESV_ID,:O_TRX_DESC,:PRODUCT,:NUMBER_DIALED,:GEN_CASHIER_ID,:REVENUE_AMT," +
            ":PASSER_BY_NAME,:AR_LED_CREDIT,:AR_LED_DEBIT,:AR_STATE,:FOLIO_NO,:INVOICE_NO,:INSERT_USER,:INSERT_DATE,:UPDATE_USER,:UPDATE_DATE,:FIXED_CHARGES_YN,:TA_COMMISSIONABLE_YN," +
            ":EURO_EXCHANGE_RATE,:TAX_GENERATED_YN,:ARRANGEMENT_ID,:TRNS_ACTIVITY_DATE,:TRNS_FROM_ACCT,:TRNS_TO_ACCT,:BILL_NO,:REVISION_NO,:RESV_DEPOSIT_ID,:TARGET_RESORT,:INH_DEBIT," +
            ":INH_CREDIT,:LINK_TRX_NO,:FOLIO_TYPE,:COMPRESSED_YN,:NAME_TAX_TYPE,:TAX_INV_NO,:PAYMENT_TYPE,:DISPLAY_YN,:COLL_AGENT_POSTING_YN,:FISCAL_TRX_CODE_TYPE,:DEFERRED_TAXES_YN," +
            ":COMMENTS,:AUTHORIZER_ID,:APPROVAL_CODE,:APPROVAL_DATE,:APPROVAL_STATUS,:TAX_ELEMENTS,:COMP_LINK_TRX_NO,:COMP_LINK_TRX_CODE,:POSTING_DATE,:MTRX_NO_AGAINST_PACKAGE," +
            ":ADVANCED_GENERATE_YN,:FOREX_TYPE,:FOREX_COMM_PERC,:FOREX_COMM_AMOUNT,:ARTICLE_ID,:CHECK_FILE_ID,:TA_COMMISSION_NET_YN,:SOURCE_COMMISSION_NET_YN,:TO_RESV_NAME_ID," +
            ":ROOM_NTS,:COMP_TYPE_CODE,:FISCAL_BILL_NO,:ACC_TYPE_FLAG,:PACKAGE_ALLOWANCE,:ORIGINAL_RESV_NAME_ID,:ORIGINAL_ROOM,:COUPON_NO,:ORG_AR_LED_DEBIT,:SETTLEMENT_FLAG," +
            ":EXT_TRX_ID,:PROFIT_LOSS_FLAG,:CLOSURE_NO,:PROFORMA_YN,:ALLOWANCE_TYPE,:ADV_GENERATE_ADJUSTMENT,:ADV_GENERATE_TRX_CODE,:HOLD_YN,:TRX_SERVICE_TYPE,:ORG_BILL_NO,:ORG_FOLIO_TYPE," +
            ":POSTING_TYPE,:PARALLEL_GUEST_CREDIT,:PARALLEL_GUEST_DEBIT,:PARALLEL_CURRENCY,:EXCHANGE_DIFFERENCE_YN,:MEMBERSHIP_ID,:PARALLEL_NET_AMOUNT,:PARALLEL_GROSS_AMOUNT,:EXCHANGE_TYPE," +
            ":EXCHANGE_DATE,:INSTALLMENTS,:CONTRACT_GUEST_DEBIT,:CONTRACT_GUEST_CREDIT,:CONTRACT_NET_AMOUNT,:CONTRACT_GROSS_AMOUNT,:CONTRACT_CURRENCY,:CALC_POINTS_YN,:AR_CHARGE_TRANSFER_YN," +
            ":PROCESSED_8300_YN,:ASB_FLAG,:POSTIT_YN,:POSTIT_NO,:ROUTING_DATE,:PACKAGE_TRX_TYPE,:EXT_SYS_RESULT_MSG,:CC_TRX_FEE_AMOUNT,:CHANGE_DUE,:POSTING_SOURCE_NAME_ID,:AUTO_SETTLE_YN," +
            ":QUEUE_NAME,:DEP_TAX_TRANSFERED_YN,:ESIGNED_RECEIPT_NAME,:BONUS_CHECK_ID,:AUTO_CREDITBILL_YN,:POSTING_RHYTHM,:FBA_CERTIFICATE_NUMBER,:EXP_ORIGINAL_INVOICE,:EXP_INVOICE_TYPE," +
            ":ASB_TAX_FLAG,:ASB_ONLY_POST_TAXES_ONCE_YN,:ROUND_LINK_TRXNO,:ROUND_FACTOR_YN,:DEP_POSTING_FLAG,:EFFECTIVE_DATE,:PACKAGE_ARRANGEMENT_CODE,:CORRECTION_YN,:ROUTED_YN," +
            ":UPSELL_CHARGE_YN,:REVERSE_PAYMENT_TRX_NO,:ADVANCE_BILL_YN,:ADVANCE_BILL_REVERSED_YN,:GP_AWARD_CODE,:ORG_POSTED_AMOUNT,:INC_TAX_DEDUCTED_YN,:GP_AWARD_CANCELLED_YN," +
            ":GP_AWARD_CANCEL_CODE,:CC_REFUND_POSTING,:ELECTRONIC_VOUCHER_NO,:ROOM_NTS_EFFECTIVE,:SERVICE_RECOVERY_ADJUSTMENT_YN,:SERVICE_RECOVERY_DEPT_CODE,:THRESHOLD_DIVERSION_ID," +
            ":THRESHOLD_ENTITY_TYPE,:THRESHOLD_ENTITY_QTY,:THRESHOLD_TREATMENT_FLAG,:PAYMENT_SURCHARGE_AMT,:PAYMENT_SURCHARGE_TYPE,:PROPERTY_BILL_PREFIX,:EXCH_DIFF_TRX_NO," +
            ":DEPOSIT_TRANSACTION_ID,:ASSOCIATED_TRX_NO,:STAMP_DUTY_YN,:ASSOCIATED_RECPT_NO,:TAX_RATE,:TAX_RATE_TYPE,:VAT_OFFSET_YN,:FOREX_TAX_YN)";

            return query;
        }
        */
    }
}
