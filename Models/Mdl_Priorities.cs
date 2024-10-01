using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Priorities
    {
        public string? RESORT { get; set; }
        public string? PRIORITY_CODE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? PRIORITY_SEQ_NUMBER { get; set; }
        public string? COLOR_CODE { get; set; }
        public string? COLOR_CODE_DESCRIPTION { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
