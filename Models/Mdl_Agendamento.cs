
namespace WorkerOperaCloud.Models
{
    public class Mdl_Agendamento
    {
        public string? ID { get; set; }
        public string? RESORT { get; set; }
        public string? TIPOARQUIVO { get; set; }
        public string? ATIVO { get; set; }
        public string? TIPO_AGENDAMENTO { get; set; }
        public string? EXECUTAR_EM { get; set; }
        public string? DIAS_SEMANA { get; set; }
        public string? ARQUIVO_INTEGRACAO { get; set; }
        public string? REPETIR_RB { get; set; }
        public string? REPETIR_CADA_HHMM { get; set; }
        public string? EXECUTAR_EM_HORA { get; set; }
        public int? MESES_DIA { get; set; }
        public string? PROXIMA_EXECUCAO { get; set; }
        public string? PROXIMA_EXECUCAO_HORA { get; set; }
        public string? IN_EXEC { get; set; }

    }
}
