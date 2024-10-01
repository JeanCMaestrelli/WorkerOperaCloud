using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Jobs;
using WorkerOperaCloud.Services.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using WorkerOperaCloud.Services;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System.Xml.Linq;
using Dapper;

namespace WorkerOperaCloud.Repository.Jobs
{
    public class BusinessdateJob : IServiceJobs
    {
        private readonly ILogger<BusinessdateJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private readonly StringBuilder LoggBuilder = new ();
        private readonly Stopwatch _stopwatch = new ();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public BusinessdateJob(ILogger<BusinessdateJob> logger, DapperContext context, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Businessdate>(filePath, LoggBuilder);

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
                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where RESORT = '{trial.RESORT}' and BUSINESS_DATE = TO_DATE('{trial.BUSINESS_DATE}', 'dd/mm/yyyy hh24:mi:ss')";
                            var result = command.Connection.Query<Mdl_Businessdate>(query);

                            if (result.ToList().Count > 0)
                            {
                                if (result.ToList().Count > 1)
                                {
                                    erros = "1";
                                    LoggBuilder.AppendLine($"  ******  {DateTime.Now}  Registro duplicado, QTD: {result.ToList().Count}. \n{trial.Serialize()} ****** ");
                                }
                                else
                                {
                                    var bdUpdate = Convert.ToDateTime(result.FirstOrDefault().RNA_UPDATEDATE.ToString());//data vinda do banco
                                    var xmlUpdate = Convert.ToDateTime(trial.RNA_UPDATEDATE);//data vinda do xml
                                    var update = DateTime.Compare(xmlUpdate, bdUpdate);

                                    if (update == 1 || trial.DELETED_FLAG == "Y")//se data xml maior entao update
                                    {
                                        if (regAtualizados == 0)
                                            queryUpdate = _Job_Exec.GetQuery(TIPO_AGENDAMENTO, "U", LoggBuilder);

                                        try
                                        {
                                            command.CommandText = (queryUpdate + $"RESORT = '{trial.RESORT}' and BUSINESS_DATE = TO_DATE('{trial.BUSINESS_DATE}', 'dd/mm/yyyy hh24:mi:ss')"); 
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
                                if(regIntegrados == 0)
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
                            LoggBuilder.AppendLine($"\n      {DateTime.Now}  Erro Exception: {e.Message}");
                        }
                    }

                    command.Connection.Close();
                }
                catch (Exception e)
                {
                    erros = "1";
                    command.Parameters.Clear();
                    LoggBuilder.AppendLine($"\n      {DateTime.Now} Fechando conexão...");
                    if (command.Connection.State == System.Data.ConnectionState.Open)
                        command.Connection.Close();
                    LoggBuilder.AppendLine($"\n      {DateTime.Now} Erro Exception: {e.Message}");
                }
            }
            else
            {
                if (erros == "0") { LoggBuilder.AppendLine($"\n      {DateTime.Now} Arquivo vazio, Sem registros para integrar."); }
            }

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Businessdate businessdate)
        {
            parametros.Parameters.Add("WEATHER", businessdate.WEATHER);
            parametros.Parameters.Add("RESORT", businessdate.RESORT);
            parametros.Parameters.Add("BUSINESS_DATE", businessdate.BUSINESS_DATE.ToString());
            parametros.Parameters.Add("STATE", businessdate.STATE);
            parametros.Parameters.Add("INSERT_DATE", businessdate.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", businessdate.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", businessdate.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", businessdate.UPDATE_USER);
            parametros.Parameters.Add("MANDATORY_PROCS_PROGRESS", businessdate.MANDATORY_PROCS_PROGRESS);
            parametros.Parameters.Add("NA_TRAN_ACTION_ID", businessdate.NA_TRAN_ACTION_ID);
            parametros.Parameters.Add("LEDGERS_BALANCED_YN", businessdate.LEDGERS_BALANCED_YN);
            parametros.Parameters.Add("LEDGERS_FIXED_YN", businessdate.LEDGERS_FIXED_YN);
            parametros.Parameters.Add("LEDGERS_FIXED_DATE", businessdate.LEDGERS_FIXED_DATE.ToString());
            parametros.Parameters.Add("QRUSH_SMS_SENT_ON", businessdate.QRUSH_SMS_SENT_ON.ToString());
            parametros.Parameters.Add("QRUSH_SMS_SENT_BY", businessdate.QRUSH_SMS_SENT_BY);
            parametros.Parameters.Add("PMS_ACTIVE_YN", businessdate.PMS_ACTIVE_YN);
            parametros.Parameters.Add("CI_CHECKS_RECONCILIATION_FLAG", businessdate.CI_CHECKS_RECONCILIATION_FLAG);
            parametros.Parameters.Add("RNA_INSERTDATE", businessdate.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", businessdate.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", businessdate.DELETED_FLAG);

            return parametros;
        }

    }
}
