using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Articles_Codes
    {
        public string? RESORT { get; set; } = "";
        public string? ARTICLE_ID { get; set; } = "";
        public string? ARTICLE_CODE { get; set; } = "";
        public string? TRX_CODE { get; set; } = "";
        public string? DESCRIPTION { get; set; } = "";
        public string? DEFAULT_PRICE { get; set; } = "";
        public DateTime? INACTIVE_DATE { get; set; } = null;
        public DateTime? INSERT_DATE { get; set; } = null;
        public string? INSERT_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; } = null;
        public string? UPDATE_USER { get; set; } = "";
        public string? ORDER_BY { get; set; } = "";
        public string? POSTIT_YN { get; set; } = "";
        public string? POSTIT_COLOUR { get; set; } = "";
        public string? UPC_CODE { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; }=null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
