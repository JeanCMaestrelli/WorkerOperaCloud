using Dapper;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Services.Interfaces;
using WorkerOperaCloud.Triggers.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WorkerOperaCloud.Services
{
    public class Job_Exec
    {
        private readonly ILogger<Worker> _logger;
        private readonly DapperContext _context;

        public Job_Exec(ILogger<Worker> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        public string InsertJobExec(Mdl_Job_Exec Job_Exec)
        {
            var query = "insert into JOBS_EXEC (ID,ID_JOB,JOB,RESORT,INICIO) " +
                    "values (:ID,:ID_JOB,:JOB,:RESORT,:INICIO)";

            Random randNum = new Random();
            for (int i = 0; i <= 12; i++)
                Job_Exec.ID += randNum.Next(0, 9).ToString();
            try
            {
                using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
                {
                    command.Parameters.Clear();
                    command.Parameters.Add("ID", Job_Exec.ID);
                    command.Parameters.Add("ID_JOB", Job_Exec.ID_JOB);
                    command.Parameters.Add("JOB", Job_Exec.JOB);
                    command.Parameters.Add("RESORT", Job_Exec.RESORT);
                    command.Parameters.Add("INICIO", Job_Exec.INICIO);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                    return Job_Exec.ID;
                }
            }
            catch
            {
                return "";
            }
        }
        public ResponseDto DeletJobExec(string ID)
        {
            var query = "delete from JOBS_EXEC where ID = :ID";

            try
            {
                using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
                {
                    command.Parameters.Clear();
                    command.Parameters.Add("ID", ID);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                    return new ResponseDto
                    {
                        IsOk = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResponseDto
                {
                    IsOk = false,
                    Error = ex.Message
                };
            }
        }
        public void InsertLog(string id_job, string tipoArquivo, string erros, Stopwatch _stopwatch, StringBuilder LoggBuilder, string DataExecucao)
        {
            try
            {
                var time = _stopwatch.Elapsed.ToString().Split(".")[0];
                var query = "insert into JOBSLOGS (ID,ID_JOB,TIPOARQUIVO,LOG,ERRO,TEMPO_INTEGRACAO,DATA_EXECUCAO,HORA_EXECUCAO) " +
                    "values (:ID,:ID_JOB,:TIPOARQUIVO,:LOG,:ERRO,:TEMPO_INTEGRACAO,:DATA_EXECUCAO,:HORA_EXECUCAO)";
                string ID = "";

                Random randNum = new Random();
                for (int i = 0; i <= 10; i++)
                    ID += randNum.Next(0, 9).ToString();

                string LOG = LoggBuilder.ToString();

                using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
                {
                    command.Parameters.Clear();
                    command.Parameters.Add("ID", ID);
                    command.Parameters.Add("ID_JOB", id_job);
                    command.Parameters.Add("TIPOARQUIVO", tipoArquivo);
                    command.Parameters.Add("LOG", LoggBuilder.ToString());
                    command.Parameters.Add("ERRO", erros);
                    command.Parameters.Add("TEMPO_INTEGRACAO", time);
                    command.Parameters.Add("DATA_EXECUCAO", DataExecucao.Split(" ")[0]);
                    command.Parameters.Add("HORA_EXECUCAO", DataExecucao.Split(" ")[1]);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{DateTime.Now} {e.Message}");
            }
        }
        public void moverArquivoIntegrado(string filePath, StringBuilder LoggBuilder)
        {

            string arquivo = "";
            try
            {
                arquivo = filePath.Split("\\")[filePath.Split("\\").Length - 1];
                var instr = filePath.Substring(0, filePath.Length - arquivo.Length);
                instr += "Integrados";
                var novoArq = instr + "\\" + arquivo.Split(".")[0] + "_" + DateTime.Now.ToString("dd-MM-yyyy_HHmmss") + "." + arquivo.Split(".")[1];
                LoggBuilder.AppendLine($"      {DateTime.Now} Verificando se o diretório de arquivos Integrados existe...");
                if (Directory.Exists(instr))
                {
                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!! Diretório Existe, Movendo o arquivo {arquivo} para o diretório de arquivos integrados...");
                    File.Move(filePath, novoArq);
                }
                else
                {
                    LoggBuilder.AppendLine($"      {DateTime.Now} NÃO EXISTE!!!, Criando diretório...");
                    Directory.CreateDirectory(instr);
                    LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Diretório Criado...");
                    LoggBuilder.AppendLine($"      {DateTime.Now} Movendo o arquivo {arquivo} para o diretório de arquivos integrados...");

                    File.Move(filePath, novoArq);
                }
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, Movido...");
            }
            catch (Exception e)
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas ao mover o arquivo {arquivo} para o diretório de arquivos integrados, Execption: {e.Message}");
            }
        }
        private DataTable GetHeaderTable(string TIPO_AGENDAMENTO, StringBuilder LoggBuilder)
        {
            DataTable dt = new DataTable();
            var query = $"select * from {TIPO_AGENDAMENTO} where rownum = 1";
            using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
            {
                try
                {
                    command.Connection.Open();
                    OracleDataReader reader = command.ExecuteReader();
                    dt.Load(reader);
                    reader.Close();
                    reader.Dispose();
                    command.Connection.Close();
                    return dt;
                }
                catch (OracleException e)
                {
                    LoggBuilder.Append($"Erro ao Consultar GetHeaderTable {TIPO_AGENDAMENTO}, Exception: " + e.Message);
                    return new DataTable();
                }
            }

        }
        public Tuple<bool,int> ConsistirColunas(string line, string TIPO_AGENDAMENTO, string csv, StringBuilder LoggBuilder)
        {
            bool retorno = true;
            var tbcoluns = GetHeaderTable(TIPO_AGENDAMENTO, LoggBuilder);
            var csvcoluns = line.Split(';');
            var qtdcsv = csvcoluns.Length;
            int x = 0;

            if (tbcoluns.Columns.Count>0 && csvcoluns.Length>0)
            {
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas da tabela    {TIPO_AGENDAMENTO}    existem no arquivo    {csv.ToUpper()}."); }
                    if (!csvcoluns.Contains(col.ColumnName))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ColumnName.ToUpper()}    não existe no CSV.");
                    }
                }
                if (retorno) {LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
                x = 0;
                foreach (var col in csvcoluns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas do arquivo    {csv.ToUpper()}    existem na tabela    {TIPO_AGENDAMENTO}."); }
                    if (!tbcoluns.Columns.Contains(col))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ToUpper()}    não existe na tabela    {TIPO_AGENDAMENTO}.");
                    }
                }
                if (retorno) { LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
            }
            else
            {
                LoggBuilder.AppendLine($"\n            **** Erro ao consistir colunas do CSV e TABELA, repository Job_Exec.");
                retorno = false;
            }
            var tupla = Tuple.Create<bool, int>(retorno, tbcoluns.Columns.Count);
            return tupla;
        }
        public Tuple<bool, int> ConsistirColunas(string[] line, string TIPO_AGENDAMENTO, string csv, StringBuilder LoggBuilder)
        {
            bool retorno = true;
            var tbcoluns = GetHeaderTable(TIPO_AGENDAMENTO, LoggBuilder);
            var csvcoluns = line;
            var qtdcsv = csvcoluns.Length;
            int x = 0;

            if (tbcoluns.Columns.Count > 0 && csvcoluns.Length > 0)
            {
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas da tabela    {TIPO_AGENDAMENTO}    existem no arquivo    {csv.ToUpper()}."); }
                    if (!csvcoluns.Contains(col.ColumnName))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ColumnName.ToUpper()}    não existe no CSV.");
                    }
                }
                if (retorno) { LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
                x = 0;
                foreach (var col in csvcoluns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas do arquivo    {csv.ToUpper()}    existem na tabela    {TIPO_AGENDAMENTO}."); }
                    if (!tbcoluns.Columns.Contains(col))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ToUpper()}    não existe na tabela    {TIPO_AGENDAMENTO}.");
                    }
                }
                if (retorno) { LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
            }
            else
            {
                LoggBuilder.AppendLine($"\n            **** Erro ao consistir colunas do CSV e TABELA, repository Job_Exec.");
                retorno = false;
            }
            var tupla = Tuple.Create<bool, int>(retorno, tbcoluns.Columns.Count);
            return tupla;
        }
        public string GetQuery(string TIPO_AGENDAMENTO,string tipo, StringBuilder LoggBuilder)
        {
            var tbcoluns = GetHeaderTable(TIPO_AGENDAMENTO, LoggBuilder);

            if (tbcoluns.Columns.Count == 0)
            {
                return "";
            }
            
            string query = "";
            if (tipo == "I")
            {
                var x = 1;
                query = $"insert into {TIPO_AGENDAMENTO} values (";
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == tbcoluns.Columns.Count)
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss')";
                        else if (col.DataType.Name == "Decimal")
                            query += $"TO_NUMBER(REPLACE(:{col.ColumnName}, '.',','))";
                        else
                            query += ":" + col.ColumnName;
                    }
                    else
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss'),\n";
                        else if (col.DataType.Name == "Decimal")
                            query += $"TO_NUMBER(REPLACE(:{col.ColumnName}, '.',',')),\n";
                        else
                            query += ":" + col.ColumnName + ",\n";
                    }
                    x++;
                }
                query += "\n)";
            }
            else if (tipo == "U")
            {
                var x = 1;
                query = $"update {TIPO_AGENDAMENTO} set ";
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == tbcoluns.Columns.Count)
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"{col.ColumnName} = TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss')\n";
                        else if (col.DataType.Name == "Decimal")
                            query += $"{col.ColumnName} = TO_NUMBER(REPLACE(:{col.ColumnName}, '.',','))\n";
                        else
                            query += $"{col.ColumnName} = :{col.ColumnName}\n";
                    }
                    else
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"{col.ColumnName} = TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss'),\n";
                        else if (col.DataType.Name == "Decimal")
                            query += $"{col.ColumnName} = TO_NUMBER(REPLACE(:{col.ColumnName}, '.',',')),\n";
                        else
                            query += $"{col.ColumnName} = :{col.ColumnName},\n";
                    }
                    x++;
                }
                query += " where ";
            }
            else if (tipo == "S")
            {
                var x = 1;
                query = $"select * from {TIPO_AGENDAMENTO} where ";
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == tbcoluns.Columns.Count)
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"{col.ColumnName} = TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss')\n";
                        else if (col.DataType.Name == "Decimal")
                            query += $"{col.ColumnName} = TO_NUMBER(REPLACE(:{col.ColumnName}, '.',','))\n";
                        else
                            query += $"{col.ColumnName} = :{col.ColumnName}\n";
                    }
                    else
                    {
                        if (col.DataType.Name == "DateTime")
                            query += $"{col.ColumnName} = TO_DATE(:{col.ColumnName}, 'dd/mm/yyyy hh24:mi:ss') and \n";
                        else if (col.DataType.Name == "Decimal")
                            query += $"{col.ColumnName} = TO_NUMBER(REPLACE(:{col.ColumnName}, '.',',')) and \n";
                        else
                            query += $"{col.ColumnName} = :{col.ColumnName} and \n";
                    }
                    x++;
                }
            }

            return query;
        }
        public List<T> Get_List<T>(string filePath, StringBuilder LoggBuilder)
        {
            List<T> Csv;
            string subjson = "";

            LoggBuilder.AppendLine($"      {DateTime.Now} Verificando se arquivo de integração existe: {filePath}");
            if (File.Exists(filePath))
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!, O Aquivo Existe.");
            }
            else
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} NÃO!!!, Arquivo Inexistente.");
                LoggBuilder.AppendLine($"      {DateTime.Now} Encerrando a integração.");
                return new List<T>();
            }

            try
            {
                LoggBuilder.AppendLine($"      {DateTime.Now} Efetuando leitura de arquivo de integração: {filePath}");

                XDocument doc = XDocument.Load(filePath);

                string json = JsonConvert.SerializeXNode(doc, Newtonsoft.Json.Formatting.None, false);

                int ini = json.IndexOf("{\"G_1\":[") + 7;
                if (ini == 6)
                {
                    ini = ini = json.IndexOf("{\"G_1\":{") + 7;
                    subjson = "[";
                }

                int fin = json.IndexOf("]}}") + 1;
                if(fin == 0)
                {
                    fin = fin = json.IndexOf("}}}") + 1;
                    if (fin == 0)
                    {
                        return new List<T>();
                    }
                    subjson += json.Substring(ini, fin - ini);
                    subjson += "]";
                }
                else
                    subjson = json.Substring(ini, fin - ini);

                Csv = JsonConvert.DeserializeObject<List<T>>(subjson, new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });
            }
            catch (Exception e)
            {

                LoggBuilder.AppendLine($"      {DateTime.Now} Problemas na leitura do arquivo, Execption: {e.Message}");
                return new List<T>();
            }

            moverArquivoIntegrado(filePath, LoggBuilder);

            return Csv;
        }
        public Tuple<bool, int, DataTable> ConsistirColunas(string[] line, string TIPO_AGENDAMENTO, string csv, StringBuilder LoggBuilder,bool RetDataTable)
        {
            bool retorno = true;
            var tbcoluns = GetHeaderTable(TIPO_AGENDAMENTO, LoggBuilder);
            var csvcoluns = line;
            var qtdcsv = csvcoluns.Length;
            int x = 0;

            if (tbcoluns.Columns.Count > 0 && csvcoluns.Length > 0)
            {
                foreach (DataColumn col in tbcoluns.Columns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas da tabela    {TIPO_AGENDAMENTO}    existem no arquivo    {csv.ToUpper()}."); }
                    if (!csvcoluns.Contains(col.ColumnName))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ColumnName.ToUpper()}    não existe no CSV.");
                    }
                }
                if (retorno) { LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
                x = 0;
                foreach (var col in csvcoluns)
                {
                    if (x == 0) { x++; LoggBuilder.AppendLine($"\n      {DateTime.Now} Verificando se todas as colunas do arquivo    {csv.ToUpper()}    existem na tabela    {TIPO_AGENDAMENTO}."); }
                    if (!tbcoluns.Columns.Contains(col))
                    {
                        retorno = false;
                        LoggBuilder.AppendLine($"            **** Coluna    {col.ToUpper()}    não existe na tabela    {TIPO_AGENDAMENTO}.");
                    }
                }
                if (retorno) { LoggBuilder.AppendLine($"      {DateTime.Now} OK!!!\n"); }
            }
            else
            {
                LoggBuilder.AppendLine($"\n            **** Erro ao consistir colunas do CSV e TABELA, repository Job_Exec.");
                retorno = false;
            }
            var tupla = Tuple.Create<bool, int, DataTable>(retorno, tbcoluns.Columns.Count, tbcoluns);
            return tupla;
        }
    }
}
