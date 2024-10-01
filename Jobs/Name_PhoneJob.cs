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
    public class Name_PhoneJob: IServiceJobs
    {
        private readonly ILogger<Name_PhoneJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Name_PhoneJob(ILogger<Name_PhoneJob> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Name_Phone>(filePath, LoggBuilder);

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
                            var _where = $"PHONE_ID = '{trial.PHONE_ID}' and " +
                                $"NAME_ID = '{trial.NAME_ID}' and " +
                                $"PHONE_TYPE = '{trial.PHONE_TYPE}' and " +
                                $"PHONE_ROLE = '{trial.PHONE_ROLE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Name_Phone>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Name_Phone NamePhone)
        {
            parametros.Parameters.Add("PHONE_ID", NamePhone.PHONE_ID);
            parametros.Parameters.Add("NAME_ID", NamePhone.NAME_ID);
            parametros.Parameters.Add("PHONE_TYPE", NamePhone.PHONE_TYPE);
            parametros.Parameters.Add("PHONE_ROLE", NamePhone.PHONE_ROLE);
            parametros.Parameters.Add("PHONE_NUMBER", NamePhone.PHONE_NUMBER);
            parametros.Parameters.Add("INSERT_DATE", NamePhone.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", NamePhone.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", NamePhone.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", NamePhone.UPDATE_USER);
            parametros.Parameters.Add("INACTIVE_DATE", NamePhone.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("END_DATE", NamePhone.END_DATE.ToString());
            parametros.Parameters.Add("BEGIN_DATE", NamePhone.BEGIN_DATE.ToString());
            parametros.Parameters.Add("ADDRESS_ID", NamePhone.ADDRESS_ID);
            parametros.Parameters.Add("PRIMARY_YN", NamePhone.PRIMARY_YN);
            parametros.Parameters.Add("DISPLAY_SEQ", NamePhone.DISPLAY_SEQ);
            parametros.Parameters.Add("LAPTOP_CHANGE", NamePhone.LAPTOP_CHANGE);
            parametros.Parameters.Add("INDEX_PHONE", NamePhone.INDEX_PHONE);
            parametros.Parameters.Add("EXTENSION", NamePhone.EXTENSION);
            parametros.Parameters.Add("EMAIL_FORMAT", NamePhone.EMAIL_FORMAT);
            parametros.Parameters.Add("SHARE_EMAIL_YN", NamePhone.SHARE_EMAIL_YN);
            parametros.Parameters.Add("DEFAULT_CONFIRMATION_YN", NamePhone.DEFAULT_CONFIRMATION_YN);
            parametros.Parameters.Add("EMAIL_LANGUAGE", NamePhone.EMAIL_LANGUAGE);
            parametros.Parameters.Add("MOBILE_AUDIO_KEY_YN", NamePhone.MOBILE_AUDIO_KEY_YN);
            parametros.Parameters.Add("COUNTRY_DIALING_CODE", NamePhone.COUNTRY_DIALING_CODE);
            parametros.Parameters.Add("VALID_YN", NamePhone.VALID_YN);
            parametros.Parameters.Add("PHONE_COUNTRY_CODE", NamePhone.PHONE_COUNTRY_CODE);
            parametros.Parameters.Add("RNA_INSERTDATE", NamePhone.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", NamePhone.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", NamePhone.DELETED_FLAG);

            return parametros;
        }

    }
}
