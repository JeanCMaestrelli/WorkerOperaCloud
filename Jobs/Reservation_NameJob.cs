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
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Reservation_NameJob : IServiceJobs
    {
        private readonly ILogger<Reservation_NameJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Reservation_NameJob(ILogger<Reservation_NameJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Reservation_Name>(filePath, LoggBuilder);

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
                                        $"RESV_NAME_ID = '{trial.RESV_NAME_ID}' and " +
                                        $"NAME_ID = '{trial.NAME_ID}' and " +
                                        $"NAME_USAGE_TYPE = '{trial.NAME_USAGE_TYPE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Reservation_Name>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Reservation_Name Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT.ToString());
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID.ToString());
            parametros.Parameters.Add("NAME_ID", Trxs_Codes.NAME_ID.ToString());
            parametros.Parameters.Add("NAME_USAGE_TYPE", Trxs_Codes.NAME_USAGE_TYPE.ToString());
            parametros.Parameters.Add("CONTACT_NAME_ID", Trxs_Codes.CONTACT_NAME_ID.ToString());
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER.ToString());
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("RESV_STATUS", Trxs_Codes.RESV_STATUS.ToString());
            parametros.Parameters.Add("COMMISSION_CODE", Trxs_Codes.COMMISSION_CODE.ToString());
            parametros.Parameters.Add("ADDRESS_ID", Trxs_Codes.ADDRESS_ID.ToString());
            parametros.Parameters.Add("PHONE_ID", Trxs_Codes.PHONE_ID.ToString());
            parametros.Parameters.Add("FAX_YN", Trxs_Codes.FAX_YN.ToString());
            parametros.Parameters.Add("MAIL_YN", Trxs_Codes.MAIL_YN.ToString());
            parametros.Parameters.Add("PRINT_RATE_YN", Trxs_Codes.PRINT_RATE_YN.ToString());
            parametros.Parameters.Add("REPORT_ID", Trxs_Codes.REPORT_ID.ToString());
            parametros.Parameters.Add("RESV_NO", Trxs_Codes.RESV_NO.ToString());
            parametros.Parameters.Add("CONFIRMATION_NO", Trxs_Codes.CONFIRMATION_NO.ToString());
            parametros.Parameters.Add("BEGIN_DATE", Trxs_Codes.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", Trxs_Codes.END_DATE.ToString());
            parametros.Parameters.Add("FAX_ID", Trxs_Codes.FAX_ID.ToString());
            parametros.Parameters.Add("EMAIL_ID", Trxs_Codes.EMAIL_ID.ToString());
            parametros.Parameters.Add("EMAIL_YN", Trxs_Codes.EMAIL_YN.ToString());
            parametros.Parameters.Add("CONSUMER_YN", Trxs_Codes.CONSUMER_YN.ToString());
            parametros.Parameters.Add("CREDIT_CARD_ID", Trxs_Codes.CREDIT_CARD_ID.ToString());
            parametros.Parameters.Add("FINANCIALLY_RESPONSIBLE_YN", Trxs_Codes.FINANCIALLY_RESPONSIBLE_YN.ToString());
            parametros.Parameters.Add("PAYMENT_METHOD", Trxs_Codes.PAYMENT_METHOD.ToString());
            parametros.Parameters.Add("INTERMEDIARY_YN", Trxs_Codes.INTERMEDIARY_YN.ToString());
            parametros.Parameters.Add("POSTING_ALLOWED_YN", Trxs_Codes.POSTING_ALLOWED_YN.ToString());
            parametros.Parameters.Add("DISPLAY_COLOR", Trxs_Codes.DISPLAY_COLOR.ToString());
            parametros.Parameters.Add("ACTUAL_CHECK_IN_DATE", Trxs_Codes.ACTUAL_CHECK_IN_DATE.ToString());
            parametros.Parameters.Add("TRUNC_ACTUAL_CHECK_IN_DATE", Trxs_Codes.TRUNC_ACTUAL_CHECK_IN_DATE.ToString());
            parametros.Parameters.Add("ACTUAL_CHECK_OUT_DATE", Trxs_Codes.ACTUAL_CHECK_OUT_DATE.ToString());
            parametros.Parameters.Add("TRUNC_ACTUAL_CHECK_OUT_DATE", Trxs_Codes.TRUNC_ACTUAL_CHECK_OUT_DATE.ToString());
            parametros.Parameters.Add("CREDIT_LIMIT", Trxs_Codes.CREDIT_LIMIT.ToString());
            parametros.Parameters.Add("AUTHORIZED_BY", Trxs_Codes.AUTHORIZED_BY.ToString());
            parametros.Parameters.Add("PARENT_RESV_NAME_ID", Trxs_Codes.PARENT_RESV_NAME_ID.ToString());
            parametros.Parameters.Add("CANCELLATION_NO", Trxs_Codes.CANCELLATION_NO.ToString());
            parametros.Parameters.Add("CANCELLATION_REASON_CODE", Trxs_Codes.CANCELLATION_REASON_CODE.ToString());
            parametros.Parameters.Add("CANCELLATION_REASON_DESC", Trxs_Codes.CANCELLATION_REASON_DESC.ToString());
            parametros.Parameters.Add("ARRIVAL_TRANSPORT_TYPE", Trxs_Codes.ARRIVAL_TRANSPORT_TYPE.ToString());
            parametros.Parameters.Add("ARRIVAL_STATION_CODE", Trxs_Codes.ARRIVAL_STATION_CODE.ToString());
            parametros.Parameters.Add("ARRIVAL_CARRIER_CODE", Trxs_Codes.ARRIVAL_CARRIER_CODE.ToString());
            parametros.Parameters.Add("ARRIVAL_TRANSPORT_CODE", Trxs_Codes.ARRIVAL_TRANSPORT_CODE.ToString());
            parametros.Parameters.Add("ARRIVAL_DATE_TIME", Trxs_Codes.ARRIVAL_DATE_TIME.ToString());
            parametros.Parameters.Add("ARRIVAL_ESTIMATE_TIME", Trxs_Codes.ARRIVAL_ESTIMATE_TIME.ToString());
            parametros.Parameters.Add("ARRIVAL_TRANPORTATION_YN", Trxs_Codes.ARRIVAL_TRANPORTATION_YN.ToString());
            parametros.Parameters.Add("ARRIVAL_COMMENTS", Trxs_Codes.ARRIVAL_COMMENTS.ToString());
            parametros.Parameters.Add("DEPARTURE_TRANSPORT_TYPE", Trxs_Codes.DEPARTURE_TRANSPORT_TYPE.ToString());
            parametros.Parameters.Add("DEPARTURE_STATION_CODE", Trxs_Codes.DEPARTURE_STATION_CODE.ToString());
            parametros.Parameters.Add("DEPARTURE_CARRIER_CODE", Trxs_Codes.DEPARTURE_CARRIER_CODE.ToString());
            parametros.Parameters.Add("DEPARTURE_TRANSPORT_CODE", Trxs_Codes.DEPARTURE_TRANSPORT_CODE.ToString());
            parametros.Parameters.Add("DEPARTURE_DATE_TIME", Trxs_Codes.DEPARTURE_DATE_TIME.ToString());
            parametros.Parameters.Add("DEPARTURE_ESTIMATE_TIME", Trxs_Codes.DEPARTURE_ESTIMATE_TIME.ToString());
            parametros.Parameters.Add("DEPARTURE_TRANSPORTATION_YN", Trxs_Codes.DEPARTURE_TRANSPORTATION_YN.ToString());
            parametros.Parameters.Add("DEPARTURE_COMMENTS", Trxs_Codes.DEPARTURE_COMMENTS.ToString());
            parametros.Parameters.Add("CANCELLATION_DATE", Trxs_Codes.CANCELLATION_DATE.ToString());
            parametros.Parameters.Add("GUARANTEE_CODE", Trxs_Codes.GUARANTEE_CODE.ToString());
            parametros.Parameters.Add("WL_REASON_DESCRIPTION", Trxs_Codes.WL_REASON_DESCRIPTION.ToString());
            parametros.Parameters.Add("WL_REASON_CODE", Trxs_Codes.WL_REASON_CODE.ToString());
            parametros.Parameters.Add("WL_PRIORITY", Trxs_Codes.WL_PRIORITY.ToString());
            parametros.Parameters.Add("DO_NOT_MOVE_ROOM", Trxs_Codes.DO_NOT_MOVE_ROOM.ToString());
            parametros.Parameters.Add("EXTERNAL_REFERENCE", Trxs_Codes.EXTERNAL_REFERENCE.ToString());
            parametros.Parameters.Add("PARTY_CODE", Trxs_Codes.PARTY_CODE.ToString());
            parametros.Parameters.Add("WALKIN_YN", Trxs_Codes.WALKIN_YN.ToString());
            parametros.Parameters.Add("ORIGINAL_END_DATE", Trxs_Codes.ORIGINAL_END_DATE.ToString());
            parametros.Parameters.Add("APPROVAL_AMOUNT_CALC_METHOD", Trxs_Codes.APPROVAL_AMOUNT_CALC_METHOD.ToString());
            parametros.Parameters.Add("AMOUNT_PERCENT", Trxs_Codes.AMOUNT_PERCENT.ToString());
            parametros.Parameters.Add("NAME_TAX_TYPE", Trxs_Codes.NAME_TAX_TYPE.ToString());
            parametros.Parameters.Add("TAX_EXEMPT_NO", Trxs_Codes.TAX_EXEMPT_NO.ToString());
            parametros.Parameters.Add("ROOM_FEATURES", Trxs_Codes.ROOM_FEATURES.ToString());
            parametros.Parameters.Add("WL_TELEPHONE_NO", Trxs_Codes.WL_TELEPHONE_NO.ToString());
            parametros.Parameters.Add("VIDEO_CHECKOUT_YN", Trxs_Codes.VIDEO_CHECKOUT_YN.ToString());
            parametros.Parameters.Add("DISCOUNT_AMT", Trxs_Codes.DISCOUNT_AMT.ToString());
            parametros.Parameters.Add("DISCOUNT_PRCNT", Trxs_Codes.DISCOUNT_PRCNT.ToString());
            parametros.Parameters.Add("DISCOUNT_REASON_CODE", Trxs_Codes.DISCOUNT_REASON_CODE.ToString());
            parametros.Parameters.Add("COMMISSION_PAID", Trxs_Codes.COMMISSION_PAID.ToString());
            parametros.Parameters.Add("COMMISSION_HOLD_CODE", Trxs_Codes.COMMISSION_HOLD_CODE.ToString());
            parametros.Parameters.Add("TRUNC_BEGIN_DATE", Trxs_Codes.TRUNC_BEGIN_DATE.ToString());
            parametros.Parameters.Add("TRUNC_END_DATE", Trxs_Codes.TRUNC_END_DATE.ToString());
            parametros.Parameters.Add("SGUEST_NAME", Trxs_Codes.SGUEST_NAME.ToString());
            parametros.Parameters.Add("MEMBERSHIP_ID", Trxs_Codes.MEMBERSHIP_ID.ToString());
            parametros.Parameters.Add("UDFC01", Trxs_Codes.UDFC01.ToString());
            parametros.Parameters.Add("UDFC02", Trxs_Codes.UDFC02.ToString());
            parametros.Parameters.Add("UDFC03", Trxs_Codes.UDFC03.ToString());
            parametros.Parameters.Add("UDFC04", Trxs_Codes.UDFC04.ToString());
            parametros.Parameters.Add("UDFC05", Trxs_Codes.UDFC05.ToString());
            parametros.Parameters.Add("UDFC06", Trxs_Codes.UDFC06.ToString());
            parametros.Parameters.Add("UDFC07", Trxs_Codes.UDFC07.ToString());
            parametros.Parameters.Add("UDFC08", Trxs_Codes.UDFC08.ToString());
            parametros.Parameters.Add("UDFC09", Trxs_Codes.UDFC09.ToString());
            parametros.Parameters.Add("UDFC10", Trxs_Codes.UDFC10.ToString());
            parametros.Parameters.Add("UDFC11", Trxs_Codes.UDFC11.ToString());
            parametros.Parameters.Add("UDFC12", Trxs_Codes.UDFC12.ToString());
            parametros.Parameters.Add("UDFC13", Trxs_Codes.UDFC13.ToString());
            parametros.Parameters.Add("UDFC14", Trxs_Codes.UDFC14.ToString());
            parametros.Parameters.Add("UDFC15", Trxs_Codes.UDFC15.ToString());
            parametros.Parameters.Add("UDFC16", Trxs_Codes.UDFC16.ToString());
            parametros.Parameters.Add("UDFC17", Trxs_Codes.UDFC17.ToString());
            parametros.Parameters.Add("UDFC18", Trxs_Codes.UDFC18.ToString());
            parametros.Parameters.Add("UDFC19", Trxs_Codes.UDFC19.ToString());
            parametros.Parameters.Add("UDFC20", Trxs_Codes.UDFC20.ToString());
            parametros.Parameters.Add("UDFC21", Trxs_Codes.UDFC21.ToString());
            parametros.Parameters.Add("UDFC22", Trxs_Codes.UDFC22.ToString());
            parametros.Parameters.Add("UDFC23", Trxs_Codes.UDFC23.ToString());
            parametros.Parameters.Add("UDFC24", Trxs_Codes.UDFC24.ToString());
            parametros.Parameters.Add("UDFC25", Trxs_Codes.UDFC25.ToString());
            parametros.Parameters.Add("UDFC26", Trxs_Codes.UDFC26.ToString());
            parametros.Parameters.Add("UDFC27", Trxs_Codes.UDFC27.ToString());
            parametros.Parameters.Add("UDFC28", Trxs_Codes.UDFC28.ToString());
            parametros.Parameters.Add("UDFC29", Trxs_Codes.UDFC29.ToString());
            parametros.Parameters.Add("UDFC30", Trxs_Codes.UDFC30.ToString());
            parametros.Parameters.Add("UDFC31", Trxs_Codes.UDFC31.ToString());
            parametros.Parameters.Add("UDFC32", Trxs_Codes.UDFC32.ToString());
            parametros.Parameters.Add("UDFC33", Trxs_Codes.UDFC33.ToString());
            parametros.Parameters.Add("UDFC34", Trxs_Codes.UDFC34.ToString());
            parametros.Parameters.Add("UDFC35", Trxs_Codes.UDFC35.ToString());
            parametros.Parameters.Add("UDFC36", Trxs_Codes.UDFC36.ToString());
            parametros.Parameters.Add("UDFC37", Trxs_Codes.UDFC37.ToString());
            parametros.Parameters.Add("UDFC38", Trxs_Codes.UDFC38.ToString());
            parametros.Parameters.Add("UDFC39", Trxs_Codes.UDFC39.ToString());
            parametros.Parameters.Add("UDFC40", Trxs_Codes.UDFC40.ToString());
            parametros.Parameters.Add("UDFN01", Trxs_Codes.UDFN01.ToString());
            parametros.Parameters.Add("UDFN02", Trxs_Codes.UDFN02.ToString());
            parametros.Parameters.Add("UDFN03", Trxs_Codes.UDFN03.ToString());
            parametros.Parameters.Add("UDFN04", Trxs_Codes.UDFN04.ToString());
            parametros.Parameters.Add("UDFN05", Trxs_Codes.UDFN05.ToString());
            parametros.Parameters.Add("UDFN06", Trxs_Codes.UDFN06.ToString());
            parametros.Parameters.Add("UDFN07", Trxs_Codes.UDFN07.ToString());
            parametros.Parameters.Add("UDFN08", Trxs_Codes.UDFN08.ToString());
            parametros.Parameters.Add("UDFN09", Trxs_Codes.UDFN09.ToString());
            parametros.Parameters.Add("UDFN10", Trxs_Codes.UDFN10.ToString());
            parametros.Parameters.Add("UDFN11", Trxs_Codes.UDFN11.ToString());
            parametros.Parameters.Add("UDFN12", Trxs_Codes.UDFN12.ToString());
            parametros.Parameters.Add("UDFN13", Trxs_Codes.UDFN13.ToString());
            parametros.Parameters.Add("UDFN14", Trxs_Codes.UDFN14.ToString());
            parametros.Parameters.Add("UDFN15", Trxs_Codes.UDFN15.ToString());
            parametros.Parameters.Add("UDFN16", Trxs_Codes.UDFN16.ToString());
            parametros.Parameters.Add("UDFN17", Trxs_Codes.UDFN17.ToString());
            parametros.Parameters.Add("UDFN18", Trxs_Codes.UDFN18.ToString());
            parametros.Parameters.Add("UDFN19", Trxs_Codes.UDFN19.ToString());
            parametros.Parameters.Add("UDFN20", Trxs_Codes.UDFN20.ToString());
            parametros.Parameters.Add("UDFN21", Trxs_Codes.UDFN21.ToString());
            parametros.Parameters.Add("UDFN22", Trxs_Codes.UDFN22.ToString());
            parametros.Parameters.Add("UDFN23", Trxs_Codes.UDFN23.ToString());
            parametros.Parameters.Add("UDFN24", Trxs_Codes.UDFN24.ToString());
            parametros.Parameters.Add("UDFN25", Trxs_Codes.UDFN25.ToString());
            parametros.Parameters.Add("UDFN26", Trxs_Codes.UDFN26.ToString());
            parametros.Parameters.Add("UDFN27", Trxs_Codes.UDFN27.ToString());
            parametros.Parameters.Add("UDFN28", Trxs_Codes.UDFN28.ToString());
            parametros.Parameters.Add("UDFN29", Trxs_Codes.UDFN29.ToString());
            parametros.Parameters.Add("UDFN30", Trxs_Codes.UDFN30.ToString());
            parametros.Parameters.Add("UDFN31", Trxs_Codes.UDFN31.ToString());
            parametros.Parameters.Add("UDFN32", Trxs_Codes.UDFN32.ToString());
            parametros.Parameters.Add("UDFN33", Trxs_Codes.UDFN33.ToString());
            parametros.Parameters.Add("UDFN34", Trxs_Codes.UDFN34.ToString());
            parametros.Parameters.Add("UDFN35", Trxs_Codes.UDFN35.ToString());
            parametros.Parameters.Add("UDFN36", Trxs_Codes.UDFN36.ToString());
            parametros.Parameters.Add("UDFN37", Trxs_Codes.UDFN37.ToString());
            parametros.Parameters.Add("UDFN38", Trxs_Codes.UDFN38.ToString());
            parametros.Parameters.Add("UDFN39", Trxs_Codes.UDFN39.ToString());
            parametros.Parameters.Add("UDFN40", Trxs_Codes.UDFN40.ToString());
            parametros.Parameters.Add("UDFD01", Trxs_Codes.UDFD01.ToString());
            parametros.Parameters.Add("UDFD02", Trxs_Codes.UDFD02.ToString());
            parametros.Parameters.Add("UDFD03", Trxs_Codes.UDFD03.ToString());
            parametros.Parameters.Add("UDFD04", Trxs_Codes.UDFD04.ToString());
            parametros.Parameters.Add("UDFD05", Trxs_Codes.UDFD05.ToString());
            parametros.Parameters.Add("UDFD06", Trxs_Codes.UDFD06.ToString());
            parametros.Parameters.Add("UDFD07", Trxs_Codes.UDFD07.ToString());
            parametros.Parameters.Add("UDFD08", Trxs_Codes.UDFD08.ToString());
            parametros.Parameters.Add("UDFD09", Trxs_Codes.UDFD09.ToString());
            parametros.Parameters.Add("UDFD10", Trxs_Codes.UDFD10.ToString());
            parametros.Parameters.Add("UDFD11", Trxs_Codes.UDFD11.ToString());
            parametros.Parameters.Add("UDFD12", Trxs_Codes.UDFD12.ToString());
            parametros.Parameters.Add("UDFD13", Trxs_Codes.UDFD13.ToString());
            parametros.Parameters.Add("UDFD14", Trxs_Codes.UDFD14.ToString());
            parametros.Parameters.Add("UDFD15", Trxs_Codes.UDFD15.ToString());
            parametros.Parameters.Add("UDFD16", Trxs_Codes.UDFD16.ToString());
            parametros.Parameters.Add("UDFD17", Trxs_Codes.UDFD17.ToString());
            parametros.Parameters.Add("UDFD18", Trxs_Codes.UDFD18.ToString());
            parametros.Parameters.Add("UDFD19", Trxs_Codes.UDFD19.ToString());
            parametros.Parameters.Add("UDFD20", Trxs_Codes.UDFD20.ToString());
            parametros.Parameters.Add("INSERT_ACTION_INSTANCE_ID", Trxs_Codes.INSERT_ACTION_INSTANCE_ID.ToString());
            parametros.Parameters.Add("DML_SEQ_NO", Trxs_Codes.DML_SEQ_NO.ToString());
            parametros.Parameters.Add("BUSINESS_DATE_CREATED", Trxs_Codes.BUSINESS_DATE_CREATED.ToString());
            parametros.Parameters.Add("TURNDOWN_YN", Trxs_Codes.TURNDOWN_YN.ToString());
            parametros.Parameters.Add("ROOM_INSTRUCTIONS", Trxs_Codes.ROOM_INSTRUCTIONS.ToString());
            parametros.Parameters.Add("ROOM_SERVICE_TIME", Trxs_Codes.ROOM_SERVICE_TIME.ToString());
            parametros.Parameters.Add("EVENT_ID", Trxs_Codes.EVENT_ID.ToString());
            parametros.Parameters.Add("REVENUE_TYPE_CODE", Trxs_Codes.REVENUE_TYPE_CODE.ToString());
            parametros.Parameters.Add("HURDLE", Trxs_Codes.HURDLE.ToString());
            parametros.Parameters.Add("HURDLE_OVERRIDE", Trxs_Codes.HURDLE_OVERRIDE.ToString());
            parametros.Parameters.Add("RATEABLE_VALUE", Trxs_Codes.RATEABLE_VALUE.ToString());
            parametros.Parameters.Add("RESTRICTION_OVERRIDE", Trxs_Codes.RESTRICTION_OVERRIDE.ToString());
            parametros.Parameters.Add("YIELDABLE_YN", Trxs_Codes.YIELDABLE_YN.ToString());
            parametros.Parameters.Add("SGUEST_FIRSTNAME", Trxs_Codes.SGUEST_FIRSTNAME.ToString());
            parametros.Parameters.Add("GUEST_LAST_NAME", Trxs_Codes.GUEST_LAST_NAME.ToString());
            parametros.Parameters.Add("GUEST_FIRST_NAME", Trxs_Codes.GUEST_FIRST_NAME.ToString());
            parametros.Parameters.Add("GUEST_LAST_NAME_SDX", Trxs_Codes.GUEST_LAST_NAME_SDX.ToString());
            parametros.Parameters.Add("GUEST_FIRST_NAME_SDX", Trxs_Codes.GUEST_FIRST_NAME_SDX.ToString());
            parametros.Parameters.Add("CHANNEL", Trxs_Codes.CHANNEL.ToString());
            parametros.Parameters.Add("SHARE_SEQ_NO", Trxs_Codes.SHARE_SEQ_NO.ToString());
            parametros.Parameters.Add("GUEST_SIGNATURE", Trxs_Codes.GUEST_SIGNATURE.ToString());
            parametros.Parameters.Add("EXTENSION_ID", Trxs_Codes.EXTENSION_ID.ToString());
            parametros.Parameters.Add("RESV_CONTACT_ID", Trxs_Codes.RESV_CONTACT_ID.ToString());
            parametros.Parameters.Add("BILLING_CONTACT_ID", Trxs_Codes.BILLING_CONTACT_ID.ToString());
            parametros.Parameters.Add("RES_INSERT_SOURCE", Trxs_Codes.RES_INSERT_SOURCE.ToString());
            parametros.Parameters.Add("RES_INSERT_SOURCE_TYPE", Trxs_Codes.RES_INSERT_SOURCE_TYPE.ToString());
            parametros.Parameters.Add("MASTER_SHARE", Trxs_Codes.MASTER_SHARE.ToString());
            parametros.Parameters.Add("REGISTRATION_CARD_NO", Trxs_Codes.REGISTRATION_CARD_NO.ToString());
            parametros.Parameters.Add("TIAD", Trxs_Codes.TIAD.ToString());
            parametros.Parameters.Add("PURPOSE_OF_STAY", Trxs_Codes.PURPOSE_OF_STAY.ToString());
            parametros.Parameters.Add("REINSTATE_DATE", Trxs_Codes.REINSTATE_DATE.ToString());
            parametros.Parameters.Add("PURGE_DATE", Trxs_Codes.PURGE_DATE.ToString());
            parametros.Parameters.Add("LAST_SETTLE_DATE", Trxs_Codes.LAST_SETTLE_DATE.ToString());
            parametros.Parameters.Add("LAST_PERIODIC_FOLIO_DATE", Trxs_Codes.LAST_PERIODIC_FOLIO_DATE.ToString());
            parametros.Parameters.Add("PERIODIC_FOLIO_FREQ", Trxs_Codes.PERIODIC_FOLIO_FREQ.ToString());
            parametros.Parameters.Add("CONFIRMATION_LEG_NO", Trxs_Codes.CONFIRMATION_LEG_NO.ToString());
            parametros.Parameters.Add("GUEST_STATUS", Trxs_Codes.GUEST_STATUS.ToString());
            parametros.Parameters.Add("GUEST_TYPE", Trxs_Codes.GUEST_TYPE.ToString());
            parametros.Parameters.Add("CHECKIN_DURATION", Trxs_Codes.CHECKIN_DURATION.ToString());
            parametros.Parameters.Add("AUTHORIZER_ID", Trxs_Codes.AUTHORIZER_ID.ToString());
            parametros.Parameters.Add("LAST_ONLINE_PRINT_SEQ", Trxs_Codes.LAST_ONLINE_PRINT_SEQ.ToString());
            parametros.Parameters.Add("ENTRY_POINT", Trxs_Codes.ENTRY_POINT.ToString());
            parametros.Parameters.Add("ENTRY_DATE", Trxs_Codes.ENTRY_DATE.ToString());
            parametros.Parameters.Add("FOLIO_TEXT1", Trxs_Codes.FOLIO_TEXT1.ToString());
            parametros.Parameters.Add("FOLIO_TEXT2", Trxs_Codes.FOLIO_TEXT2.ToString());
            parametros.Parameters.Add("PSEUDO_MEM_TYPE", Trxs_Codes.PSEUDO_MEM_TYPE.ToString());
            parametros.Parameters.Add("PSEUDO_MEM_TOTAL_POINTS", Trxs_Codes.PSEUDO_MEM_TOTAL_POINTS.ToString());
            parametros.Parameters.Add("COMP_TYPE_CODE", Trxs_Codes.COMP_TYPE_CODE.ToString());
            parametros.Parameters.Add("UNI_CARD_ID", Trxs_Codes.UNI_CARD_ID.ToString());
            parametros.Parameters.Add("EXP_CHECKINRES_ID", Trxs_Codes.EXP_CHECKINRES_ID.ToString());
            parametros.Parameters.Add("ORIGINAL_BEGIN_DATE", Trxs_Codes.ORIGINAL_BEGIN_DATE.ToString());
            parametros.Parameters.Add("OWNER_FF_FLAG", Trxs_Codes.OWNER_FF_FLAG.ToString());
            parametros.Parameters.Add("COMMISSION_PAYOUT_TO", Trxs_Codes.COMMISSION_PAYOUT_TO.ToString());
            parametros.Parameters.Add("PRE_CHARGING_YN", Trxs_Codes.PRE_CHARGING_YN.ToString());
            parametros.Parameters.Add("POST_CHARGING_YN", Trxs_Codes.POST_CHARGING_YN.ToString());
            parametros.Parameters.Add("POST_CO_FLAG", Trxs_Codes.POST_CO_FLAG.ToString());
            parametros.Parameters.Add("FOLIO_CLOSE_DATE", Trxs_Codes.FOLIO_CLOSE_DATE.ToString());
            parametros.Parameters.Add("SCHEDULE_CHECKOUT_YN", Trxs_Codes.SCHEDULE_CHECKOUT_YN.ToString());
            parametros.Parameters.Add("CUSTOM_REFERENCE", Trxs_Codes.CUSTOM_REFERENCE.ToString());
            parametros.Parameters.Add("GUARANTEE_CODE_PRE_CI", Trxs_Codes.GUARANTEE_CODE_PRE_CI.ToString());
            parametros.Parameters.Add("AWARD_MEMBERSHIP_ID", Trxs_Codes.AWARD_MEMBERSHIP_ID.ToString());
            parametros.Parameters.Add("ESIGNED_REG_CARD_NAME", Trxs_Codes.ESIGNED_REG_CARD_NAME.ToString());
            parametros.Parameters.Add("STATISTICAL_ROOM_TYPE", Trxs_Codes.STATISTICAL_ROOM_TYPE.ToString());
            parametros.Parameters.Add("STATISTICAL_RATE_TIER", Trxs_Codes.STATISTICAL_RATE_TIER.ToString());
            parametros.Parameters.Add("YM_CODE", Trxs_Codes.YM_CODE.ToString());
            parametros.Parameters.Add("KEY_VALID_UNTIL", Trxs_Codes.KEY_VALID_UNTIL.ToString());
            parametros.Parameters.Add("PRE_REGISTERED_YN", Trxs_Codes.PRE_REGISTERED_YN.ToString());
            parametros.Parameters.Add("TAX_REGISTRATION_NO", Trxs_Codes.TAX_REGISTRATION_NO.ToString());
            parametros.Parameters.Add("VISA_NUMBER", Trxs_Codes.VISA_NUMBER.ToString());
            parametros.Parameters.Add("VISA_ISSUE_DATE", Trxs_Codes.VISA_ISSUE_DATE.ToString());
            parametros.Parameters.Add("VISA_EXPIRATION_DATE", Trxs_Codes.VISA_EXPIRATION_DATE.ToString());
            parametros.Parameters.Add("TAX_NO_OF_STAYS", Trxs_Codes.TAX_NO_OF_STAYS.ToString());
            parametros.Parameters.Add("ASB_PRORATED_YN", Trxs_Codes.ASB_PRORATED_YN.ToString());
            parametros.Parameters.Add("AUTO_SETTLE_DAYS", Trxs_Codes.AUTO_SETTLE_DAYS.ToString());
            parametros.Parameters.Add("AUTO_SETTLE_YN", Trxs_Codes.AUTO_SETTLE_YN.ToString());
            parametros.Parameters.Add("SPLIT_FROM_RESV_NAME_ID", Trxs_Codes.SPLIT_FROM_RESV_NAME_ID.ToString());
            parametros.Parameters.Add("NEXT_DESTINATION", Trxs_Codes.NEXT_DESTINATION.ToString());
            parametros.Parameters.Add("DATE_OF_ARRIVAL_IN_COUNTRY", Trxs_Codes.DATE_OF_ARRIVAL_IN_COUNTRY.ToString());
            parametros.Parameters.Add("PRE_ARR_REVIEWED_DT", Trxs_Codes.PRE_ARR_REVIEWED_DT.ToString());
            parametros.Parameters.Add("PRE_ARR_REVIEWED_USER", Trxs_Codes.PRE_ARR_REVIEWED_USER.ToString());
            parametros.Parameters.Add("BONUS_CHECK_ID", Trxs_Codes.BONUS_CHECK_ID.ToString());
            parametros.Parameters.Add("MOBILE_AUDIO_KEY_YN", Trxs_Codes.MOBILE_AUDIO_KEY_YN.ToString());
            parametros.Parameters.Add("DIRECT_BILL_VERIFY_RESPONSE", Trxs_Codes.DIRECT_BILL_VERIFY_RESPONSE.ToString());
            parametros.Parameters.Add("ADDRESSEE_NAME_ID", Trxs_Codes.ADDRESSEE_NAME_ID.ToString());
            parametros.Parameters.Add("SUPER_SEARCH_INDEX_TEXT", Trxs_Codes.SUPER_SEARCH_INDEX_TEXT.ToString());
            parametros.Parameters.Add("AUTO_CHECKIN_YN", Trxs_Codes.AUTO_CHECKIN_YN.ToString());
            parametros.Parameters.Add("EMAIL_FOLIO_YN", Trxs_Codes.EMAIL_FOLIO_YN.ToString());
            parametros.Parameters.Add("EMAIL_ADDRESS", Trxs_Codes.EMAIL_ADDRESS.ToString());
            parametros.Parameters.Add("SPG_UPGRADE_CONFIRMED_ROOMTYPE", Trxs_Codes.SPG_UPGRADE_CONFIRMED_ROOMTYPE.ToString());
            parametros.Parameters.Add("SPG_UPGRADE_REASON_CODE", Trxs_Codes.SPG_UPGRADE_REASON_CODE.ToString());
            parametros.Parameters.Add("SPG_SUITE_NIGHT_AWARD_STATUS", Trxs_Codes.SPG_SUITE_NIGHT_AWARD_STATUS.ToString());
            parametros.Parameters.Add("SPG_DISCLOSE_ROOM_TYPE_YN", Trxs_Codes.SPG_DISCLOSE_ROOM_TYPE_YN.ToString());
            parametros.Parameters.Add("AMENITY_ELIGIBLE_YN", Trxs_Codes.AMENITY_ELIGIBLE_YN.ToString());
            parametros.Parameters.Add("AMENITY_LEVEL_CODE", Trxs_Codes.AMENITY_LEVEL_CODE.ToString());
            parametros.Parameters.Add("BASE_RATE_CURRENCY_CODE", Trxs_Codes.BASE_RATE_CURRENCY_CODE.ToString());
            parametros.Parameters.Add("BASE_RATE_CODE", Trxs_Codes.BASE_RATE_CODE.ToString());
            parametros.Parameters.Add("LOCAL_BASE_RATE_AMOUNT", Trxs_Codes.LOCAL_BASE_RATE_AMOUNT.ToString());
            parametros.Parameters.Add("PHONE_DISPLAY_NAME_YN", Trxs_Codes.PHONE_DISPLAY_NAME_YN.ToString());
            parametros.Parameters.Add("MOBILE_CHKOUT_ALLOWED", Trxs_Codes.MOBILE_CHKOUT_ALLOWED.ToString());
            parametros.Parameters.Add("MOBILE_VIEW_FOLIO_ALLOWED", Trxs_Codes.MOBILE_VIEW_FOLIO_ALLOWED.ToString());
            parametros.Parameters.Add("HK_EXPECTED_SERVICE_TIME", Trxs_Codes.HK_EXPECTED_SERVICE_TIME.ToString());
            parametros.Parameters.Add("ELIGIBLE_FOR_UPGRADE_YN", Trxs_Codes.ELIGIBLE_FOR_UPGRADE_YN.ToString());
            parametros.Parameters.Add("BEGIN_SYSTEM_DATE_TIME", Trxs_Codes.BEGIN_SYSTEM_DATE_TIME.ToString());
            parametros.Parameters.Add("MOBILE_CHECKIN_ALLOWED_YN", Trxs_Codes.MOBILE_CHECKIN_ALLOWED_YN.ToString());
            parametros.Parameters.Add("QUOTE_ID", Trxs_Codes.QUOTE_ID.ToString());
            parametros.Parameters.Add("MANUAL_CHECKOUT_STATUS", Trxs_Codes.MANUAL_CHECKOUT_STATUS.ToString());
            parametros.Parameters.Add("MOBILE_PREFERRED_CURRENCY", Trxs_Codes.MOBILE_PREFERRED_CURRENCY.ToString());
            parametros.Parameters.Add("MOBILE_ACTION_ALERT_ISSUED", Trxs_Codes.MOBILE_ACTION_ALERT_ISSUED.ToString());
            parametros.Parameters.Add("EXTERNAL_EFOLIO_YN", Trxs_Codes.EXTERNAL_EFOLIO_YN.ToString());
            parametros.Parameters.Add("OPT_IN_BATCH_FOL_YN", Trxs_Codes.OPT_IN_BATCH_FOL_YN.ToString());
            parametros.Parameters.Add("OPERA_ESIGNED_REG_CARD_YN", Trxs_Codes.OPERA_ESIGNED_REG_CARD_YN.ToString());
            parametros.Parameters.Add("RESV_GUID", Trxs_Codes.RESV_GUID.ToString());
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG.ToString());

            return parametros;
        }

    }
}
