using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Memberships
    {
        public string? MEMBERSHIP_ID { get; set; }
        public string? NAME_ID { get; set; }
        public string? MEMBERSHIP_TYPE { get; set; }
        public string? MEMBERSHIP_CARD_NO { get; set; }
        public string? MEMBERSHIP_LEVEL { get; set; }
        public string? NAME_ON_CARD { get; set; }
        public string? COMMENTS { get; set; }
        public DateTime? JOINED_DATE { get; set; }
        public DateTime? EXPIRATION_DATE { get; set; }
        public string? CREDIT_LIMIT { get; set; }
        public string? PRIMARY_AIRLINE_PARTNER { get; set; }
        public string? POINT_INDICATOR { get; set; }
        public string? CURRENT_POINTS { get; set; }
        public string? MEMBER_INDICATOR { get; set; }
        public string? MEMBER_SUBTYPE { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? ORDER_BY { get; set; }
        public DateTime? PROCESS_EXPIRATION_DATE { get; set; }
        public string? ENROLLMENT_CODE { get; set; }
        public string? GRACE_PERIOD_INDICATOR { get; set; }
        public string? MEMBERSHIP_STATUS { get; set; }
        public string? TRACK_DATA { get; set; }
        public string? EARNING_PREFERENCE { get; set; }
        public string? CHAIN_CODE { get; set; }
        public string? ENROLLMENT_SOURCE { get; set; }
        public string? ENROLLED_AT { get; set; }
        public string? RANK_VALUE { get; set; }
        public string? DEVICE_CODE { get; set; }
        public DateTime? DEVICE_DISABLE_DATE { get; set; }
        public string? PARTNER_MEMBERSHIP_ID { get; set; }
        public DateTime? MBRPREF_CHANGED_DATE { get; set; }
        public string? EXCLUDE_FROM_BATCH { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
