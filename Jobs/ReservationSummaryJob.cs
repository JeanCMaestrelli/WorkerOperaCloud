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
    public class ReservationSummaryJob : IServiceJobs
    {
        private readonly ILogger<ReservationSummaryJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public ReservationSummaryJob(ILogger<ReservationSummaryJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Reservation_Summary>(filePath, LoggBuilder);

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
                            var _where = $"ID = '{trial.ID}' and " +
                                        $"RESORT = '{trial.RESORT}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Reservation_Summary>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Reservation_Summary Trxs_Codes)
        {
            parametros.Parameters.Add("ID", Trxs_Codes.ID);
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("EVENT_TYPE", Trxs_Codes.EVENT_TYPE);
            parametros.Parameters.Add("EVENT_ID", Trxs_Codes.EVENT_ID);
            parametros.Parameters.Add("CONSIDERED_DATE", Trxs_Codes.CONSIDERED_DATE.ToString());
            parametros.Parameters.Add("ROOM_CATEGORY", Trxs_Codes.ROOM_CATEGORY);
            parametros.Parameters.Add("ROOM_CLASS", Trxs_Codes.ROOM_CLASS);
            parametros.Parameters.Add("MARKET_CODE", Trxs_Codes.MARKET_CODE);
            parametros.Parameters.Add("SOURCE_CODE", Trxs_Codes.SOURCE_CODE);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("REGION_CODE", Trxs_Codes.REGION_CODE);
            parametros.Parameters.Add("GROUP_ID", Trxs_Codes.GROUP_ID);
            parametros.Parameters.Add("RESV_TYPE", Trxs_Codes.RESV_TYPE);
            parametros.Parameters.Add("RESV_INV_TYPE", Trxs_Codes.RESV_INV_TYPE);
            parametros.Parameters.Add("PSUEDO_ROOM_YN", Trxs_Codes.PSUEDO_ROOM_YN);
            parametros.Parameters.Add("ARR_ROOMS", Trxs_Codes.ARR_ROOMS);
            parametros.Parameters.Add("ADULTS", Trxs_Codes.ADULTS);
            parametros.Parameters.Add("CHILDREN", Trxs_Codes.CHILDREN);
            parametros.Parameters.Add("DEP_ROOMS", Trxs_Codes.DEP_ROOMS);
            parametros.Parameters.Add("NO_ROOMS", Trxs_Codes.NO_ROOMS);
            parametros.Parameters.Add("GROSS_RATE", Trxs_Codes.GROSS_RATE);
            parametros.Parameters.Add("NET_ROOM_REVENUE", Trxs_Codes.NET_ROOM_REVENUE);
            parametros.Parameters.Add("EXTRA_REVENUE", Trxs_Codes.EXTRA_REVENUE);
            parametros.Parameters.Add("OO_ROOMS", Trxs_Codes.OO_ROOMS);
            parametros.Parameters.Add("OS_ROOMS", Trxs_Codes.OS_ROOMS);
            parametros.Parameters.Add("REMAINING_BLOCK_ROOMS", Trxs_Codes.REMAINING_BLOCK_ROOMS);
            parametros.Parameters.Add("PICKEDUP_BLOCK_ROOMS", Trxs_Codes.PICKEDUP_BLOCK_ROOMS);
            parametros.Parameters.Add("SINGLE_OCCUPANCY", Trxs_Codes.SINGLE_OCCUPANCY);
            parametros.Parameters.Add("MULTIPLE_OCCUPANCY", Trxs_Codes.MULTIPLE_OCCUPANCY);
            parametros.Parameters.Add("BLOCK_STATUS", Trxs_Codes.BLOCK_STATUS);
            parametros.Parameters.Add("ARR_PERSONS", Trxs_Codes.ARR_PERSONS);
            parametros.Parameters.Add("DEP_PERSONS", Trxs_Codes.DEP_PERSONS);
            parametros.Parameters.Add("WL_ROOMS", Trxs_Codes.WL_ROOMS);
            parametros.Parameters.Add("WL_PERSONS", Trxs_Codes.WL_PERSONS);
            parametros.Parameters.Add("DAY_USE_ROOMS", Trxs_Codes.DAY_USE_ROOMS);
            parametros.Parameters.Add("DAY_USE_PERSONS", Trxs_Codes.DAY_USE_PERSONS);
            parametros.Parameters.Add("BOOKING_STATUS", Trxs_Codes.BOOKING_STATUS);
            parametros.Parameters.Add("RESV_STATUS", Trxs_Codes.RESV_STATUS);
            parametros.Parameters.Add("DAY_USE_YN", Trxs_Codes.DAY_USE_YN);
            parametros.Parameters.Add("CHANNEL", Trxs_Codes.CHANNEL);
            parametros.Parameters.Add("COUNTRY", Trxs_Codes.COUNTRY);
            parametros.Parameters.Add("NATIONALITY", Trxs_Codes.NATIONALITY);
            parametros.Parameters.Add("CRIBS", Trxs_Codes.CRIBS);
            parametros.Parameters.Add("EXTRA_BEDS", Trxs_Codes.EXTRA_BEDS);
            parametros.Parameters.Add("ADULTS_TAX_FREE", Trxs_Codes.ADULTS_TAX_FREE);
            parametros.Parameters.Add("CHILDREN_TAX_FREE", Trxs_Codes.CHILDREN_TAX_FREE);
            parametros.Parameters.Add("RATE_CATEGORY", Trxs_Codes.RATE_CATEGORY);
            parametros.Parameters.Add("RATE_CLASS", Trxs_Codes.RATE_CLASS);
            parametros.Parameters.Add("ROOM_REVENUE", Trxs_Codes.ROOM_REVENUE);
            parametros.Parameters.Add("FOOD_REVENUE", Trxs_Codes.FOOD_REVENUE);
            parametros.Parameters.Add("OTHER_REVENUE", Trxs_Codes.OTHER_REVENUE);
            parametros.Parameters.Add("TOTAL_REVENUE", Trxs_Codes.TOTAL_REVENUE);
            parametros.Parameters.Add("NON_REVENUE", Trxs_Codes.NON_REVENUE);
            parametros.Parameters.Add("ALLOTMENT_HEADER_ID", Trxs_Codes.ALLOTMENT_HEADER_ID);
            parametros.Parameters.Add("ROOM_REVENUE_TAX", Trxs_Codes.ROOM_REVENUE_TAX);
            parametros.Parameters.Add("FOOD_REVENUE_TAX", Trxs_Codes.FOOD_REVENUE_TAX);
            parametros.Parameters.Add("OTHER_REVENUE_TAX", Trxs_Codes.OTHER_REVENUE_TAX);
            parametros.Parameters.Add("TOTAL_REVENUE_TAX", Trxs_Codes.TOTAL_REVENUE_TAX);
            parametros.Parameters.Add("NON_REVENUE_TAX", Trxs_Codes.NON_REVENUE_TAX);
            parametros.Parameters.Add("CITY", Trxs_Codes.CITY);
            parametros.Parameters.Add("ZIP_CODE", Trxs_Codes.ZIP_CODE);
            parametros.Parameters.Add("DISTRICT", Trxs_Codes.DISTRICT);
            parametros.Parameters.Add("STATE", Trxs_Codes.STATE);
            parametros.Parameters.Add("CHILDREN1", Trxs_Codes.CHILDREN1);
            parametros.Parameters.Add("CHILDREN2", Trxs_Codes.CHILDREN2);
            parametros.Parameters.Add("CHILDREN3", Trxs_Codes.CHILDREN3);
            parametros.Parameters.Add("CHILDREN4", Trxs_Codes.CHILDREN4);
            parametros.Parameters.Add("CHILDREN5", Trxs_Codes.CHILDREN5);
            parametros.Parameters.Add("OWNER_FF_FLAG", Trxs_Codes.OWNER_FF_FLAG);
            parametros.Parameters.Add("OWNER_RENTAL_FLAG", Trxs_Codes.OWNER_RENTAL_FLAG);
            parametros.Parameters.Add("FC_GROSS_RATE", Trxs_Codes.FC_GROSS_RATE);
            parametros.Parameters.Add("FC_NET_ROOM_REVENUE", Trxs_Codes.FC_NET_ROOM_REVENUE);
            parametros.Parameters.Add("FC_EXTRA_REVENUE", Trxs_Codes.FC_EXTRA_REVENUE);
            parametros.Parameters.Add("FC_ROOM_REVENUE", Trxs_Codes.FC_ROOM_REVENUE);
            parametros.Parameters.Add("FC_FOOD_REVENUE", Trxs_Codes.FC_FOOD_REVENUE);
            parametros.Parameters.Add("FC_OTHER_REVENUE", Trxs_Codes.FC_OTHER_REVENUE);
            parametros.Parameters.Add("FC_TOTAL_REVENUE", Trxs_Codes.FC_TOTAL_REVENUE);
            parametros.Parameters.Add("FC_NON_REVENUE", Trxs_Codes.FC_NON_REVENUE);
            parametros.Parameters.Add("FC_ROOM_REVENUE_TAX", Trxs_Codes.FC_ROOM_REVENUE_TAX);
            parametros.Parameters.Add("FC_FOOD_REVENUE_TAX", Trxs_Codes.FC_FOOD_REVENUE_TAX);
            parametros.Parameters.Add("FC_OTHER_REVENUE_TAX", Trxs_Codes.FC_OTHER_REVENUE_TAX);
            parametros.Parameters.Add("FC_TOTAL_REVENUE_TAX", Trxs_Codes.FC_TOTAL_REVENUE_TAX);
            parametros.Parameters.Add("FC_NON_REVENUE_TAX", Trxs_Codes.FC_NON_REVENUE_TAX);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("EXCHANGE_DATE", Trxs_Codes.EXCHANGE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_BUSINESS_DATE", Trxs_Codes.UPDATE_BUSINESS_DATE.ToString());
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("CENTRAL_CURRENCY_CODE", Trxs_Codes.CENTRAL_CURRENCY_CODE);
            parametros.Parameters.Add("CENTRAL_EXCHANGE_RATE", Trxs_Codes.CENTRAL_EXCHANGE_RATE);
            parametros.Parameters.Add("TRUNC_BEGIN_DATE", Trxs_Codes.TRUNC_BEGIN_DATE.ToString());
            parametros.Parameters.Add("TRUNC_END_DATE", Trxs_Codes.TRUNC_END_DATE.ToString());
            parametros.Parameters.Add("BUSINESS_DATE_CREATED", Trxs_Codes.BUSINESS_DATE_CREATED.ToString());
            parametros.Parameters.Add("RES_INSERT_SOURCE", Trxs_Codes.RES_INSERT_SOURCE);
            parametros.Parameters.Add("PARENT_COMPANY_ID", Trxs_Codes.PARENT_COMPANY_ID);
            parametros.Parameters.Add("AGENT_ID", Trxs_Codes.AGENT_ID);
            parametros.Parameters.Add("GENDER", Trxs_Codes.GENDER);
            parametros.Parameters.Add("VIP_STATUS", Trxs_Codes.VIP_STATUS);
            parametros.Parameters.Add("QUANTITY", Trxs_Codes.QUANTITY);
            parametros.Parameters.Add("TURNDOWN_STATUS", Trxs_Codes.TURNDOWN_STATUS);
            parametros.Parameters.Add("BOOKED_ROOM_CATEGORY", Trxs_Codes.BOOKED_ROOM_CATEGORY);
            parametros.Parameters.Add("SOURCE_PROF_ID", Trxs_Codes.SOURCE_PROF_ID);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
