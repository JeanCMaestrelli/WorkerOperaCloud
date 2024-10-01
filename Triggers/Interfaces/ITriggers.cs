
using WorkerOperaCloud.Models;

namespace WorkerOperaCloud.Triggers.Interfaces
{
    public interface ITriggers
    {
        public bool VerificarAgendamento(Mdl_Agendamento job);
    }
}
