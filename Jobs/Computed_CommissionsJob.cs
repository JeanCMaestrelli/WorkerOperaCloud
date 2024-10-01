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
    public class Computed_CommissionsJob : IServiceJobs
    {
        private readonly ILogger<Computed_CommissionsJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Computed_CommissionsJob(ILogger<Computed_CommissionsJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Computed_Commissions>(filePath, LoggBuilder);

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
                                $"TRAVEL_AGENT_ID = '{trial.TRAVEL_AGENT_ID}' and " +
                                $"RESV_NAME_ID = '{trial.RESV_NAME_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Computed_Commissions>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Computed_Commissions Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("PAYMENT_ID", Trxs_Codes.PAYMENT_ID);
            parametros.Parameters.Add("TRAVEL_AGENT_ID", Trxs_Codes.TRAVEL_AGENT_ID);
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID);
            parametros.Parameters.Add("COMMISSIONABLE_REVENUE", Trxs_Codes.COMMISSIONABLE_REVENUE.Replace(".", ","));
            parametros.Parameters.Add("GROSS_COMM_AMT", Trxs_Codes.GROSS_COMM_AMT.Replace(".", ","));
            parametros.Parameters.Add("PREPAID_COMM", Trxs_Codes.PREPAID_COMM.Replace(".", ","));
            parametros.Parameters.Add("AR_AMOUNT", Trxs_Codes.AR_AMOUNT.Replace(".", ","));
            parametros.Parameters.Add("VAT_AMOUNT", Trxs_Codes.VAT_AMOUNT.Replace(".", ","));
            parametros.Parameters.Add("COMM_STATUS", Trxs_Codes.COMM_STATUS);
            parametros.Parameters.Add("COMMISSION_HOLD_CODE", Trxs_Codes.COMMISSION_HOLD_CODE);
            parametros.Parameters.Add("COMMISSION_HOLD_DESC", Trxs_Codes.COMMISSION_HOLD_DESC);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("COMM_TYPE", Trxs_Codes.COMM_TYPE);
            parametros.Parameters.Add("TA_COMM_CODE", Trxs_Codes.TA_COMM_CODE);
            parametros.Parameters.Add("MANUAL_EDIT_YN", Trxs_Codes.MANUAL_EDIT_YN);
            parametros.Parameters.Add("PAYEE_TYPE", Trxs_Codes.PAYEE_TYPE);
            parametros.Parameters.Add("MANUAL_RESV_YN", Trxs_Codes.MANUAL_RESV_YN);
            parametros.Parameters.Add("REMARKS", Trxs_Codes.REMARKS);
            parametros.Parameters.Add("PPD_REMARKS", Trxs_Codes.PPD_REMARKS);
            parametros.Parameters.Add("PPD_EDIT_YN", Trxs_Codes.PPD_EDIT_YN);
            parametros.Parameters.Add("DECIMAL_POSITIONS", Trxs_Codes.DECIMAL_POSITIONS.Replace(".", ","));
            parametros.Parameters.Add("EXCHANGE_RATE", Trxs_Codes.EXCHANGE_RATE.Replace(".", ","));
            parametros.Parameters.Add("BUSINESS_DATE_CREATED", Trxs_Codes.BUSINESS_DATE_CREATED.ToString());
            parametros.Parameters.Add("COMM_CODE_DIFF_YN", Trxs_Codes.COMM_CODE_DIFF_YN);
            parametros.Parameters.Add("DEPARTURE", Trxs_Codes.DEPARTURE.ToString());
            parametros.Parameters.Add("ADJUSTMENT_NOTE", Trxs_Codes.ADJUSTMENT_NOTE);
            parametros.Parameters.Add("AR_YN", Trxs_Codes.AR_YN);
            parametros.Parameters.Add("TAX_FILE_STATUS", Trxs_Codes.TAX_FILE_STATUS);
            parametros.Parameters.Add("TAX_FILE_DATE", Trxs_Codes.TAX_FILE_DATE.ToString());
            parametros.Parameters.Add("OWNER_COMM_PROCESSED_YN", Trxs_Codes.OWNER_COMM_PROCESSED_YN);
            parametros.Parameters.Add("COMMISSION_ID", Trxs_Codes.COMMISSION_ID);
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

        /*
        private List<Mdl_Computed_Commissions> Get_List_Csv(string filePath)
        {
            List<Mdl_Computed_Commissions> ListCsv = new List<Mdl_Computed_Commissions>();
            Mdl_Computed_Commissions Csv;
            int x = 1;
            Tuple<bool,int> tupla = new Tuple<bool, int>(false,0);

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
                return ListCsv;
            }

            try
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} Efetuando leitura de arquivo de integração: {filePath}");
                TextFieldParser reader = new TextFieldParser(filePath);
                reader.Delimiters = new string[] { ";" };
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Arquivo aberto para leitura.");
                
                while (!reader.EndOfData)
                {
                    var line = reader.ReadFields();
                    if(line != null && x == 1)
                    {
                        tupla = _Job_Exec.ConsistirColunas(line, _TIPO_AGENDAMENTO, filePath, LoggBuilder);
                        if (!tupla.Item1)
                        {
                            reader.Close();
                            erros = "1";
                            break;
                        }
                    }
                    else if (x >= 2)
                    {
                        if (line != null)
                        {
                            var campos = line;
                            if (campos.Length != tupla.Item2)
                            {
                                erros = "1";
                                LoggBuilder.AppendLine($"      **** ERRO Linha {x}, ( Qtd Campos {campos.Length} | Qtd Colunas {tupla.Item2} )");
                                x++;
                                continue;
                            }

                            Csv = new Mdl_Computed_Commissions()
                            {
                                RESORT = campos[0],
                                PAYMENT_ID = campos[1],
                                TRAVEL_AGENT_ID = campos[2],
                                RESV_NAME_ID = campos[3],
                                COMMISSIONABLE_REVENUE = campos[4],
                                GROSS_COMM_AMT = campos[5],
                                PREPAID_COMM = campos[6],
                                AR_AMOUNT = campos[7],
                                VAT_AMOUNT = campos[8],
                                COMM_STATUS = campos[9],
                                COMMISSION_HOLD_CODE = campos[10],
                                COMMISSION_HOLD_DESC = campos[11],
                                INSERT_USER = campos[12],
                                INSERT_DATE = campos[13].Trim().Split(" ")[0],
                                UPDATE_USER = campos[14],
                                UPDATE_DATE = campos[15].Trim().Split(" ")[0],
                                COMM_TYPE = campos[16],
                                TA_COMM_CODE = campos[17],
                                MANUAL_EDIT_YN = campos[18],
                                PAYEE_TYPE = campos[19],
                                MANUAL_RESV_YN = campos[20],
                                REMARKS = campos[21],
                                PPD_REMARKS = campos[22],
                                PPD_EDIT_YN = campos[23],
                                DECIMAL_POSITIONS = campos[24],
                                EXCHANGE_RATE = campos[25],
                                BUSINESS_DATE_CREATED = campos[26].Trim().Split(" ")[0],
                                COMM_CODE_DIFF_YN = campos[27],
                                DEPARTURE = campos[28].Trim().Split(" ")[0],
                                ADJUSTMENT_NOTE = campos[29],
                                AR_YN = campos[30],
                                TAX_FILE_STATUS = campos[31],
                                TAX_FILE_DATE = campos[32].Trim().Split(" ")[0],
                                OWNER_COMM_PROCESSED_YN = campos[33],
                                COMMISSION_ID = campos[34],
                                BEGIN_DATE = campos[35].Trim().Split(" ")[0],
                                END_DATE = campos[36].Trim().Split(" ")[0]
                            };

                            ListCsv.Add(Csv);
                        }
                    }
                    x++;
                }
                x = 1;
                reader.Close();
            }
            catch (Exception e)
            {
                erros = "1";
                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas na leitura do arquivo linha {x}, Execption: {e.Message}");
                return new List<Mdl_Computed_Commissions>();
            }

            if (erros == "1")
            {
                return new List<Mdl_Computed_Commissions>();
            }

            _Job_Exec.moverArquivoIntegrado(filePath, LoggBuilder);

            return ListCsv;
        }

        private string GetInsertQuery()
        {
            var query = "insert into COMPUTED_COMMISSIONS " +
            " values " +
            "(:RESORT,:PAYMENT_ID,:TRAVEL_AGENT_ID,:RESV_NAME_ID,:COMMISSIONABLE_REVENUE,:GROSS_COMM_AMT,:PREPAID_COMM,:AR_AMOUNT,:VAT_AMOUNT,:COMM_STATUS," +
            ":COMMISSION_HOLD_CODE,:COMMISSION_HOLD_DESC,:INSERT_USER,:INSERT_DATE,:UPDATE_USER,:UPDATE_DATE,:COMM_TYPE,:TA_COMM_CODE,:MANUAL_EDIT_YN,:PAYEE_TYPE," +
            ":MANUAL_RESV_YN,:REMARKS,:PPD_REMARKS,:PPD_EDIT_YN,:DECIMAL_POSITIONS,:EXCHANGE_RATE,:BUSINESS_DATE_CREATED,:COMM_CODE_DIFF_YN,:DEPARTURE," +
            ":ADJUSTMENT_NOTE,:AR_YN,:TAX_FILE_STATUS,:TAX_FILE_DATE,:OWNER_COMM_PROCESSED_YN,:COMMISSION_ID,:BEGIN_DATE,:END_DATE)";

            return query;
        }
        */
    }
}
