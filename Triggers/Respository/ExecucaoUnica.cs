using Oracle.ManagedDataAccess.Client;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Triggers.Interfaces;

namespace WorkerOperaCloud.Triggers.Respository
{
    public class ExecucaoUnica : ITriggers
    {

        private readonly ILogger<Worker> _logger;
        private readonly DapperContext _context;
        private readonly IConfiguration _config;

        public ExecucaoUnica(ILogger<Worker> logger, DapperContext context,
            IConfiguration config)
        {
            _logger = logger;
            _context = context;
            _config = config;
        }
        public bool VerificarAgendamento(Mdl_Agendamento job)
        {
            DateTime Agora;
            DateTime Executar;
            int resExecutar;

            try
            {
                if (job.EXECUTAR_EM == null)
                    return false;

                var auxExe = job.EXECUTAR_EM.Split("/");
                var executar = $"{auxExe[1]}/{auxExe[0]}/{auxExe[2].Split(" ")[0]} {job.EXECUTAR_EM_HORA}";
                Executar = Convert.ToDateTime(executar);
                Agora = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                resExecutar = DateTime.Compare(Executar, Agora);

                if (resExecutar == 0)
                {
                    if (job.ID == null)
                        return false;
                    SetarInativo(job);
                    return true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return false;
        }

        private bool SetarInativo(Mdl_Agendamento Job)
        {
            var query = $"update jobs set ATIVO = '0' where id = '{Job.ID}'";
            using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
            {
                try
                {
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                    _logger.LogInformation($"O job {Job.TIPO_AGENDAMENTO} de ID {Job.ID} e tipo execução única foi executado e será marcado como inativo.");
                    return true;

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao atualizar a ativo do job de ID: {Job.ID}, Exception: {ex.Message}");
                    return false;
                }

            }
        }
    }
}
