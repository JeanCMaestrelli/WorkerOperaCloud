using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Postal_Codes_Chain
    {
        public string? CHAIN_CODE { get; set; }
        public string? SEQ { get; set; }
        public string? COUNTRY_CODE { get; set; }
        public string? STATE_CODE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? REGION_CODE { get; set; }
        public string? POSTAL_CODE_FROM { get; set; }
        public string? POSTAL_CODE_TO { get; set; }
        public string? FISCAL_REGION_CODE { get; set; }
        public string? CITY { get; set; }
        public string? SCITY { get; set; }
        public string? DISTRICT { get; set; }
        public string? DELETED_YN { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? TERRITORY_CODE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
