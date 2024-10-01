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
    public class EventReservationJob : IServiceJobs
    {
        private readonly ILogger<EventReservationJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public EventReservationJob(ILogger<EventReservationJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_EventReservation>(filePath, LoggBuilder);

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
                                $"RESV_NAME_ID = '{trial.RESV_NAME_ID}' and " +
                                $"EVENT_ID = '{trial.EVENT_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_EventReservation>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_EventReservation Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID);
            parametros.Parameters.Add("EVENT_ID", Trxs_Codes.EVENT_ID);
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("BOOK_ID", Trxs_Codes.BOOK_ID);
            parametros.Parameters.Add("PKG_ID", Trxs_Codes.PKG_ID);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("ROOM_CATEGORY", Trxs_Codes.ROOM_CATEGORY);
            parametros.Parameters.Add("SETUP_CODE", Trxs_Codes.SETUP_CODE);
            parametros.Parameters.Add("SETUP_DESC", Trxs_Codes.SETUP_DESC);
            parametros.Parameters.Add("SETUP_TIME", Trxs_Codes.SETUP_TIME);
            parametros.Parameters.Add("SETDOWN_TIME", Trxs_Codes.SETDOWN_TIME);
            parametros.Parameters.Add("ATTENDEES", Trxs_Codes.ATTENDEES);
            parametros.Parameters.Add("REVENUE_TYPE", Trxs_Codes.REVENUE_TYPE);
            parametros.Parameters.Add("RATECODE", Trxs_Codes.RATECODE);
            parametros.Parameters.Add("FIXED_RATE_YN", Trxs_Codes.FIXED_RATE_YN);
            parametros.Parameters.Add("HOURLY_YN", Trxs_Codes.HOURLY_YN);
            parametros.Parameters.Add("RATE_AMOUNT", Trxs_Codes.RATE_AMOUNT.Replace(".",","));
            parametros.Parameters.Add("SHARED_YN", Trxs_Codes.SHARED_YN);
            parametros.Parameters.Add("DONT_MOVE_YN", Trxs_Codes.DONT_MOVE_YN);
            parametros.Parameters.Add("NOISY_YN", Trxs_Codes.NOISY_YN);
            parametros.Parameters.Add("DISCOUNT_AMOUNT", Trxs_Codes.DISCOUNT_AMOUNT.Replace(".", ","));
            parametros.Parameters.Add("DISCOUNT_PERCENTAGE", Trxs_Codes.DISCOUNT_PERCENTAGE.Replace(".", ","));
            parametros.Parameters.Add("DISCOUNT_REASON_CODE", Trxs_Codes.DISCOUNT_REASON_CODE);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("ROOM_RESORT", Trxs_Codes.ROOM_RESORT);
            parametros.Parameters.Add("RATE_TYPE", Trxs_Codes.RATE_TYPE);
            parametros.Parameters.Add("MINIMUM_REVENUE_YN", Trxs_Codes.MINIMUM_REVENUE_YN);
            parametros.Parameters.Add("MINIMUM_REVENUE", Trxs_Codes.MINIMUM_REVENUE.Replace(".", ","));
            parametros.Parameters.Add("RENTAL_AMOUNT", Trxs_Codes.RENTAL_AMOUNT.Replace(".", ","));
            parametros.Parameters.Add("INC_SETUP_IN_HOURLY_RATE_YN", Trxs_Codes.INC_SETUP_IN_HOURLY_RATE_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

        /*
        private List<Mdl_EventReservation> Get_List_Csv(string filePath)
        {
            List<Mdl_EventReservation> ListCsv = new List<Mdl_EventReservation>();
            Mdl_EventReservation Csv;
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
                var reader = new StreamReader(File.OpenRead(filePath));
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Arquivo aberto para leitura.");
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if(line != null && x == 1)
                    {
                        tupla = _Job_Exec.ConsistirColunas(line, _TIPO_AGENDAMENTO, filePath, LoggBuilder);
                        if (!tupla.Item1)
                        {
                            reader.Close();
                            reader.Dispose();
                            erros = "1";
                            break;
                        }
                    }
                    else if (x >= 2)
                    {
                        if (line != null)
                        {
                            var campos = line.Split(';');
                            if (campos.Length != tupla.Item2)
                            {
                                erros = "1";
                                LoggBuilder.AppendLine($"      **** ERRO Linha {x}, ( Qtd Campos {campos.Length} | Qtd Colunas {tupla.Item2} )");
                                x++;
                                continue;
                            }

                            Csv = new Mdl_EventReservation()
                            {
                                RESORT = campos[0],
                                RESV_NAME_ID = campos[1],
                                EVENT_ID = campos[2],
                                BEGIN_DATE = campos[3].Trim().Split(" ")[0].ToString(),
                                END_DATE = campos[4].Trim().Split(" ")[0].ToString(),
                                BOOK_ID = campos[5],
                                PKG_ID = campos[6],
                                ROOM = campos[7],
                                ROOM_CLASS = campos[8],
                                ROOM_CATEGORY = campos[9],
                                SETUP_CODE = campos[10],
                                SETUP_DESC = campos[11],
                                SETUP_TIME = campos[12],
                                SETDOWN_TIME = campos[13],
                                ATTENDEES = campos[14],
                                REVENUE_TYPE = campos[15],
                                RATECODE = campos[16],
                                FIXED_RATE_YN = campos[17],
                                HOURLY_YN = campos[18],
                                RATE_AMOUNT = campos[19],
                                SHARED_YN = campos[20],
                                DONT_MOVE_YN = campos[21],
                                NOISY_YN = campos[22],
                                DISCOUNT_AMOUNT = campos[23],
                                DISCOUNT_PERCENTAGE = campos[24],
                                DISCOUNT_REASON_CODE = campos[25],
                                INSERT_USER = campos[26],
                                INSERT_DATE = campos[27].Trim().Split(" ")[0].ToString(),
                                UPDATE_USER = campos[28],
                                UPDATE_DATE = campos[29].Trim().Split(" ")[0].ToString(),
                                ROOM_RESORT = campos[30],
                                RATE_TYPE = campos[31],
                                MINIMUM_REVENUE_YN = campos[32],
                                MINIMUM_REVENUE = campos[33],
                                RENTAL_AMOUNT = campos[34],
                                INC_SETUP_IN_HOURLY_RATE_YN = campos[35]
                            };

                            ListCsv.Add(Csv);
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
                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas na leitura do arquivo linha {x}, Execption: {e.Message}");
                return new List<Mdl_EventReservation>();
            }

            if (erros == "1")
            {
                return new List<Mdl_EventReservation>();
            }

            _Job_Exec.moverArquivoIntegrado(filePath, LoggBuilder);

            return ListCsv;
        }

        private string GetInsertQuery()
        {
            var query = "insert into EVENT$RESERVATION " +
            " values " +
            "(:RESORT,:RESV_NAME_ID,:EVENT_ID,:BEGIN_DATE,:END_DATE,:BOOK_ID,:PKG_ID,:ROOM,:ROOM_CLASS,:ROOM_CATEGORY," +
            ":SETUP_CODE,:SETUP_DESC,:SETUP_TIME,:SETDOWN_TIME,:ATTENDEES,:REVENUE_TYPE,:RATECODE,:FIXED_RATE_YN,:HOURLY_YN," +
            ":RATE_AMOUNT,:SHARED_YN,:DONT_MOVE_YN,:NOISY_YN,:DISCOUNT_AMOUNT,:DISCOUNT_PERCENTAGE,:DISCOUNT_REASON_CODE,:INSERT_USER," +
            ":INSERT_DATE,:UPDATE_USER,:UPDATE_DATE,:ROOM_RESORT,:RATE_TYPE,:MINIMUM_REVENUE_YN,:MINIMUM_REVENUE,:RENTAL_AMOUNT," +
            ":INC_SETUP_IN_HOURLY_RATE_YN)";

            return query;
        }
        */
    }
}
