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
    public class Trxs_CodesJob: IServiceJobs
    {
        private readonly ILogger<Trxs_CodesJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Trxs_CodesJob(ILogger<Trxs_CodesJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Trxs_Codes>(filePath, LoggBuilder);

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
                                $"TRX_CODE = '{trial.TRX_CODE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Trxs_Codes>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Trxs_Codes Trxs_Codes)
        {
            parametros.Parameters.Add("TAX_INCLUSIVE_YN", Trxs_Codes.TAX_INCLUSIVE_YN);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("FREQUENT_FLYER_YN", Trxs_Codes.FREQUENT_FLYER_YN);
            parametros.Parameters.Add("TRX_CODE", Trxs_Codes.TRX_CODE);
            parametros.Parameters.Add("TC_GROUP", Trxs_Codes.TC_GROUP);
            parametros.Parameters.Add("TC_SUBGROUP", Trxs_Codes.TC_SUBGROUP);
            parametros.Parameters.Add("TCL_CODE_DFLT_CL1", Trxs_Codes.TCL_CODE_DFLT_CL1);
            parametros.Parameters.Add("TCL_CODE_DFLT_CL2", Trxs_Codes.TCL_CODE_DFLT_CL2);
            parametros.Parameters.Add("CLASS_1_MANDATORY_YN", Trxs_Codes.CLASS_1_MANDATORY_YN);
            parametros.Parameters.Add("CLASS_2_MANDATORY_YN", Trxs_Codes.CLASS_2_MANDATORY_YN);
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("TC_TRANSACTION_TYPE", Trxs_Codes.TC_TRANSACTION_TYPE);
            parametros.Parameters.Add("IS_MANUAL_POST_ALLOWED", Trxs_Codes.IS_MANUAL_POST_ALLOWED);
            parametros.Parameters.Add("RESULT_INCLUDED_IN_SUM_ARRAY", Trxs_Codes.RESULT_INCLUDED_IN_SUM_ARRAY);
            parametros.Parameters.Add("CC_TYPE", Trxs_Codes.CC_TYPE);
            parametros.Parameters.Add("COMMISSION", Trxs_Codes.COMMISSION);
            parametros.Parameters.Add("CC_CODE", Trxs_Codes.CC_CODE);
            parametros.Parameters.Add("IND_BILLING", Trxs_Codes.IND_BILLING);
            parametros.Parameters.Add("IND_AR", Trxs_Codes.IND_AR);
            parametros.Parameters.Add("CURRENCY", Trxs_Codes.CURRENCY);
            parametros.Parameters.Add("IND_REVENUE_GP", Trxs_Codes.IND_REVENUE_GP);
            parametros.Parameters.Add("ADJ_TRX_CODE", Trxs_Codes.ADJ_TRX_CODE);
            parametros.Parameters.Add("ARRANGE_CODE", Trxs_Codes.ARRANGE_CODE);
            parametros.Parameters.Add("EXPENSE_FOLIO", Trxs_Codes.EXPENSE_FOLIO);
            parametros.Parameters.Add("GROUP_FOLIO", Trxs_Codes.GROUP_FOLIO);
            parametros.Parameters.Add("REV_BUCKET_ID", Trxs_Codes.REV_BUCKET_ID);
            parametros.Parameters.Add("REV_GP_ID", Trxs_Codes.REV_GP_ID);
            parametros.Parameters.Add("IND_CASH", Trxs_Codes.IND_CASH);
            parametros.Parameters.Add("IND_DEPOSIT_YN", Trxs_Codes.IND_DEPOSIT_YN);
            parametros.Parameters.Add("DEFERRED_YN", Trxs_Codes.DEFERRED_YN);
            parametros.Parameters.Add("AR_NAME_ID", Trxs_Codes.AR_NAME_ID);
            parametros.Parameters.Add("TRX_CODE_TYPE", Trxs_Codes.TRX_CODE_TYPE);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("TC_RESORT", Trxs_Codes.TC_RESORT);
            parametros.Parameters.Add("TC_BOF_REF_CODE", Trxs_Codes.TC_BOF_REF_CODE);
            parametros.Parameters.Add("TC_BOF_INTERFACE", Trxs_Codes.TC_BOF_INTERFACE);
            parametros.Parameters.Add("TC_RESORT2", Trxs_Codes.TC_RESORT2);
            parametros.Parameters.Add("TC_BOF_REF_CODE2", Trxs_Codes.TC_BOF_REF_CODE2);
            parametros.Parameters.Add("TC_BOF_INTERFACE2", Trxs_Codes.TC_BOF_INTERFACE2);
            parametros.Parameters.Add("CRS_TAX_DESC", Trxs_Codes.CRS_TAX_DESC);
            parametros.Parameters.Add("TAX_CODE_NO", Trxs_Codes.TAX_CODE_NO);
            parametros.Parameters.Add("TRX_ACTION_ID", Trxs_Codes.TRX_ACTION_ID);
            parametros.Parameters.Add("EXPORT_BUCKET", Trxs_Codes.EXPORT_BUCKET);
            parametros.Parameters.Add("INH_SALES_YN", Trxs_Codes.INH_SALES_YN);
            parametros.Parameters.Add("INH_PAY_YN", Trxs_Codes.INH_PAY_YN);
            parametros.Parameters.Add("INH_DEPOSIT_YN", Trxs_Codes.INH_DEPOSIT_YN);
            parametros.Parameters.Add("FISCAL_TRX_CODE_TYPE", Trxs_Codes.FISCAL_TRX_CODE_TYPE);
            parametros.Parameters.Add("COMP_YN", Trxs_Codes.COMP_YN);
            parametros.Parameters.Add("DEFAULT_PRICE", Trxs_Codes.DEFAULT_PRICE);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("PAYMENT_TAX_INVOICE_YN", Trxs_Codes.PAYMENT_TAX_INVOICE_YN);
            parametros.Parameters.Add("INTERNAL_YN", Trxs_Codes.INTERNAL_YN);
            parametros.Parameters.Add("FISCAL_PAYMENT_YN", Trxs_Codes.FISCAL_PAYMENT_YN);
            parametros.Parameters.Add("COMP_NIGHTS_YN", Trxs_Codes.COMP_NIGHTS_YN);
            parametros.Parameters.Add("ROTATION_REV_YN", Trxs_Codes.ROTATION_REV_YN);
            parametros.Parameters.Add("OWNER_REV_YN", Trxs_Codes.OWNER_REV_YN);
            parametros.Parameters.Add("CHECK_NO_MANDATORY_YN", Trxs_Codes.CHECK_NO_MANDATORY_YN);
            parametros.Parameters.Add("DED_OWNER_REV_YN", Trxs_Codes.DED_OWNER_REV_YN);
            parametros.Parameters.Add("NON_TAXABLE_YN", Trxs_Codes.NON_TAXABLE_YN);
            parametros.Parameters.Add("COMP_PAYMENT_YN", Trxs_Codes.COMP_PAYMENT_YN);
            parametros.Parameters.Add("MIN_AMT", Trxs_Codes.MIN_AMT);
            parametros.Parameters.Add("MAX_AMT", Trxs_Codes.MAX_AMT);
            parametros.Parameters.Add("TRX_SERVICE_TYPE", Trxs_Codes.TRX_SERVICE_TYPE);
            parametros.Parameters.Add("DAILY_PLAN_FOLIO", Trxs_Codes.DAILY_PLAN_FOLIO);
            parametros.Parameters.Add("TRX_CODE_DISPLAY", Trxs_Codes.TRX_CODE_DISPLAY);
            parametros.Parameters.Add("INCLUDE_IN_DEPOSIT_RULE_YN", Trxs_Codes.INCLUDE_IN_DEPOSIT_RULE_YN);
            parametros.Parameters.Add("MANUAL_POST_COVERS_YN", Trxs_Codes.MANUAL_POST_COVERS_YN);
            parametros.Parameters.Add("INCLUDE_IN_8300_YN", Trxs_Codes.INCLUDE_IN_8300_YN);
            parametros.Parameters.Add("ROUND_FACTOR_YN", Trxs_Codes.ROUND_FACTOR_YN);
            parametros.Parameters.Add("DEPOSIT_POSTING_ONLY_YN", Trxs_Codes.DEPOSIT_POSTING_ONLY_YN);
            parametros.Parameters.Add("TRX_TAX_TYPE_CODE", Trxs_Codes.TRX_TAX_TYPE_CODE);
            parametros.Parameters.Add("DEPOSIT_TYPE", Trxs_Codes.DEPOSIT_TYPE);
            parametros.Parameters.Add("GP_POINTS_REDEMPTION_YN", Trxs_Codes.GP_POINTS_REDEMPTION_YN);
            parametros.Parameters.Add("E_INVOICE_YN", Trxs_Codes.E_INVOICE_YN);
            parametros.Parameters.Add("CORP_PROP_FLAG", Trxs_Codes.CORP_PROP_FLAG);
            parametros.Parameters.Add("CORPORATE_DESCRIPTION", Trxs_Codes.CORPORATE_DESCRIPTION);
            parametros.Parameters.Add("PRINT_RECEIPT_YN", Trxs_Codes.PRINT_RECEIPT_YN);
            parametros.Parameters.Add("SERVICE_RECOVERY_TRX_CODE", Trxs_Codes.SERVICE_RECOVERY_TRX_CODE);
            parametros.Parameters.Add("EXTERNAL_PAYMENT_CODE", Trxs_Codes.EXTERNAL_PAYMENT_CODE);
            parametros.Parameters.Add("ACCOUNTING_CODE", Trxs_Codes.ACCOUNTING_CODE);
            parametros.Parameters.Add("QUANTITY_CODE", Trxs_Codes.QUANTITY_CODE);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
