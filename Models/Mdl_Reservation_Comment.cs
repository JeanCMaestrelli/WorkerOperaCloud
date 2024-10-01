using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Reservation_Comment
    {
        public string? RESORT { get; set; }
        public string? RESV_COMMENT_ID { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? RESV_NAME_ID { get; set; }
        public string? COMMENT_TYPE { get; set; }
        public string? COMMENTS { get; set; }
        public string? INSERT_ACTION_INSTANCE_ID { get; set; }
        public string? DML_SEQ_NO { get; set; }
        public string? NOTE_CODE { get; set; }
        public string? NOTE_RESORT { get; set; }
        public string? INTERNAL_YN { get; set; }
        public string? EXTERNAL_COMMENT_ID { get; set; }
        public string? USER_MODIFIABLE_YN { get; set; }
        public string? NOTE_TITLE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
