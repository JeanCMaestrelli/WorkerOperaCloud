using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Trxs_Codes_Arrangement
    {
        public string? RESORT { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public string? ARRANGEMENT_ID { get; set; }
        public string? ARRANGEMENT_CODE { get; set; }
        public string? TYPE { get; set; }
        public string? ARRANGEMENT_DESC { get; set; }
        public string? ELIGIBLE_YN { get; set; }
        public string? EXPORT_BUCKET_TYPE { get; set; }
        public string? COMP_YN { get; set; }
        public string? INHERIT_AUTH_RATECODE_YN { get; set; }
        public string? ROUTING_PERCENT { get; set; }
        public string? ROUTING_AMOUNT { get; set; }
        public string? ROUTING_COVERS { get; set; }
        public string? DAY1 { get; set; }
        public string? DAY2 { get; set; }
        public string? DAY3 { get; set; }
        public string? DAY4 { get; set; }
        public string? DAY5 { get; set; }
        public string? DAY6 { get; set; }
        public string? DAY7 { get; set; }
        public string? DAILY_YN { get; set; }
        public string? ARR_TAX_TYPE_CODE { get; set; }
        public string? REVENUE_YN { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? BUCKET_VALUE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
