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
    public class Department : IServiceJobs
    {
        private readonly ILogger<Department> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Department(ILogger<Department> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Department>(filePath, LoggBuilder);

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
                            var _where = $"RESORT = '{trial.RESORT}' and " +
                                $"DEPT_ID = '{trial.DEPT_ID}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Department>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Department Trxs_Codes)
        {
            parametros.Parameters.Add("RESORT", Trxs_Codes.RESORT);
            parametros.Parameters.Add("DEPT_ID", Trxs_Codes.DEPT_ID);
            parametros.Parameters.Add("DEPT_NAME", Trxs_Codes.DEPT_NAME);
            parametros.Parameters.Add("DEPT_MANAGER_PAGER", Trxs_Codes.DEPT_MANAGER_PAGER);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("CAN_DELETE_YN", Trxs_Codes.CAN_DELETE_YN);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("MESSAGE_TEXT", Trxs_Codes.MESSAGE_TEXT);
            parametros.Parameters.Add("DEPT_EMAIL", Trxs_Codes.DEPT_EMAIL);
            parametros.Parameters.Add("ALLOW_ON_TASK_SHEET_YN", Trxs_Codes.ALLOW_ON_TASK_SHEET_YN);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

        /*private List<Mdl_Department> Get_List_Csv(string filePath)
        {
            List<Mdl_Department> ListCsv = new List<Mdl_Department>();
            Mdl_Department Csv;
            int x = 1;
            Tuple<bool,int> tupla = new Tuple<bool, int>(false,0);

            LoggBuilder.AppendLine($"      {DateTime.Now} Verificando se arquivo de integração existe: {filePath}");
            if (File.Exists(filePath))
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, O Aquivo Existe.");
            }
            else
            {
                erros = "1";
                LoggBuilder.AppendLine($"      {DateTime.Now} NÃO!!!, Arquivo Inexistente.");
                LoggBuilder.AppendLine($"      {DateTime.Now} Encerrando a integração.");
                return ListCsv;
            }

            try
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} Efetuando leitura de arquivo de integração: {filePath}");
                TextFieldParser reader = new TextFieldParser(filePath);
                reader.Delimiters = new string[] { ";" };
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Arquivo aberto para leitura.");
                
                while (!reader.EndOfData)
                {
                    var line = reader.ReadFields();
                    if(line != null && x == 1)
                    {
                        tupla = _Job_Exec.ConsistirColunas(line, _TIPO_AGENDAMENTO, filePath, LoggBuilder);
                        if (!tupla.Item1)
                        {
                            reader.Close();
                            erros = "1";
                            break;
                        }
                    }
                    else if (x >= 2)
                    {
                        if (line != null)
                        {
                            var campos = line;
                            if (campos.Length != tupla.Item2)
                            {
                                erros = "1";
                                LoggBuilder.AppendLine($"      **** ERRO Linha {x}, ( Qtd Campos {campos.Length} | Qtd Colunas {tupla.Item2} )");
                                x++;
                                continue;
                            }

                            Csv = new Mdl_Department()
                            {
                                RESORT = campos[0],
                                DEPT_ID = campos[1],
                                DEPT_NAME = campos[2],
                                DEPT_MANAGER_PAGER = campos[3],
                                INACTIVE_DATE = campos[4].Trim().Split(" ")[0],
                                INSERT_USER = campos[5],
                                INSERT_DATE = campos[6].Trim().Split(" ")[0],
                                UPDATE_USER = campos[7],
                                UPDATE_DATE = campos[8].Trim().Split(" ")[0],
                                CAN_DELETE_YN = campos[9],
                                ORDER_BY = campos[10],
                                MESSAGE_TEXT = campos[11],
                                DEPT_EMAIL = campos[12],
                                ALLOW_ON_TASK_SHEET_YN = campos[13]
                            };

                            ListCsv.Add(Csv);
                        }
                    }
                    x++;
                }
                x = 1;
                reader.Close();
            }
            catch (Exception e)
            {
                erros = "1";
                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas na leitura do arquivo linha {x}, Execption: {e.Message}");
                return new List<Mdl_Department>();
            }

            if (erros == "1")
            {
                return new List<Mdl_Department>();
            }

            _Job_Exec.moverArquivoIntegrado(filePath, LoggBuilder);

            return ListCsv;
        }

        private string GetInsertQuery()
        {
            var query = "insert into DEPARTMENT " +
            " values " +
            "(:RESORT,:DEPT_ID,:DEPT_NAME,:DEPT_MANAGER_PAGER,:INACTIVE_DATE,:INSERT_USER,:INSERT_DATE," +
            ":UPDATE_USER,:UPDATE_DATE,:CAN_DELETE_YN,:ORDER_BY,:MESSAGE_TEXT,:DEPT_EMAIL,:ALLOW_ON_TASK_SHEET_YN)";

            return query;
        }*/
    }
}
