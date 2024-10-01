using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Reservation_Name
    {
        public DateTime? ACTUAL_CHECK_IN_DATE { get; set; }
        public DateTime? ACTUAL_CHECK_OUT_DATE { get; set; }
        public string? ADDRESSEE_NAME_ID { get; set; } = "";
        public string? ADDRESS_ID { get; set; } = "";
        public string? AMENITY_ELIGIBLE_YN { get; set; } = "";
        public string? AMENITY_LEVEL_CODE { get; set; } = "";
        public string? AMOUNT_PERCENT { get; set; } = "";
        public string? APPROVAL_AMOUNT_CALC_METHOD { get; set; } = "";
        public string? ARRIVAL_CARRIER_CODE { get; set; } = "";
        public string? ARRIVAL_COMMENTS { get; set; } = "";
        public DateTime? ARRIVAL_DATE_TIME { get; set; }
        public DateTime? ARRIVAL_ESTIMATE_TIME { get; set; }
        public string? ARRIVAL_STATION_CODE { get; set; } = "";
        public string? ARRIVAL_TRANPORTATION_YN { get; set; } = "";
        public string? ARRIVAL_TRANSPORT_CODE { get; set; } = "";
        public string? ARRIVAL_TRANSPORT_TYPE { get; set; } = "";
        public string? ASB_PRORATED_YN { get; set; } = "";
        public string? AUTHORIZED_BY { get; set; } = "";
        public string? AUTHORIZER_ID { get; set; } = "";
        public string? AUTO_CHECKIN_YN { get; set; } = "";
        public string? AUTO_SETTLE_DAYS { get; set; } = "";
        public string? AUTO_SETTLE_YN { get; set; } = "";
        public string? AWARD_MEMBERSHIP_ID { get; set; } = "";
        public string? BASE_RATE_CODE { get; set; } = "";
        public string? BASE_RATE_CURRENCY_CODE { get; set; } = "";
        public DateTime? BEGIN_DATE { get; set; }
        public DateTime? BEGIN_SYSTEM_DATE_TIME { get; set; }
        public string? BILLING_CONTACT_ID { get; set; } = "";
        public string? BONUS_CHECK_ID { get; set; } = "";
        public DateTime? BUSINESS_DATE_CREATED { get; set; }
        public DateTime? CANCELLATION_DATE { get; set; }
        public string? CANCELLATION_NO { get; set; } = "";
        public string? CANCELLATION_REASON_CODE { get; set; } = "";
        public string? CANCELLATION_REASON_DESC { get; set; } = "";
        public string? CHANNEL { get; set; } = "";
        public string? CHECKIN_DURATION { get; set; } = "";
        public string? COMMISSION_CODE { get; set; } = "";
        public string? COMMISSION_HOLD_CODE { get; set; } = "";
        public string? COMMISSION_PAID { get; set; } = "";
        public string? COMMISSION_PAYOUT_TO { get; set; } = "";
        public string? COMP_TYPE_CODE { get; set; } = "";
        public string? CONFIRMATION_LEG_NO { get; set; } = "";
        public string? CONFIRMATION_NO { get; set; } = "";
        public string? CONSUMER_YN { get; set; } = "";
        public string? CONTACT_NAME_ID { get; set; } = "";
        public string? CREDIT_CARD_ID { get; set; } = "";
        public string? CREDIT_LIMIT { get; set; } = "";
        public string? CUSTOM_REFERENCE { get; set; } = "";
        public DateTime? DATE_OF_ARRIVAL_IN_COUNTRY { get; set; }
        public string? DELETED_FLAG { get; set; } = "";
        public string? DEPARTURE_CARRIER_CODE { get; set; } = "";
        public string? DEPARTURE_COMMENTS { get; set; } = "";
        public DateTime? DEPARTURE_DATE_TIME { get; set; }
        public DateTime? DEPARTURE_ESTIMATE_TIME { get; set; }
        public string? DEPARTURE_STATION_CODE { get; set; } = "";
        public string? DEPARTURE_TRANSPORTATION_YN { get; set; } = "";
        public string? DEPARTURE_TRANSPORT_CODE { get; set; } = "";
        public string? DEPARTURE_TRANSPORT_TYPE { get; set; } = "";
        public string? DIRECT_BILL_VERIFY_RESPONSE { get; set; } = "";
        public string? DISCOUNT_AMT { get; set; } = "";
        public string? DISCOUNT_PRCNT { get; set; } = "";
        public string? DISCOUNT_REASON_CODE { get; set; } = "";
        public string? DISPLAY_COLOR { get; set; } = "";
        public string? DML_SEQ_NO { get; set; } = "";
        public string? DO_NOT_MOVE_ROOM { get; set; } = "";
        public string? ELIGIBLE_FOR_UPGRADE_YN { get; set; } = "";
        public string? EMAIL_ADDRESS { get; set; } = "";
        public string? EMAIL_FOLIO_YN { get; set; } = "";
        public string? EMAIL_ID { get; set; } = "";
        public string? EMAIL_YN { get; set; } = "";
        public DateTime? END_DATE { get; set; }
        public DateTime? ENTRY_DATE { get; set; }
        public string? ENTRY_POINT { get; set; } = "";
        public string? ESIGNED_REG_CARD_NAME { get; set; } = "";
        public string? EVENT_ID { get; set; } = "";
        public string? EXP_CHECKINRES_ID { get; set; } = "";
        public string? EXTENSION_ID { get; set; } = "";
        public string? EXTERNAL_EFOLIO_YN { get; set; } = "";
        public string? EXTERNAL_REFERENCE { get; set; } = "";
        public string? FAX_ID { get; set; } = "";
        public string? FAX_YN { get; set; } = "";
        public string? FINANCIALLY_RESPONSIBLE_YN { get; set; } = "";
        public DateTime? FOLIO_CLOSE_DATE { get; set; }
        public string? FOLIO_TEXT1 { get; set; } = "";
        public string? FOLIO_TEXT2 { get; set; } = "";
        public string? GUARANTEE_CODE { get; set; } = "";
        public string? GUARANTEE_CODE_PRE_CI { get; set; } = "";
        public string? GUEST_FIRST_NAME { get; set; } = "";
        public string? GUEST_FIRST_NAME_SDX { get; set; } = "";
        public string? GUEST_LAST_NAME { get; set; } = "";
        public string? GUEST_LAST_NAME_SDX { get; set; } = "";
        public string? GUEST_SIGNATURE { get; set; } = "";
        public string? GUEST_STATUS { get; set; } = "";
        public string? GUEST_TYPE { get; set; } = "";
        public string? HK_EXPECTED_SERVICE_TIME { get; set; } = "";
        public string? HURDLE { get; set; } = "";
        public string? HURDLE_OVERRIDE { get; set; } = "";
        public string? INSERT_ACTION_INSTANCE_ID { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; } = "";
        public string? INTERMEDIARY_YN { get; set; } = "";
        public DateTime? KEY_VALID_UNTIL { get; set; }
        public string? LAST_ONLINE_PRINT_SEQ { get; set; } = "";
        public DateTime? LAST_PERIODIC_FOLIO_DATE { get; set; }
        public DateTime? LAST_SETTLE_DATE { get; set; }
        public string? LOCAL_BASE_RATE_AMOUNT { get; set; } = "";
        public string? MAIL_YN { get; set; } = "";
        public string? MANUAL_CHECKOUT_STATUS { get; set; } = "";
        public string? MASTER_SHARE { get; set; } = "";
        public string? MEMBERSHIP_ID { get; set; } = "";
        public DateTime? MOBILE_ACTION_ALERT_ISSUED { get; set; }
        public string? MOBILE_AUDIO_KEY_YN { get; set; } = "";
        public string? MOBILE_CHECKIN_ALLOWED_YN { get; set; } = "";
        public string? MOBILE_CHKOUT_ALLOWED { get; set; } = "";
        public string? MOBILE_PREFERRED_CURRENCY { get; set; } = "";
        public string? MOBILE_VIEW_FOLIO_ALLOWED { get; set; } = "";
        public string? NAME_ID { get; set; } = "";
        public string? NAME_TAX_TYPE { get; set; } = "";
        public string? NAME_USAGE_TYPE { get; set; } = "";
        public string? NEXT_DESTINATION { get; set; } = "";
        public string? OPERA_ESIGNED_REG_CARD_YN { get; set; } = "";
        public string? OPT_IN_BATCH_FOL_YN { get; set; } = "";
        public DateTime? ORIGINAL_BEGIN_DATE { get; set; }
        public DateTime? ORIGINAL_END_DATE { get; set; }
        public string? OWNER_FF_FLAG { get; set; } = "";
        public string? PARENT_RESV_NAME_ID { get; set; } = "";
        public string? PARTY_CODE { get; set; } = "";
        public string? PAYMENT_METHOD { get; set; } = "";
        public string? PERIODIC_FOLIO_FREQ { get; set; } = "";
        public string? PHONE_DISPLAY_NAME_YN { get; set; } = "";
        public string? PHONE_ID { get; set; } = "";
        public string? POST_CHARGING_YN { get; set; } = "";
        public string? POST_CO_FLAG { get; set; } = "";
        public string? POSTING_ALLOWED_YN { get; set; } = "";
        public DateTime? PRE_ARR_REVIEWED_DT { get; set; }
        public string? PRE_ARR_REVIEWED_USER { get; set; } = "";
        public string? PRE_CHARGING_YN { get; set; } = "";
        public string? PRE_REGISTERED_YN { get; set; } = "";
        public string? PRINT_RATE_YN { get; set; } = "";
        public string? PSEUDO_MEM_TOTAL_POINTS { get; set; } = "";
        public string? PSEUDO_MEM_TYPE { get; set; } = "";
        public DateTime? PURGE_DATE { get; set; }
        public string? PURPOSE_OF_STAY { get; set; } = "";
        public string? QUOTE_ID { get; set; } = "";
        public string? RATEABLE_VALUE { get; set; } = "";
        public string? REGISTRATION_CARD_NO { get; set; } = "";
        public DateTime? REINSTATE_DATE { get; set; }
        public string? REPORT_ID { get; set; } = "";
        public string? RES_INSERT_SOURCE { get; set; } = "";
        public string? RES_INSERT_SOURCE_TYPE { get; set; } = "";
        public string? RESORT { get; set; } = "";
        public string? RESTRICTION_OVERRIDE { get; set; } = "";
        public string? RESV_CONTACT_ID { get; set; } = "";
        public string? RESV_GUID { get; set; } = "";
        public string? RESV_NAME_ID { get; set; } = "";
        public string? RESV_NO { get; set; } = "";
        public string? RESV_STATUS { get; set; } = "";
        public string? REVENUE_TYPE_CODE { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; }
        public DateTime? RNA_UPDATEDATE { get; set; }
        public string? ROOM_FEATURES { get; set; } = "";
        public string? ROOM_INSTRUCTIONS { get; set; } = "";
        public string? ROOM_SERVICE_TIME { get; set; } = "";
        public string? SCHEDULE_CHECKOUT_YN { get; set; } = "";
        public string? SGUEST_FIRSTNAME { get; set; } = "";
        public string? SGUEST_NAME { get; set; } = "";
        public string? SHARE_SEQ_NO { get; set; } = "";
        public string? SPG_DISCLOSE_ROOM_TYPE_YN { get; set; } = "";
        public string? SPG_SUITE_NIGHT_AWARD_STATUS { get; set; } = "";
        public string? SPG_UPGRADE_CONFIRMED_ROOMTYPE { get; set; } = "";
        public string? SPG_UPGRADE_REASON_CODE { get; set; } = "";
        public string? SPLIT_FROM_RESV_NAME_ID { get; set; } = "";
        public string? STATISTICAL_RATE_TIER { get; set; } = "";
        public string? STATISTICAL_ROOM_TYPE { get; set; } = "";
        public string? SUPER_SEARCH_INDEX_TEXT { get; set; } = "";
        public string? TAX_EXEMPT_NO { get; set; } = "";
        public string? TAX_NO_OF_STAYS { get; set; } = "";
        public string? TAX_REGISTRATION_NO { get; set; } = "";
        public string? TIAD { get; set; } = "";
        public DateTime? TRUNC_ACTUAL_CHECK_IN_DATE { get; set; }
        public DateTime? TRUNC_ACTUAL_CHECK_OUT_DATE { get; set; }
        public DateTime? TRUNC_BEGIN_DATE { get; set; }
        public DateTime? TRUNC_END_DATE { get; set; }
        public string? TURNDOWN_YN { get; set; } = "";
        public string? UDFC01 { get; set; } = "";
        public string? UDFC02 { get; set; } = "";
        public string? UDFC03 { get; set; } = "";
        public string? UDFC04 { get; set; } = "";
        public string? UDFC05 { get; set; } = "";
        public string? UDFC06 { get; set; } = "";
        public string? UDFC07 { get; set; } = "";
        public string? UDFC08 { get; set; } = "";
        public string? UDFC09 { get; set; } = "";
        public string? UDFC10 { get; set; } = "";
        public string? UDFC11 { get; set; } = "";
        public string? UDFC12 { get; set; } = "";
        public string? UDFC13 { get; set; } = "";
        public string? UDFC14 { get; set; } = "";
        public string? UDFC15 { get; set; } = "";
        public string? UDFC16 { get; set; } = "";
        public string? UDFC17 { get; set; } = "";
        public string? UDFC18 { get; set; } = "";
        public string? UDFC19 { get; set; } = "";
        public string? UDFC20 { get; set; } = "";
        public string? UDFC21 { get; set; } = "";
        public string? UDFC22 { get; set; } = "";
        public string? UDFC23 { get; set; } = "";
        public string? UDFC24 { get; set; } = "";
        public string? UDFC25 { get; set; } = "";
        public string? UDFC26 { get; set; } = "";
        public string? UDFC27 { get; set; } = "";
        public string? UDFC28 { get; set; } = "";
        public string? UDFC29 { get; set; } = "";
        public string? UDFC30 { get; set; } = "";
        public string? UDFC31 { get; set; } = "";
        public string? UDFC32 { get; set; } = "";
        public string? UDFC33 { get; set; } = "";
        public string? UDFC34 { get; set; } = "";
        public string? UDFC35 { get; set; } = "";
        public string? UDFC36 { get; set; } = "";
        public string? UDFC37 { get; set; } = "";
        public string? UDFC38 { get; set; } = "";
        public string? UDFC39 { get; set; } = "";
        public string? UDFC40 { get; set; } = "";
        public DateTime? UDFD01 { get; set; }
        public DateTime? UDFD02 { get; set; }
        public DateTime? UDFD03 { get; set; }
        public DateTime? UDFD04 { get; set; }
        public DateTime? UDFD05 { get; set; }
        public DateTime? UDFD06 { get; set; }
        public DateTime? UDFD07 { get; set; }
        public DateTime? UDFD08 { get; set; }
        public DateTime? UDFD09 { get; set; }
        public DateTime? UDFD10 { get; set; }
        public DateTime? UDFD11 { get; set; }
        public DateTime? UDFD12 { get; set; }
        public DateTime? UDFD13 { get; set; }
        public DateTime? UDFD14 { get; set; }
        public DateTime? UDFD15 { get; set; }
        public DateTime? UDFD16 { get; set; }
        public DateTime? UDFD17 { get; set; }
        public DateTime? UDFD18 { get; set; }
        public DateTime? UDFD19 { get; set; }
        public DateTime? UDFD20 { get; set; }
        public string? UDFN01 { get; set; } = "";
        public string? UDFN02 { get; set; } = "";
        public string? UDFN03 { get; set; } = "";
        public string? UDFN04 { get; set; } = "";
        public string? UDFN05 { get; set; } = "";
        public string? UDFN06 { get; set; } = "";
        public string? UDFN07 { get; set; } = "";
        public string? UDFN08 { get; set; } = "";
        public string? UDFN09 { get; set; } = "";
        public string? UDFN10 { get; set; } = "";
        public string? UDFN11 { get; set; } = "";
        public string? UDFN12 { get; set; } = "";
        public string? UDFN13 { get; set; } = "";
        public string? UDFN14 { get; set; } = "";
        public string? UDFN15 { get; set; } = "";
        public string? UDFN16 { get; set; } = "";
        public string? UDFN17 { get; set; } = "";
        public string? UDFN18 { get; set; } = "";
        public string? UDFN19 { get; set; } = "";
        public string? UDFN20 { get; set; } = "";
        public string? UDFN21 { get; set; } = "";
        public string? UDFN22 { get; set; } = "";
        public string? UDFN23 { get; set; } = "";
        public string? UDFN24 { get; set; } = "";
        public string? UDFN25 { get; set; } = "";
        public string? UDFN26 { get; set; } = "";
        public string? UDFN27 { get; set; } = "";
        public string? UDFN28 { get; set; } = "";
        public string? UDFN29 { get; set; } = "";
        public string? UDFN30 { get; set; } = "";
        public string? UDFN31 { get; set; } = "";
        public string? UDFN32 { get; set; } = "";
        public string? UDFN33 { get; set; } = "";
        public string? UDFN34 { get; set; } = "";
        public string? UDFN35 { get; set; } = "";
        public string? UDFN36 { get; set; } = "";
        public string? UDFN37 { get; set; } = "";
        public string? UDFN38 { get; set; } = "";
        public string? UDFN39 { get; set; } = "";
        public string? UDFN40 { get; set; } = "";
        public string? UNI_CARD_ID { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; } = "";
        public string? VIDEO_CHECKOUT_YN { get; set; } = "";
        public DateTime? VISA_EXPIRATION_DATE { get; set; }
        public DateTime? VISA_ISSUE_DATE { get; set; }
        public string? VISA_NUMBER { get; set; } = "";
        public string? WALKIN_YN { get; set; } = "";
        public string? WL_PRIORITY { get; set; } = "";
        public string? WL_REASON_CODE { get; set; } = "";
        public string? WL_REASON_DESCRIPTION { get; set; } = "";
        public string? WL_TELEPHONE_NO { get; set; } = "";
        public string? YIELDABLE_YN { get; set; } = "";
        public string? YM_CODE { get; set; } = "";

    }
}
