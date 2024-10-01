using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Resort_Origins_Of_Booking
    {
        public string? RESORT { get; set; }
        public string? SOURCE_CODE { get; set; }
        public string? PARENT_SOURCE_CODE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? SELL_SEQUENCE { get; set; }
        public string? SC_ORDERBY { get; set; }
        public string? INTERNET_SALES_YN { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
