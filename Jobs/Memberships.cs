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
using WorkerOperaCloud.Repository.Jobs;
using Dapper;

namespace WorkerOperaCloud.Jobs
{
    public class Memberships : IServiceJobs
    {
        private readonly ILogger<Memberships> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Memberships(ILogger<Memberships> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Memberships>(filePath, LoggBuilder);

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
                            var _where = $"MEMBERSHIP_ID = '{trial.MEMBERSHIP_ID}' and " +
                                $"NAME_ID = '{trial.NAME_ID}' and " +
                                $"MEMBERSHIP_TYPE = '{trial.MEMBERSHIP_TYPE}' and " +
                                $"MEMBERSHIP_CARD_NO = '{trial.MEMBERSHIP_CARD_NO}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Memberships>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Memberships Trxs_Codes)
        {
            parametros.Parameters.Add("MEMBERSHIP_ID", Trxs_Codes.MEMBERSHIP_ID);
            parametros.Parameters.Add("NAME_ID", Trxs_Codes.NAME_ID);
            parametros.Parameters.Add("MEMBERSHIP_TYPE", Trxs_Codes.MEMBERSHIP_TYPE);
            parametros.Parameters.Add("MEMBERSHIP_CARD_NO", Trxs_Codes.MEMBERSHIP_CARD_NO);
            parametros.Parameters.Add("MEMBERSHIP_LEVEL", Trxs_Codes.MEMBERSHIP_LEVEL);
            parametros.Parameters.Add("NAME_ON_CARD", Trxs_Codes.NAME_ON_CARD);
            parametros.Parameters.Add("COMMENTS", Trxs_Codes.COMMENTS);
            parametros.Parameters.Add("JOINED_DATE", Trxs_Codes.JOINED_DATE.ToString());
            parametros.Parameters.Add("EXPIRATION_DATE", Trxs_Codes.EXPIRATION_DATE.ToString());
            parametros.Parameters.Add("CREDIT_LIMIT", Trxs_Codes.CREDIT_LIMIT);
            parametros.Parameters.Add("PRIMARY_AIRLINE_PARTNER", Trxs_Codes.PRIMARY_AIRLINE_PARTNER);
            parametros.Parameters.Add("POINT_INDICATOR", Trxs_Codes.POINT_INDICATOR);
            parametros.Parameters.Add("CURRENT_POINTS", Trxs_Codes.CURRENT_POINTS);
            parametros.Parameters.Add("MEMBER_INDICATOR", Trxs_Codes.MEMBER_INDICATOR);
            parametros.Parameters.Add("MEMBER_SUBTYPE", Trxs_Codes.MEMBER_SUBTYPE);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("PROCESS_EXPIRATION_DATE", Trxs_Codes.PROCESS_EXPIRATION_DATE.ToString());
            parametros.Parameters.Add("ENROLLMENT_CODE", Trxs_Codes.ENROLLMENT_CODE);
            parametros.Parameters.Add("GRACE_PERIOD_INDICATOR", Trxs_Codes.GRACE_PERIOD_INDICATOR);
            parametros.Parameters.Add("MEMBERSHIP_STATUS", Trxs_Codes.MEMBERSHIP_STATUS);
            parametros.Parameters.Add("TRACK_DATA", Trxs_Codes.TRACK_DATA);
            parametros.Parameters.Add("EARNING_PREFERENCE", Trxs_Codes.EARNING_PREFERENCE);
            parametros.Parameters.Add("CHAIN_CODE", Trxs_Codes.CHAIN_CODE);
            parametros.Parameters.Add("ENROLLMENT_SOURCE", Trxs_Codes.ENROLLMENT_SOURCE);
            parametros.Parameters.Add("ENROLLED_AT", Trxs_Codes.ENROLLED_AT);
            parametros.Parameters.Add("RANK_VALUE", Trxs_Codes.RANK_VALUE);
            parametros.Parameters.Add("DEVICE_CODE", Trxs_Codes.DEVICE_CODE);
            parametros.Parameters.Add("DEVICE_DISABLE_DATE", Trxs_Codes.DEVICE_DISABLE_DATE.ToString());
            parametros.Parameters.Add("PARTNER_MEMBERSHIP_ID", Trxs_Codes.PARTNER_MEMBERSHIP_ID);
            parametros.Parameters.Add("MBRPREF_CHANGED_DATE", Trxs_Codes.MBRPREF_CHANGED_DATE.ToString());
            parametros.Parameters.Add("EXCLUDE_FROM_BATCH", Trxs_Codes.EXCLUDE_FROM_BATCH);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

    }
}
