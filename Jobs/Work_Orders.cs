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
    public class Work_Orders : IServiceJobs
    {
        private readonly ILogger<Work_Orders> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Work_Orders(ILogger<Work_Orders> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Work_Orders>(filePath, LoggBuilder);

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
                                $"WO_NUMBER = '{trial.WO_NUMBER}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Work_Orders>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Work_Orders Trxs_Codes)
        {
            parametros.Parameters.Add("WO_NUMBER", Trxs_Codes.WO_NUMBER);
            parametros.Parameters.Add("ACT_TYPE", Trxs_Codes.ACT_TYPE);
            parametros.Parameters.Add("MASTER_SUB", Trxs_Codes.MASTER_SUB);
            parametros.Parameters.Add("CREATED_DATE", Trxs_Codes.CREATED_DATE.ToString());
            parametros.Parameters.Add("CREATED_BY", Trxs_Codes.CREATED_BY);
            parametros.Parameters.Add("PROBLEM_DESC", Trxs_Codes.PROBLEM_DESC);
            parametros.Parameters.Add("NOTES", Trxs_Codes.NOTES);
            parametros.Parameters.Add("ASSIGNED_BY", Trxs_Codes.ASSIGNED_BY);
            parametros.Parameters.Add("ASSIGNED_ON_DATE", Trxs_Codes.ASSIGNED_ON_DATE.ToString());
            parametros.Parameters.Add("ASSIGNED_TO", Trxs_Codes.ASSIGNED_TO);
            parametros.Parameters.Add("TAKEN_BY", Trxs_Codes.TAKEN_BY);
            parametros.Parameters.Add("TAKEN_DATE", Trxs_Codes.TAKEN_DATE.ToString());
            parametros.Parameters.Add("RELEASED_BY", Trxs_Codes.RELEASED_BY);
            parametros.Parameters.Add("RELEASED_DATE", Trxs_Codes.RELEASED_DATE.ToString());
            parametros.Parameters.Add("COMPLETED_BY", Trxs_Codes.COMPLETED_BY);
            parametros.Parameters.Add("COMPLETED_DATE", Trxs_Codes.COMPLETED_DATE.ToString());
            parametros.Parameters.Add("DUE_DATE", Trxs_Codes.DUE_DATE.ToString());
            parametros.Parameters.Add("SHOW_ON", Trxs_Codes.SHOW_ON.ToString());
            parametros.Parameters.Add("TOTAL_LABOR_COST", Trxs_Codes.TOTAL_LABOR_COST);
            parametros.Parameters.Add("TOTAL_PARTS_COST", Trxs_Codes.TOTAL_PARTS_COST);
            parametros.Parameters.Add("USER_EXT", Trxs_Codes.USER_EXT);
            parametros.Parameters.Add("DEPT_OF_ACTION", Trxs_Codes.DEPT_OF_ACTION);
            parametros.Parameters.Add("GUEST_ROOM_YN", Trxs_Codes.GUEST_ROOM_YN);
            parametros.Parameters.Add("PRIORITY_CHANGED_YN", Trxs_Codes.PRIORITY_CHANGED_YN);
            parametros.Parameters.Add("EST_TIME_TO_COMPLETE", Trxs_Codes.EST_TIME_TO_COMPLETE);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("CATEGORY_CODE", Trxs_Codes.CATEGORY_CODE);
            parametros.Parameters.Add("REASON_CODE", Trxs_Codes.REASON_CODE);
            parametros.Parameters.Add("LOCATION_CODE", Trxs_Codes.LOCATION_CODE);
            parametros.Parameters.Add("PRIORITY_CODE", Trxs_Codes.PRIORITY_CODE);
            parametros.Parameters.Add("PARENT_WO_NUMBER", Trxs_Codes.PARENT_WO_NUMBER);
            parametros.Parameters.Add("STATUS_CODE", Trxs_Codes.STATUS_CODE);
            parametros.Parameters.Add("TASK_CODE", Trxs_Codes.TASK_CODE);
            parametros.Parameters.Add("TASKITEM_NUMBER", Trxs_Codes.TASKITEM_NUMBER);
            parametros.Parameters.Add("TYPE_CODE", Trxs_Codes.TYPE_CODE);
            parametros.Parameters.Add("PLANT_ITEM_CODE", Trxs_Codes.PLANT_ITEM_CODE);
            parametros.Parameters.Add("EST_UOT_CODE", Trxs_Codes.EST_UOT_CODE);
            parametros.Parameters.Add("DEPENDING_ON_WO_NUMBER", Trxs_Codes.DEPENDING_ON_WO_NUMBER);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("GUEST_ORIGINATED_YN", Trxs_Codes.GUEST_ORIGINATED_YN);
            parametros.Parameters.Add("START_DATE", Trxs_Codes.START_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("PRIVATE_YN", Trxs_Codes.PRIVATE_YN);
            parametros.Parameters.Add("FO_ROOM_STATUS", Trxs_Codes.FO_ROOM_STATUS);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("DOWNLOAD_RESORT", Trxs_Codes.DOWNLOAD_RESORT);
            parametros.Parameters.Add("DOWNLOAD_DATE", Trxs_Codes.DOWNLOAD_DATE.ToString());
            parametros.Parameters.Add("DOWNLOAD_SREP", Trxs_Codes.DOWNLOAD_SREP);
            parametros.Parameters.Add("UPLOAD_DATE", Trxs_Codes.UPLOAD_DATE.ToString());
            parametros.Parameters.Add("LAPTOP_CHANGE", Trxs_Codes.LAPTOP_CHANGE);
            parametros.Parameters.Add("TRACECODE", Trxs_Codes.TRACECODE);
            parametros.Parameters.Add("SURVEY_ID", Trxs_Codes.SURVEY_ID);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("ATTENDEES", Trxs_Codes.ATTENDEES);
            parametros.Parameters.Add("EXTERNAL_SYSTEM_ID", Trxs_Codes.EXTERNAL_SYSTEM_ID);
            parametros.Parameters.Add("EXTERNAL_SYSTEM", Trxs_Codes.EXTERNAL_SYSTEM);
            parametros.Parameters.Add("SEND_METHOD", Trxs_Codes.SEND_METHOD);
            parametros.Parameters.Add("COMPLETED_YN", Trxs_Codes.COMPLETED_YN);
            parametros.Parameters.Add("ACT_CLASS", Trxs_Codes.ACT_CLASS);
            parametros.Parameters.Add("AUTHOR", Trxs_Codes.AUTHOR);
            parametros.Parameters.Add("ATTACHMENT", Trxs_Codes.ATTACHMENT);
            parametros.Parameters.Add("GENERATED_BY_FREQ_ID", Trxs_Codes.GENERATED_BY_FREQ_ID);
            parametros.Parameters.Add("BFILE_LOCATOR", OracleDbType.BFile).Value = Trxs_Codes.BFILE_LOCATOR;
            parametros.Parameters.Add("EST_RM_NIGHTS", Trxs_Codes.EST_RM_NIGHTS);
            parametros.Parameters.Add("EST_RM_REVENUE", Trxs_Codes.EST_RM_REVENUE);
            parametros.Parameters.Add("EST_CAT_REVENUE", Trxs_Codes.EST_CAT_REVENUE);
            parametros.Parameters.Add("EST_OTHER_REVENUE", Trxs_Codes.EST_OTHER_REVENUE);
            parametros.Parameters.Add("REQUEST_TEMPLATE_ID", Trxs_Codes.REQUEST_TEMPLATE_ID);
            parametros.Parameters.Add("REQUEST_TYPE_ID", Trxs_Codes.REQUEST_TYPE_ID);
            parametros.Parameters.Add("CAMPAIGN_STATUS_CODE", Trxs_Codes.CAMPAIGN_STATUS_CODE);
            parametros.Parameters.Add("GENERATED_BY_CAMPAIGN", Trxs_Codes.GENERATED_BY_CAMPAIGN);
            parametros.Parameters.Add("RESULT", Trxs_Codes.RESULT);
            parametros.Parameters.Add("REQUEST_TYPE_TEMPLATES_ID", Trxs_Codes.REQUEST_TYPE_TEMPLATES_ID);
            parametros.Parameters.Add("NOTIFIED_YN", Trxs_Codes.NOTIFIED_YN);
            parametros.Parameters.Add("DEPOSIT_AMOUNT", Trxs_Codes.DEPOSIT_AMOUNT);
            parametros.Parameters.Add("DEPOSIT_OWNER", Trxs_Codes.DEPOSIT_OWNER);
            parametros.Parameters.Add("ACTIVITY_AMOUNT", Trxs_Codes.ACTIVITY_AMOUNT);
            parametros.Parameters.Add("GUEST_TYPE", Trxs_Codes.GUEST_TYPE);
            parametros.Parameters.Add("ATTACHMENT_LOCATION", Trxs_Codes.ATTACHMENT_LOCATION);
            parametros.Parameters.Add("INTERNAL_YN", Trxs_Codes.INTERNAL_YN);
            parametros.Parameters.Add("DATABASE_ID", Trxs_Codes.DATABASE_ID);
            parametros.Parameters.Add("ATTACHMENT_OWNER", Trxs_Codes.ATTACHMENT_OWNER);
            parametros.Parameters.Add("MINUTES_BEFORE_ALERT", Trxs_Codes.MINUTES_BEFORE_ALERT);
            parametros.Parameters.Add("GLOBAL_YN", Trxs_Codes.GLOBAL_YN);
            parametros.Parameters.Add("TIMEZONE_CONVERTED_YN", Trxs_Codes.TIMEZONE_CONVERTED_YN);
            parametros.Parameters.Add("ORIG_WO_NUMBER", Trxs_Codes.ORIG_WO_NUMBER);
            parametros.Parameters.Add("PROPOSAL_SENT_DATE", Trxs_Codes.PROPOSAL_SENT_DATE.ToString());
            parametros.Parameters.Add("PROPOSAL_VIEW_TOKEN", Trxs_Codes.PROPOSAL_VIEW_TOKEN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;

        }

    }
}
