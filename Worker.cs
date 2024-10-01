using WorkerOperaCloud.Models;
using WorkerOperaCloud.Repository.Jobs;
using WorkerOperaCloud.Services.Interfaces;

namespace WorkerOperaCloud
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScheduler _IServiceScheduler;

        public Worker(ILogger<Worker> logger, IServiceScheduler IServiceScheduler)
        {
            _logger = logger;
            _IServiceScheduler = IServiceScheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"WorkerOperaCloud rodando desde: {DateTime.Now}");
            while (!stoppingToken.IsCancellationRequested)
            {
                _IServiceScheduler.VerificarAgendamentos();
                 await Task.Delay(1000, stoppingToken);
            }
        }
    }
}