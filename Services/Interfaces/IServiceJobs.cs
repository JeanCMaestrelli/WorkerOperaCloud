
namespace WorkerOperaCloud.Services.Interfaces
{
    public  interface  IServiceJobs
    {
        void IntegrarJobAsync(string filePath, string id_job, string TIPO_AGENDAMENTO);
    }
}
