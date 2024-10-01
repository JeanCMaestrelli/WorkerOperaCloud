using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_EventReservation
    {
        public string? RESORT { get; set; } = "";
        public string? RESV_NAME_ID { get; set; } = "";
        public string? EVENT_ID { get; set; } = "";
        public DateTime? BEGIN_DATE { get; set; }
        public DateTime? END_DATE { get; set; }
        public string? BOOK_ID { get; set; } = "";
        public string? PKG_ID { get; set; } = "";
        public string? ROOM { get; set; } = "";
        public string? ROOM_CLASS { get; set; } = "";
        public string? ROOM_CATEGORY { get; set; } = "";
        public string? SETUP_CODE { get; set; } = "";
        public string? SETUP_DESC { get; set; } = "";
        public string? SETUP_TIME { get; set; } = "";
        public string? SETDOWN_TIME { get; set; } = "";
        public string? ATTENDEES { get; set; } = "";
        public string? REVENUE_TYPE { get; set; } = "";
        public string? RATECODE { get; set; } = "";
        public string? FIXED_RATE_YN { get; set; } = "";
        public string? HOURLY_YN { get; set; } = "";
        public string? RATE_AMOUNT { get; set; } = "";
        public string? SHARED_YN { get; set; } = "";
        public string? DONT_MOVE_YN { get; set; } = "";
        public string? NOISY_YN { get; set; } = "";
        public string? DISCOUNT_AMOUNT { get; set; } = "";
        public string? DISCOUNT_PERCENTAGE { get; set; } = "";
        public string? DISCOUNT_REASON_CODE { get; set; } = "";
        public string? INSERT_USER { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; }
        public string? ROOM_RESORT { get; set; } = "";
        public string? RATE_TYPE { get; set; } = "";
        public string? MINIMUM_REVENUE_YN { get; set; } = "";
        public string? MINIMUM_REVENUE { get; set; } = "";
        public string? RENTAL_AMOUNT { get; set; } = "";
        public string? INC_SETUP_IN_HOURLY_RATE_YN { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
