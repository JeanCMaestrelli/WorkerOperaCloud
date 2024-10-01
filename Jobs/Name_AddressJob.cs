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
    public class Name_AddressJob : IServiceJobs
    {
        private readonly ILogger<Name_AddressJob> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Name_AddressJob(ILogger<Name_AddressJob> logger, DapperContext context,  Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Name_Adress>(filePath, LoggBuilder);

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
                            var _where = $"ADDRESS_ID = '{trial.ADDRESS_ID}' and " +
                                $"NAME_ID = '{trial.NAME_ID}' and " +
                                $"ADDRESS_TYPE = '{trial.ADDRESS_TYPE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} \nwhere {_where}";

                            var result = command.Connection.Query<Mdl_Name_Adress>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Name_Adress NameAdress)
        {
            parametros.Parameters.Add("ADDRESS_ID", NameAdress.ADDRESS_ID);
            parametros.Parameters.Add("NAME_ID", NameAdress.NAME_ID);
            parametros.Parameters.Add("ADDRESS_TYPE", NameAdress.ADDRESS_TYPE);
            parametros.Parameters.Add("INSERT_DATE", NameAdress.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", NameAdress.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", NameAdress.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", NameAdress.UPDATE_USER);
            parametros.Parameters.Add("BEGIN_DATE", NameAdress.BEGIN_DATE.ToString());
            parametros.Parameters.Add("END_DATE", NameAdress.END_DATE.ToString());
            parametros.Parameters.Add("ADDRESS1", NameAdress.ADDRESS1);
            parametros.Parameters.Add("ADDRESS2", NameAdress.ADDRESS2);
            parametros.Parameters.Add("ADDRESS3", NameAdress.ADDRESS3);
            parametros.Parameters.Add("ADDRESS4", NameAdress.ADDRESS4);
            parametros.Parameters.Add("CITY", NameAdress.CITY);
            parametros.Parameters.Add("COUNTRY", NameAdress.COUNTRY);
            parametros.Parameters.Add("PROVINCE", NameAdress.PROVINCE);
            parametros.Parameters.Add("STATE", NameAdress.STATE);
            parametros.Parameters.Add("ZIP_CODE", NameAdress.ZIP_CODE);
            parametros.Parameters.Add("INACTIVE_DATE", NameAdress.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("PRIMARY_YN", NameAdress.PRIMARY_YN);
            parametros.Parameters.Add("FOREIGN_COUNTRY", NameAdress.FOREIGN_COUNTRY);
            parametros.Parameters.Add("IN_CARE_OF", NameAdress.IN_CARE_OF);
            parametros.Parameters.Add("CITY_EXT", NameAdress.CITY_EXT);
            parametros.Parameters.Add("LAPTOP_CHANGE", NameAdress.LAPTOP_CHANGE);
            parametros.Parameters.Add("LANGUAGE_CODE", NameAdress.LANGUAGE_CODE);
            parametros.Parameters.Add("CLEANSED_STATUS", NameAdress.CLEANSED_STATUS);
            parametros.Parameters.Add("CLEANSED_DATETIME", NameAdress.CLEANSED_DATETIME.ToString());
            parametros.Parameters.Add("CLEANSED_ERRORMSG", NameAdress.CLEANSED_ERRORMSG);
            parametros.Parameters.Add("CLEANSED_VALIDATIONSTATUS", NameAdress.CLEANSED_VALIDATIONSTATUS);
            parametros.Parameters.Add("CLEANSED_MATCHSTATUS", NameAdress.CLEANSED_MATCHSTATUS);
            parametros.Parameters.Add("BARCODE", NameAdress.BARCODE);
            parametros.Parameters.Add("LAST_UPDATED_RESORT", NameAdress.LAST_UPDATED_RESORT);
            parametros.Parameters.Add("RNA_INSERTDATE", NameAdress.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", NameAdress.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", NameAdress.DELETED_FLAG);

            return parametros;
        }
    }
}

