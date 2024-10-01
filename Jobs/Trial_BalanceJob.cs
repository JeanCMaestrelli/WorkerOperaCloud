using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Services;
using WorkerOperaCloud.Services.Interfaces;

namespace WorkerOperaCloud.Jobs
{
    public class Trial_BalanceJob : IServiceJobs
    {
        private readonly ILogger<Trial_BalanceJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Trial_BalanceJob(ILogger<Trial_BalanceJob> logger, DapperContext context,  Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Trial_Balance>(filePath, LoggBuilder);

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
                                $"TRX_DATE = TO_DATE('{trial.TRX_DATE}', 'dd/mm/yyyy hh24:mi:ss') and " +
                                $"TRX_CODE = '{trial.TRX_CODE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Trial_Balance>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Trial_Balance trial_Balance)
        {
            parametros.Parameters.Add("RESORT", trial_Balance.RESORT);
            parametros.Parameters.Add("DESCRIPTION", trial_Balance.DESCRIPTION);
            parametros.Parameters.Add("TRX_DATE", trial_Balance.TRX_DATE.ToString());
            parametros.Parameters.Add("ROOM_CLASS", trial_Balance.ROOM_CLASS);
            parametros.Parameters.Add("TRX_CODE", trial_Balance.TRX_CODE);
            parametros.Parameters.Add("DEP_LED_DEBIT", trial_Balance.DEP_LED_DEBIT);
            parametros.Parameters.Add("DEP_LED_CREDIT", trial_Balance.DEP_LED_CREDIT);
            parametros.Parameters.Add("GUEST_LED_DEBIT", trial_Balance.GUEST_LED_DEBIT);
            parametros.Parameters.Add("GUEST_LED_CREDIT", trial_Balance.GUEST_LED_CREDIT);
            parametros.Parameters.Add("PACKAGE_LED_DEBIT", trial_Balance.PACKAGE_LED_DEBIT);
            parametros.Parameters.Add("PACKAGE_LED_CREDIT", trial_Balance.PACKAGE_LED_CREDIT);
            parametros.Parameters.Add("AR_LED_DEBIT", trial_Balance.AR_LED_DEBIT);
            parametros.Parameters.Add("AR_LED_CREDIT", trial_Balance.AR_LED_CREDIT);
            parametros.Parameters.Add("REVENUE", trial_Balance.REVENUE);
            parametros.Parameters.Add("TRX_AMOUNT", trial_Balance.TRX_AMOUNT);
            parametros.Parameters.Add("NET_AMOUNT", trial_Balance.NET_AMOUNT);
            parametros.Parameters.Add("GROSS_AMOUNT", trial_Balance.GROSS_AMOUNT);
            parametros.Parameters.Add("DEP_LED_DEBIT_MM", trial_Balance.DEP_LED_DEBIT_MM);
            parametros.Parameters.Add("DEP_LED_CREDIT_MM", trial_Balance.DEP_LED_CREDIT_MM);
            parametros.Parameters.Add("GUEST_LED_DEBIT_MM", trial_Balance.GUEST_LED_DEBIT_MM);
            parametros.Parameters.Add("GUEST_LED_CREDIT_MM", trial_Balance.GUEST_LED_CREDIT_MM);
            parametros.Parameters.Add("PACKAGE_LED_DEBIT_MM", trial_Balance.PACKAGE_LED_DEBIT_MM);
            parametros.Parameters.Add("PACKAGE_LED_CREDIT_MM", trial_Balance.PACKAGE_LED_CREDIT_MM);
            parametros.Parameters.Add("AR_LED_DEBIT_MM", trial_Balance.AR_LED_DEBIT_MM);
            parametros.Parameters.Add("AR_LED_CREDIT_MM", trial_Balance.AR_LED_CREDIT_MM);
            parametros.Parameters.Add("REVENUE_MM", trial_Balance.REVENUE_MM);
            parametros.Parameters.Add("TRX_AMOUNT_MM", trial_Balance.TRX_AMOUNT_MM);
            parametros.Parameters.Add("NET_AMOUNT_MM", trial_Balance.NET_AMOUNT_MM);
            parametros.Parameters.Add("GROSS_AMOUNT_MM", trial_Balance.GROSS_AMOUNT_MM);
            parametros.Parameters.Add("DEP_LED_DEBIT_YY", trial_Balance.DEP_LED_DEBIT_YY);
            parametros.Parameters.Add("DEP_LED_CREDIT_YY", trial_Balance.DEP_LED_CREDIT_YY);
            parametros.Parameters.Add("GUEST_LED_DEBIT_YY", trial_Balance.GUEST_LED_DEBIT_YY);
            parametros.Parameters.Add("GUEST_LED_CREDIT_YY", trial_Balance.GUEST_LED_CREDIT_YY);
            parametros.Parameters.Add("PACKAGE_LED_DEBIT_YY", trial_Balance.PACKAGE_LED_DEBIT_YY);
            parametros.Parameters.Add("PACKAGE_LED_CREDIT_YY", trial_Balance.PACKAGE_LED_CREDIT_YY);
            parametros.Parameters.Add("AR_LED_DEBIT_YY", trial_Balance.AR_LED_DEBIT_YY);
            parametros.Parameters.Add("AR_LED_CREDIT_YY", trial_Balance.AR_LED_CREDIT_YY);
            parametros.Parameters.Add("REVENUE_YY", trial_Balance.REVENUE_YY);
            parametros.Parameters.Add("TRX_AMOUNT_YY", trial_Balance.TRX_AMOUNT_YY);
            parametros.Parameters.Add("NET_AMOUNT_YY", trial_Balance.NET_AMOUNT_YY);
            parametros.Parameters.Add("GROSS_AMOUNT_YY", trial_Balance.GROSS_AMOUNT_YY);
            parametros.Parameters.Add("NON_REVENUE_AMT", trial_Balance.NON_REVENUE_AMT);
            parametros.Parameters.Add("INH_CREDIT", trial_Balance.INH_CREDIT);
            parametros.Parameters.Add("INH_DEBIT", trial_Balance.INH_DEBIT);
            parametros.Parameters.Add("INTERNAL_DB_PAYMENTS", trial_Balance.INTERNAL_DB_PAYMENTS);
            parametros.Parameters.Add("PACKAGE_LED_TAX", trial_Balance.PACKAGE_LED_TAX);
            parametros.Parameters.Add("TAX1_AMT", trial_Balance.TAX1_AMT);
            parametros.Parameters.Add("TAX2_AMT", trial_Balance.TAX2_AMT);
            parametros.Parameters.Add("TAX3_AMT", trial_Balance.TAX3_AMT);
            parametros.Parameters.Add("TAX4_AMT", trial_Balance.TAX4_AMT);
            parametros.Parameters.Add("TAX5_AMT", trial_Balance.TAX5_AMT);
            parametros.Parameters.Add("TAX6_AMT", trial_Balance.TAX6_AMT);
            parametros.Parameters.Add("TAX7_AMT", trial_Balance.TAX7_AMT);
            parametros.Parameters.Add("TAX8_AMT", trial_Balance.TAX8_AMT);
            parametros.Parameters.Add("TAX9_AMT", trial_Balance.TAX9_AMT);
            parametros.Parameters.Add("TAX10_AMT", trial_Balance.TAX10_AMT);
            parametros.Parameters.Add("NET1_AMT", trial_Balance.NET1_AMT);
            parametros.Parameters.Add("NET2_AMT", trial_Balance.NET2_AMT);
            parametros.Parameters.Add("NET3_AMT", trial_Balance.NET3_AMT);
            parametros.Parameters.Add("NET4_AMT", trial_Balance.NET4_AMT);
            parametros.Parameters.Add("NET5_AMT", trial_Balance.NET5_AMT);
            parametros.Parameters.Add("NET6_AMT", trial_Balance.NET6_AMT);
            parametros.Parameters.Add("NET7_AMT", trial_Balance.NET7_AMT);
            parametros.Parameters.Add("NET8_AMT", trial_Balance.NET8_AMT);
            parametros.Parameters.Add("NET9_AMT", trial_Balance.NET9_AMT);
            parametros.Parameters.Add("NET10_AMT", trial_Balance.NET10_AMT);
            parametros.Parameters.Add("OWNER_LED_DEBIT", trial_Balance.OWNER_LED_DEBIT);
            parametros.Parameters.Add("OWNER_LED_CREDIT", trial_Balance.OWNER_LED_CREDIT);
            parametros.Parameters.Add("TAX11_AMT", trial_Balance.TAX11_AMT);
            parametros.Parameters.Add("TAX12_AMT", trial_Balance.TAX12_AMT);
            parametros.Parameters.Add("TAX13_AMT", trial_Balance.TAX13_AMT);
            parametros.Parameters.Add("TAX14_AMT", trial_Balance.TAX14_AMT);
            parametros.Parameters.Add("TAX15_AMT", trial_Balance.TAX15_AMT);
            parametros.Parameters.Add("TAX16_AMT", trial_Balance.TAX16_AMT);
            parametros.Parameters.Add("TAX17_AMT", trial_Balance.TAX17_AMT);
            parametros.Parameters.Add("TAX18_AMT", trial_Balance.TAX18_AMT);
            parametros.Parameters.Add("TAX19_AMT", trial_Balance.TAX19_AMT);
            parametros.Parameters.Add("TAX20_AMT", trial_Balance.TAX20_AMT);
            parametros.Parameters.Add("NET11_AMT", trial_Balance.NET11_AMT);
            parametros.Parameters.Add("NET12_AMT", trial_Balance.NET12_AMT);
            parametros.Parameters.Add("NET13_AMT", trial_Balance.NET13_AMT);
            parametros.Parameters.Add("NET14_AMT", trial_Balance.NET14_AMT);
            parametros.Parameters.Add("NET15_AMT", trial_Balance.NET15_AMT);
            parametros.Parameters.Add("NET16_AMT", trial_Balance.NET16_AMT);
            parametros.Parameters.Add("NET17_AMT", trial_Balance.NET17_AMT);
            parametros.Parameters.Add("NET18_AMT", trial_Balance.NET18_AMT);
            parametros.Parameters.Add("NET19_AMT", trial_Balance.NET19_AMT);
            parametros.Parameters.Add("NET20_AMT", trial_Balance.NET20_AMT);
            parametros.Parameters.Add("DEP_FOLIO_DEBIT", trial_Balance.DEP_FOLIO_DEBIT);
            parametros.Parameters.Add("RNA_INSERTDATE", trial_Balance.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", trial_Balance.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", trial_Balance.DELETED_FLAG);

            return parametros;
        }

    }
}
