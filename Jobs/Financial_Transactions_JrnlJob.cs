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
using System.Runtime.CompilerServices;
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Financial_Transactions_JrnlJob : IServiceJobs
    {
        private readonly ILogger<Mdl_Financial_Transactions_Jrnl> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Financial_Transactions_JrnlJob(ILogger<Mdl_Financial_Transactions_Jrnl> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Financial_Transactions_Jrnl>(filePath, LoggBuilder);

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
                            var _where = $"RESORT = '{trial.RESORT}' and \n" +
                                $"TRX_NO = '{trial.TRX_NO}' and \n" +
                                $"FOLIO_VIEW = '{trial.FOLIO_VIEW}' and \n" +
                                $"FOLIO_NO = '{trial.FOLIO_NO}' and \n" +
                                $"ORIGINAL_RESV_NAME_ID = '{trial.ORIGINAL_RESV_NAME_ID}' and \n" +
                                $"FIN_DML_SEQ_NO = '{trial.FIN_DML_SEQ_NO}' and \n" +
                                $"NAME_ID = '{trial.NAME_ID}' and \n" +
                                $"SYSTEM_DATE = TO_DATE('{trial.SYSTEM_DATE}', 'dd/mm/yyyy hh24:mi:ss') \n";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Financial_Transactions_Jrnl>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Financial_Transactions_Jrnl Trxs_Codes)
        {
            parametros.Parameters.Add("RECPT_TYPE", Trxs_Codes.RECPT_TYPE);
            parametros.Parameters.Add("RECPT_NO", Trxs_Codes.RECPT_NO);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("EURO_EXCHANGE_RATE", Trxs_Codes.EURO_EXCHANGE_RATE);
            parametros.Parameters.Add("TAX_INCLUSIVE_YN", Trxs_Codes.TAX_INCLUSIVE_YN);
            parametros.Parameters.Add("NET_AMOUNT", Trxs_Codes.NET_AMOUNT);
            parametros.Parameters.Add("GROSS_AMOUNT", Trxs_Codes.GROSS_AMOUNT);
            parametros.Parameters.Add("REVENUE_AMT", Trxs_Codes.REVENUE_AMT);
            parametros.Parameters.Add("PASSER_BY_NAME", Trxs_Codes.PASSER_BY_NAME);
            parametros.Parameters.Add("TRX_NO", Trxs_Codes.TRX_NO);
            parametros.Parameters.Add("FT_SUBTYPE", Trxs_Codes.FT_SUBTYPE);
            parametros.Parameters.Add("TC_GROUP", Trxs_Codes.TC_GROUP);
            parametros.Parameters.Add("TC_SUBGROUP", Trxs_Codes.TC_SUBGROUP);
            parametros.Parameters.Add("TRX_CODE", Trxs_Codes.TRX_CODE);
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID);
            parametros.Parameters.Add("TRX_DATE", Trxs_Codes.TRX_DATE.ToString());
            parametros.Parameters.Add("BUSINESS_DATE", Trxs_Codes.BUSINESS_DATE.ToString());
            parametros.Parameters.Add("CURRENCY", Trxs_Codes.CURRENCY);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("TRX_NO_ADJUST", Trxs_Codes.TRX_NO_ADJUST);
            parametros.Parameters.Add("TRX_NO_ADDED_BY", Trxs_Codes.TRX_NO_ADDED_BY);
            parametros.Parameters.Add("TRX_NO_AGAINST_PACKAGE", Trxs_Codes.TRX_NO_AGAINST_PACKAGE);
            parametros.Parameters.Add("TRX_NO_HEADER", Trxs_Codes.TRX_NO_HEADER);
            parametros.Parameters.Add("AR_NUMBER", Trxs_Codes.AR_NUMBER);
            parametros.Parameters.Add("CASHIER_ID", Trxs_Codes.CASHIER_ID);
            parametros.Parameters.Add("FT_GENERATED_TYPE", Trxs_Codes.FT_GENERATED_TYPE);
            parametros.Parameters.Add("REASON_CODE", Trxs_Codes.REASON_CODE);
            parametros.Parameters.Add("QUANTITY", Trxs_Codes.QUANTITY);
            parametros.Parameters.Add("PRICE_PER_UNIT", Trxs_Codes.PRICE_PER_UNIT);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("TCL_CODE1", Trxs_Codes.TCL_CODE1);
            parametros.Parameters.Add("TCL_CODE2", Trxs_Codes.TCL_CODE2);
            parametros.Parameters.Add("GUEST_ACCOUNT_CREDIT", Trxs_Codes.GUEST_ACCOUNT_CREDIT);
            parametros.Parameters.Add("GUEST_ACCOUNT_DEBIT", Trxs_Codes.GUEST_ACCOUNT_DEBIT);
            parametros.Parameters.Add("TRX_AMOUNT", Trxs_Codes.TRX_AMOUNT);
            parametros.Parameters.Add("POSTED_AMOUNT", Trxs_Codes.POSTED_AMOUNT);
            parametros.Parameters.Add("PACKAGE_CREDIT", Trxs_Codes.PACKAGE_CREDIT);
            parametros.Parameters.Add("PACKAGE_DEBIT", Trxs_Codes.PACKAGE_DEBIT);
            parametros.Parameters.Add("FOLIO_VIEW", Trxs_Codes.FOLIO_VIEW);
            parametros.Parameters.Add("REMARK", Trxs_Codes.REMARK);
            parametros.Parameters.Add("REFERENCE", Trxs_Codes.REFERENCE);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("CREDIT_CARD_ID", Trxs_Codes.CREDIT_CARD_ID);
            parametros.Parameters.Add("NAME_ID", Trxs_Codes.NAME_ID);
            parametros.Parameters.Add("MARKET_CODE", Trxs_Codes.MARKET_CODE);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("DEFERRED_YN", Trxs_Codes.DEFERRED_YN);
            parametros.Parameters.Add("EXCHANGE_RATE", Trxs_Codes.EXCHANGE_RATE);
            parametros.Parameters.Add("DEP_LED_CREDIT", Trxs_Codes.DEP_LED_CREDIT);
            parametros.Parameters.Add("DEP_LED_DEBIT", Trxs_Codes.DEP_LED_DEBIT);
            parametros.Parameters.Add("HOTEL_ACCT", Trxs_Codes.HOTEL_ACCT);
            parametros.Parameters.Add("IND_ADJUSTMENT_YN", Trxs_Codes.IND_ADJUSTMENT_YN);
            parametros.Parameters.Add("ROUTING_INSTRN_ID", Trxs_Codes.ROUTING_INSTRN_ID);
            parametros.Parameters.Add("FROM_RESV_ID", Trxs_Codes.FROM_RESV_ID);
            parametros.Parameters.Add("CASHIER_DEBIT", Trxs_Codes.CASHIER_DEBIT);
            parametros.Parameters.Add("CASHIER_CREDIT", Trxs_Codes.CASHIER_CREDIT);
            parametros.Parameters.Add("TRAN_ACTION_ID", Trxs_Codes.TRAN_ACTION_ID);
            parametros.Parameters.Add("OLD_TRAN_ACTION_ID", Trxs_Codes.OLD_TRAN_ACTION_ID);
            parametros.Parameters.Add("U_D_FLAG", Trxs_Codes.U_D_FLAG);
            parametros.Parameters.Add("FIN_DML_SEQNO", Trxs_Codes.FIN_DML_SEQNO);
            parametros.Parameters.Add("INVOICE_NO", Trxs_Codes.INVOICE_NO);
            parametros.Parameters.Add("AR_LED_CREDIT", Trxs_Codes.AR_LED_CREDIT);
            parametros.Parameters.Add("AR_LED_DEBIT", Trxs_Codes.AR_LED_DEBIT);
            parametros.Parameters.Add("AR_STATE", Trxs_Codes.AR_STATE);
            parametros.Parameters.Add("FOLIO_NO", Trxs_Codes.FOLIO_NO);
            parametros.Parameters.Add("FIXED_CHARGES_YN", Trxs_Codes.FIXED_CHARGES_YN);
            parametros.Parameters.Add("TA_COMMISSIONABLE_YN", Trxs_Codes.TA_COMMISSIONABLE_YN);
            parametros.Parameters.Add("CHEQUE_NUMBER", Trxs_Codes.CHEQUE_NUMBER);
            parametros.Parameters.Add("CASHIER_OPENING_BALANCE", Trxs_Codes.CASHIER_OPENING_BALANCE);
            parametros.Parameters.Add("INVOICE_CLOSE_DATE", Trxs_Codes.INVOICE_CLOSE_DATE.ToString());
            parametros.Parameters.Add("AR_TRANSFER_DATE", Trxs_Codes.AR_TRANSFER_DATE.ToString());
            parametros.Parameters.Add("SOURCE_CODE", Trxs_Codes.SOURCE_CODE);
            parametros.Parameters.Add("O_TRX_DESC", Trxs_Codes.O_TRX_DESC);
            parametros.Parameters.Add("PRODUCT", Trxs_Codes.PRODUCT);
            parametros.Parameters.Add("NUMBER_DIALED", Trxs_Codes.NUMBER_DIALED);
            parametros.Parameters.Add("GEN_CASHIER_ID", Trxs_Codes.GEN_CASHIER_ID);
            parametros.Parameters.Add("TRNS_ACTIVITY_DATE", Trxs_Codes.TRNS_ACTIVITY_DATE.ToString());
            parametros.Parameters.Add("TRNS_FROM_ACCT", Trxs_Codes.TRNS_FROM_ACCT);
            parametros.Parameters.Add("TRNS_TO_ACCT", Trxs_Codes.TRNS_TO_ACCT);
            parametros.Parameters.Add("TARGET_RESORT", Trxs_Codes.TARGET_RESORT);
            parametros.Parameters.Add("INH_DEBIT", Trxs_Codes.INH_DEBIT);
            parametros.Parameters.Add("INH_CREDIT", Trxs_Codes.INH_CREDIT);
            parametros.Parameters.Add("LINK_TRX_NO", Trxs_Codes.LINK_TRX_NO);
            parametros.Parameters.Add("NAME_TAX_TYPE", Trxs_Codes.NAME_TAX_TYPE);
            parametros.Parameters.Add("BILL_NO", Trxs_Codes.BILL_NO);
            parametros.Parameters.Add("DISPLAY_YN", Trxs_Codes.DISPLAY_YN);
            parametros.Parameters.Add("COLL_AGENT_POSTING_YN", Trxs_Codes.COLL_AGENT_POSTING_YN);
            parametros.Parameters.Add("FISCAL_TRX_CODE_TYPE", Trxs_Codes.FISCAL_TRX_CODE_TYPE);
            parametros.Parameters.Add("DEFERRED_TAXES_YN", Trxs_Codes.DEFERRED_TAXES_YN);
            parametros.Parameters.Add("TAX_INV_NO", Trxs_Codes.TAX_INV_NO);
            parametros.Parameters.Add("PAYMENT_TYPE", Trxs_Codes.PAYMENT_TYPE);
            parametros.Parameters.Add("FOLIO_TYPE", Trxs_Codes.FOLIO_TYPE);
            parametros.Parameters.Add("ACTION_ID", Trxs_Codes.ACTION_ID);
            parametros.Parameters.Add("AUTHORIZER_ID", Trxs_Codes.AUTHORIZER_ID);
            parametros.Parameters.Add("APPROVAL_CODE", Trxs_Codes.APPROVAL_CODE);
            parametros.Parameters.Add("APPROVAL_DATE", Trxs_Codes.APPROVAL_DATE.ToString());
            parametros.Parameters.Add("APPROVAL_STATUS", Trxs_Codes.APPROVAL_STATUS);
            parametros.Parameters.Add("COMP_LINK_TRX_NO", Trxs_Codes.COMP_LINK_TRX_NO);
            parametros.Parameters.Add("COMP_LINK_TRX_CODE", Trxs_Codes.COMP_LINK_TRX_CODE);
            parametros.Parameters.Add("POSTING_DATE", Trxs_Codes.POSTING_DATE.ToString());
            parametros.Parameters.Add("MTRX_NO_AGAINST_PACKAGE", Trxs_Codes.MTRX_NO_AGAINST_PACKAGE);
            parametros.Parameters.Add("ADVANCED_GENERATE_YN", Trxs_Codes.ADVANCED_GENERATE_YN);
            parametros.Parameters.Add("FOREX_TYPE", Trxs_Codes.FOREX_TYPE);
            parametros.Parameters.Add("FOREX_COMM_PERC", Trxs_Codes.FOREX_COMM_PERC);
            parametros.Parameters.Add("FOREX_COMM_AMOUNT", Trxs_Codes.FOREX_COMM_AMOUNT);
            parametros.Parameters.Add("ARTICLE_ID", Trxs_Codes.ARTICLE_ID);
            parametros.Parameters.Add("TO_RESV_NAME_ID", Trxs_Codes.TO_RESV_NAME_ID);
            parametros.Parameters.Add("ROOM_NTS", Trxs_Codes.ROOM_NTS);
            parametros.Parameters.Add("COMP_TYPE_CODE", Trxs_Codes.COMP_TYPE_CODE);
            parametros.Parameters.Add("ACC_TYPE_FLAG", Trxs_Codes.ACC_TYPE_FLAG);
            parametros.Parameters.Add("FISCAL_BILL_NO", Trxs_Codes.FISCAL_BILL_NO);
            parametros.Parameters.Add("INVOICE_TYPE", Trxs_Codes.INVOICE_TYPE);
            parametros.Parameters.Add("TAX_ELEMENTS", Trxs_Codes.TAX_ELEMENTS);
            parametros.Parameters.Add("PACKAGE_ALLOWANCE", Trxs_Codes.PACKAGE_ALLOWANCE);
            parametros.Parameters.Add("ORIGINAL_RESV_NAME_ID", Trxs_Codes.ORIGINAL_RESV_NAME_ID);
            parametros.Parameters.Add("ORIGINAL_ROOM", Trxs_Codes.ORIGINAL_ROOM);
            parametros.Parameters.Add("COUPON_NO", Trxs_Codes.COUPON_NO);
            parametros.Parameters.Add("ORG_AR_LED_DEBIT", Trxs_Codes.ORG_AR_LED_DEBIT);
            parametros.Parameters.Add("SETTLEMENT_FLAG", Trxs_Codes.SETTLEMENT_FLAG);
            parametros.Parameters.Add("PROFIT_LOSS_FLAG", Trxs_Codes.PROFIT_LOSS_FLAG);
            parametros.Parameters.Add("CLOSURE_NO", Trxs_Codes.CLOSURE_NO);
            parametros.Parameters.Add("PROFORMA_YN", Trxs_Codes.PROFORMA_YN);
            parametros.Parameters.Add("ALLOWANCE_TYPE", Trxs_Codes.ALLOWANCE_TYPE);
            parametros.Parameters.Add("ADV_GENERATE_ADJUSTMENT", Trxs_Codes.ADV_GENERATE_ADJUSTMENT);
            parametros.Parameters.Add("ADV_GENERATE_TRX_CODE", Trxs_Codes.ADV_GENERATE_TRX_CODE);
            parametros.Parameters.Add("HOLD_YN", Trxs_Codes.HOLD_YN);
            parametros.Parameters.Add("TRX_SERVICE_TYPE", Trxs_Codes.TRX_SERVICE_TYPE);
            parametros.Parameters.Add("ORG_BILL_NO", Trxs_Codes.ORG_BILL_NO);
            parametros.Parameters.Add("ORG_FOLIO_TYPE", Trxs_Codes.ORG_FOLIO_TYPE);
            parametros.Parameters.Add("ARRANGEMENT_ID", Trxs_Codes.ARRANGEMENT_ID);
            parametros.Parameters.Add("CHECK_FILE_ID", Trxs_Codes.CHECK_FILE_ID);
            parametros.Parameters.Add("COMMENTS", Trxs_Codes.COMMENTS);
            parametros.Parameters.Add("COMPRESSED_YN", Trxs_Codes.COMPRESSED_YN);
            parametros.Parameters.Add("COVERS", Trxs_Codes.COVERS);
            parametros.Parameters.Add("FIN_DML_SEQ_NO", Trxs_Codes.FIN_DML_SEQ_NO);
            parametros.Parameters.Add("RESV_DEPOSIT_ID", Trxs_Codes.RESV_DEPOSIT_ID);
            parametros.Parameters.Add("REVISION_NO", Trxs_Codes.REVISION_NO);
            parametros.Parameters.Add("SOURCE_COMMISSION_NET_YN", Trxs_Codes.SOURCE_COMMISSION_NET_YN);
            parametros.Parameters.Add("TAX_GENERATED_YN", Trxs_Codes.TAX_GENERATED_YN);
            parametros.Parameters.Add("TA_COMMISSION_NET_YN", Trxs_Codes.TA_COMMISSION_NET_YN);
            parametros.Parameters.Add("POSTING_TYPE", Trxs_Codes.POSTING_TYPE);
            parametros.Parameters.Add("PARALLEL_GUEST_CREDIT", Trxs_Codes.PARALLEL_GUEST_CREDIT);
            parametros.Parameters.Add("PARALLEL_GUEST_DEBIT", Trxs_Codes.PARALLEL_GUEST_DEBIT);
            parametros.Parameters.Add("PARALLEL_CURRENCY", Trxs_Codes.PARALLEL_CURRENCY);
            parametros.Parameters.Add("EXCHANGE_DIFFERENCE_YN", Trxs_Codes.EXCHANGE_DIFFERENCE_YN);
            parametros.Parameters.Add("MEMBERSHIP_ID", Trxs_Codes.MEMBERSHIP_ID);
            parametros.Parameters.Add("PARALLEL_NET_AMOUNT", Trxs_Codes.PARALLEL_NET_AMOUNT);
            parametros.Parameters.Add("PARALLEL_GROSS_AMOUNT", Trxs_Codes.PARALLEL_GROSS_AMOUNT);
            parametros.Parameters.Add("EXCHANGE_TYPE", Trxs_Codes.EXCHANGE_TYPE);
            parametros.Parameters.Add("EXCHANGE_DATE", Trxs_Codes.EXCHANGE_DATE.ToString());
            parametros.Parameters.Add("INSTALLMENTS", Trxs_Codes.INSTALLMENTS);
            parametros.Parameters.Add("CONTRACT_GUEST_DEBIT", Trxs_Codes.CONTRACT_GUEST_DEBIT);
            parametros.Parameters.Add("CONTRACT_GUEST_CREDIT", Trxs_Codes.CONTRACT_GUEST_CREDIT);
            parametros.Parameters.Add("CONTRACT_NET_AMOUNT", Trxs_Codes.CONTRACT_NET_AMOUNT);
            parametros.Parameters.Add("CONTRACT_GROSS_AMOUNT", Trxs_Codes.CONTRACT_GROSS_AMOUNT);
            parametros.Parameters.Add("CONTRACT_CURRENCY", Trxs_Codes.CONTRACT_CURRENCY);
            parametros.Parameters.Add("CALC_POINTS_YN", Trxs_Codes.CALC_POINTS_YN);
            parametros.Parameters.Add("AR_CHARGE_TRANSFER_YN", Trxs_Codes.AR_CHARGE_TRANSFER_YN);
            parametros.Parameters.Add("ASB_FLAG", Trxs_Codes.ASB_FLAG);
            parametros.Parameters.Add("POSTIT_YN", Trxs_Codes.POSTIT_YN);
            parametros.Parameters.Add("POSTIT_NO", Trxs_Codes.POSTIT_NO);
            parametros.Parameters.Add("ROUTING_DATE", Trxs_Codes.ROUTING_DATE.ToString());
            parametros.Parameters.Add("PACKAGE_TRX_TYPE", Trxs_Codes.PACKAGE_TRX_TYPE);
            parametros.Parameters.Add("CC_TRX_FEE_AMOUNT", Trxs_Codes.CC_TRX_FEE_AMOUNT);
            parametros.Parameters.Add("CHANGE_DUE", Trxs_Codes.CHANGE_DUE);
            parametros.Parameters.Add("POSTING_SOURCE_NAME_ID", Trxs_Codes.POSTING_SOURCE_NAME_ID);
            parametros.Parameters.Add("AUTO_SETTLE_YN", Trxs_Codes.AUTO_SETTLE_YN);
            parametros.Parameters.Add("QUEUE_NAME", Trxs_Codes.QUEUE_NAME);
            parametros.Parameters.Add("DEP_TAX_TRANSFERED_YN", Trxs_Codes.DEP_TAX_TRANSFERED_YN);
            parametros.Parameters.Add("ESIGNED_RECEIPT_NAME", Trxs_Codes.ESIGNED_RECEIPT_NAME);
            parametros.Parameters.Add("BONUS_CHECK_ID", Trxs_Codes.BONUS_CHECK_ID);
            parametros.Parameters.Add("AUTO_CREDITBILL_YN", Trxs_Codes.AUTO_CREDITBILL_YN);
            parametros.Parameters.Add("POSTING_RHYTHM", Trxs_Codes.POSTING_RHYTHM);
            parametros.Parameters.Add("FBA_CERTIFICATE_NUMBER", Trxs_Codes.FBA_CERTIFICATE_NUMBER);
            parametros.Parameters.Add("EXP_ORIGINAL_INVOICE", Trxs_Codes.EXP_ORIGINAL_INVOICE);
            parametros.Parameters.Add("EXP_INVOICE_TYPE", Trxs_Codes.EXP_INVOICE_TYPE);
            parametros.Parameters.Add("ASB_TAX_FLAG", Trxs_Codes.ASB_TAX_FLAG);
            parametros.Parameters.Add("ASB_ONLY_POST_TAXES_ONCE_YN", Trxs_Codes.ASB_ONLY_POST_TAXES_ONCE_YN);
            parametros.Parameters.Add("ROUND_LINK_TRXNO", Trxs_Codes.ROUND_LINK_TRXNO);
            parametros.Parameters.Add("ROUND_FACTOR_YN", Trxs_Codes.ROUND_FACTOR_YN);
            parametros.Parameters.Add("SYSTEM_DATE", Trxs_Codes.SYSTEM_DATE.ToString());
            parametros.Parameters.Add("JRNL_BUSINESS_DATE", Trxs_Codes.JRNL_BUSINESS_DATE.ToString());
            parametros.Parameters.Add("JRNL_USER", Trxs_Codes.JRNL_USER);
            parametros.Parameters.Add("DEP_POSTING_FLAG", Trxs_Codes.DEP_POSTING_FLAG);
            parametros.Parameters.Add("EFFECTIVE_DATE", Trxs_Codes.EFFECTIVE_DATE.ToString());
            parametros.Parameters.Add("PACKAGE_ARRANGEMENT_CODE", Trxs_Codes.PACKAGE_ARRANGEMENT_CODE);
            parametros.Parameters.Add("CORRECTION_YN", Trxs_Codes.CORRECTION_YN);
            parametros.Parameters.Add("ROUTED_YN", Trxs_Codes.ROUTED_YN);
            parametros.Parameters.Add("UPSELL_CHARGE_YN", Trxs_Codes.UPSELL_CHARGE_YN);
            parametros.Parameters.Add("REVERSE_PAYMENT_TRX_NO", Trxs_Codes.REVERSE_PAYMENT_TRX_NO);
            parametros.Parameters.Add("ADVANCE_BILL_YN", Trxs_Codes.ADVANCE_BILL_YN);
            parametros.Parameters.Add("ADVANCE_BILL_REVERSED_YN", Trxs_Codes.ADVANCE_BILL_REVERSED_YN);
            parametros.Parameters.Add("ORG_POSTED_AMOUNT", Trxs_Codes.ORG_POSTED_AMOUNT);
            parametros.Parameters.Add("INC_TAX_DEDUCTED_YN", Trxs_Codes.INC_TAX_DEDUCTED_YN);
            parametros.Parameters.Add("ROOM_NTS_EFFECTIVE", Trxs_Codes.ROOM_NTS_EFFECTIVE);
            parametros.Parameters.Add("THRESHOLD_DIVERSION_ID", Trxs_Codes.THRESHOLD_DIVERSION_ID);
            parametros.Parameters.Add("THRESHOLD_ENTITY_TYPE", Trxs_Codes.THRESHOLD_ENTITY_TYPE);
            parametros.Parameters.Add("THRESHOLD_ENTITY_QTY", Trxs_Codes.THRESHOLD_ENTITY_QTY);
            parametros.Parameters.Add("THRESHOLD_TREATMENT_FLAG", Trxs_Codes.THRESHOLD_TREATMENT_FLAG);
            parametros.Parameters.Add("EXCH_DIFF_TRX_NO", Trxs_Codes.EXCH_DIFF_TRX_NO);
            parametros.Parameters.Add("DEPOSIT_TRANSACTION_ID", Trxs_Codes.DEPOSIT_TRANSACTION_ID);
            parametros.Parameters.Add("ASSOCIATED_TRX_NO", Trxs_Codes.ASSOCIATED_TRX_NO);
            parametros.Parameters.Add("STAMP_DUTY_YN", Trxs_Codes.STAMP_DUTY_YN);
            parametros.Parameters.Add("ASSOCIATED_RECPT_NO", Trxs_Codes.ASSOCIATED_RECPT_NO);
            parametros.Parameters.Add("TAX_RATE", Trxs_Codes.TAX_RATE);
            parametros.Parameters.Add("TAX_RATE_TYPE", Trxs_Codes.TAX_RATE_TYPE);
            parametros.Parameters.Add("VAT_OFFSET_YN", Trxs_Codes.VAT_OFFSET_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }
    }
}
