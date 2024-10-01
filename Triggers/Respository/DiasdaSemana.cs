using Oracle.ManagedDataAccess.Client;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Triggers.Interfaces;

namespace WorkerOperaCloud.Triggers.Respository
{
    public class DiasdaSemana : ITriggers
    {

        private readonly ILogger<Worker> _logger;
        private readonly DapperContext _context;
        private readonly IConfiguration _config;

        public DiasdaSemana(ILogger<Worker> logger, DapperContext context,
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
            string proxexec = "";
            string ProxExex = "";

            try
            {
                //condição Ativo esta sendo filtrado pela query

                if (job.EXECUTAR_EM == null)
                    return false;
                if (job.DIAS_SEMANA == null)
                    return false;

                var Dias = job.DIAS_SEMANA.Split("-");

                var auxExe = job.EXECUTAR_EM.Split("/");
                var executar = $"{auxExe[1]}/{auxExe[0]}/{auxExe[2].Split(" ")[0]} {job.EXECUTAR_EM_HORA}";
                Executar = Convert.ToDateTime(executar);
                Agora = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                resExecutar = DateTime.Compare(Executar, Agora);

                ProxExex = job.PROXIMA_EXECUCAO + " " + job.PROXIMA_EXECUCAO_HORA;

                if ((job.PROXIMA_EXECUCAO == null && job.PROXIMA_EXECUCAO_HORA == null && resExecutar == 0) || 
                    (!string.IsNullOrEmpty(ProxExex.Trim().Replace(" ", null)) && DateTime.Equals(Agora,Convert.ToDateTime(ProxExex)) && job.DIAS_SEMANA != null) 
                    && job.DIAS_SEMANA != null)
                {
                    var ProxDia = ((int)Agora.DayOfWeek == 0) ? 7 : (int)Agora.DayOfWeek;
                    var sair = true;
                    do
                    {
                        ProxDia = ProxDia + 1 == 8 ? 1 : ProxDia + 1;
                        for (int i = 0; i < Dias.Length; i++)
                        {
                            if (Dias[i] == ProxDia.ToString())
                            {
                                sair = false;
                                break;

                            }
                        }
                    }
                    while (sair);
                    sair = true;

                    var hoje = ((int)Agora.DayOfWeek == 0) ? 7 : (int)Agora.DayOfWeek;

                    if (ProxDia > hoje)
                    {
                        ProxDia = ProxDia - hoje;
                    }

                    proxexec = Agora.AddDays((double)ProxDia).ToString();

                    if (job.ID == null)
                        return false;
                    SetarProximaExecucao(job, proxexec);
                    return true;


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                //stringBuilder.AppendLine();
            }
            return false;
        }

        private bool SetarProximaExecucao(Mdl_Agendamento Job, string ProxExec)
        {
            var query = $"update jobs set PROXIMA_EXECUCAO = '{ProxExec.Split(" ")[0]}', PROXIMA_EXECUCAO_HORA = '{ProxExec.Split(" ")[1]}' where id = '{Job.ID}'";
            using (OracleCommand command = new OracleCommand(query, _context.CreateConnection()))
            {
                try
                {
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                    _logger.LogInformation($"Proxima execução do job {Job.TIPO_AGENDAMENTO} setada para {ProxExec}.");
                    return true;

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao atualizar a proxima execução do job de ID: {Job.ID} na data: {ProxExec}, Exception: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
