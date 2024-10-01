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
    public class Reservation_Daily_Element_Name : IServiceJobs
    {
        private readonly ILogger<Reservation_Daily_Element_Name> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Reservation_Daily_Element_Name(ILogger<Reservation_Daily_Element_Name> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Reservation_Daily_Element_Name>(filePath, LoggBuilder);

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
                                        $"RESV_DAILY_EL_SEQ = '{trial.RESV_DAILY_EL_SEQ}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \n where {_where}";

                            var result = command.Connection.Query<Mdl_Reservation_Daily_Element_Name>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Reservation_Daily_Element_Name Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("RESV_NAME_ID", Trxs_Codes.RESV_NAME_ID);
            parametros.Parameters.Add("RESERVATION_DATE", Trxs_Codes.RESERVATION_DATE.ToString());
            parametros.Parameters.Add("RESV_DAILY_EL_SEQ", Trxs_Codes.RESV_DAILY_EL_SEQ);
            parametros.Parameters.Add("TRAVEL_AGENT_ID", Trxs_Codes.TRAVEL_AGENT_ID);
            parametros.Parameters.Add("COMPANY_ID", Trxs_Codes.COMPANY_ID);
            parametros.Parameters.Add("SOURCE_ID", Trxs_Codes.SOURCE_ID);
            parametros.Parameters.Add("GROUP_ID", Trxs_Codes.GROUP_ID);
            parametros.Parameters.Add("SHARE_PAYMENT_TYPE", Trxs_Codes.SHARE_PAYMENT_TYPE);
            parametros.Parameters.Add("SHARE_AMOUNT", Trxs_Codes.SHARE_AMOUNT);
            parametros.Parameters.Add("SHARE_PRCNT", Trxs_Codes.SHARE_PRCNT);
            parametros.Parameters.Add("ADULTS", Trxs_Codes.ADULTS);
            parametros.Parameters.Add("CHILDREN", Trxs_Codes.CHILDREN);
            parametros.Parameters.Add("DISCOUNT_AMT", Trxs_Codes.DISCOUNT_AMT);
            parametros.Parameters.Add("DISCOUNT_PRCNT", Trxs_Codes.DISCOUNT_PRCNT);
            parametros.Parameters.Add("DISCOUNT_REASON_CODE", Trxs_Codes.DISCOUNT_REASON_CODE);
            parametros.Parameters.Add("FIXED_RATE_YN", Trxs_Codes.FIXED_RATE_YN);
            parametros.Parameters.Add("BASE_RATE_AMOUNT", Trxs_Codes.BASE_RATE_AMOUNT);
            parametros.Parameters.Add("AUTO_POST_AMOUNT", Trxs_Codes.AUTO_POST_AMOUNT);
            parametros.Parameters.Add("SHARE_PRIORITY", Trxs_Codes.SHARE_PRIORITY);
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("LAST_SHARE_CALCULATION", Trxs_Codes.LAST_SHARE_CALCULATION);
            parametros.Parameters.Add("INSERT_ACTION_INSTANCE_ID", Trxs_Codes.INSERT_ACTION_INSTANCE_ID);
            parametros.Parameters.Add("DML_SEQ_NO", Trxs_Codes.DML_SEQ_NO);
            parametros.Parameters.Add("NET_ROOM_AMT", Trxs_Codes.NET_ROOM_AMT);
            parametros.Parameters.Add("ROOM_TAX", Trxs_Codes.ROOM_TAX);
            parametros.Parameters.Add("PKG_AMT", Trxs_Codes.PKG_AMT);
            parametros.Parameters.Add("PKG_TAX", Trxs_Codes.PKG_TAX);
            parametros.Parameters.Add("GROSS_RATE_AMT", Trxs_Codes.GROSS_RATE_AMT);
            parametros.Parameters.Add("ADULTS_TAX_FREE", Trxs_Codes.ADULTS_TAX_FREE);
            parametros.Parameters.Add("CHILDREN_TAX_FREE", Trxs_Codes.CHILDREN_TAX_FREE);
            parametros.Parameters.Add("CHILDREN1", Trxs_Codes.CHILDREN1);
            parametros.Parameters.Add("CHILDREN2", Trxs_Codes.CHILDREN2);
            parametros.Parameters.Add("CHILDREN3", Trxs_Codes.CHILDREN3);
            parametros.Parameters.Add("RATE_CODE", Trxs_Codes.RATE_CODE);
            parametros.Parameters.Add("CURRENCY_CODE", Trxs_Codes.CURRENCY_CODE);
            parametros.Parameters.Add("EXCHANGE_POSTING_TYPE", Trxs_Codes.EXCHANGE_POSTING_TYPE);
            parametros.Parameters.Add("MEMBERSHIP_POINTS", Trxs_Codes.MEMBERSHIP_POINTS);
            parametros.Parameters.Add("CHILDREN4", Trxs_Codes.CHILDREN4);
            parametros.Parameters.Add("CHILDREN5", Trxs_Codes.CHILDREN5);
            parametros.Parameters.Add("COMMISSION_CODE", Trxs_Codes.COMMISSION_CODE);
            parametros.Parameters.Add("AWARD_CODE_1", Trxs_Codes.AWARD_CODE_1);
            parametros.Parameters.Add("AWARD_CODE_2", Trxs_Codes.AWARD_CODE_2);
            parametros.Parameters.Add("AWARD_CODE_3", Trxs_Codes.AWARD_CODE_3);
            parametros.Parameters.Add("AWARD_CODE_4", Trxs_Codes.AWARD_CODE_4);
            parametros.Parameters.Add("AWARD_CODE_5", Trxs_Codes.AWARD_CODE_5);
            parametros.Parameters.Add("AWARD_VOUCHER_1", Trxs_Codes.AWARD_VOUCHER_1);
            parametros.Parameters.Add("AWARD_VOUCHER_2", Trxs_Codes.AWARD_VOUCHER_2);
            parametros.Parameters.Add("AWARD_VOUCHER_3", Trxs_Codes.AWARD_VOUCHER_3);
            parametros.Parameters.Add("AWARD_VOUCHER_4", Trxs_Codes.AWARD_VOUCHER_4);
            parametros.Parameters.Add("AWARD_VOUCHER_5", Trxs_Codes.AWARD_VOUCHER_5);
            parametros.Parameters.Add("DO_NOT_MOVE_YN", Trxs_Codes.DO_NOT_MOVE_YN);
            parametros.Parameters.Add("AWARD_CODE", Trxs_Codes.AWARD_CODE);
            parametros.Parameters.Add("POINTS", Trxs_Codes.POINTS);
            parametros.Parameters.Add("UPSELL_CHARGE", Trxs_Codes.UPSELL_CHARGE);
            parametros.Parameters.Add("SHARE_AMOUNT_ORIGINAL", Trxs_Codes.SHARE_AMOUNT_ORIGINAL);
            parametros.Parameters.Add("POINTS_ELIGIBILITY_CODE", Trxs_Codes.POINTS_ELIGIBILITY_CODE);
            parametros.Parameters.Add("COMMISSION_PAID", Trxs_Codes.COMMISSION_PAID);
            parametros.Parameters.Add("RESV_CONTACT_ID", Trxs_Codes.RESV_CONTACT_ID);
            parametros.Parameters.Add("BILLING_CONTACT_ID", Trxs_Codes.BILLING_CONTACT_ID);
            parametros.Parameters.Add("REFERRAL_YN", Trxs_Codes.REFERRAL_YN);
            parametros.Parameters.Add("BXGY_DISCOUNT_YN", Trxs_Codes.BXGY_DISCOUNT_YN);
            parametros.Parameters.Add("COMMISSIONABLE_YN", Trxs_Codes.COMMISSIONABLE_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
