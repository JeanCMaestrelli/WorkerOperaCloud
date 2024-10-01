using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Allotment_Header
    {
        public string? RESORT { get; set; } = "";
        public string? ALLOTMENT_HEADER_ID { get; set; } = "";
        public string? ALLOTMENT_TYPE { get; set; } = "";
        public DateTime? BEGIN_DATE { get; set; }
        public DateTime? END_DATE { get; set; }
        public DateTime? SHOULDER_BEGIN_DATE { get; set; }
        public DateTime? SHOULDER_END_DATE { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public DateTime? INV_CUTOFF_DATE { get; set; }
        public DateTime? CANCELLATION_DATE { get; set; }
        public DateTime? DATE_OPENED_FOR_PICKUP { get; set; }
        public DateTime? METHOD_DUE { get; set; }
        public DateTime? RMS_DECISION { get; set; }
        public DateTime? RMS_FOLLOWUP { get; set; }
        public DateTime? CAT_DECISION { get; set; }
        public DateTime? CAT_FOLLOWUP { get; set; }
        public DateTime? CAT_CUTOFF { get; set; }
        public DateTime? CAT_CANX_DATE { get; set; }
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
        public DateTime? DOWNLOAD_DATE { get; set; }
        public DateTime? UPLOAD_DATE { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
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
        public DateTime? ORIGINAL_BEGIN_DATE { get; set; }
        public DateTime? ORIGINAL_END_DATE { get; set; }
        public DateTime? ARRIVAL_TIME { get; set; }
        public DateTime? DEPARTURE_TIME { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public DateTime? SENT_DATE { get; set; }
        public DateTime? REPLY_DATE { get; set; }
        public DateTime? DATE_PEL { get; set; }
        public DateTime? DATE_ACL { get; set; }
        public DateTime? DATE_TDL { get; set; }
        public DateTime? DATE_CFL { get; set; }
        public DateTime? DATE_LSL { get; set; }
        public DateTime? UPDATE_DATE_EXTERNAL { get; set; }
        public DateTime? BEGIN_DATE_ORIGINAL { get; set; }
        public DateTime? END_DATE_ORIGINAL { get; set; }
        public DateTime? ORIGINAL_BEGIN_DATE_HOLIDEX { get; set; }
        public DateTime? DUE_DATE_ORD { get; set; }
        public DateTime? BEO_LAST_PRINT { get; set; }
        public DateTime? LINK_DATE { get; set; }
        public DateTime? DISTRIBUTED_DATE { get; set; }
        public DateTime? PROPOSAL_SENT_DATE { get; set; }
        public string? DESCRIPTION { get; set; } = "";
        public string? ALLOTMENT_CODE { get; set; } = "";
        public string? MASTER_NAME_ID { get; set; } = "";
        public string? COMPANY_NAME_ID { get; set; } = "";
        public string? AGENT_NAME_ID { get; set; } = "";
        public string? SOURCE_NAME_ID { get; set; } = "";
        public string? CANCEL_RULE { get; set; } = "";
        public string? RATE_CODE { get; set; } = "";
        public string? BOOKING_STATUS { get; set; } = "";
        public string? BOOKING_STATUS_ORDER { get; set; } = "";
        public string? STATUS { get; set; } = "";
        public string? ELASTIC { get; set; } = "";
        public string? INV_CUTOFF_DAYS { get; set; } = "";
        public string? TENTATIVE_LEVEL { get; set; } = "";
        public string? INFO { get; set; } = "";
        public string? MARKET_CODE { get; set; } = "";
        public string? SOURCE { get; set; } = "";
        public string? CHANNEL { get; set; } = "";
        public string? AVG_PEOPLE_PER_ROOM { get; set; } = "";
        public string? ORIGINAL_RATE_CODE { get; set; } = "";
        public string? BOOKING_ID { get; set; } = "";
        public string? CANCELLATION_NO { get; set; } = "";
        public string? CANCELLATION_CODE { get; set; } = "";
        public string? CANCELLATION_DESC { get; set; } = "";
        public string? GUARANTEE_CODE { get; set; } = "";
        public string? ROOMS_PER_DAY { get; set; } = "";
        public string? AVERAGE_RATE { get; set; } = "";
        public string? RESERVE_INVENTORY_YN { get; set; } = "";
        public string? ALLOTMENT_ORIGION { get; set; } = "";
        public string? SUPER_BLOCK_ID { get; set; } = "";
        public string? SUPER_BLOCK_RESORT { get; set; } = "";
        public string? ACTION_ID { get; set; } = "";
        public string? DML_SEQ_NO { get; set; } = "";
        public string? CONTACT_NAME_ID { get; set; } = "";
        public string? ALIAS { get; set; } = "";
        public string? SALES_ID { get; set; } = "";
        public string? PAYMENT_METHOD { get; set; } = "";
        public string? RIV_MARKET_SEGMENT { get; set; } = "";
        public string? EXCHANGE_POSTING_TYPE { get; set; } = "";
        public string? CURRENCY_CODE { get; set; } = "";
        public string? EXCHANGE_RATE { get; set; } = "";
        public string? MAINMARKET { get; set; } = "";
        public string? TRACECODE { get; set; } = "";
        public string? OWNER_RESORT { get; set; } = "";
        public string? OWNER { get; set; } = "";
        public string? OWNER_CODE { get; set; } = "";
        public string? RMS_OWNER_RESORT { get; set; } = "";
        public string? RMS_OWNER { get; set; } = "";
        public string? RMS_OWNER_CODE { get; set; } = "";
        public string? CAT_OWNER_RESORT { get; set; } = "";
        public string? CAT_OWNER { get; set; } = "";
        public string? CAT_OWNER_CODE { get; set; } = "";
        public string? BOOKINGTYPE { get; set; } = "";
        public string? BOOKINGMETHOD { get; set; } = "";
        public string? RMS_CURRENCY { get; set; } = "";
        public string? RMS_QUOTE_CURR { get; set; } = "";
        public string? RMS_EXCHANGE { get; set; } = "";
        public string? ATTENDEES { get; set; } = "";
        public string? CAT_STATUS { get; set; } = "";
        public string? CAT_CURRENCY { get; set; } = "";
        public string? CAT_QUOTE_CURR { get; set; } = "";
        public string? CAT_EXCHANGE { get; set; } = "";
        public string? CAT_CANX_NO { get; set; } = "";
        public string? CAT_CANX_CODE { get; set; } = "";
        public string? CAT_CANX_DESC { get; set; } = "";
        public string? INFOBOARD { get; set; } = "";
        public string? BFST_YN { get; set; } = "";
        public string? BFST_PRICE { get; set; } = "";
        public string? BFST_DESC { get; set; } = "";
        public string? PORTERAGE_YN { get; set; } = "";
        public string? PORTERAGE_PRICE { get; set; } = "";
        public string? COMMISSION { get; set; } = "";
        public string? DETAILS_OK_YN { get; set; } = "";
        public string? DISTRIBUTED_YN { get; set; } = "";
        public string? CONTRACT_NR { get; set; } = "";
        public string? FUNCTIONTYPE { get; set; } = "";
        public string? REPRESENTATIVE { get; set; } = "";
        public string? DEFAULT_PM_RESV_NAME_ID { get; set; } = "";
        public string? CATERINGONLY_YN { get; set; } = "";
        public string? EVENTS_GUARANTEED_YN { get; set; } = "";
        public string? TAX_AMOUNT { get; set; } = "";
        public string? SERVICE_CHARGE { get; set; } = "";
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
        public string? DOWNLOAD_RESORT { get; set; } = "";
        public string? DOWNLOAD_SREP { get; set; } = "";
        public string? LAPTOP_CHANGE { get; set; } = "";
        public string? EXTERNAL_REFERENCE { get; set; } = "";
        public string? EXTERNAL_LOCKED { get; set; } = "";
        public string? PROFILE_ID { get; set; } = "";
        public string? RESORT_BOOKED { get; set; } = "";
        public string? INSERT_USER { get; set; } = "";
        public string? UPDATE_USER { get; set; } = "";
        public string? MANUAL_CUTOFF { get; set; } = "";
        public string? SNAPSHOT_SETUP { get; set; } = "";
        public string? TBD_RATES { get; set; } = "";
        public string? DESTINATION { get; set; } = "";
        public string? LEAD_SOURCE { get; set; } = "";
        public string? PROGRAM { get; set; } = "";
        public string? COMPETITION { get; set; } = "";
        public string? CONTROL_BLOCK_YN { get; set; } = "";
        public string? CRS_GTD_YN { get; set; } = "";
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
        public string? SYNCHRONIZE_YN { get; set; } = "";
        public string? MTG_REVENUE { get; set; } = "";
        public string? MTG_BUDGET { get; set; } = "";
        public string? COMP_ROOMS_FIXED_YN { get; set; } = "";
        public string? COMP_ROOMS { get; set; } = "";
        public string? COMP_PER_STAY_YN { get; set; } = "";
        public string? COMP_ROOM_VALUE { get; set; } = "";
        public string? UDESCRIPTION { get; set; } = "";
        public string? XDESCRIPTION { get; set; } = "";
        public string? XUDESCRIPTION { get; set; } = "";
        public string? RM_COMMISSION_1 { get; set; } = "";
        public string? RM_COMMISSION_2 { get; set; } = "";
        public string? FB_COMMISSION_1 { get; set; } = "";
        public string? FB_COMMISSION_2 { get; set; } = "";
        public string? CATERING_PKGS_YN { get; set; } = "";
        public string? AGENT_CONTACT_NAME_ID { get; set; } = "";
        public string? SHOW_RATE_AMOUNT_YN { get; set; } = "";
        public string? PRINT_RATE_YN { get; set; } = "";
        public string? LEAD_TYPE { get; set; } = "";
        public string? LEAD_ORIGIN { get; set; } = "";
        public string? LEADSTATUS { get; set; } = "";
        public string? SENT_YN { get; set; } = "";
        public string? SENT_VIA { get; set; } = "";
        public string? SENT_BY { get; set; } = "";
        public string? REPLY_STATUS { get; set; } = "";
        public string? REPLY_VIA { get; set; } = "";
        public string? REPLY_BY { get; set; } = "";
        public string? TDL_REASON { get; set; } = "";
        public string? LEAD_NEW_YN { get; set; } = "";
        public string? LEAD_RECEIVED_YN { get; set; } = "";
        public string? LEADSEND1 { get; set; } = "";
        public string? LEADSEND2 { get; set; } = "";
        public string? LEADSEND3 { get; set; } = "";
        public string? COM_METHOD1 { get; set; } = "";
        public string? COM_METHOD2 { get; set; } = "";
        public string? COM_METHOD3 { get; set; } = "";
        public string? COM_ADDRESS1 { get; set; } = "";
        public string? COM_ADDRESS2 { get; set; } = "";
        public string? COM_ADDRESS3 { get; set; } = "";
        public string? LEAD_ERROR { get; set; } = "";
        public string? RESP_TIME { get; set; } = "";
        public string? RESP_TIME_CODE { get; set; } = "";
        public string? HIDE_ACC_INFO_YN { get; set; } = "";
        public string? PENDING_SEND_YN { get; set; } = "";
        public string? SEND_TO_CENTRAL_YN { get; set; } = "";
        public string? CREDIT_CARD_ID { get; set; } = "";
        public string? SYNC_CONTRACT_YN { get; set; } = "";
        public string? EXCLUSION_MESSAGE { get; set; } = "";
        public string? POT_ROOM_NIGHTS { get; set; } = "";
        public string? POT_ROOM_REVENUE { get; set; } = "";
        public string? POT_FB_REVENUE { get; set; } = "";
        public string? POT_OTHER_REVENUE { get; set; } = "";
        public string? COMMISSIONABLE_YN { get; set; } = "";
        public string? COMMISSIONABLE_PERC { get; set; } = "";
        public string? FIT_DISCOUNT_PERC { get; set; } = "";
        public string? FIT_DISCOUNT_LEVEL { get; set; } = "";
        public string? BFST_INCL_YN { get; set; } = "";
        public string? BFST_INCL_PRICE { get; set; } = "";
        public string? SERVICE_INCL_YN { get; set; } = "";
        public string? SERVICE_PERC { get; set; } = "";
        public string? DBL_RM_SUPPLEMENT_YN { get; set; } = "";
        public string? DBL_RM_SUPPLEMENT_PRICE { get; set; } = "";
        public string? TAX_INCLUDED_YN { get; set; } = "";
        public string? TAX_INCLUDED_PERC { get; set; } = "";
        public string? CENTRAL_OWNER { get; set; } = "";
        public string? RATE_OVERRIDE { get; set; } = "";
        public string? SELL_THRU_YN { get; set; } = "";
        public string? SERVICE_FEE_YN { get; set; } = "";
        public string? SERVICE_FEE { get; set; } = "";
        public string? CAT_ITEM_DISCOUNT { get; set; } = "";
        public string? ROOMING_LIST_RULES { get; set; } = "";
        public string? FLAT_RATE_YN { get; set; } = "";
        public string? TOURCODE { get; set; } = "";
        public string? BLOCK_TYPE { get; set; } = "";
        public string? GREEK_CONTRACT_NR { get; set; } = "";
        public string? TA_RECORD_LOCATOR { get; set; } = "";
        public string? UALIAS { get; set; } = "";
        public string? RATE_OVERRIDE_REASON { get; set; } = "";
        public string? PUBLISH_RATES_YN { get; set; } = "";
        public string? TAX_TYPE { get; set; } = "";
        public string? ATTACHMENT_URL { get; set; } = "";
        public string? LEADCHANGE_BYPROPERTY_YN { get; set; } = "";
        public string? KEEP_LEAD_CONTROL_YN { get; set; } = "";
        public string? ALLOW_ALTERNATE_DATES_YN { get; set; } = "";
        public string? REGENERATED_LEAD_YN { get; set; } = "";
        public string? SUB_ALLOTMENT_YN { get; set; } = "";
        public string? ORMS_BLOCK_CLASS { get; set; } = "";
        public string? LOST_TO_PROPERTY { get; set; } = "";
        public string? CXL_PENALTY { get; set; } = "";
        public string? ORMS_FINAL_BLOCK { get; set; } = "";
        public string? FIT_DISCOUNT_TYPE { get; set; } = "";
        public string? ORMS_TRANSIENT_BLOCK { get; set; } = "";
        public string? HLX_DEPOSIT_DAYS { get; set; } = "";
        public string? HLX_CANX_NOTICE_DAYS { get; set; } = "";
        public string? HLX_RETURN_EACH_DAY_YN { get; set; } = "";
        public string? HLX_COMMISSIONABLE_YN { get; set; } = "";
        public string? HLX_DI_SECURED_YN { get; set; } = "";
        public string? HLX_DD_SECURED_YN { get; set; } = "";
        public string? HLX_RATES_GNR_SECURED_YN { get; set; } = "";
        public string? HLX_RATE_ALL_SECURED_YN { get; set; } = "";
        public string? HLX_HOUSINGINFO_SECURED_YN { get; set; } = "";
        public string? ISAC_OPPTY_ID { get; set; } = "";
        public string? TLP_RESPONSEID { get; set; } = "";
        public string? TLP_URL { get; set; } = "";
        public string? FB_AGENDA_CURR { get; set; } = "";
        public string? FIT_CONTRACT_MODE { get; set; } = "";
        public string? PROPOSAL_SHOW_SPACENAME_YN { get; set; } = "";
        public string? PROPOSAL_SHOW_EVENTPRICE_YN { get; set; } = "";
        public string? PROPOSAL_OWNER_SELECTION { get; set; } = "";
        public string? PROPOSAL_DECISION_SELECTION { get; set; } = "";
        public string? PROPOSAL_VIEW_TOKEN { get; set; } = "";
        public string? ALLOTMENT_CLASSIFICATION { get; set; } = "";
        public string? SUPER_SEARCH_INDEX_TEXT { get; set; } = "";
        public string? RATE_PROTECTION { get; set; } = "";
        public string? NON_COMPETE { get; set; } = "";
        public string? CONVERSION_CODE { get; set; } = "";
        public string? RANKING_CODE { get; set; } = "";
        public string? NON_COMPETE_CODE { get; set; } = "";
        public string? RATE_GUARANTEED_YN { get; set; } = "";
        public string? PROPOSAL_SHOW_PMS_ROOM_TYPE_YN { get; set; } = "";
        public string? SC_QUOTE_ID { get; set; } = "";
        public string? OFFSET_TYPE { get; set; } = "";
        public string? PROPOSAL_FOLLOWUP_SELECTION { get; set; } = "";
        public string? PROPOSAL_INCL_ALT_NAMES_YN { get; set; } = "";
        public string? ORIG_ALLOTMENT_HEADER_ID { get; set; } = "";
        public string? WEB_BOOKABLE_YN { get; set; } = "";
        public string? PROPOSAL_COMBINE_EVENTS_YN { get; set; } = "";
        public string? PROPOSAL_SPACE_MEASUREMENT { get; set; } = "";
        public string? AUTO_LOAD_FORECAST_YN { get; set; } = "";
        public string? ORMS_FORECAST_REVIEW_REASON { get; set; } = "";
        public string? MAR_HOUSE_PROTECT_YN { get; set; } = "";
        public string? MAR_ROLL_END_DATE_YN { get; set; } = "";
        public string? MAR_EVENT_TYPE { get; set; } = "";
        public string? FS_OVERBOOKING_YN { get; set; } = "";
        public string? BLOCK_TRX_CODE { get; set; } = "";
        public string? BWI_LEAD_ID { get; set; } = "";
        public string? BWI_URL { get; set; } = "";
        public string? GIID { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
