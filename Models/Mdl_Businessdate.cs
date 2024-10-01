
namespace WorkerOperaCloud.Models
{
    public class Mdl_Businessdate : Mdl_REN
    {
        public string? WEATHER { get; set; } = "";
        public string? RESORT { get; set; } = "";
        public DateTime? BUSINESS_DATE { get; set; }
        public string? STATE { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; } = "";
        public string? MANDATORY_PROCS_PROGRESS { get; set; } = "";
        public string? NA_TRAN_ACTION_ID { get; set; } = "";
        public string? LEDGERS_BALANCED_YN { get; set; } = "";
        public string? LEDGERS_FIXED_YN { get; set; } = "";
        public DateTime? LEDGERS_FIXED_DATE { get; set; }
        public DateTime? QRUSH_SMS_SENT_ON { get; set; }
        public string? QRUSH_SMS_SENT_BY { get; set; } = "";
        public string? PMS_ACTIVE_YN { get; set; } = "";
        public string? CI_CHECKS_RECONCILIATION_FLAG { get; set; } = "";

    }
}
