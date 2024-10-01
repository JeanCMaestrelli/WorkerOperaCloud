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
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Folio_TaxJob : IServiceJobs
    {
        private readonly ILogger<Folio_TaxJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Folio_TaxJob(ILogger<Folio_TaxJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Folio_Tax>(filePath, LoggBuilder);

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
                                $"BILL_NO = '{trial.BILL_NO}' and " +
                                $"FOLIO_NO = '{trial.FOLIO_NO}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Folio_Tax>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Folio_Tax Trxs_Codes)
        {
            parametros.Parameters.Add("BUSINESS_DATE", Trxs_Codes.BUSINESS_DATE.ToString());
            parametros.Parameters.Add("BILL_NO", Trxs_Codes.BILL_NO);
            parametros.Parameters.Add("BILL_GENERATION_DATE", Trxs_Codes.BILL_GENERATION_DATE.ToString());
            parametros.Parameters.Add("NAME_ID", Trxs_Codes.NAME_ID);
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("REVISION_NO", Trxs_Codes.REVISION_NO);
            parametros.Parameters.Add("FOLIO_VIEW", Trxs_Codes.FOLIO_VIEW);
            parametros.Parameters.Add("TOT_REV_TAXABLE", Trxs_Codes.TOT_REV_TAXABLE);
            parametros.Parameters.Add("TOT_NONREV_TAXABLE", Trxs_Codes.TOT_NONREV_TAXABLE);
            parametros.Parameters.Add("TOT_REV_NONTAXABLE", Trxs_Codes.TOT_REV_NONTAXABLE);
            parametros.Parameters.Add("TOT_NONREV_NONTAXABLE", Trxs_Codes.TOT_NONREV_NONTAXABLE);
            parametros.Parameters.Add("DEPOSIT", Trxs_Codes.DEPOSIT);
            parametros.Parameters.Add("CASHPAY", Trxs_Codes.CASHPAY);
            parametros.Parameters.Add("CCPAY", Trxs_Codes.CCPAY);
            parametros.Parameters.Add("CLPAY", Trxs_Codes.CLPAY);
            parametros.Parameters.Add("PAIDOUT", Trxs_Codes.PAIDOUT);
            parametros.Parameters.Add("TOTAL_NET", Trxs_Codes.TOTAL_NET);
            parametros.Parameters.Add("TOTAL_GROSS", Trxs_Codes.TOTAL_GROSS);
            parametros.Parameters.Add("TOTAL_NONTAXABLE", Trxs_Codes.TOTAL_NONTAXABLE);
            parametros.Parameters.Add("TAX1_AMT", Trxs_Codes.TAX1_AMT);
            parametros.Parameters.Add("TAX2_AMT", Trxs_Codes.TAX2_AMT);
            parametros.Parameters.Add("TAX3_AMT", Trxs_Codes.TAX3_AMT);
            parametros.Parameters.Add("TAX4_AMT", Trxs_Codes.TAX4_AMT);
            parametros.Parameters.Add("TAX5_AMT", Trxs_Codes.TAX5_AMT);
            parametros.Parameters.Add("TAX6_AMT", Trxs_Codes.TAX6_AMT);
            parametros.Parameters.Add("TAX7_AMT", Trxs_Codes.TAX7_AMT);
            parametros.Parameters.Add("TAX8_AMT", Trxs_Codes.TAX8_AMT);
            parametros.Parameters.Add("TAX9_AMT", Trxs_Codes.TAX9_AMT);
            parametros.Parameters.Add("TAX10_AMT", Trxs_Codes.TAX10_AMT);
            parametros.Parameters.Add("NET1_AMT", Trxs_Codes.NET1_AMT);
            parametros.Parameters.Add("NET2_AMT", Trxs_Codes.NET2_AMT);
            parametros.Parameters.Add("NET3_AMT", Trxs_Codes.NET3_AMT);
            parametros.Parameters.Add("NET4_AMT", Trxs_Codes.NET4_AMT);
            parametros.Parameters.Add("NET5_AMT", Trxs_Codes.NET5_AMT);
            parametros.Parameters.Add("NET6_AMT", Trxs_Codes.NET6_AMT);
            parametros.Parameters.Add("NET7_AMT", Trxs_Codes.NET7_AMT);
            parametros.Parameters.Add("NET8_AMT", Trxs_Codes.NET8_AMT);
            parametros.Parameters.Add("NET9_AMT", Trxs_Codes.NET9_AMT);
            parametros.Parameters.Add("NET10_AMT", Trxs_Codes.NET10_AMT);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("CLARPAY", Trxs_Codes.CLARPAY);
            parametros.Parameters.Add("STATUS", Trxs_Codes.STATUS);
            parametros.Parameters.Add("QUEUE_NAME", Trxs_Codes.QUEUE_NAME);
            parametros.Parameters.Add("ASSOCIATED_BILL_NO", Trxs_Codes.ASSOCIATED_BILL_NO);
            parametros.Parameters.Add("NAME_TAX_TYPE", Trxs_Codes.NAME_TAX_TYPE);
            parametros.Parameters.Add("TAX_ID", Trxs_Codes.TAX_ID);
            parametros.Parameters.Add("PAYEE_NAME_ID", Trxs_Codes.PAYEE_NAME_ID);
            parametros.Parameters.Add("FOLIO_TYPE", Trxs_Codes.FOLIO_TYPE);
            parametros.Parameters.Add("BILL_START_DATE", Trxs_Codes.BILL_START_DATE.ToString());
            parametros.Parameters.Add("COMPRESS_BILL_NO", Trxs_Codes.COMPRESS_BILL_NO);
            parametros.Parameters.Add("FOLIO_NO", Trxs_Codes.FOLIO_NO);
            parametros.Parameters.Add("ACCOUNT_CODE", Trxs_Codes.ACCOUNT_CODE);
            parametros.Parameters.Add("CL_TRX_NO", Trxs_Codes.CL_TRX_NO);
            parametros.Parameters.Add("CASHIER_ID", Trxs_Codes.CASHIER_ID);
            parametros.Parameters.Add("INVOICE_NO", Trxs_Codes.INVOICE_NO);
            parametros.Parameters.Add("BILL_PAYMENT_TRX_NO", Trxs_Codes.BILL_PAYMENT_TRX_NO);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("ASSOCIATED_BILL_DATE", Trxs_Codes.ASSOCIATED_BILL_DATE.ToString());
            parametros.Parameters.Add("FISCAL_BILL_NO", Trxs_Codes.FISCAL_BILL_NO);
            parametros.Parameters.Add("COLL_TAX1", Trxs_Codes.COLL_TAX1);
            parametros.Parameters.Add("COLL_TAX2", Trxs_Codes.COLL_TAX2);
            parametros.Parameters.Add("COLL_TAX3", Trxs_Codes.COLL_TAX3);
            parametros.Parameters.Add("COLL_TAX4", Trxs_Codes.COLL_TAX4);
            parametros.Parameters.Add("COLL_TAX5", Trxs_Codes.COLL_TAX5);
            parametros.Parameters.Add("FISCAL_BILL_CHECK_DIGIT", Trxs_Codes.FISCAL_BILL_CHECK_DIGIT);
            parametros.Parameters.Add("PALM_VIDEO_FLAG", Trxs_Codes.PALM_VIDEO_FLAG);
            parametros.Parameters.Add("XTAX1_AMT", Trxs_Codes.XTAX1_AMT);
            parametros.Parameters.Add("XTAX2_AMT", Trxs_Codes.XTAX2_AMT);
            parametros.Parameters.Add("XTAX3_AMT", Trxs_Codes.XTAX3_AMT);
            parametros.Parameters.Add("XTAX4_AMT", Trxs_Codes.XTAX4_AMT);
            parametros.Parameters.Add("XTAX5_AMT", Trxs_Codes.XTAX5_AMT);
            parametros.Parameters.Add("XTAX6_AMT", Trxs_Codes.XTAX6_AMT);
            parametros.Parameters.Add("XTAX7_AMT", Trxs_Codes.XTAX7_AMT);
            parametros.Parameters.Add("XTAX8_AMT", Trxs_Codes.XTAX8_AMT);
            parametros.Parameters.Add("XTAX9_AMT", Trxs_Codes.XTAX9_AMT);
            parametros.Parameters.Add("XTAX10_AMT", Trxs_Codes.XTAX10_AMT);
            parametros.Parameters.Add("XNET1_AMT", Trxs_Codes.XNET1_AMT);
            parametros.Parameters.Add("XNET2_AMT", Trxs_Codes.XNET2_AMT);
            parametros.Parameters.Add("XNET3_AMT", Trxs_Codes.XNET3_AMT);
            parametros.Parameters.Add("XNET4_AMT", Trxs_Codes.XNET4_AMT);
            parametros.Parameters.Add("XNET5_AMT", Trxs_Codes.XNET5_AMT);
            parametros.Parameters.Add("XNET6_AMT", Trxs_Codes.XNET6_AMT);
            parametros.Parameters.Add("XNET7_AMT", Trxs_Codes.XNET7_AMT);
            parametros.Parameters.Add("XNET8_AMT", Trxs_Codes.XNET8_AMT);
            parametros.Parameters.Add("XNET9_AMT", Trxs_Codes.XNET9_AMT);
            parametros.Parameters.Add("XNET10_AMT", Trxs_Codes.XNET10_AMT);
            parametros.Parameters.Add("BILL_PREFIX", Trxs_Codes.BILL_PREFIX);
            parametros.Parameters.Add("NO_OF_PAGES", Trxs_Codes.NO_OF_PAGES);
            parametros.Parameters.Add("BILL_SEQ_NO", Trxs_Codes.BILL_SEQ_NO);
            parametros.Parameters.Add("PAGE_NUMBER", Trxs_Codes.PAGE_NUMBER);
            parametros.Parameters.Add("PTAX1_AMT", Trxs_Codes.PTAX1_AMT);
            parametros.Parameters.Add("PTAX2_AMT", Trxs_Codes.PTAX2_AMT);
            parametros.Parameters.Add("PTAX3_AMT", Trxs_Codes.PTAX3_AMT);
            parametros.Parameters.Add("PTAX4_AMT", Trxs_Codes.PTAX4_AMT);
            parametros.Parameters.Add("PTAX5_AMT", Trxs_Codes.PTAX5_AMT);
            parametros.Parameters.Add("PTAX6_AMT", Trxs_Codes.PTAX6_AMT);
            parametros.Parameters.Add("PTAX7_AMT", Trxs_Codes.PTAX7_AMT);
            parametros.Parameters.Add("PTAX8_AMT", Trxs_Codes.PTAX8_AMT);
            parametros.Parameters.Add("PTAX9_AMT", Trxs_Codes.PTAX9_AMT);
            parametros.Parameters.Add("PTAX10_AMT", Trxs_Codes.PTAX10_AMT);
            parametros.Parameters.Add("PNET1_AMT", Trxs_Codes.PNET1_AMT);
            parametros.Parameters.Add("PNET2_AMT", Trxs_Codes.PNET2_AMT);
            parametros.Parameters.Add("PNET3_AMT", Trxs_Codes.PNET3_AMT);
            parametros.Parameters.Add("PNET4_AMT", Trxs_Codes.PNET4_AMT);
            parametros.Parameters.Add("PNET5_AMT", Trxs_Codes.PNET5_AMT);
            parametros.Parameters.Add("PNET6_AMT", Trxs_Codes.PNET6_AMT);
            parametros.Parameters.Add("PNET7_AMT", Trxs_Codes.PNET7_AMT);
            parametros.Parameters.Add("PNET8_AMT", Trxs_Codes.PNET8_AMT);
            parametros.Parameters.Add("PNET9_AMT", Trxs_Codes.PNET9_AMT);
            parametros.Parameters.Add("PNET10_AMT", Trxs_Codes.PNET10_AMT);
            parametros.Parameters.Add("BILL_GENERATION_TIME", Trxs_Codes.BILL_GENERATION_TIME);
            parametros.Parameters.Add("POSTIT_YN", Trxs_Codes.POSTIT_YN);
            parametros.Parameters.Add("POSTIT_NO", Trxs_Codes.POSTIT_NO);
            parametros.Parameters.Add("TERMINAL", Trxs_Codes.TERMINAL);
            parametros.Parameters.Add("FISCAL_UNIT_CONTROL_CODE", Trxs_Codes.FISCAL_UNIT_CONTROL_CODE);
            parametros.Parameters.Add("FOLIO_ATTACHMENT_LINK_ID", Trxs_Codes.FOLIO_ATTACHMENT_LINK_ID);
            parametros.Parameters.Add("FOLIO_ATTACHMENT_STATUS", Trxs_Codes.FOLIO_ATTACHMENT_STATUS);
            parametros.Parameters.Add("CREDIT_BILL_GENERATED_YN", Trxs_Codes.CREDIT_BILL_GENERATED_YN);
            parametros.Parameters.Add("ADDRESSEE_NAME_ID", Trxs_Codes.ADDRESSEE_NAME_ID);
            parametros.Parameters.Add("SIGNATURE_HASH", Trxs_Codes.SIGNATURE_HASH);
            parametros.Parameters.Add("LAST_SIGNATURE_HASH", Trxs_Codes.LAST_SIGNATURE_HASH);
            parametros.Parameters.Add("SIGNATURE_KEY_VERSION", Trxs_Codes.SIGNATURE_KEY_VERSION);
            parametros.Parameters.Add("FOLIO_ADDRESS", Trxs_Codes.FOLIO_ADDRESS);
            parametros.Parameters.Add("FOLIO_ADDRESS_CORRECTED_YN", Trxs_Codes.FOLIO_ADDRESS_CORRECTED_YN);
            parametros.Parameters.Add("TAX11_AMT", Trxs_Codes.TAX11_AMT);
            parametros.Parameters.Add("TAX12_AMT", Trxs_Codes.TAX12_AMT);
            parametros.Parameters.Add("TAX13_AMT", Trxs_Codes.TAX13_AMT);
            parametros.Parameters.Add("TAX14_AMT", Trxs_Codes.TAX14_AMT);
            parametros.Parameters.Add("TAX15_AMT", Trxs_Codes.TAX15_AMT);
            parametros.Parameters.Add("TAX16_AMT", Trxs_Codes.TAX16_AMT);
            parametros.Parameters.Add("TAX17_AMT", Trxs_Codes.TAX17_AMT);
            parametros.Parameters.Add("TAX18_AMT", Trxs_Codes.TAX18_AMT);
            parametros.Parameters.Add("TAX19_AMT", Trxs_Codes.TAX19_AMT);
            parametros.Parameters.Add("TAX20_AMT", Trxs_Codes.TAX20_AMT);
            parametros.Parameters.Add("NET11_AMT", Trxs_Codes.NET11_AMT);
            parametros.Parameters.Add("NET12_AMT", Trxs_Codes.NET12_AMT);
            parametros.Parameters.Add("NET13_AMT", Trxs_Codes.NET13_AMT);
            parametros.Parameters.Add("NET14_AMT", Trxs_Codes.NET14_AMT);
            parametros.Parameters.Add("NET15_AMT", Trxs_Codes.NET15_AMT);
            parametros.Parameters.Add("NET16_AMT", Trxs_Codes.NET16_AMT);
            parametros.Parameters.Add("NET17_AMT", Trxs_Codes.NET17_AMT);
            parametros.Parameters.Add("NET18_AMT", Trxs_Codes.NET18_AMT);
            parametros.Parameters.Add("NET19_AMT", Trxs_Codes.NET19_AMT);
            parametros.Parameters.Add("NET20_AMT", Trxs_Codes.NET20_AMT);
            parametros.Parameters.Add("PTAX11_AMT", Trxs_Codes.PTAX11_AMT);
            parametros.Parameters.Add("PTAX12_AMT", Trxs_Codes.PTAX12_AMT);
            parametros.Parameters.Add("PTAX13_AMT", Trxs_Codes.PTAX13_AMT);
            parametros.Parameters.Add("PTAX14_AMT", Trxs_Codes.PTAX14_AMT);
            parametros.Parameters.Add("PTAX15_AMT", Trxs_Codes.PTAX15_AMT);
            parametros.Parameters.Add("PTAX16_AMT", Trxs_Codes.PTAX16_AMT);
            parametros.Parameters.Add("PTAX17_AMT", Trxs_Codes.PTAX17_AMT);
            parametros.Parameters.Add("PTAX18_AMT", Trxs_Codes.PTAX18_AMT);
            parametros.Parameters.Add("PTAX19_AMT", Trxs_Codes.PTAX19_AMT);
            parametros.Parameters.Add("PTAX20_AMT", Trxs_Codes.PTAX20_AMT);
            parametros.Parameters.Add("PNET11_AMT", Trxs_Codes.PNET11_AMT);
            parametros.Parameters.Add("PNET12_AMT", Trxs_Codes.PNET12_AMT);
            parametros.Parameters.Add("PNET13_AMT", Trxs_Codes.PNET13_AMT);
            parametros.Parameters.Add("PNET14_AMT", Trxs_Codes.PNET14_AMT);
            parametros.Parameters.Add("PNET15_AMT", Trxs_Codes.PNET15_AMT);
            parametros.Parameters.Add("PNET16_AMT", Trxs_Codes.PNET16_AMT);
            parametros.Parameters.Add("PNET17_AMT", Trxs_Codes.PNET17_AMT);
            parametros.Parameters.Add("PNET18_AMT", Trxs_Codes.PNET18_AMT);
            parametros.Parameters.Add("PNET19_AMT", Trxs_Codes.PNET19_AMT);
            parametros.Parameters.Add("PNET20_AMT", Trxs_Codes.PNET20_AMT);
            parametros.Parameters.Add("XTAX11_AMT", Trxs_Codes.XTAX11_AMT);
            parametros.Parameters.Add("XTAX12_AMT", Trxs_Codes.XTAX12_AMT);
            parametros.Parameters.Add("XTAX13_AMT", Trxs_Codes.XTAX13_AMT);
            parametros.Parameters.Add("XTAX14_AMT", Trxs_Codes.XTAX14_AMT);
            parametros.Parameters.Add("XTAX15_AMT", Trxs_Codes.XTAX15_AMT);
            parametros.Parameters.Add("XTAX16_AMT", Trxs_Codes.XTAX16_AMT);
            parametros.Parameters.Add("XTAX17_AMT", Trxs_Codes.XTAX17_AMT);
            parametros.Parameters.Add("XTAX18_AMT", Trxs_Codes.XTAX18_AMT);
            parametros.Parameters.Add("XTAX19_AMT", Trxs_Codes.XTAX19_AMT);
            parametros.Parameters.Add("XTAX20_AMT", Trxs_Codes.XTAX20_AMT);
            parametros.Parameters.Add("XNET11_AMT", Trxs_Codes.XNET11_AMT);
            parametros.Parameters.Add("XNET12_AMT", Trxs_Codes.XNET12_AMT);
            parametros.Parameters.Add("XNET13_AMT", Trxs_Codes.XNET13_AMT);
            parametros.Parameters.Add("XNET14_AMT", Trxs_Codes.XNET14_AMT);
            parametros.Parameters.Add("XNET15_AMT", Trxs_Codes.XNET15_AMT);
            parametros.Parameters.Add("XNET16_AMT", Trxs_Codes.XNET16_AMT);
            parametros.Parameters.Add("XNET17_AMT", Trxs_Codes.XNET17_AMT);
            parametros.Parameters.Add("XNET18_AMT", Trxs_Codes.XNET18_AMT);
            parametros.Parameters.Add("XNET19_AMT", Trxs_Codes.XNET19_AMT);
            parametros.Parameters.Add("XNET20_AMT", Trxs_Codes.XNET20_AMT);
            parametros.Parameters.Add("DEPOSIT_REQ_RECEIPT_NO", Trxs_Codes.DEPOSIT_REQ_RECEIPT_NO);
            parametros.Parameters.Add("PTOT_REV_TAXABLE", Trxs_Codes.PTOT_REV_TAXABLE);
            parametros.Parameters.Add("PTOT_NONREV_TAXABLE", Trxs_Codes.PTOT_NONREV_TAXABLE);
            parametros.Parameters.Add("PTOT_REV_NONTAXABLE", Trxs_Codes.PTOT_REV_NONTAXABLE);
            parametros.Parameters.Add("PTOT_NONREV_NONTAXABLE", Trxs_Codes.PTOT_NONREV_NONTAXABLE);
            parametros.Parameters.Add("E_INVOICE_STATUS", Trxs_Codes.E_INVOICE_STATUS);
            parametros.Parameters.Add("E_INVOICE_NUMBER", Trxs_Codes.E_INVOICE_NUMBER);
            parametros.Parameters.Add("FOLIO_TAX_SEQ_NO", Trxs_Codes.FOLIO_TAX_SEQ_NO);
            parametros.Parameters.Add("WORKING_DOC_ID", Trxs_Codes.WORKING_DOC_ID);
            parametros.Parameters.Add("HAS_WATERMARK_YN", Trxs_Codes.HAS_WATERMARK_YN);
            parametros.Parameters.Add("ASSOCIATED_FISCAL_BILL_NO", Trxs_Codes.ASSOCIATED_FISCAL_BILL_NO);
            parametros.Parameters.Add("ASSOCIATED_FISCAL_BILL_DATE", Trxs_Codes.ASSOCIATED_FISCAL_BILL_DATE.ToString());
            parametros.Parameters.Add("ASSOCIATED_FISCAL_BILL_TIME", Trxs_Codes.ASSOCIATED_FISCAL_BILL_TIME);
            parametros.Parameters.Add("PROPERTY_BILL_PREFIX", Trxs_Codes.PROPERTY_BILL_PREFIX);
            parametros.Parameters.Add("E_ARCHIVE_NUMBER", Trxs_Codes.E_ARCHIVE_NUMBER);
            parametros.Parameters.Add("E_ARCHIVE_STATUS", Trxs_Codes.E_ARCHIVE_STATUS);
            parametros.Parameters.Add("E_ARCHIVE_ETTN_NUMBER", Trxs_Codes.E_ARCHIVE_ETTN_NUMBER);
            parametros.Parameters.Add("E_ARCHIVE_REPORT_NO", Trxs_Codes.E_ARCHIVE_REPORT_NO);
            parametros.Parameters.Add("CASH_REGISTER_INV_NO", Trxs_Codes.CASH_REGISTER_INV_NO);
            parametros.Parameters.Add("CASH_REGISTER_ID", Trxs_Codes.CASH_REGISTER_ID);
            parametros.Parameters.Add("FISCAL_INVOICE_CURRENCY", Trxs_Codes.FISCAL_INVOICE_CURRENCY);
            parametros.Parameters.Add("FISCAL_INVOICE_CURRENCY_RATE", Trxs_Codes.FISCAL_INVOICE_CURRENCY_RATE);
            parametros.Parameters.Add("SEQ_NO", Trxs_Codes.SEQ_NO);
            parametros.Parameters.Add("VOID_REASON", Trxs_Codes.VOID_REASON);
            parametros.Parameters.Add("TAX1_RATE", Trxs_Codes.TAX1_RATE);
            parametros.Parameters.Add("TAX2_RATE", Trxs_Codes.TAX2_RATE);
            parametros.Parameters.Add("TAX3_RATE", Trxs_Codes.TAX3_RATE);
            parametros.Parameters.Add("TAX4_RATE", Trxs_Codes.TAX4_RATE);
            parametros.Parameters.Add("TAX5_RATE", Trxs_Codes.TAX5_RATE);
            parametros.Parameters.Add("TAX6_RATE", Trxs_Codes.TAX6_RATE);
            parametros.Parameters.Add("TAX7_RATE", Trxs_Codes.TAX7_RATE);
            parametros.Parameters.Add("TAX8_RATE", Trxs_Codes.TAX8_RATE);
            parametros.Parameters.Add("TAX9_RATE", Trxs_Codes.TAX9_RATE);
            parametros.Parameters.Add("TAX10_RATE", Trxs_Codes.TAX10_RATE);
            parametros.Parameters.Add("TAX11_RATE", Trxs_Codes.TAX11_RATE);
            parametros.Parameters.Add("TAX12_RATE", Trxs_Codes.TAX12_RATE);
            parametros.Parameters.Add("TAX13_RATE", Trxs_Codes.TAX13_RATE);
            parametros.Parameters.Add("TAX14_RATE", Trxs_Codes.TAX14_RATE);
            parametros.Parameters.Add("TAX15_RATE", Trxs_Codes.TAX15_RATE);
            parametros.Parameters.Add("TAX16_RATE", Trxs_Codes.TAX16_RATE);
            parametros.Parameters.Add("TAX17_RATE", Trxs_Codes.TAX17_RATE);
            parametros.Parameters.Add("TAX18_RATE", Trxs_Codes.TAX18_RATE);
            parametros.Parameters.Add("TAX19_RATE", Trxs_Codes.TAX19_RATE);
            parametros.Parameters.Add("TAX20_RATE", Trxs_Codes.TAX20_RATE);
            parametros.Parameters.Add("TAX1_RATE_TYPE", Trxs_Codes.TAX1_RATE_TYPE);
            parametros.Parameters.Add("TAX2_RATE_TYPE", Trxs_Codes.TAX2_RATE_TYPE);
            parametros.Parameters.Add("TAX3_RATE_TYPE", Trxs_Codes.TAX3_RATE_TYPE);
            parametros.Parameters.Add("TAX4_RATE_TYPE", Trxs_Codes.TAX4_RATE_TYPE);
            parametros.Parameters.Add("TAX5_RATE_TYPE", Trxs_Codes.TAX5_RATE_TYPE);
            parametros.Parameters.Add("TAX6_RATE_TYPE", Trxs_Codes.TAX6_RATE_TYPE);
            parametros.Parameters.Add("TAX7_RATE_TYPE", Trxs_Codes.TAX7_RATE_TYPE);
            parametros.Parameters.Add("TAX8_RATE_TYPE", Trxs_Codes.TAX8_RATE_TYPE);
            parametros.Parameters.Add("TAX9_RATE_TYPE", Trxs_Codes.TAX9_RATE_TYPE);
            parametros.Parameters.Add("TAX10_RATE_TYPE", Trxs_Codes.TAX10_RATE_TYPE);
            parametros.Parameters.Add("TAX11_RATE_TYPE", Trxs_Codes.TAX11_RATE_TYPE);
            parametros.Parameters.Add("TAX12_RATE_TYPE", Trxs_Codes.TAX12_RATE_TYPE);
            parametros.Parameters.Add("TAX13_RATE_TYPE", Trxs_Codes.TAX13_RATE_TYPE);
            parametros.Parameters.Add("TAX14_RATE_TYPE", Trxs_Codes.TAX14_RATE_TYPE);
            parametros.Parameters.Add("TAX15_RATE_TYPE", Trxs_Codes.TAX15_RATE_TYPE);
            parametros.Parameters.Add("TAX16_RATE_TYPE", Trxs_Codes.TAX16_RATE_TYPE);
            parametros.Parameters.Add("TAX17_RATE_TYPE", Trxs_Codes.TAX17_RATE_TYPE);
            parametros.Parameters.Add("TAX18_RATE_TYPE", Trxs_Codes.TAX18_RATE_TYPE);
            parametros.Parameters.Add("TAX19_RATE_TYPE", Trxs_Codes.TAX19_RATE_TYPE);
            parametros.Parameters.Add("TAX20_RATE_TYPE", Trxs_Codes.TAX20_RATE_TYPE);
            parametros.Parameters.Add("TRANSACTION_SIGNATURE", Trxs_Codes.TRANSACTION_SIGNATURE);
            parametros.Parameters.Add("PAYEE_NAME", Trxs_Codes.PAYEE_NAME);
            parametros.Parameters.Add("PAYEE_ZIP_CODE", Trxs_Codes.PAYEE_ZIP_CODE);

            //long INS_TIMESTAMP = new DateTimeOffset(Trxs_Codes.INS_TIMESTAMP).ToUnixTimeSeconds();
            parametros.Parameters.Add("INS_TIMESTAMP", Trxs_Codes.INS_TIMESTAMP.ToString());

            //long UPD_TIMESTAMP = new DateTimeOffset(Trxs_Codes.UPD_TIMESTAMP).ToUnixTimeSeconds();
            parametros.Parameters.Add("UPD_TIMESTAMP", Trxs_Codes.UPD_TIMESTAMP.ToString());

            parametros.Parameters.Add("FOREX_TAX_YN", Trxs_Codes.FOREX_TAX_YN);
            parametros.Parameters.Add("TRANSACT_SIGNATURE_HASH", Trxs_Codes.TRANSACT_SIGNATURE_HASH);
            parametros.Parameters.Add("TRANSACT_LAST_SIGNATURE_HASH", Trxs_Codes.TRANSACT_LAST_SIGNATURE_HASH);
            parametros.Parameters.Add("TRANSACT_SIGNATURE_KEY_VERSION", Trxs_Codes.TRANSACT_SIGNATURE_KEY_VERSION);
            parametros.Parameters.Add("DAILY_RUNNING_TOTAL", Trxs_Codes.DAILY_RUNNING_TOTAL);
            parametros.Parameters.Add("PERPETUAL_AMOUNT", Trxs_Codes.PERPETUAL_AMOUNT);
            parametros.Parameters.Add("REAL_PERPETUAL_AMOUNT", Trxs_Codes.REAL_PERPETUAL_AMOUNT);
            parametros.Parameters.Add("FISCAL_SESSION_NO", Trxs_Codes.FISCAL_SESSION_NO);
            parametros.Parameters.Add("ASSOCIATED_SEQ_NO", Trxs_Codes.ASSOCIATED_SEQ_NO);
            parametros.Parameters.Add("FISCAL_BILL_GENERATION_DATE", Trxs_Codes.FISCAL_BILL_GENERATION_DATE.ToString());
            parametros.Parameters.Add("FISCAL_BILL_GENERATION_TIME", Trxs_Codes.FISCAL_BILL_GENERATION_TIME);
            parametros.Parameters.Add("HOTEL_NAME", Trxs_Codes.HOTEL_NAME);
            parametros.Parameters.Add("RESORT_FULL_ADDRESS", Trxs_Codes.RESORT_FULL_ADDRESS);
            parametros.Parameters.Add("NAF_APE_HOTEL_CODE", Trxs_Codes.NAF_APE_HOTEL_CODE);
            parametros.Parameters.Add("RESORT_CITY", Trxs_Codes.RESORT_CITY);
            parametros.Parameters.Add("RESORT_ZIP_CODE", Trxs_Codes.RESORT_ZIP_CODE);
            parametros.Parameters.Add("RESORT_COUNTRY", Trxs_Codes.RESORT_COUNTRY);
            parametros.Parameters.Add("PROPERTY_TAX_NO", Trxs_Codes.PROPERTY_TAX_NO);
            parametros.Parameters.Add("BUSINESS_ID", Trxs_Codes.BUSINESS_ID);
            parametros.Parameters.Add("SOFTWARE_VERSION", Trxs_Codes.SOFTWARE_VERSION);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
