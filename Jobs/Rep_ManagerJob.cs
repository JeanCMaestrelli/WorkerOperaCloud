using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Repository.Jobs;
using WorkerOperaCloud.Services;
using WorkerOperaCloud.Services.Interfaces;

namespace WorkerOperaCloud.Jobs
{
    public class Rep_ManagerJob : IServiceJobs
    {
        private readonly ILogger<Rep_ManagerJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Rep_ManagerJob(ILogger<Rep_ManagerJob> logger, DapperContext context,  Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Rep_Manager>(filePath, LoggBuilder);

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
                            var _where = $"BUSINESS_DATE = TO_DATE('{trial.BUSINESS_DATE.ToString()}', 'dd/mm/yyyy hh24:mi:ss') and " +
                                        $"ROOM_CLASS = '{trial.ROOM_CLASS}' and " +
                                        $"RESORT = '{trial.RESORT}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Rep_Manager>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Rep_Manager RepManager)
        {
            parametros.Parameters.Add("BUSINESS_DATE", RepManager.BUSINESS_DATE.ToString());
            parametros.Parameters.Add("ROOM_CLASS", RepManager.ROOM_CLASS);
            parametros.Parameters.Add("RESORT", RepManager.RESORT);
            parametros.Parameters.Add("OCC_ROOM", RepManager.OCC_ROOM);
            parametros.Parameters.Add("COMP_ROOM", RepManager.COMP_ROOM);
            parametros.Parameters.Add("HOUSE_USE_ROOM", RepManager.HOUSE_USE_ROOM);
            parametros.Parameters.Add("DAYUSE_ROOM", RepManager.DAYUSE_ROOM);
            parametros.Parameters.Add("AVAIL_ROOM", RepManager.AVAIL_ROOM);
            parametros.Parameters.Add("OOO_ROOMS", RepManager.OOO_ROOMS);
            parametros.Parameters.Add("OS_ROOMS", RepManager.OS_ROOMS);
            parametros.Parameters.Add("ADR_ROOM_WO_COMP_HOUSE", RepManager.ADR_ROOM_WO_COMP_HOUSE);
            parametros.Parameters.Add("AVERAGE_DAILY_REVENUE", RepManager.AVERAGE_DAILY_REVENUE);
            parametros.Parameters.Add("GUEST_IN_HOUSE", RepManager.GUEST_IN_HOUSE);
            parametros.Parameters.Add("PER_OCCUPANCY", RepManager.PER_OCCUPANCY);
            parametros.Parameters.Add("PER_OCC_WO_COMP_HOUSE", RepManager.PER_OCC_WO_COMP_HOUSE);
            parametros.Parameters.Add("PER_OCC_WO_COMP_HOUSE_OO", RepManager.PER_OCC_WO_COMP_HOUSE_OO);
            parametros.Parameters.Add("ARRIVAL_ROOM", RepManager.ARRIVAL_ROOM);
            parametros.Parameters.Add("ARRIVAL_PRS", RepManager.ARRIVAL_PRS);
            parametros.Parameters.Add("DEPARTURE_ROOM", RepManager.DEPARTURE_ROOM);
            parametros.Parameters.Add("DEPARTURE_PRS", RepManager.DEPARTURE_PRS);
            parametros.Parameters.Add("EXT_STAY_ROOM", RepManager.EXT_STAY_ROOM);
            parametros.Parameters.Add("EXT_STAY_PRS", RepManager.EXT_STAY_PRS);
            parametros.Parameters.Add("EARLY_DEP_ROOM", RepManager.EARLY_DEP_ROOM);
            parametros.Parameters.Add("EARLY_DEP_PRS", RepManager.EARLY_DEP_PRS);
            parametros.Parameters.Add("WALKIN_ROOM", RepManager.WALKIN_ROOM);
            parametros.Parameters.Add("WALKIN_PRS", RepManager.WALKIN_PRS);
            parametros.Parameters.Add("NOSHOW_ROOM", RepManager.NOSHOW_ROOM);
            parametros.Parameters.Add("NOSHOW_PRS", RepManager.NOSHOW_PRS);
            parametros.Parameters.Add("CANCEL_RESERVATION", RepManager.CANCEL_RESERVATION);
            parametros.Parameters.Add("CANCEL_ROOMS", RepManager.CANCEL_ROOMS);
            parametros.Parameters.Add("LATE_CANCEL_RESERVATION", RepManager.LATE_CANCEL_RESERVATION);
            parametros.Parameters.Add("LATE_CANCEL_ROOMS", RepManager.LATE_CANCEL_ROOMS);
            parametros.Parameters.Add("RESERVATION", RepManager.RESERVATION);
            parametros.Parameters.Add("TURNAWAY", RepManager.TURNAWAY);
            parametros.Parameters.Add("TOMORROW_ARRIVAL_ROOM", RepManager.TOMORROW_ARRIVAL_ROOM);
            parametros.Parameters.Add("TOMORROW_ARRIVAL_PRS", RepManager.TOMORROW_ARRIVAL_PRS);
            parametros.Parameters.Add("TOMORROW_DEPARTURE_ROOM", RepManager.TOMORROW_DEPARTURE_ROOM);
            parametros.Parameters.Add("TOMORROW_DEPARTURE_PRS", RepManager.TOMORROW_DEPARTURE_PRS);
            parametros.Parameters.Add("FOOD_BEV_REVENUE", RepManager.FOOD_BEV_REVENUE);
            parametros.Parameters.Add("OCC_WEEK", RepManager.OCC_WEEK);
            parametros.Parameters.Add("OCC_MONTH", RepManager.OCC_MONTH);
            parametros.Parameters.Add("OCC_YEAR", RepManager.OCC_YEAR);
            parametros.Parameters.Add("ROOM_REVENUE", RepManager.ROOM_REVENUE);
            parametros.Parameters.Add("CLEAN_ROOM", RepManager.CLEAN_ROOM);
            parametros.Parameters.Add("INSPECTED_ROOM", RepManager.INSPECTED_ROOM);
            parametros.Parameters.Add("PAYMENT", RepManager.PAYMENT);
            parametros.Parameters.Add("GROUP_REVENUE", RepManager.GROUP_REVENUE);
            parametros.Parameters.Add("GROUP_ROOM", RepManager.GROUP_ROOM);
            parametros.Parameters.Add("VIP_GUEST", RepManager.VIP_GUEST);
            parametros.Parameters.Add("NO_OF_GROUP", RepManager.NO_OF_GROUP);
            parametros.Parameters.Add("GROUP_PRS", RepManager.GROUP_PRS);
            parametros.Parameters.Add("GROUP_ROOM_REV", RepManager.GROUP_ROOM_REV);
            parametros.Parameters.Add("ADULTS_IN_HOUSE", RepManager.ADULTS_IN_HOUSE);
            parametros.Parameters.Add("CHILDREN_IN_HOUSE", RepManager.CHILDREN_IN_HOUSE);
            parametros.Parameters.Add("OTHER_REVENUE", RepManager.OTHER_REVENUE);
            parametros.Parameters.Add("TOTAL_REVENUE", RepManager.TOTAL_REVENUE);
            parametros.Parameters.Add("PHYSICAL_ROOM", RepManager.PHYSICAL_ROOM);
            parametros.Parameters.Add("OCC_TOMMORROW", RepManager.OCC_TOMMORROW);
            parametros.Parameters.Add("SINGLE_OCC_ROOM", RepManager.SINGLE_OCC_ROOM);
            parametros.Parameters.Add("MULTI_OCC_ROOM", RepManager.MULTI_OCC_ROOM);
            parametros.Parameters.Add("ROOM_TAX", RepManager.ROOM_TAX);
            parametros.Parameters.Add("FOOD_BEV_TAX", RepManager.FOOD_BEV_TAX);
            parametros.Parameters.Add("OTHER_TAX", RepManager.OTHER_TAX);
            parametros.Parameters.Add("TOTAL_TAX", RepManager.TOTAL_TAX);
            parametros.Parameters.Add("GROUP_TAX", RepManager.GROUP_TAX);
            parametros.Parameters.Add("GROUP_ROOM_TAX", RepManager.GROUP_ROOM_TAX);
            parametros.Parameters.Add("FIT_GUESTS_TODAY", RepManager.FIT_GUESTS_TODAY);
            parametros.Parameters.Add("AGENT_ROOMS_TODAY", RepManager.AGENT_ROOMS_TODAY);
            parametros.Parameters.Add("COMPANY_ROOMS_TODAY", RepManager.COMPANY_ROOMS_TODAY);
            parametros.Parameters.Add("SOURCE_ROOMS_TODAY", RepManager.SOURCE_ROOMS_TODAY);
            parametros.Parameters.Add("DEF_ARRIVALS_TODAY", RepManager.DEF_ARRIVALS_TODAY);
            parametros.Parameters.Add("TNT_ARRIVALS_TODAY", RepManager.TNT_ARRIVALS_TODAY);
            parametros.Parameters.Add("DIRTY_ROOMS_TODAY", RepManager.DIRTY_ROOMS_TODAY);
            parametros.Parameters.Add("BIRTHDAYS", RepManager.BIRTHDAYS);
            parametros.Parameters.Add("DBLS_AS_SGLS_TODAY", RepManager.DBLS_AS_SGLS_TODAY);
            parametros.Parameters.Add("CANCELLATIONS_MADE_TODAY", RepManager.CANCELLATIONS_MADE_TODAY);
            parametros.Parameters.Add("ROOM_NIGHTS_RESERVED_TODAY", RepManager.ROOM_NIGHTS_RESERVED_TODAY);
            parametros.Parameters.Add("FIT_REVENUE_TODAY", RepManager.FIT_REVENUE_TODAY);
            parametros.Parameters.Add("MEMBER_TOTAL_TAX", RepManager.MEMBER_TOTAL_TAX);
            parametros.Parameters.Add("MEM_REVENUE_TODAY", RepManager.MEM_REVENUE_TODAY);
            parametros.Parameters.Add("FIT_DEP_TODAY", RepManager.FIT_DEP_TODAY);
            parametros.Parameters.Add("FIT_DEP_PRS_TODAY", RepManager.FIT_DEP_PRS_TODAY);
            parametros.Parameters.Add("MEM_DEP_PRS_TODAY", RepManager.MEM_DEP_PRS_TODAY);
            parametros.Parameters.Add("FIT_MEM_DEP_PRS", RepManager.FIT_MEM_DEP_PRS);
            parametros.Parameters.Add("MEM_INHOUSE_PRS_TODAY", RepManager.MEM_INHOUSE_PRS_TODAY);
            parametros.Parameters.Add("BEDS_AVL_TODAY", RepManager.BEDS_AVL_TODAY);
            parametros.Parameters.Add("CRIBS_TODAY", RepManager.CRIBS_TODAY);
            parametros.Parameters.Add("AVERAGE_AGE_TODAY", RepManager.AVERAGE_AGE_TODAY);
            parametros.Parameters.Add("ROLLAWAYS_TODAY", RepManager.ROLLAWAYS_TODAY);
            parametros.Parameters.Add("ADULTS_FREE_TODAY", RepManager.ADULTS_FREE_TODAY);
            parametros.Parameters.Add("CHILDREN_FREE_TODAY", RepManager.CHILDREN_FREE_TODAY);
            parametros.Parameters.Add("RACK_RATE_OCC", RepManager.RACK_RATE_OCC);
            parametros.Parameters.Add("YIELD_ALL_ROOMS", RepManager.YIELD_ALL_ROOMS);
            parametros.Parameters.Add("YIELD_OCC_ROOMS", RepManager.YIELD_OCC_ROOMS);
            parametros.Parameters.Add("RACK_RATE_ALL", RepManager.RACK_RATE_ALL);
            parametros.Parameters.Add("FIT_ROOMS_TODAY", RepManager.FIT_ROOMS_TODAY);
            parametros.Parameters.Add("GROUP_ROOMS_TODAY", RepManager.GROUP_ROOMS_TODAY);
            parametros.Parameters.Add("FIT_MEM_DEP_ROOMS", RepManager.FIT_MEM_DEP_ROOMS);
            parametros.Parameters.Add("MEM_DEP_ROOMS_TODAY", RepManager.MEM_DEP_ROOMS_TODAY);
            parametros.Parameters.Add("FIT_ROOM_REVENUE", RepManager.FIT_ROOM_REVENUE);
            parametros.Parameters.Add("INHOUSE_MAX_OCCUPANCY", RepManager.INHOUSE_MAX_OCCUPANCY);
            parametros.Parameters.Add("EXT_NOSHOW_PRS", RepManager.EXT_NOSHOW_PRS);
            parametros.Parameters.Add("EXT_NOSHOW_ROOM", RepManager.EXT_NOSHOW_ROOM);
            parametros.Parameters.Add("EXT_NOSHOW_RES", RepManager.EXT_NOSHOW_RES);
            parametros.Parameters.Add("CHILDREN1", RepManager.CHILDREN1);
            parametros.Parameters.Add("CHILDREN2", RepManager.CHILDREN2);
            parametros.Parameters.Add("CHILDREN3", RepManager.CHILDREN3);
            parametros.Parameters.Add("CHILDREN4", RepManager.CHILDREN4);
            parametros.Parameters.Add("CHILDREN5", RepManager.CHILDREN5);
            parametros.Parameters.Add("OCC_NEXT_31_DAYS", RepManager.OCC_NEXT_31_DAYS);
            parametros.Parameters.Add("OCC_NEXT_365_DAYS", RepManager.OCC_NEXT_365_DAYS);
            parametros.Parameters.Add("OCC_REST_OF_MONTH", RepManager.OCC_REST_OF_MONTH);
            parametros.Parameters.Add("OCC_REST_OF_YEAR", RepManager.OCC_REST_OF_YEAR);
            parametros.Parameters.Add("COMPANY_ROOM_REVENUE", RepManager.COMPANY_ROOM_REVENUE);
            parametros.Parameters.Add("COMPANY_ROOM_TAX", RepManager.COMPANY_ROOM_TAX);
            parametros.Parameters.Add("COMPANY_TOTAL_REVENUE", RepManager.COMPANY_TOTAL_REVENUE);
            parametros.Parameters.Add("COMPANY_TOTAL_TAX", RepManager.COMPANY_TOTAL_TAX);
            parametros.Parameters.Add("AGENT_ROOM_REVENUE", RepManager.AGENT_ROOM_REVENUE);
            parametros.Parameters.Add("AGENT_ROOM_TAX", RepManager.AGENT_ROOM_TAX);
            parametros.Parameters.Add("AGENT_TOTAL_REVENUE", RepManager.AGENT_TOTAL_REVENUE);
            parametros.Parameters.Add("AGENT_TOTAL_TAX", RepManager.AGENT_TOTAL_TAX);
            parametros.Parameters.Add("REPEAT_ROOMS", RepManager.REPEAT_ROOMS);
            parametros.Parameters.Add("REPEAT_PERSONS", RepManager.REPEAT_PERSONS);
            parametros.Parameters.Add("REPEAT_ROOM_REVENUE", RepManager.REPEAT_ROOM_REVENUE);
            parametros.Parameters.Add("REPEAT_ROOM_TAX", RepManager.REPEAT_ROOM_TAX);
            parametros.Parameters.Add("REPEAT_TOTAL_REVENUE", RepManager.REPEAT_TOTAL_REVENUE);
            parametros.Parameters.Add("REPEAT_TOTAL_TAX", RepManager.REPEAT_TOTAL_TAX);
            parametros.Parameters.Add("NOSHOW_RESERVATIONS", RepManager.NOSHOW_RESERVATIONS);
            parametros.Parameters.Add("DAY_USE_RESERVATIONS", RepManager.DAY_USE_RESERVATIONS);
            parametros.Parameters.Add("ARRIVAL_RESERVATIONS", RepManager.ARRIVAL_RESERVATIONS);
            parametros.Parameters.Add("ROOMS_CANCELLED_TODAY", RepManager.ROOMS_CANCELLED_TODAY);
            parametros.Parameters.Add("OWNER_ROOMS", RepManager.OWNER_ROOMS);
            parametros.Parameters.Add("OWNER_ROOM_REVENUE", RepManager.OWNER_ROOM_REVENUE);
            parametros.Parameters.Add("OWNER_OTHER_REVENUE", RepManager.OWNER_OTHER_REVENUE);
            parametros.Parameters.Add("OWNER_FOOD_BEV_REVENUE", RepManager.OWNER_FOOD_BEV_REVENUE);
            parametros.Parameters.Add("FF_ROOMS", RepManager.FF_ROOMS);
            parametros.Parameters.Add("FF_ROOM_REVENUE", RepManager.FF_ROOM_REVENUE);
            parametros.Parameters.Add("FF_OTHER_REVENUE", RepManager.FF_OTHER_REVENUE);
            parametros.Parameters.Add("FF_FOOD_BEV_REVENUE", RepManager.FF_FOOD_BEV_REVENUE);
            parametros.Parameters.Add("CENTRAL_CURRENCY_CODE", RepManager.CENTRAL_CURRENCY_CODE);
            parametros.Parameters.Add("CENTRAL_EXCHANGE_RATE", RepManager.CENTRAL_EXCHANGE_RATE);
            parametros.Parameters.Add("PHYSICAL_BEDS", RepManager.PHYSICAL_BEDS);
            parametros.Parameters.Add("OCC_BEDS", RepManager.OCC_BEDS);
            parametros.Parameters.Add("COMP_BEDS", RepManager.COMP_BEDS);
            parametros.Parameters.Add("HOUSE_USE_BEDS", RepManager.HOUSE_USE_BEDS);
            parametros.Parameters.Add("OOO_BEDS", RepManager.OOO_BEDS);
            parametros.Parameters.Add("OS_BEDS", RepManager.OS_BEDS);
            parametros.Parameters.Add("COMP_ADULTS", RepManager.COMP_ADULTS);
            parametros.Parameters.Add("COMP_CHILDREN", RepManager.COMP_CHILDREN);
            parametros.Parameters.Add("HOUSE_USE_ADULTS", RepManager.HOUSE_USE_ADULTS);
            parametros.Parameters.Add("HOUSE_USE_CHILDREN", RepManager.HOUSE_USE_CHILDREN);
            parametros.Parameters.Add("FIT_MEM_ROOMS", RepManager.FIT_MEM_ROOMS);
            parametros.Parameters.Add("FIT_MEM_ROOM_REVENUE", RepManager.FIT_MEM_ROOM_REVENUE);
            parametros.Parameters.Add("FIT_MEM_ROOM_REVENUE_TAX", RepManager.FIT_MEM_ROOM_REVENUE_TAX);
            parametros.Parameters.Add("FIT_MEM_TOTAL_REVENUE", RepManager.FIT_MEM_TOTAL_REVENUE);
            parametros.Parameters.Add("FIT_MEM_TOTAL_REVENUE_TAX", RepManager.FIT_MEM_TOTAL_REVENUE_TAX);
            parametros.Parameters.Add("FIT_MEM_LOS_NIGHTS", RepManager.FIT_MEM_LOS_NIGHTS);
            parametros.Parameters.Add("FIT_MEM_LOS_RESV", RepManager.FIT_MEM_LOS_RESV);
            parametros.Parameters.Add("BLK_MEM_ROOMS", RepManager.BLK_MEM_ROOMS);
            parametros.Parameters.Add("BLK_MEM_ROOM_REVENUE", RepManager.BLK_MEM_ROOM_REVENUE);
            parametros.Parameters.Add("BLK_MEM_ROOM_REVENUE_TAX", RepManager.BLK_MEM_ROOM_REVENUE_TAX);
            parametros.Parameters.Add("BLK_MEM_TOTAL_REVENUE", RepManager.BLK_MEM_TOTAL_REVENUE);
            parametros.Parameters.Add("BLK_MEM_TOTAL_REVENUE_TAX", RepManager.BLK_MEM_TOTAL_REVENUE_TAX);
            parametros.Parameters.Add("BLK_MEM_LOS_NIGHTS", RepManager.BLK_MEM_LOS_NIGHTS);
            parametros.Parameters.Add("BLK_MEM_LOS_RESV", RepManager.BLK_MEM_LOS_RESV);
            parametros.Parameters.Add("ADV_FOOD_REVENUE", RepManager.ADV_FOOD_REVENUE);
            parametros.Parameters.Add("ADV_NON_REVENUE", RepManager.ADV_NON_REVENUE);
            parametros.Parameters.Add("ADV_OTHER_REVENUE", RepManager.ADV_OTHER_REVENUE);
            parametros.Parameters.Add("ADV_ROOM_REVENUE", RepManager.ADV_ROOM_REVENUE);
            parametros.Parameters.Add("ADV_TOTAL_FOOD_TAX", RepManager.ADV_TOTAL_FOOD_TAX);
            parametros.Parameters.Add("ADV_TOTAL_NON_REVENUE_TAX", RepManager.ADV_TOTAL_NON_REVENUE_TAX);
            parametros.Parameters.Add("ADV_TOTAL_OTHER_TAX", RepManager.ADV_TOTAL_OTHER_TAX);
            parametros.Parameters.Add("ADV_TOTAL_REVENUE", RepManager.ADV_TOTAL_REVENUE);
            parametros.Parameters.Add("ADV_TOTAL_ROOM_TAX", RepManager.ADV_TOTAL_ROOM_TAX);
            parametros.Parameters.Add("ADV_TOTAL_TAX", RepManager.ADV_TOTAL_TAX);
            parametros.Parameters.Add("FF_RENT_FOOD_BEV_REV", RepManager.FF_RENT_FOOD_BEV_REV);
            parametros.Parameters.Add("FF_RENT_OTHER_REV", RepManager.FF_RENT_OTHER_REV);
            parametros.Parameters.Add("FF_RENT_ROOMS", RepManager.FF_RENT_ROOMS);
            parametros.Parameters.Add("FF_RENT_ROOM_REV", RepManager.FF_RENT_ROOM_REV);
            parametros.Parameters.Add("OWNER_RENT_FOOD_BEV_REV", RepManager.OWNER_RENT_FOOD_BEV_REV);
            parametros.Parameters.Add("OWNER_RENT_OTHER_REV", RepManager.OWNER_RENT_OTHER_REV);
            parametros.Parameters.Add("OWNER_RENT_ROOMS", RepManager.OWNER_RENT_ROOMS);
            parametros.Parameters.Add("OWNER_RENT_ROOMS_OOO", RepManager.OWNER_RENT_ROOMS_OOO);
            parametros.Parameters.Add("OWNER_RENT_ROOM_REV", RepManager.OWNER_RENT_ROOM_REV);
            parametros.Parameters.Add("OWNER_ROOMS_OOO", RepManager.OWNER_ROOMS_OOO);
            parametros.Parameters.Add("FLGD_ROOM_REVENUE", RepManager.FLGD_ROOM_REVENUE);
            parametros.Parameters.Add("FLGD_FOOD_REVENUE", RepManager.FLGD_FOOD_REVENUE);
            parametros.Parameters.Add("FLGD_NON_REVENUE", RepManager.FLGD_NON_REVENUE);
            parametros.Parameters.Add("FLGD_OTHER_REVENUE", RepManager.FLGD_OTHER_REVENUE);
            parametros.Parameters.Add("FLGD_TOTAL_ROOM_TAX", RepManager.FLGD_TOTAL_ROOM_TAX);
            parametros.Parameters.Add("FLGD_TOTAL_FOOD_TAX", RepManager.FLGD_TOTAL_FOOD_TAX);
            parametros.Parameters.Add("FLGD_TOTAL_NON_REVENUE_TAX", RepManager.FLGD_TOTAL_NON_REVENUE_TAX);
            parametros.Parameters.Add("FLGD_TOTAL_OTHER_TAX", RepManager.FLGD_TOTAL_OTHER_TAX);
            parametros.Parameters.Add("FLGD_TOTAL_REVENUE", RepManager.FLGD_TOTAL_REVENUE);
            parametros.Parameters.Add("FLGD_TOTAL_TAX", RepManager.FLGD_TOTAL_TAX);
            parametros.Parameters.Add("FLGD_PAYMENT", RepManager.FLGD_PAYMENT);
            parametros.Parameters.Add("DAYUSE_REST_OF_YEAR", RepManager.DAYUSE_REST_OF_YEAR);
            parametros.Parameters.Add("DAYUSE_REST_OF_MONTH", RepManager.DAYUSE_REST_OF_MONTH);
            parametros.Parameters.Add("DAYUSE_NEXT_365_DAYS", RepManager.DAYUSE_NEXT_365_DAYS);
            parametros.Parameters.Add("DAYUSE_NEXT_31_DAYS", RepManager.DAYUSE_NEXT_31_DAYS);
            parametros.Parameters.Add("DAYUSE_YEAR", RepManager.DAYUSE_YEAR);
            parametros.Parameters.Add("DAYUSE_MONTH", RepManager.DAYUSE_MONTH);
            parametros.Parameters.Add("DAYUSE_WEEK", RepManager.DAYUSE_WEEK);
            parametros.Parameters.Add("OWNER_ROOMS_IN_HOTEL", RepManager.OWNER_ROOMS_IN_HOTEL);
            parametros.Parameters.Add("EXT_NOSHOW_CRS_RES", RepManager.EXT_NOSHOW_CRS_RES);
            parametros.Parameters.Add("DAYUSE_TOMORROW", RepManager.DAYUSE_TOMORROW);
            parametros.Parameters.Add("COMP_REST_OF_YEAR", RepManager.COMP_REST_OF_YEAR);
            parametros.Parameters.Add("COMP_REST_OF_MONTH", RepManager.COMP_REST_OF_MONTH);
            parametros.Parameters.Add("COMP_NEXT_365_DAYS", RepManager.COMP_NEXT_365_DAYS);
            parametros.Parameters.Add("COMP_NEXT_31_DAYS", RepManager.COMP_NEXT_31_DAYS);
            parametros.Parameters.Add("COMP_WEEK", RepManager.COMP_WEEK);
            parametros.Parameters.Add("COMP_TOMORROW", RepManager.COMP_TOMORROW);
            parametros.Parameters.Add("HOUSE_USE_REST_OF_YEAR", RepManager.HOUSE_USE_REST_OF_YEAR);
            parametros.Parameters.Add("HOUSE_USE_REST_OF_MONTH", RepManager.HOUSE_USE_REST_OF_MONTH);
            parametros.Parameters.Add("HOUSE_USE_NEXT_365_DAYS", RepManager.HOUSE_USE_NEXT_365_DAYS);
            parametros.Parameters.Add("HOUSE_USE_NEXT_31_DAYS", RepManager.HOUSE_USE_NEXT_31_DAYS);
            parametros.Parameters.Add("HOUSE_USE_WEEK", RepManager.HOUSE_USE_WEEK);
            parametros.Parameters.Add("HOUSE_USE_TOMORROW", RepManager.HOUSE_USE_TOMORROW);
            parametros.Parameters.Add("OOO_ROOMS_REST_OF_YEAR", RepManager.OOO_ROOMS_REST_OF_YEAR);
            parametros.Parameters.Add("OOO_ROOMS_REST_OF_MONTH", RepManager.OOO_ROOMS_REST_OF_MONTH);
            parametros.Parameters.Add("OOO_ROOMS_NEXT_365_DAYS", RepManager.OOO_ROOMS_NEXT_365_DAYS);
            parametros.Parameters.Add("OOO_ROOMS_NEXT_31_DAYS", RepManager.OOO_ROOMS_NEXT_31_DAYS);
            parametros.Parameters.Add("OOO_ROOMS_WEEK", RepManager.OOO_ROOMS_WEEK);
            parametros.Parameters.Add("OOO_ROOMS_TOMORROW", RepManager.OOO_ROOMS_TOMORROW);
            parametros.Parameters.Add("OS_ROOMS_REST_OF_YEAR", RepManager.OS_ROOMS_REST_OF_YEAR);
            parametros.Parameters.Add("OS_ROOMS_REST_OF_MONTH", RepManager.OS_ROOMS_REST_OF_MONTH);
            parametros.Parameters.Add("OS_ROOMS_NEXT_365_DAYS", RepManager.OS_ROOMS_NEXT_365_DAYS);
            parametros.Parameters.Add("OS_ROOMS_NEXT_31_DAYS", RepManager.OS_ROOMS_NEXT_31_DAYS);
            parametros.Parameters.Add("OS_ROOMS_WEEK", RepManager.OS_ROOMS_WEEK);
            parametros.Parameters.Add("OS_ROOMS_TOMORROW", RepManager.OS_ROOMS_TOMORROW);
            parametros.Parameters.Add("DAYUSE_ADULTS", RepManager.DAYUSE_ADULTS);
            parametros.Parameters.Add("DAYUSE_CHILDREN", RepManager.DAYUSE_CHILDREN);
            parametros.Parameters.Add("ES_OCC_ROOMS", RepManager.ES_OCC_ROOMS);
            parametros.Parameters.Add("ES_COMP_ROOMS", RepManager.ES_COMP_ROOMS);
            parametros.Parameters.Add("ES_HOUSE_USE_ROOMS", RepManager.ES_HOUSE_USE_ROOMS);
            parametros.Parameters.Add("ES_TOTAL_REVENUE", RepManager.ES_TOTAL_REVENUE);
            parametros.Parameters.Add("ES_ROOM_REVENUE", RepManager.ES_ROOM_REVENUE);
            parametros.Parameters.Add("ES_FOOD_REVENUE", RepManager.ES_FOOD_REVENUE);
            parametros.Parameters.Add("ES_OTHER_REVENUE", RepManager.ES_OTHER_REVENUE);
            parametros.Parameters.Add("ES_NON_REVENUE", RepManager.ES_NON_REVENUE);
            parametros.Parameters.Add("ES_TOTAL_TAX", RepManager.ES_TOTAL_TAX);
            parametros.Parameters.Add("ES_ROOM_TAX", RepManager.ES_ROOM_TAX);
            parametros.Parameters.Add("ES_FOOD_TAX", RepManager.ES_FOOD_TAX);
            parametros.Parameters.Add("ES_OTHER_TAX", RepManager.ES_OTHER_TAX);
            parametros.Parameters.Add("ES_NON_REVENUE_TAX", RepManager.ES_NON_REVENUE_TAX);
            parametros.Parameters.Add("ES_ADV_TOTAL_REVENUE", RepManager.ES_ADV_TOTAL_REVENUE);
            parametros.Parameters.Add("ES_ADV_ROOM_REVENUE", RepManager.ES_ADV_ROOM_REVENUE);
            parametros.Parameters.Add("ES_ADV_FOOD_REVENUE", RepManager.ES_ADV_FOOD_REVENUE);
            parametros.Parameters.Add("ES_ADV_OTHER_REVENUE", RepManager.ES_ADV_OTHER_REVENUE);
            parametros.Parameters.Add("ES_ADV_NON_REVENUE", RepManager.ES_ADV_NON_REVENUE);
            parametros.Parameters.Add("ES_ADV_TOTAL_TAX", RepManager.ES_ADV_TOTAL_TAX);
            parametros.Parameters.Add("ES_ADV_ROOM_TAX", RepManager.ES_ADV_ROOM_TAX);
            parametros.Parameters.Add("ES_ADV_FOOD_TAX", RepManager.ES_ADV_FOOD_TAX);
            parametros.Parameters.Add("ES_ADV_OTHER_TAX", RepManager.ES_ADV_OTHER_TAX);
            parametros.Parameters.Add("ES_ADV_NON_REVENUE_TAX", RepManager.ES_ADV_NON_REVENUE_TAX);
            parametros.Parameters.Add("RNA_INSERTDATE", RepManager.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", RepManager.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", RepManager.DELETED_FLAG);

            return parametros;
        }

    }
}

