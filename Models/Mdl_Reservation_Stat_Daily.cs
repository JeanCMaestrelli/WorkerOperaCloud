﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Reservation_Stat_Daily
    {
        public string? RESORT { get; set; }
        public DateTime? BUSINESS_DATE { get; set; }
        public string? NAME_ID { get; set; }
        public string? RATE_CODE { get; set; }
        public string? SOURCE_CODE { get; set; }
        public string? MARKET_CODE { get; set; }
        public string? ROOM_CATEGORY { get; set; }
        public string? COMPANY_ID { get; set; }
        public string? AGENT_ID { get; set; }
        public string? GROUP_ID { get; set; }
        public string? SOURCE_PROF_ID { get; set; }
        public string? RESV_STATUS { get; set; }
        public DateTime? TRUNC_BEGIN_DATE { get; set; }
        public DateTime? TRUNC_END_DATE { get; set; }
        public string? RESV_NAME_ID { get; set; }
        public string? QUANTITY { get; set; }
        public string? PHYSICAL_QUANTITY { get; set; }
        public string? DUE_OUT_YN { get; set; }
        public string? ROOM_RESV_STATUS { get; set; }
        public string? ADULTS { get; set; }
        public string? CHILDREN { get; set; }
        public string? ROOM_ADULTS { get; set; }
        public string? ROOM_CHILDREN { get; set; }
        public string? PRIMARY_YN { get; set; }
        public string? ALLOTMENT_HEADER_ID { get; set; }
        public string? ROOM_REVENUE { get; set; }
        public string? FOOD_REVENUE { get; set; }
        public string? PACKAGE_ROOM_REVENUE { get; set; }
        public string? PACKAGE_FOOD_REVENUE { get; set; }
        public string? TOTAL_ROOM_TAX { get; set; }
        public string? TOTAL_FOOD_TAX { get; set; }
        public string? TOTAL_PACKAGE_REVENUE { get; set; }
        public string? TOTAL_REVENUE { get; set; }
        public string? TOTAL_TAX { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public DateTime? ORIGINAL_END_DATE { get; set; }
        public string? WALKIN_YN { get; set; }
        public DateTime? RESERVATION_DATE { get; set; }
        public string? ROOM_CLASS { get; set; }
        public string? VIP_STATUS { get; set; }
        public DateTime? CANCELLATION_DATE { get; set; }
        public DateTime? BIRTH_DATE { get; set; }
        public string? ROOM { get; set; }
        public string? MEMBERSHIP_ID { get; set; }
        public string? CONTACT_ID { get; set; }
        public string? RATE_CATEGORY { get; set; }
        public string? MARKET_MAIN_GROUP { get; set; }
        public string? SOURCE_MAIN_GROUP { get; set; }
        public string? CHANNEL { get; set; }
        public string? COUNTRY { get; set; }
        public string? REGION_CODE { get; set; }
        public string? NATIONALITY { get; set; }
        public string? PSUEDO_ROOM_YN { get; set; }
        public string? ADULTS_TAX_FREE { get; set; }
        public string? CHILDREN_TAX_FREE { get; set; }
        public string? STAY_ROOMS { get; set; }
        public string? STAY_PERSONS { get; set; }
        public string? STAY_ADULTS { get; set; }
        public string? STAY_CHILDREN { get; set; }
        public string? ARR_ROOMS { get; set; }
        public string? ARR_PERSONS { get; set; }
        public string? DEP_ROOMS { get; set; }
        public string? DEP_PERSONS { get; set; }
        public string? DAY_USE_ROOMS { get; set; }
        public string? DAY_USE_PERSONS { get; set; }
        public string? CANCELLED_ROOMS { get; set; }
        public string? CANCELLED_PERSONS { get; set; }
        public string? NO_SHOW_ROOMS { get; set; }
        public string? NO_SHOW_PERSONS { get; set; }
        public string? SINGLE_OCCUPANCY { get; set; }
        public string? MULTIPLE_OCCUPANCY { get; set; }
        public string? CRIBS { get; set; }
        public string? EXTRA_BEDS { get; set; }
        public string? OTHER_REVENUE { get; set; }
        public string? PACKAGE_OTHER_REVENUE { get; set; }
        public string? TOTAL_OTHER_TAX { get; set; }
        public string? COUNTRY_MAIN_GROUP { get; set; }
        public string? STATE { get; set; }
        public string? FISCAL_REGION_CODE { get; set; }
        public string? NON_REVENUE { get; set; }
        public string? PACKAGE_NON_REVENUE { get; set; }
        public string? TOTAL_NON_REVENUE_TAX { get; set; }
        public string? PR_ROOM_REVENUE { get; set; }
        public string? PR_FOOD_REVENUE { get; set; }
        public string? PR_PACKAGE_ROOM_REVENUE { get; set; }
        public string? PR_PACKAGE_FOOD_REVENUE { get; set; }
        public string? PR_TOTAL_ROOM_TAX { get; set; }
        public string? PR_TOTAL_FOOD_TAX { get; set; }
        public string? PR_TOTAL_PACKAGE_REVENUE { get; set; }
        public string? PR_TOTAL_REVENUE { get; set; }
        public string? PR_TOTAL_TAX { get; set; }
        public string? PR_OTHER_REVENUE { get; set; }
        public string? PR_PACKAGE_OTHER_REVENUE { get; set; }
        public string? PR_TOTAL_OTHER_TAX { get; set; }
        public string? PR_NON_REVENUE { get; set; }
        public string? PR_PACKAGE_NON_REVENUE { get; set; }
        public string? PR_TOTAL_NON_REVENUE_TAX { get; set; }
        public string? NIGHTS { get; set; }
        public string? NO_OF_STAYS { get; set; }
        public string? RESERVATION_NIGHTS { get; set; }
        public string? RESERVATION_ARRIVALS { get; set; }
        public string? RESERVATION_NO_OF_STAYS { get; set; }
        public string? DAY_USE_RESERVATIONS { get; set; }
        public string? CANCELLED_RESERVATIONS { get; set; }
        public string? NO_SHOW_RESERVATIONS { get; set; }
        public string? CITY { get; set; }
        public string? ZIP_CODE { get; set; }
        public string? DISTRICT { get; set; }
        public string? CASH_ROOM_NTS { get; set; }
        public string? COMP_ROOM_NTS { get; set; }
        public string? CASH_ROOM_REVENUE { get; set; }
        public string? COMP_ROOM_REVENUE { get; set; }
        public string? CHILDREN1 { get; set; }
        public string? CHILDREN2 { get; set; }
        public string? CHILDREN3 { get; set; }
        public string? CHILDREN4 { get; set; }
        public string? CHILDREN5 { get; set; }
        public string? FF_MEMBERSHIP_TYPE { get; set; }
        public string? FG_MEMBERSHIP_TYPE { get; set; }
        public string? PROMOTION_CODE { get; set; }
        public string? RES_INSERT_SOURCE { get; set; }
        public string? RES_INSERT_SOURCE_TYPE { get; set; }
        public string? OWNER_FF_FLAG { get; set; }
        public string? OWNER_RENTAL_FLAG { get; set; }
        public string? CENTRAL_CURRENCY_CODE { get; set; }
        public string? CENTRAL_EXCHANGE_RATE { get; set; }
        public string? BOOKED_ROOM_CATEGORY { get; set; }
        public DateTime? BUSINESS_DATE_CREATED { get; set; }
        public string? RATE_AMOUNT { get; set; }
        public string? PARENT_COMPANY_ID { get; set; }
        public string? HOUSE_USE_YN { get; set; }
        public string? COMPLIMENTARY_YN { get; set; }
        public string? BI_RESV_NAME_ID { get; set; }
        public string? ADV_FOOD_REVENUE { get; set; }
        public string? ADV_NON_REVENUE { get; set; }
        public string? ADV_OTHER_REVENUE { get; set; }
        public string? ADV_ROOM_REVENUE { get; set; }
        public string? ADV_TOTAL_FOOD_TAX { get; set; }
        public string? ADV_TOTAL_NON_REVENUE_TAX { get; set; }
        public string? ADV_TOTAL_OTHER_TAX { get; set; }
        public string? ADV_TOTAL_REVENUE { get; set; }
        public string? ADV_TOTAL_ROOM_TAX { get; set; }
        public string? ADV_TOTAL_TAX { get; set; }
        public string? PR_ADV_FOOD_REVENUE { get; set; }
        public string? PR_ADV_NON_REVENUE { get; set; }
        public string? PR_ADV_OTHER_REVENUE { get; set; }
        public string? PR_ADV_ROOM_REVENUE { get; set; }
        public string? PR_ADV_TOTAL_FOOD_TAX { get; set; }
        public string? PR_ADV_TOTAL_NON_REVENUE_TAX { get; set; }
        public string? PR_ADV_TOTAL_OTHER_TAX { get; set; }
        public string? PR_ADV_TOTAL_REVENUE { get; set; }
        public string? PR_ADV_TOTAL_ROOM_TAX { get; set; }
        public string? PR_ADV_TOTAL_TAX { get; set; }
        public string? UPSOLD_REVENUE { get; set; }
        public string? FLGD_ROOM_REVENUE { get; set; }
        public string? FLGD_FOOD_REVENUE { get; set; }
        public string? FLGD_NON_REVENUE { get; set; }
        public string? FLGD_OTHER_REVENUE { get; set; }
        public string? FLGD_TOTAL_ROOM_TAX { get; set; }
        public string? FLGD_TOTAL_FOOD_TAX { get; set; }
        public string? FLGD_TOTAL_NON_REVENUE_TAX { get; set; }
        public string? FLGD_TOTAL_OTHER_TAX { get; set; }
        public string? FLGD_TOTAL_REVENUE { get; set; }
        public string? FLGD_TOTAL_TAX { get; set; }
        public string? CONTACT_YN { get; set; }
        public string? EXTENDED_STAY_TIER { get; set; }
        public string? RS_ADV_TOTAL_REVENUE { get; set; }
        public string? RS_ADV_ROOM_REVENUE { get; set; }
        public string? RS_ADV_FOOD_REVENUE { get; set; }
        public string? RS_ADV_OTHER_REVENUE { get; set; }
        public string? RS_ADV_NON_REVENUE { get; set; }
        public string? RS_ADV_TOTAL_TAX { get; set; }
        public string? RS_ADV_ROOM_TAX { get; set; }
        public string? RS_ADV_FOOD_TAX { get; set; }
        public string? RS_ADV_OTHER_TAX { get; set; }
        public string? RS_ADV_NON_REVENUE_TAX { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
