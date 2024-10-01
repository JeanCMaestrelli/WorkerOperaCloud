using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Reservation_Daily_Elements
    {
        public string? RESORT { get; set; }
        public DateTime? RESERVATION_DATE { get; set; }
        public string? RESV_DAILY_EL_SEQ { get; set; }
        public string? RESV_STATUS { get; set; }
        public string? EXTERNAL_REFERENCE { get; set; }
        public string? ROOM_CLASS { get; set; }
        public string? ROOM_CATEGORY { get; set; }
        public string? BOOKED_ROOM_CATEGORY { get; set; }
        public string? ROOM { get; set; }
        public string? CANCELLATION_NO { get; set; }
        public DateTime? CANCELLATION_DATE { get; set; }
        public string? CANCELLATION_CODE { get; set; }
        public string? CANCELLATION_REASON_DESC { get; set; }
        public string? GUARANTEE_CODE { get; set; }
        public string? MARKET_CODE { get; set; }
        public string? ORIGIN_OF_BOOKING { get; set; }
        public string? EXCHANGE_RATE { get; set; }
        public string? ORIGINAL_BASE_RATE { get; set; }
        public string? BASE_RATE_AMOUNT { get; set; }
        public string? RATE_AMOUNT { get; set; }
        public string? ROOM_COST { get; set; }
        public string? QUANTITY { get; set; }
        public string? ADULTS { get; set; }
        public string? CHILDREN { get; set; }
        public string? PHYSICAL_QUANTITY { get; set; }
        public string? ALLOTMENT_HEADER_ID { get; set; }
        public string? DAY_USE_YN { get; set; }
        public string? DUE_OUT_YN { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? INSERT_ACTION_INSTANCE_ID { get; set; }
        public string? DML_SEQ_NO { get; set; }
        public string? EXT_SEQ_NO { get; set; }
        public string? EXT_SEG_NO { get; set; }
        public string? CRIBS { get; set; }
        public string? EXTRA_BEDS { get; set; }
        public string? ALLOTMENT_RECORD_TYPE { get; set; }
        public string? BLOCK_RESORT { get; set; }
        public string? BLOCK_ID { get; set; }
        public string? TURNDOWN_STATUS { get; set; }
        public string? AWD_UPGR_FROM { get; set; }
        public string? AWD_UPGR_TO { get; set; }
        public string? HK_EXPECTED_SERVICE_TIME { get; set; }
        public string? ROOM_INSTRUCTIONS { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
