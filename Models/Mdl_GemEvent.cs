using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_GemEvent
    {
        public string? EVENT_ID { get; set; }
        public string? MASTER_EVENT_ID { get; set; }
        public string? RESORT { get; set; }
        public string? BOOK_ID { get; set; }
        public string? EV_TYPE { get; set; }
        public string? EV_NAME { get; set; }
        public string? EV_STATUS { get; set; }
        public string? PKG_ID { get; set; }
        public string? WAITLIST_YN { get; set; }
        public string? TURNTO_STATUS { get; set; }
        public string? GROUP_ID { get; set; }
        public string? ATTENDEES { get; set; }
        public string? ACTUAL_ATTENDEES { get; set; }
        public string? ACTUAL_MANUAL { get; set; }
        public DateTime? START_DATE { get; set; }
        public DateTime? END_DATE { get; set; }
        public DateTime? BLOCKSTART { get; set; }
        public DateTime? BLOCKEND { get; set; }
        public string? GUARANTEED { get; set; }
        public string? DOORCARD { get; set; }
        public string? ROOM { get; set; }
        public string? ROOM_SETUP { get; set; }
        public string? SETUP_TIME { get; set; }
        public string? SETDOWN_TIME { get; set; }
        public string? TRACECODE { get; set; }
        public string? DONT_MOVE_YN { get; set; }
        public string? PROBLEM_YN { get; set; }
        public string? WL_IGNORE_YN { get; set; }
        public string? MASTER_YN { get; set; }
        public string? EVENT_LINK_ID { get; set; }
        public string? INSPECTED_YN { get; set; }
        public DateTime? INSPECTED_DATE { get; set; }
        public string? INSPECTED_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public string? EV_RESORT { get; set; }
        public string? DOORCARD_YN { get; set; }
        public string? EVENT_LINK_TYPE { get; set; }
        public string? PKG_EXP_ATTENDEES { get; set; }
        public string? PKG_GUA_ATTENDEES { get; set; }
        public string? PKG_ACT_ATTENDEES { get; set; }
        public string? V6_EVENT_ID { get; set; }
        public string? FORECAST_REVENUE_ONLY_YN { get; set; }
        public string? EXCLUDE_FROM_FORECAST_YN { get; set; }
        public string? PKG_NAME { get; set; }
        public string? PKG_LINK { get; set; }
        public string? PKG_EV_ID { get; set; }
        public string? SET_ATTENDEES { get; set; }
        public string? FBA_ID { get; set; }
        public string? SELECT_RATECODE_IN_CENTRAL_YN { get; set; }
        public string? DETAILED_POSTING_YN { get; set; }
        public string? ALLOW_REGISTRY_YN { get; set; }
        public string? ORIG_EVENT_ID { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
