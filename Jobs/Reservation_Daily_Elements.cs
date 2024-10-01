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
    public class Reservation_Daily_Elements : IServiceJobs
    {
        private readonly ILogger<Reservation_Daily_Elements> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Reservation_Daily_Elements(ILogger<Reservation_Daily_Elements> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Reservation_Daily_Elements>(filePath, LoggBuilder);

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
                                        $"RESV_DAILY_EL_SEQ = '{trial.RESV_DAILY_EL_SEQ}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Reservation_Daily_Elements>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Reservation_Daily_Elements Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("RESERVATION_DATE", Trxs_Codes.RESERVATION_DATE.ToString());
            parametros.Parameters.Add("RESV_DAILY_EL_SEQ", Trxs_Codes.RESV_DAILY_EL_SEQ);
            parametros.Parameters.Add("RESV_STATUS", Trxs_Codes.RESV_STATUS);
            parametros.Parameters.Add("EXTERNAL_REFERENCE", Trxs_Codes.EXTERNAL_REFERENCE);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("ROOM_CATEGORY", Trxs_Codes.ROOM_CATEGORY);
            parametros.Parameters.Add("BOOKED_ROOM_CATEGORY", Trxs_Codes.BOOKED_ROOM_CATEGORY);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("CANCELLATION_NO", Trxs_Codes.CANCELLATION_NO);
            parametros.Parameters.Add("CANCELLATION_DATE", Trxs_Codes.CANCELLATION_DATE.ToString());
            parametros.Parameters.Add("CANCELLATION_CODE", Trxs_Codes.CANCELLATION_CODE);
            parametros.Parameters.Add("CANCELLATION_REASON_DESC", Trxs_Codes.CANCELLATION_REASON_DESC);
            parametros.Parameters.Add("GUARANTEE_CODE", Trxs_Codes.GUARANTEE_CODE);
            parametros.Parameters.Add("MARKET_CODE", Trxs_Codes.MARKET_CODE);
            parametros.Parameters.Add("ORIGIN_OF_BOOKING", Trxs_Codes.ORIGIN_OF_BOOKING);
            parametros.Parameters.Add("EXCHANGE_RATE", Trxs_Codes.EXCHANGE_RATE);
            parametros.Parameters.Add("ORIGINAL_BASE_RATE", Trxs_Codes.ORIGINAL_BASE_RATE);
            parametros.Parameters.Add("BASE_RATE_AMOUNT", Trxs_Codes.BASE_RATE_AMOUNT);
            parametros.Parameters.Add("RATE_AMOUNT", Trxs_Codes.RATE_AMOUNT);
            parametros.Parameters.Add("ROOM_COST", Trxs_Codes.ROOM_COST);
            parametros.Parameters.Add("QUANTITY", Trxs_Codes.QUANTITY);
            parametros.Parameters.Add("ADULTS", Trxs_Codes.ADULTS);
            parametros.Parameters.Add("CHILDREN", Trxs_Codes.CHILDREN);
            parametros.Parameters.Add("PHYSICAL_QUANTITY", Trxs_Codes.PHYSICAL_QUANTITY);
            parametros.Parameters.Add("ALLOTMENT_HEADER_ID", Trxs_Codes.ALLOTMENT_HEADER_ID);
            parametros.Parameters.Add("DAY_USE_YN", Trxs_Codes.DAY_USE_YN);
            parametros.Parameters.Add("DUE_OUT_YN", Trxs_Codes.DUE_OUT_YN);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("INSERT_ACTION_INSTANCE_ID", Trxs_Codes.INSERT_ACTION_INSTANCE_ID);
            parametros.Parameters.Add("DML_SEQ_NO", Trxs_Codes.DML_SEQ_NO);
            parametros.Parameters.Add("EXT_SEQ_NO", Trxs_Codes.EXT_SEQ_NO);
            parametros.Parameters.Add("EXT_SEG_NO", Trxs_Codes.EXT_SEG_NO);
            parametros.Parameters.Add("CRIBS", Trxs_Codes.CRIBS);
            parametros.Parameters.Add("EXTRA_BEDS", Trxs_Codes.EXTRA_BEDS);
            parametros.Parameters.Add("ALLOTMENT_RECORD_TYPE", Trxs_Codes.ALLOTMENT_RECORD_TYPE);
            parametros.Parameters.Add("BLOCK_RESORT", Trxs_Codes.BLOCK_RESORT);
            parametros.Parameters.Add("BLOCK_ID", Trxs_Codes.BLOCK_ID);
            parametros.Parameters.Add("TURNDOWN_STATUS", Trxs_Codes.TURNDOWN_STATUS);
            parametros.Parameters.Add("AWD_UPGR_FROM", Trxs_Codes.AWD_UPGR_FROM);
            parametros.Parameters.Add("AWD_UPGR_TO", Trxs_Codes.AWD_UPGR_TO);
            parametros.Parameters.Add("HK_EXPECTED_SERVICE_TIME", Trxs_Codes.HK_EXPECTED_SERVICE_TIME);
            parametros.Parameters.Add("ROOM_INSTRUCTIONS", Trxs_Codes.ROOM_INSTRUCTIONS);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
