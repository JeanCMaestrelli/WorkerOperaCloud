using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Reservation_Summary
    {
        public string? ID { get; set; }
        public string? RESORT { get; set; }
        public string? EVENT_TYPE { get; set; }
        public string? EVENT_ID { get; set; }
        public DateTime? CONSIDERED_DATE { get; set; }
        public string? ROOM_CATEGORY { get; set; }
        public string? ROOM_CLASS { get; set; }
        public string? MARKET_CODE { get; set; }
        public string? SOURCE_CODE { get; set; }
        public string? RATE_CODE { get; set; }
        public string? REGION_CODE { get; set; }
        public string? GROUP_ID { get; set; }
        public string? RESV_TYPE { get; set; }
        public string? RESV_INV_TYPE { get; set; }
        public string? PSUEDO_ROOM_YN { get; set; }
        public string? ARR_ROOMS { get; set; }
        public string? ADULTS { get; set; }
        public string? CHILDREN { get; set; }
        public string? DEP_ROOMS { get; set; }
        public string? NO_ROOMS { get; set; }
        public string? GROSS_RATE { get; set; }
        public string? NET_ROOM_REVENUE { get; set; }
        public string? EXTRA_REVENUE { get; set; }
        public string? OO_ROOMS { get; set; }
        public string? OS_ROOMS { get; set; }
        public string? REMAINING_BLOCK_ROOMS { get; set; }
        public string? PICKEDUP_BLOCK_ROOMS { get; set; }
        public string? SINGLE_OCCUPANCY { get; set; }
        public string? MULTIPLE_OCCUPANCY { get; set; }
        public string? BLOCK_STATUS { get; set; }
        public string? ARR_PERSONS { get; set; }
        public string? DEP_PERSONS { get; set; }
        public string? WL_ROOMS { get; set; }
        public string? WL_PERSONS { get; set; }
        public string? DAY_USE_ROOMS { get; set; }
        public string? DAY_USE_PERSONS { get; set; }
        public string? BOOKING_STATUS { get; set; }
        public string? RESV_STATUS { get; set; }
        public string? DAY_USE_YN { get; set; }
        public string? CHANNEL { get; set; }
        public string? COUNTRY { get; set; }
        public string? NATIONALITY { get; set; }
        public string? CRIBS { get; set; }
        public string? EXTRA_BEDS { get; set; }
        public string? ADULTS_TAX_FREE { get; set; }
        public string? CHILDREN_TAX_FREE { get; set; }
        public string? RATE_CATEGORY { get; set; }
        public string? RATE_CLASS { get; set; }
        public string? ROOM_REVENUE { get; set; }
        public string? FOOD_REVENUE { get; set; }
        public string? OTHER_REVENUE { get; set; }
        public string? TOTAL_REVENUE { get; set; }
        public string? NON_REVENUE { get; set; }
        public string? ALLOTMENT_HEADER_ID { get; set; }
        public string? ROOM_REVENUE_TAX { get; set; }
        public string? FOOD_REVENUE_TAX { get; set; }
        public string? OTHER_REVENUE_TAX { get; set; }
        public string? TOTAL_REVENUE_TAX { get; set; }
        public string? NON_REVENUE_TAX { get; set; }
        public string? CITY { get; set; }
        public string? ZIP_CODE { get; set; }
        public string? DISTRICT { get; set; }
        public string? STATE { get; set; }
        public string? CHILDREN1 { get; set; }
        public string? CHILDREN2 { get; set; }
        public string? CHILDREN3 { get; set; }
        public string? CHILDREN4 { get; set; }
        public string? CHILDREN5 { get; set; }
        public string? OWNER_FF_FLAG { get; set; }
        public string? OWNER_RENTAL_FLAG { get; set; }
        public string? FC_GROSS_RATE { get; set; }
        public string? FC_NET_ROOM_REVENUE { get; set; }
        public string? FC_EXTRA_REVENUE { get; set; }
        public string? FC_ROOM_REVENUE { get; set; }
        public string? FC_FOOD_REVENUE { get; set; }
        public string? FC_OTHER_REVENUE { get; set; }
        public string? FC_TOTAL_REVENUE { get; set; }
        public string? FC_NON_REVENUE { get; set; }
        public string? FC_ROOM_REVENUE_TAX { get; set; }
        public string? FC_FOOD_REVENUE_TAX { get; set; }
        public string? FC_OTHER_REVENUE_TAX { get; set; }
        public string? FC_TOTAL_REVENUE_TAX { get; set; }
        public string? FC_NON_REVENUE_TAX { get; set; }
        public string? CURRENCY_CODE { get; set; }
        public DateTime? EXCHANGE_DATE { get; set; }
        public DateTime? UPDATE_BUSINESS_DATE { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? CENTRAL_CURRENCY_CODE { get; set; }
        public string? CENTRAL_EXCHANGE_RATE { get; set; }
        public DateTime? TRUNC_BEGIN_DATE { get; set; }
        public DateTime? TRUNC_END_DATE { get; set; }
        public DateTime? BUSINESS_DATE_CREATED { get; set; }
        public string? RES_INSERT_SOURCE { get; set; }
        public string? PARENT_COMPANY_ID { get; set; }
        public string? AGENT_ID { get; set; }
        public string? GENDER { get; set; }
        public string? VIP_STATUS { get; set; }
        public string? QUANTITY { get; set; }
        public string? TURNDOWN_STATUS { get; set; }
        public string? BOOKED_ROOM_CATEGORY { get; set; }
        public string? SOURCE_PROF_ID { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
