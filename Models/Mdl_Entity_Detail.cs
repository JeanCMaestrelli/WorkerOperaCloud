using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Entity_Detail
    {
        public string? ENTITY_NAME { get; set; }
        public string? ATTRIBUTE_CODE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? LANGUAGE_CODE { get; set; }
        public string? ORDER_BY { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? COMMENTS { get; set; }
        public string? DISPLAY_COLOR { get; set; }
        public string? TITLE_SUFFIX { get; set; }
        public string? BUSINESS_TITLE { get; set; }
        public string? CHAIN_CODE { get; set; }
        public string? MASTER_SUB_KEYWORD_YN { get; set; }
        public string? EXTERNAL_ATTRIBUTE_CODES { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
