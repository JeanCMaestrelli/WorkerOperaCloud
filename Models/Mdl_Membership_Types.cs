using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Membership_Types
    {
        public string? MEMBERSHIP_TYPE { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? MEMBERSHIP_CLASS { get; set; }
        public string? CARD_PREFIX { get; set; }
        public string? CARD_LENGTH { get; set; }
        public string? CALCULATION_METHOD { get; set; }
        public string? CALCULATION_MONTHS { get; set; }
        public string? EXPIRATION_MONTH { get; set; }
        public string? NUMERIC_VALIDATION { get; set; }
        public string? CURRENCY_CODE { get; set; }
        public string? POINTS_LABEL { get; set; }
        public string? FOLIO_MESSAGE { get; set; }
        public string? COST_PER_POINT { get; set; }
        public string? CENTRAL_SETUP_YN { get; set; }
        public string? TRANSACTION_MAX_POINTS { get; set; }
        public string? POINTS_ISSUED_CENTRALLY_YN { get; set; }
        public string? MEMBERSHIP_ACTION { get; set; }
        public string? ALLOW_SHARES_YN { get; set; }
        public string? ALLOW_ADHOC_MULTIPLIER_YN { get; set; }
        public string? UDF_CARD_VALIDATION_YN { get; set; }
        public string? AWARD_GENERATION_METHOD { get; set; }
        public string? BATCH_DELAY_PERIOD { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public string? ORDER_BY { get; set; }
        public string? GRACE_EXPIRATION_MONTH { get; set; }
        public string? CAN_DELETE_YN { get; set; }
        public string? EXCHANGE_RATE_TYPE { get; set; }
        public string? YEARS_TO_EXPIRE { get; set; }
        public string? FULFILMENT_YN { get; set; }
        public string? EXPIRATION_DATE_REQUIRED { get; set; }
        public string? LEVEL_REQUIRED { get; set; }
        public string? PRIMARY_MEMBERSHIP_YN { get; set; }
        public string? FOLIO_MESSAGE_NONMEMBERS { get; set; }
        public string? UDF_FORMULA { get; set; }
        public string? CARD_VALID_YEARS { get; set; }
        public string? UPGRADE_PERIOD { get; set; }
        public string? DOWNGRADE_PERIOD { get; set; }
        public string? ALLOW_DUP_CARD_YN { get; set; }
        public string? EXCEPTION_TYPE { get; set; }
        public string? MULTIPLE_ROOMS_LIMIT { get; set; }
        public string? BOOKER_PROGRAM_YN { get; set; }
        public string? AUTO_CARD_NO_BASED_ON { get; set; }
        public string? MEMBER_INFO_DISP_SET { get; set; }
        public string? CHAIN_CODE { get; set; }
        public string? DEFAULT_MEM_STATUS { get; set; }
        public string? ENROLLMENT_CODE_REQ_YN { get; set; }
        public string? TSC_DATE_FLAG { get; set; }
        public string? FOLIO_MESSAGE_NQ { get; set; }
        public string? FOLIO_MESSAGE_NONMEMBERS_NQ { get; set; }
        public string? SEND_CHKOUT_TO_IFC { get; set; }
        public string? PROMPT_AT_CHECKIN { get; set; }
        public string? VALIDATION_BY_IFC { get; set; }
        public string? EXTERNAL_PROCESS_DAYS { get; set; }
        public string? PROMPT_AT_NEW_RESERVATION { get; set; }
        public string? PROMPT_AT_UPDATE_RESERVATION { get; set; }
        public string? PROMPT_AT_CHECK_OUT { get; set; }
        public string? FOLIO_MESSAGE_CREDITS { get; set; }
        public string? EXTERNALLY_CONTROLLED_YN { get; set; }
        public string? CHIP_AND_PIN_YN { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
