
using Dapper;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using WorkerOperaCloud.Context;
using WorkerOperaCloud.Jobs;
using WorkerOperaCloud.Models;
using WorkerOperaCloud.Services.Interfaces;
using WorkerOperaCloud.Triggers.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace WorkerOperaCloud.Services.Repository
{
    public class ServiceScheduler : IServiceScheduler
    {
        private readonly IServiceScope scoped;
        private readonly ILogger<Worker> _logger;
        private readonly DapperContext _context;
        private readonly IConfiguration _config;
        private readonly IEnumerable<ITriggers> _ITriggers;
        private List<string>? _JobType = new List<string>();
        private List<string>? _Triggers = new List<string>();

        public ServiceScheduler(IServiceProvider ServiceProvider, ILogger<Worker> logger, DapperContext context,
            IConfiguration config, IEnumerable<ITriggers> ITriggers)
        {
            _logger = logger;
            _context = context;
            _config = config;
            _ITriggers = ITriggers;

            scoped = ServiceProvider.CreateScope();

            //### Informar a mesma sequencia do .AddScoped() no program ###
            _JobType.Add("BUSINESSDATE");
            _JobType.Add("TRIAL_BALANCE");
            _JobType.Add("NAME");
            _JobType.Add("NAME_ADDRESS");
            _JobType.Add("NAME_PHONE"); 
            _JobType.Add("REP_MANAGER");
            _JobType.Add("FINANCIAL_TRANSACTIONS");
            _JobType.Add("TRX$_CODE_ARRANGEMENT"); 
            _JobType.Add("TRX$_CODES");
            _JobType.Add("RESERVATION_NAME");
            _JobType.Add("FOLIO_TAX");
            _JobType.Add("FINANCIAL_TRANSACTIONS_JRNL");
            _JobType.Add("EVENT$RESERVATION"); 
            _JobType.Add("GEM$EVENT");
            _JobType.Add("GEM$EVENT_REVENUE");
            _JobType.Add("RESERVATION_SUMMARY");
            _JobType.Add("RESERVATION_STAT_DAILY"); 
            _JobType.Add("ALLOTMENT$HEADER");
            _JobType.Add("AR$_ACCOUNT");
            _JobType.Add("NAME$OWNER");
            _JobType.Add("NAME_COMMISSION");
            _JobType.Add("POSTAL_CODES_CHAIN");
            _JobType.Add("RESORT$_MARKETS");
            _JobType.Add("RESORT$_ROOM_CATEGORY");
            _JobType.Add("RESORT_RATE_CATEGORY");
            _JobType.Add("RESORT_RATE_CLASSES");
            _JobType.Add("RESORT_ROOM_CLASSES"); 
            _JobType.Add("COMPUTED_COMMISSIONS");
            _JobType.Add("MARKET_GROUPS"); 
            _JobType.Add("APPLICATION$_USER");
            _JobType.Add("ARTICLE$_CODES");
            _JobType.Add("DEPARTMENT");
            _JobType.Add("ENTITY_DETAIL");
            _JobType.Add("MARKET_CODES_TEMPLATE");
            _JobType.Add("PRIORITIES"); 
            _JobType.Add("RATE_CLASSES_TEMPLATE");
            _JobType.Add("RATE_HEADER");
            _JobType.Add("RESERVATION_DAILY_ELEMENT_NAME");
            _JobType.Add("RESERVATION_DAILY_ELEMENTS");
            _JobType.Add("RESERVATION_PROMOTIONS");
            _JobType.Add("RESORT");
            _JobType.Add("ROOM");
            _JobType.Add("WORK_ORDERS");
            _JobType.Add("MEMBERSHIP_TYPES");
            _JobType.Add("MEMBERSHIPS");
            _JobType.Add("NAME$XREF"); 
            _JobType.Add("PREFERENCES$");
            _JobType.Add("RESERVATION_SPECIAL_REQUESTS");
            _JobType.Add("RESERVATION_COMMENT");
            _JobType.Add("RESORT_ORIGINS_OF_BOOKING");
            _JobType.Add("ROOM$COMBO");
            _JobType.Add("ROOM_CLASSES_TEMPLATE");
            //#########################################################

            _Triggers.Add("IntervaloTempo");
            _Triggers.Add("ExecucaoUnica");
            _Triggers.Add("UnicoDiadoMes");
            _Triggers.Add("DiasdaSemana");
            
        }

        public void VerificarAgendamentos()
        {
            var jobs = GetJobs();
            if (jobs.IsOk)
            {
                if (jobs.Data != null)
                    Verificar(jobs.Data);
                else
                    _logger.LogError($"{DateTime.Now} Erro ao converter o job, job esta nulo, ServiceScheduler:107, Exception:{jobs.Error}");
            }
            else
            {
                _logger.LogInformation($"{DateTime.Now} Erro ao buscar jobs no banco de dados, ServiceScheduler:111, Exception: {jobs.Error}");
            }
        }

        private void Verificar(List<Mdl_Agendamento> jobs)
        {
            _logger.LogInformation($"{DateTime.Now} Verificando os agendamentos para executar.");
            foreach (var job in jobs)
            {
                if (job.REPETIR_RB == "0")//uma vez
                {
                    if (_Triggers != null)
                        if (_ITriggers.ElementAt(_Triggers.IndexOf("ExecucaoUnica")).VerificarAgendamento(job))
                        {
                            ExecutarAsync(job);
                        }
                }
                else if (job.REPETIR_RB == "1")//dias da semana
                {
                    if (_Triggers != null)
                        if (_ITriggers.ElementAt(_Triggers.IndexOf("DiasdaSemana")).VerificarAgendamento(job))
                        {
                            ExecutarAsync(job);
                        }
                }
                else if (job.REPETIR_RB == "2")//dia do mes
                {
                    if (_Triggers != null)
                        if (_ITriggers.ElementAt(_Triggers.IndexOf("UnicoDiadoMes")).VerificarAgendamento(job))
                        {
                            ExecutarAsync(job);
                        }
                }
                else if (job.REPETIR_RB == "3")//intervalo de tempo
                {
                    if (_Triggers != null)
                        if (_ITriggers.ElementAt(_Triggers.IndexOf("IntervaloTempo")).VerificarAgendamento(job))
                        {
                            ExecutarAsync(job);
                        }
                }
            }
        }

        private void ExecutarAsync(Mdl_Agendamento job)
        {
            if (job.ARQUIVO_INTEGRACAO == null)
                _logger.LogInformation($"{DateTime.Now} O campo ARQUIVO_INTEGRACAO esta vazio, ServiceScheduler:158");
            else if (_JobType != null && job.TIPO_AGENDAMENTO != null && job.ID != null)
            {
                Task.Run(() => {
                    var scopedService = scoped.ServiceProvider.GetRequiredService<IEnumerable<IServiceJobs>>();
                    scopedService.ElementAt(_JobType.IndexOf(job.TIPO_AGENDAMENTO)).IntegrarJobAsync(job.ARQUIVO_INTEGRACAO, job.ID,job.TIPO_AGENDAMENTO);
                });
            }
        }

        private ResponseDtoWithData<Mdl_Agendamento> GetJobs()
        {
            var query = "select ID,RESORT,TIPOARQUIVO,ATIVO,TIPO_AGENDAMENTO," +
                "EXECUTAR_EM,DIAS_SEMANA,ARQUIVO_INTEGRACAO," +
                "REPETIR_RB,REPETIR_CADA_HHMM,EXECUTAR_EM_HORA,MESES_DIA,PROXIMA_EXECUCAO,PROXIMA_EXECUCAO_HORA " +
                "from JOBS where ativo = 1";
            try
            {
                using (var connection = _context.CreateConnection())
                {
                    OracleCommand cmd = new OracleCommand(query, connection);
                    connection.Open();
                    cmd.CommandText = query;
                    OracleDataReader dr = cmd.ExecuteReader();
                    var result = connection.Query<Mdl_Agendamento>(query);
                    connection.Close();

                    return new ResponseDtoWithData<Mdl_Agendamento>
                    {
                        IsOk = true,
                        Data = result.ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResponseDtoWithData<Mdl_Agendamento>
                {
                    IsOk = false,
                    Error = ex.Message
                };
            }
        }
    }
}
