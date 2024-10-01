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
    public class GemEventJob : IServiceJobs
    {
        private readonly ILogger<GemEventJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public GemEventJob(ILogger<GemEventJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_GemEvent>(filePath, LoggBuilder);

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
                                $"EVENT_ID = '{trial.EVENT_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_GemEvent>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_GemEvent Trxs_Codes)
        {
            parametros.Parameters.Add("EVENT_ID", Trxs_Codes.EVENT_ID);
            parametros.Parameters.Add("MASTER_EVENT_ID", Trxs_Codes.MASTER_EVENT_ID);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("BOOK_ID", Trxs_Codes.BOOK_ID);
            parametros.Parameters.Add("EV_TYPE", Trxs_Codes.EV_TYPE);
            parametros.Parameters.Add("EV_NAME", Trxs_Codes.EV_NAME);
            parametros.Parameters.Add("EV_STATUS", Trxs_Codes.EV_STATUS);
            parametros.Parameters.Add("PKG_ID", Trxs_Codes.PKG_ID);
            parametros.Parameters.Add("WAITLIST_YN", Trxs_Codes.WAITLIST_YN);
            parametros.Parameters.Add("TURNTO_STATUS", Trxs_Codes.TURNTO_STATUS);
            parametros.Parameters.Add("GROUP_ID", Trxs_Codes.GROUP_ID);
            parametros.Parameters.Add("ATTENDEES", Trxs_Codes.ATTENDEES);
            parametros.Parameters.Add("ACTUAL_ATTENDEES", Trxs_Codes.ACTUAL_ATTENDEES);
            parametros.Parameters.Add("ACTUAL_MANUAL", Trxs_Codes.ACTUAL_MANUAL);
            parametros.Parameters.Add("START_DATE", Trxs_Codes.START_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("BLOCKSTART", Trxs_Codes.BLOCKSTART.ToString());
            parametros.Parameters.Add("BLOCKEND", Trxs_Codes.BLOCKEND.ToString());
            parametros.Parameters.Add("GUARANTEED", Trxs_Codes.GUARANTEED);
            parametros.Parameters.Add("DOORCARD", Trxs_Codes.DOORCARD);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("ROOM_SETUP", Trxs_Codes.ROOM_SETUP);
            parametros.Parameters.Add("SETUP_TIME", Trxs_Codes.SETUP_TIME);
            parametros.Parameters.Add("SETDOWN_TIME", Trxs_Codes.SETDOWN_TIME);
            parametros.Parameters.Add("TRACECODE", Trxs_Codes.TRACECODE);
            parametros.Parameters.Add("DONT_MOVE_YN", Trxs_Codes.DONT_MOVE_YN);
            parametros.Parameters.Add("PROBLEM_YN", Trxs_Codes.PROBLEM_YN);
            parametros.Parameters.Add("WL_IGNORE_YN", Trxs_Codes.WL_IGNORE_YN);
            parametros.Parameters.Add("MASTER_YN", Trxs_Codes.MASTER_YN);
            parametros.Parameters.Add("EVENT_LINK_ID", Trxs_Codes.EVENT_LINK_ID);
            parametros.Parameters.Add("INSPECTED_YN", Trxs_Codes.INSPECTED_YN);
            parametros.Parameters.Add("INSPECTED_DATE", Trxs_Codes.INSPECTED_DATE.ToString());
            parametros.Parameters.Add("INSPECTED_USER", Trxs_Codes.INSPECTED_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("EV_RESORT", Trxs_Codes.EV_RESORT);
            parametros.Parameters.Add("DOORCARD_YN", Trxs_Codes.DOORCARD_YN);
            parametros.Parameters.Add("EVENT_LINK_TYPE", Trxs_Codes.EVENT_LINK_TYPE);
            parametros.Parameters.Add("PKG_EXP_ATTENDEES", Trxs_Codes.PKG_EXP_ATTENDEES);
            parametros.Parameters.Add("PKG_GUA_ATTENDEES", Trxs_Codes.PKG_GUA_ATTENDEES);
            parametros.Parameters.Add("PKG_ACT_ATTENDEES", Trxs_Codes.PKG_ACT_ATTENDEES);
            parametros.Parameters.Add("V6_EVENT_ID", Trxs_Codes.V6_EVENT_ID);
            parametros.Parameters.Add("FORECAST_REVENUE_ONLY_YN", Trxs_Codes.FORECAST_REVENUE_ONLY_YN);
            parametros.Parameters.Add("EXCLUDE_FROM_FORECAST_YN", Trxs_Codes.EXCLUDE_FROM_FORECAST_YN);
            parametros.Parameters.Add("PKG_NAME", Trxs_Codes.PKG_NAME);
            parametros.Parameters.Add("PKG_LINK", Trxs_Codes.PKG_LINK);
            parametros.Parameters.Add("PKG_EV_ID", Trxs_Codes.PKG_EV_ID);
            parametros.Parameters.Add("SET_ATTENDEES", Trxs_Codes.SET_ATTENDEES);
            parametros.Parameters.Add("FBA_ID", Trxs_Codes.FBA_ID);
            parametros.Parameters.Add("SELECT_RATECODE_IN_CENTRAL_YN", Trxs_Codes.SELECT_RATECODE_IN_CENTRAL_YN);
            parametros.Parameters.Add("DETAILED_POSTING_YN", Trxs_Codes.DETAILED_POSTING_YN);
            parametros.Parameters.Add("ALLOW_REGISTRY_YN", Trxs_Codes.ALLOW_REGISTRY_YN);
            parametros.Parameters.Add("ORIG_EVENT_ID", Trxs_Codes.ORIG_EVENT_ID);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }
    }
}
