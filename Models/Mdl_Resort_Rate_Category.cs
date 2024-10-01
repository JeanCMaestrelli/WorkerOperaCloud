using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Resort_Rate_Category
    {
        public string? RESORT { get; set; }
        public string? RATE_CATEGORY { get; set; }
        public string? DESCR { get; set; }
        public string? RATE_CLASS { get; set; }
        public string? SELL_SEQUENCE { get; set; }
        public string? RATE_TIER { get; set; }
        public DateTime? BEGIN_DATE { get; set; }
        public DateTime? END_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? DISPLAY_DEFAULT_YN { get; set; }
        public string? DISPLAY_SET { get; set; }
        public string? EXPORT_BUCKET_CODE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
