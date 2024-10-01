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
    public class Room : IServiceJobs
    {
        private readonly ILogger<Room> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Room(ILogger<Room> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Room>(filePath, LoggBuilder);

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
                                $"ROOM = '{trial.ROOM}' and " +
                                $"ROOM_CATEGORY = '{trial.ROOM_CATEGORY}' and " +
                                $"ROOM_CLASS = '{trial.ROOM_CLASS}' and " +
                                $"ROOM_STATUS = '{trial.ROOM_STATUS}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Room>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Room Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("ROOM", Trxs_Codes.ROOM);
            parametros.Parameters.Add("ROOM_CATEGORY", Trxs_Codes.ROOM_CATEGORY);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("ROOM_STATUS", Trxs_Codes.ROOM_STATUS);
            parametros.Parameters.Add("SUITE_TYPE", Trxs_Codes.SUITE_TYPE);
            parametros.Parameters.Add("RM_STATUS_TO_DATE", Trxs_Codes.RM_STATUS_TO_DATE.ToString());
            parametros.Parameters.Add("RM_STATUS_REASON", Trxs_Codes.RM_STATUS_REASON);
            parametros.Parameters.Add("RM_STATUS_REMARKS", Trxs_Codes.RM_STATUS_REMARKS);
            parametros.Parameters.Add("RETURN_STATUS", Trxs_Codes.RETURN_STATUS);
            parametros.Parameters.Add("CREDITS", Trxs_Codes.CREDITS);
            parametros.Parameters.Add("CREDITS_DEPARTURE", Trxs_Codes.CREDITS_DEPARTURE);
            parametros.Parameters.Add("FO_STATUS", Trxs_Codes.FO_STATUS);
            parametros.Parameters.Add("FO_PERS", Trxs_Codes.FO_PERS);
            parametros.Parameters.Add("FRONT_DESK_LOCATION", Trxs_Codes.FRONT_DESK_LOCATION);
            parametros.Parameters.Add("SQUARE_UNITS", Trxs_Codes.SQUARE_UNITS);
            parametros.Parameters.Add("SQUARE_UNIT_MEASUREMENT", Trxs_Codes.SQUARE_UNIT_MEASUREMENT);
            parametros.Parameters.Add("FLOOR", Trxs_Codes.FLOOR);
            parametros.Parameters.Add("UNIT", Trxs_Codes.UNIT);
            parametros.Parameters.Add("PHONE_NUMBER", Trxs_Codes.PHONE_NUMBER);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("KEY_CODE", Trxs_Codes.KEY_CODE);
            parametros.Parameters.Add("PCODE", Trxs_Codes.PCODE);
            parametros.Parameters.Add("BUILDING", Trxs_Codes.BUILDING);
            parametros.Parameters.Add("MAX_OCCUPANCY", Trxs_Codes.MAX_OCCUPANCY);
            parametros.Parameters.Add("MIN_OCCUPANCY", Trxs_Codes.MIN_OCCUPANCY);
            parametros.Parameters.Add("LAST_METER_READING", Trxs_Codes.LAST_METER_READING);
            parametros.Parameters.Add("ASSIGN_STATUS", Trxs_Codes.ASSIGN_STATUS);
            parametros.Parameters.Add("RM_STATUS_FROM_DATE", Trxs_Codes.RM_STATUS_FROM_DATE.ToString());
            parametros.Parameters.Add("ASSIGN_TYPE", Trxs_Codes.ASSIGN_TYPE);
            parametros.Parameters.Add("ASSIGN_REASON", Trxs_Codes.ASSIGN_REASON);
            parametros.Parameters.Add("OCCUPANCY_CONDITION", Trxs_Codes.OCCUPANCY_CONDITION);
            parametros.Parameters.Add("HK_STATUS", Trxs_Codes.HK_STATUS);
            parametros.Parameters.Add("HK_PERS", Trxs_Codes.HK_PERS);
            parametros.Parameters.Add("HK_INSP_FLAG", Trxs_Codes.HK_INSP_FLAG);
            parametros.Parameters.Add("HK_INSP_DATE", Trxs_Codes.HK_INSP_DATE.ToString());
            parametros.Parameters.Add("HK_INSP_EMP_ID", Trxs_Codes.HK_INSP_EMP_ID);
            parametros.Parameters.Add("HK_SECTION_CODE", Trxs_Codes.HK_SECTION_CODE);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("RACK_RATE", Trxs_Codes.RACK_RATE);
            parametros.Parameters.Add("HK_EVENING_SECTION", Trxs_Codes.HK_EVENING_SECTION);
            parametros.Parameters.Add("ROOM_USE_COUNT", Trxs_Codes.ROOM_USE_COUNT);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("ASSIGN_UID", Trxs_Codes.ASSIGN_UID);
            parametros.Parameters.Add("ASSIGN_DATE", Trxs_Codes.ASSIGN_DATE.ToString());
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("OCCUPANT_DISCREPANCY", Trxs_Codes.OCCUPANT_DISCREPANCY);
            parametros.Parameters.Add("PERSON_DISCREPANCY", Trxs_Codes.PERSON_DISCREPANCY);
            parametros.Parameters.Add("SMOKING_PREFERENCE", Trxs_Codes.SMOKING_PREFERENCE);
            parametros.Parameters.Add("PSEUDO_YN", Trxs_Codes.PSEUDO_YN);
            parametros.Parameters.Add("MEETINGROOM_YN", Trxs_Codes.MEETINGROOM_YN);
            parametros.Parameters.Add("REMARKS", Trxs_Codes.REMARKS);
            parametros.Parameters.Add("SHAREABLE_YN", Trxs_Codes.SHAREABLE_YN);
            parametros.Parameters.Add("DIARY_DISPLAY_YN", Trxs_Codes.DIARY_DISPLAY_YN);
            parametros.Parameters.Add("DIARY_NAME", Trxs_Codes.DIARY_NAME);
            parametros.Parameters.Add("AREA_F", Trxs_Codes.AREA_F);
            parametros.Parameters.Add("AREA_M", Trxs_Codes.AREA_M);
            parametros.Parameters.Add("LENGTH_F", Trxs_Codes.LENGTH_F);
            parametros.Parameters.Add("LENGTH_M", Trxs_Codes.LENGTH_M);
            parametros.Parameters.Add("WIDTH_F", Trxs_Codes.WIDTH_F);
            parametros.Parameters.Add("WIDTH_M", Trxs_Codes.WIDTH_M);
            parametros.Parameters.Add("HEIGHTMIN_F", Trxs_Codes.HEIGHTMIN_F);
            parametros.Parameters.Add("HEIGHTMIN_M", Trxs_Codes.HEIGHTMIN_M);
            parametros.Parameters.Add("HEIGHTMAX_F", Trxs_Codes.HEIGHTMAX_F);
            parametros.Parameters.Add("HEIGHTMAX_M", Trxs_Codes.HEIGHTMAX_M);
            parametros.Parameters.Add("WEIGHT_F", Trxs_Codes.WEIGHT_F);
            parametros.Parameters.Add("WEIGHT_M", Trxs_Codes.WEIGHT_M);
            parametros.Parameters.Add("IMAGE_ID", Trxs_Codes.IMAGE_ID);
            parametros.Parameters.Add("LIGHT", Trxs_Codes.LIGHT);
            parametros.Parameters.Add("FACING", Trxs_Codes.FACING);
            parametros.Parameters.Add("DOORS", Trxs_Codes.DOORS);
            parametros.Parameters.Add("LOUDSPEAKERS_YN", Trxs_Codes.LOUDSPEAKERS_YN);
            parametros.Parameters.Add("TV_RADIO_SOCKETS", Trxs_Codes.TV_RADIO_SOCKETS);
            parametros.Parameters.Add("TRANSLATIONBOOTH_NUM", Trxs_Codes.TRANSLATIONBOOTH_NUM);
            parametros.Parameters.Add("MICROPHONE_SOCKETS", Trxs_Codes.MICROPHONE_SOCKETS);
            parametros.Parameters.Add("MICROPHONE_SOCKET_TYPES", Trxs_Codes.MICROPHONE_SOCKET_TYPES);
            parametros.Parameters.Add("HANDICAP_YN", Trxs_Codes.HANDICAP_YN);
            parametros.Parameters.Add("COMBO_YN", Trxs_Codes.COMBO_YN);
            parametros.Parameters.Add("MEETINGROOM_TYPE", Trxs_Codes.MEETINGROOM_TYPE);
            parametros.Parameters.Add("TURNDOWN_YN", Trxs_Codes.TURNDOWN_YN);
            parametros.Parameters.Add("MAX_SHARED_GROUPS", Trxs_Codes.MAX_SHARED_GROUPS);
            parametros.Parameters.Add("ORDER_BY2", Trxs_Codes.ORDER_BY2);
            parametros.Parameters.Add("ORDER_BY3", Trxs_Codes.ORDER_BY3);
            parametros.Parameters.Add("ORDER_BY4", Trxs_Codes.ORDER_BY4);
            parametros.Parameters.Add("ORDER_BY5", Trxs_Codes.ORDER_BY5);
            parametros.Parameters.Add("SETUP_NOTES", Trxs_Codes.SETUP_NOTES);
            parametros.Parameters.Add("CREDITS_PICKUP", Trxs_Codes.CREDITS_PICKUP);
            parametros.Parameters.Add("SERVICE_STATUS", Trxs_Codes.SERVICE_STATUS);
            parametros.Parameters.Add("FORCE_ALTERNATE_YN", Trxs_Codes.FORCE_ALTERNATE_YN);
            parametros.Parameters.Add("CREDITS_TURNDOWN", Trxs_Codes.CREDITS_TURNDOWN);
            parametros.Parameters.Add("FULL_UTILIZATION_TIME", Trxs_Codes.FULL_UTILIZATION_TIME);
            parametros.Parameters.Add("EXCLUDED_EVENT_TYPES", Trxs_Codes.EXCLUDED_EVENT_TYPES);
            parametros.Parameters.Add("ONLINE_PRINTING_YN", Trxs_Codes.ONLINE_PRINTING_YN);
            parametros.Parameters.Add("HOLD_DATE_TIME", Trxs_Codes.HOLD_DATE_TIME.ToString());
            parametros.Parameters.Add("HOLD_USER", Trxs_Codes.HOLD_USER);
            parametros.Parameters.Add("HOLD_TYPE", Trxs_Codes.HOLD_TYPE);
            parametros.Parameters.Add("COMMENTS", Trxs_Codes.COMMENTS);
            parametros.Parameters.Add("NO_OF_BEDS", Trxs_Codes.NO_OF_BEDS);
            parametros.Parameters.Add("LAST_CHECK_OUT_DATE", Trxs_Codes.LAST_CHECK_OUT_DATE.ToString());
            parametros.Parameters.Add("WEB_BOOKING_YN", Trxs_Codes.WEB_BOOKING_YN);
            parametros.Parameters.Add("MIN_ADVANCE", Trxs_Codes.MIN_ADVANCE);
            parametros.Parameters.Add("MAX_ADVANCE", Trxs_Codes.MAX_ADVANCE);
            parametros.Parameters.Add("MINIMUM_REVENUE", Trxs_Codes.MINIMUM_REVENUE);
            parametros.Parameters.Add("OVOS_UNIT_YN", Trxs_Codes.OVOS_UNIT_YN);
            parametros.Parameters.Add("MAX_CAPACITY", Trxs_Codes.MAX_CAPACITY);
            parametros.Parameters.Add("VISIBLE_ON_WEB_YN", Trxs_Codes.VISIBLE_ON_WEB_YN);
            parametros.Parameters.Add("OVOS_GRADE_CODE", Trxs_Codes.OVOS_GRADE_CODE);
            parametros.Parameters.Add("ROOM_ASSIGNMENT_RATING", Trxs_Codes.ROOM_ASSIGNMENT_RATING);
            parametros.Parameters.Add("HK_ASSIGNMENT_ORDER_BY", Trxs_Codes.HK_ASSIGNMENT_ORDER_BY);
            parametros.Parameters.Add("EVISITOR_FACILITY_ID", Trxs_Codes.EVISITOR_FACILITY_ID);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
