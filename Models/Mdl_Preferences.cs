using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Preferences
    {
        public string? PREFERENCE { get; set; }
        public string? PREFERENCE_TYPE { get; set; }
        public string? RESORT { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? CRS_PREFERENCE_YN { get; set; }
        public string? ORDER_BY { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? PREFERENCE_SEQ_ID { get; set; }
        public string? CAN_DELETE_YN { get; set; }
        public string? CHAIN_CODE { get; set; }
        public string? SEND_DELETE_REQUEST_YN { get; set; }
        public string? CORPORATE_YN { get; set; }
        public string? OWS_ALLOWED_YN { get; set; }
        public string? AMENITY_YN { get; set; }
        public string? ROOM_ASSIGNMENT_VALUE { get; set; }
        public string? PREFERENCE_ATTRIBUTE { get; set; }
        public string? HOUSEKEEPING_YN { get; set; }
        public string? PREFERENCE_SUB_TYPE { get; set; }
        public string? SOURCE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
