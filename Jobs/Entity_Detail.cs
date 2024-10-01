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
    public class Entity_Detail : IServiceJobs
    {
        private readonly ILogger<Entity_Detail> _logger;
        private readonly DapperContext _context;
        private readonly Job_Exec _Job_Exec;
        private StringBuilder LoggBuilder = new StringBuilder();
        private Stopwatch _stopwatch = new Stopwatch();
        private string erros = "0";
        private string DataExecucao = "";
        private string _TIPO_AGENDAMENTO = "";

        public Entity_Detail(ILogger<Entity_Detail> logger, DapperContext context, IConfiguration config, Job_Exec Job_Exec)
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

            var List = _Job_Exec.Get_List<Mdl_Entity_Detail>(filePath, LoggBuilder);

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
                            var _where = $"ENTITY_NAME = '{trial.ENTITY_NAME}' and " +
                                $"ATTRIBUTE_CODE = '{trial.ATTRIBUTE_CODE}'";

                            var query = $"select RNA_UPDATEDATE from {TIPO_AGENDAMENTO} where {_where}";

                            var result = command.Connection.Query<Mdl_Entity_Detail>(query);

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

        private OracleCommand MountParametros(OracleCommand parametros, Mdl_Entity_Detail Trxs_Codes)
        {
            parametros.Parameters.Add("ENTITY_NAME", Trxs_Codes.ENTITY_NAME);
            parametros.Parameters.Add("ATTRIBUTE_CODE", Trxs_Codes.ATTRIBUTE_CODE);
            parametros.Parameters.Add("DESCRIPTION", Trxs_Codes.DESCRIPTION);
            parametros.Parameters.Add("LANGUAGE_CODE", Trxs_Codes.LANGUAGE_CODE);
            parametros.Parameters.Add("ORDER_BY", Trxs_Codes.ORDER_BY);
            parametros.Parameters.Add("INSERT_DATE", Trxs_Codes.INSERT_DATE.ToString());
            parametros.Parameters.Add("INSERT_USER", Trxs_Codes.INSERT_USER);
            parametros.Parameters.Add("UPDATE_DATE", Trxs_Codes.UPDATE_DATE.ToString());
            parametros.Parameters.Add("UPDATE_USER", Trxs_Codes.UPDATE_USER);
            parametros.Parameters.Add("INACTIVE_DATE", Trxs_Codes.INACTIVE_DATE.ToString());
            parametros.Parameters.Add("COMMENTS", Trxs_Codes.COMMENTS);
            parametros.Parameters.Add("DISPLAY_COLOR", Trxs_Codes.DISPLAY_COLOR);
            parametros.Parameters.Add("TITLE_SUFFIX", Trxs_Codes.TITLE_SUFFIX);
            parametros.Parameters.Add("BUSINESS_TITLE", Trxs_Codes.BUSINESS_TITLE);
            parametros.Parameters.Add("CHAIN_CODE", Trxs_Codes.CHAIN_CODE);
            parametros.Parameters.Add("MASTER_SUB_KEYWORD_YN", Trxs_Codes.MASTER_SUB_KEYWORD_YN);
            parametros.Parameters.Add("EXTERNAL_ATTRIBUTE_CODES", Trxs_Codes.EXTERNAL_ATTRIBUTE_CODES);
            parametros.Parameters.Add("RNA_INSERTDATE", Trxs_Codes.RNA_INSERTDATE.ToString());
            parametros.Parameters.Add("RNA_UPDATEDATE", Trxs_Codes.RNA_UPDATEDATE.ToString());
            parametros.Parameters.Add("DELETED_FLAG", Trxs_Codes.DELETED_FLAG);

            return parametros;
        }

        /*
        private List<Mdl_Entity_Detail> Get_List_Csv(string filePath)
        {
            List<Mdl_Entity_Detail> ListCsv = new List<Mdl_Entity_Detail>();
            Mdl_Entity_Detail Csv;
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

                            Csv = new Mdl_Entity_Detail()
                            {
                                ENTITY_NAME = campos[0],
                                ATTRIBUTE_CODE = campos[1],
                                DESCRIPTION = campos[2],
                                LANGUAGE_CODE = campos[3],
                                ORDER_BY = campos[4],
                                INSERT_DATE = campos[5].Trim().Split(" ")[0],
                                INSERT_USER = campos[6],
                                UPDATE_DATE = campos[7].Trim().Split(" ")[0],
                                UPDATE_USER = campos[8],
                                INACTIVE_DATE = campos[9].Trim().Split(" ")[0],
                                COMMENTS = campos[10],
                                DISPLAY_COLOR = campos[11],
                                TITLE_SUFFIX = campos[12],
                                BUSINESS_TITLE = campos[13],
                                CHAIN_CODE = campos[14],
                                MASTER_SUB_KEYWORD_YN = campos[15],
                                EXTERNAL_ATTRIBUTE_CODES = campos[16]
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
                return new List<Mdl_Entity_Detail>();
            }

            if (erros == "1")
            {
                return new List<Mdl_Entity_Detail>();
            }

            _Job_Exec.moverArquivoIntegrado(filePath, LoggBuilder);

            return ListCsv;
        }

        private string GetInsertQuery()
        {
            var query = "insert into ENTITY_DETAIL " +
            " values " +
            "(:ENTITY_NAME,:ATTRIBUTE_CODE,:DESCRIPTION,:LANGUAGE_CODE,:ORDER_BY,:INSERT_DATE,:INSERT_USER,:UPDATE_DATE,:UPDATE_USER," +
            ":INACTIVE_DATE,:COMMENTS,:DISPLAY_COLOR,:TITLE_SUFFIX,:BUSINESS_TITLE,:CHAIN_CODE,:MASTER_SUB_KEYWORD_YN,:EXTERNAL_ATTRIBUTE_CODES)";

            return query;
        }*/

    }
}
