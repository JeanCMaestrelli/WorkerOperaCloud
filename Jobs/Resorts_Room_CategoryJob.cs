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
    public class Resorts_Room_CategoryJob : IServiceJobs
    {
        private readonly ILogger<Resorts_Room_CategoryJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Resorts_Room_CategoryJob(ILogger<Resorts_Room_CategoryJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Resorts_Room_Category>(filePath, LoggBuilder);

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
                                $"ROOM_CATEGORY = '{trial.ROOM_CATEGORY}' and " +
                                $"ROOM_CLASS = '{trial.ROOM_CLASS}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Resorts_Room_Category>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Resorts_Room_Category Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("ROOM_CATEGORY", Trxs_Codes.ROOM_CATEGORY);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("NUMBER_ROOMS", Trxs_Codes.NUMBER_ROOMS);
            parametros.Parameters.Add("SHORT_DESCRIPTION", Trxs_Codes.SHORT_DESCRIPTION);
            parametros.Parameters.Add("LONG_DESCRIPTION", Trxs_Codes.LONG_DESCRIPTION);
            parametros.Parameters.Add("COMPILED", Trxs_Codes.COMPILED);
            parametros.Parameters.Add("PSUEDO_ROOM_TYPE", Trxs_Codes.PSUEDO_ROOM_TYPE);
            parametros.Parameters.Add("ACTIVE_DATE", Trxs_Codes.ACTIVE_DATE.ToString());
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("REPLACES_CATEGORY", Trxs_Codes.REPLACES_CATEGORY);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("MAX_OCCUPANCY", Trxs_Codes.MAX_OCCUPANCY);
            parametros.Parameters.Add("MAX_ROLLAWAYS", Trxs_Codes.MAX_ROLLAWAYS);
            parametros.Parameters.Add("RATE_CATEGORY", Trxs_Codes.RATE_CATEGORY);
            parametros.Parameters.Add("LABEL", Trxs_Codes.LABEL);
            parametros.Parameters.Add("GENERIC_FLAG", Trxs_Codes.GENERIC_FLAG);
            parametros.Parameters.Add("SUITE_YN", Trxs_Codes.SUITE_YN);
            parametros.Parameters.Add("MEETINGROOM_YN", Trxs_Codes.MEETINGROOM_YN);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("RATE_AMOUNT", Trxs_Codes.RATE_AMOUNT);
            parametros.Parameters.Add("DEF_OCCUPANCY", Trxs_Codes.DEF_OCCUPANCY);
            parametros.Parameters.Add("IMAGE_ID", Trxs_Codes.IMAGE_ID);
            parametros.Parameters.Add("PSEUDO_YN", Trxs_Codes.PSEUDO_YN);
            parametros.Parameters.Add("SEND_TO_INTERFACE_YN", Trxs_Codes.SEND_TO_INTERFACE_YN);
            parametros.Parameters.Add("YIELDABLE_YN", Trxs_Codes.YIELDABLE_YN);
            parametros.Parameters.Add("YIELD_CATEGORY", Trxs_Codes.YIELD_CATEGORY);
            parametros.Parameters.Add("HOUSEKEEPING", Trxs_Codes.HOUSEKEEPING);
            parametros.Parameters.Add("CAN_DELETE_YN", Trxs_Codes.CAN_DELETE_YN);
            parametros.Parameters.Add("ROOMINFO_URL", Trxs_Codes.ROOMINFO_URL);
            parametros.Parameters.Add("OWNER_YN", Trxs_Codes.OWNER_YN);
            parametros.Parameters.Add("AUTO_INCLUDE_YN", Trxs_Codes.AUTO_INCLUDE_YN);
            parametros.Parameters.Add("INITIAL_ROUND_UP", Trxs_Codes.INITIAL_ROUND_UP);
            parametros.Parameters.Add("INCREMENTS", Trxs_Codes.INCREMENTS);
            parametros.Parameters.Add("ROTATION_GROUP", Trxs_Codes.ROTATION_GROUP);
            parametros.Parameters.Add("SALES_FLAG", Trxs_Codes.SALES_FLAG);
            parametros.Parameters.Add("AUTO_ROOM_ASSIGN_YN", Trxs_Codes.AUTO_ROOM_ASSIGN_YN);
            parametros.Parameters.Add("UPSELL_YN", Trxs_Codes.UPSELL_YN);
            parametros.Parameters.Add("ORMS_UPSELL_RANK", Trxs_Codes.ORMS_UPSELL_RANK);
            parametros.Parameters.Add("ORMS_UPSELL_AMT", Trxs_Codes.ORMS_UPSELL_AMT);
            parametros.Parameters.Add("ORMS_DRXTRA_ADULT", Trxs_Codes.ORMS_DRXTRA_ADULT);
            parametros.Parameters.Add("ORMS_DRXTRA_CHILD", Trxs_Codes.ORMS_DRXTRA_CHILD);
            parametros.Parameters.Add("MAX_OCCUPANCY_ADULTS", Trxs_Codes.MAX_OCCUPANCY_ADULTS);
            parametros.Parameters.Add("MAX_OCCUPANCY_CHILDREN", Trxs_Codes.MAX_OCCUPANCY_CHILDREN);
            parametros.Parameters.Add("ROOM_POOL", Trxs_Codes.ROOM_POOL);
            parametros.Parameters.Add("MEMBER_AWARD_ROOM_GRP", Trxs_Codes.MEMBER_AWARD_ROOM_GRP);
            parametros.Parameters.Add("ORMS_DRXTRA_2ND_ADULT", Trxs_Codes.ORMS_DRXTRA_2ND_ADULT);
            parametros.Parameters.Add("ORMS_DRTIER1", Trxs_Codes.ORMS_DRTIER1);
            parametros.Parameters.Add("ORMS_DRTIER2", Trxs_Codes.ORMS_DRTIER2);
            parametros.Parameters.Add("ORMS_DRTIER3", Trxs_Codes.ORMS_DRTIER3);
            parametros.Parameters.Add("AUTO_CHECKIN_YN", Trxs_Codes.AUTO_CHECKIN_YN);
            parametros.Parameters.Add("RATE_FLOOR", Trxs_Codes.RATE_FLOOR);
            parametros.Parameters.Add("MAX_FIX_BED_OCCUPANCY", Trxs_Codes.MAX_FIX_BED_OCCUPANCY);
            parametros.Parameters.Add("MAINTENANCE_YN", Trxs_Codes.MAINTENANCE_YN);
            parametros.Parameters.Add("SMOKING_PREFERENCE", Trxs_Codes.SMOKING_PREFERENCE);
            parametros.Parameters.Add("S_LABEL", Trxs_Codes.S_LABEL);
            parametros.Parameters.Add("S_BEDTYPE", Trxs_Codes.S_BEDTYPE);
            parametros.Parameters.Add("SELL_THRU_RULE_YN", Trxs_Codes.SELL_THRU_RULE_YN);
            parametros.Parameters.Add("CRS_DESCRIPTION", Trxs_Codes.CRS_DESCRIPTION);
            parametros.Parameters.Add("EVISITOR_FACILITY_ID", Trxs_Codes.EVISITOR_FACILITY_ID);
            parametros.Parameters.Add("MIN_OCCUPANCY", Trxs_Codes.MIN_OCCUPANCY);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
