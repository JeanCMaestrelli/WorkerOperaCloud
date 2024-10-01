using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Financial_Transactions_Jrnl
    {
        public string? RECPT_TYPE { get; set; } = "";
        public string? RECPT_NO { get; set; } = "";
        public string? ROOM_CLASS { get; set; } = "";
        public string? EURO_EXCHANGE_RATE { get; set; } = "";
        public string? TAX_INCLUSIVE_YN { get; set; } = "";
        public string? NET_AMOUNT { get; set; } = "";
        public string? GROSS_AMOUNT { get; set; } = "";
        public string? REVENUE_AMT { get; set; } = "";
        public string? PASSER_BY_NAME { get; set; } = "";
        public string? TRX_NO { get; set; } = "";
        public string? FT_SUBTYPE { get; set; } = "";
        public string? TC_GROUP { get; set; } = "";
        public string? TC_SUBGROUP { get; set; } = "";
        public string? TRX_CODE { get; set; } = "";
        public string? RESV_NAME_ID { get; set; } = "";
        public DateTime? TRX_DATE { get; set; }
        public DateTime? BUSINESS_DATE { get; set; }
        public string? CURRENCY { get; set; } = "";
        public string? RESORT { get; set; } = "";
        public string? TRX_NO_ADJUST { get; set; } = "";
        public string? TRX_NO_ADDED_BY { get; set; } = "";
        public string? TRX_NO_AGAINST_PACKAGE { get; set; } = "";
        public string? TRX_NO_HEADER { get; set; } = "";
        public string? AR_NUMBER { get; set; } = "";
        public string? CASHIER_ID { get; set; } = "";
        public string? FT_GENERATED_TYPE { get; set; } = "";
        public string? REASON_CODE { get; set; } = "";
        public string? QUANTITY { get; set; } = "";
        public string? PRICE_PER_UNIT { get; set; } = "";
        public string? ROOM { get; set; } = "";
        public string? TCL_CODE1 { get; set; } = "";
        public string? TCL_CODE2 { get; set; } = "";
        public string? GUEST_ACCOUNT_CREDIT { get; set; } = "";
        public string? GUEST_ACCOUNT_DEBIT { get; set; } = "";
        public string? TRX_AMOUNT { get; set; } = "";
        public string? POSTED_AMOUNT { get; set; } = "";
        public string? PACKAGE_CREDIT { get; set; } = "";
        public string? PACKAGE_DEBIT { get; set; } = "";
        public string? FOLIO_VIEW { get; set; } = "";
        public string? REMARK { get; set; } = "";
        public string? REFERENCE { get; set; } = "";
        public string? INSERT_USER { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; }
        public string? CREDIT_CARD_ID { get; set; } = "";
        public string? NAME_ID { get; set; } = "";
        public string? MARKET_CODE { get; set; } = "";
        public string? RATE_CODE { get; set; } = "";
        public string? DEFERRED_YN { get; set; } = "";
        public string? EXCHANGE_RATE { get; set; } = "";
        public string? DEP_LED_CREDIT { get; set; } = "";
        public string? DEP_LED_DEBIT { get; set; } = "";
        public string? HOTEL_ACCT { get; set; } = "";
        public string? IND_ADJUSTMENT_YN { get; set; } = "";
        public string? ROUTING_INSTRN_ID { get; set; } = "";
        public string? FROM_RESV_ID { get; set; } = "";
        public string? CASHIER_DEBIT { get; set; } = "";
        public string? CASHIER_CREDIT { get; set; } = "";
        public string? TRAN_ACTION_ID { get; set; } = "";
        public string? OLD_TRAN_ACTION_ID { get; set; } = "";
        public string? U_D_FLAG { get; set; } = "";
        public string? FIN_DML_SEQNO { get; set; } = "";
        public string? INVOICE_NO { get; set; } = "";
        public string? AR_LED_CREDIT { get; set; } = "";
        public string? AR_LED_DEBIT { get; set; } = "";
        public string? AR_STATE { get; set; } = "";
        public string? FOLIO_NO { get; set; } = "";
        public string? FIXED_CHARGES_YN { get; set; } = "";
        public string? TA_COMMISSIONABLE_YN { get; set; } = "";
        public string? CHEQUE_NUMBER { get; set; } = "";
        public string? CASHIER_OPENING_BALANCE { get; set; } = "";
        public DateTime? INVOICE_CLOSE_DATE { get; set; }
        public DateTime? AR_TRANSFER_DATE { get; set; }
        public string? SOURCE_CODE { get; set; } = "";
        public string? O_TRX_DESC { get; set; } = "";
        public string? PRODUCT { get; set; } = "";
        public string? NUMBER_DIALED { get; set; } = "";
        public string? GEN_CASHIER_ID { get; set; } = "";
        public DateTime? TRNS_ACTIVITY_DATE { get; set; }
        public string? TRNS_FROM_ACCT { get; set; } = "";
        public string? TRNS_TO_ACCT { get; set; } = "";
        public string? TARGET_RESORT { get; set; } = "";
        public string? INH_DEBIT { get; set; } = "";
        public string? INH_CREDIT { get; set; } = "";
        public string? LINK_TRX_NO { get; set; } = "";
        public string? NAME_TAX_TYPE { get; set; } = "";
        public string? BILL_NO { get; set; } = "";
        public string? DISPLAY_YN { get; set; } = "";
        public string? COLL_AGENT_POSTING_YN { get; set; } = "";
        public string? FISCAL_TRX_CODE_TYPE { get; set; } = "";
        public string? DEFERRED_TAXES_YN { get; set; } = "";
        public string? TAX_INV_NO { get; set; } = "";
        public string? PAYMENT_TYPE { get; set; } = "";
        public string? FOLIO_TYPE { get; set; } = "";
        public string? ACTION_ID { get; set; } = "";
        public string? AUTHORIZER_ID { get; set; } = "";
        public string? APPROVAL_CODE { get; set; } = "";
        public DateTime? APPROVAL_DATE { get; set; }
        public string? APPROVAL_STATUS { get; set; } = "";
        public string? COMP_LINK_TRX_NO { get; set; } = "";
        public string? COMP_LINK_TRX_CODE { get; set; } = "";
        public DateTime? POSTING_DATE { get; set; }
        public string? MTRX_NO_AGAINST_PACKAGE { get; set; } = "";
        public string? ADVANCED_GENERATE_YN { get; set; } = "";
        public string? FOREX_TYPE { get; set; } = "";
        public string? FOREX_COMM_PERC { get; set; } = "";
        public string? FOREX_COMM_AMOUNT { get; set; } = "";
        public string? ARTICLE_ID { get; set; } = "";
        public string? TO_RESV_NAME_ID { get; set; } = "";
        public string? ROOM_NTS { get; set; } = "";
        public string? COMP_TYPE_CODE { get; set; } = "";
        public string? ACC_TYPE_FLAG { get; set; } = "";
        public string? FISCAL_BILL_NO { get; set; } = "";
        public string? INVOICE_TYPE { get; set; } = "";
        public string? TAX_ELEMENTS { get; set; } = "";
        public string? PACKAGE_ALLOWANCE { get; set; } = "";
        public string? ORIGINAL_RESV_NAME_ID { get; set; } = "";
        public string? ORIGINAL_ROOM { get; set; } = "";
        public string? COUPON_NO { get; set; } = "";
        public string? ORG_AR_LED_DEBIT { get; set; } = "";
        public string? SETTLEMENT_FLAG { get; set; } = "";
        public string? PROFIT_LOSS_FLAG { get; set; } = "";
        public string? CLOSURE_NO { get; set; } = "";
        public string? PROFORMA_YN { get; set; } = "";
        public string? ALLOWANCE_TYPE { get; set; } = "";
        public string? ADV_GENERATE_ADJUSTMENT { get; set; } = "";
        public string? ADV_GENERATE_TRX_CODE { get; set; } = "";
        public string? HOLD_YN { get; set; } = "";
        public string? TRX_SERVICE_TYPE { get; set; } = "";
        public string? ORG_BILL_NO { get; set; } = "";
        public string? ORG_FOLIO_TYPE { get; set; } = "";
        public string? ARRANGEMENT_ID { get; set; } = "";
        public string? CHECK_FILE_ID { get; set; } = "";
        public string? COMMENTS { get; set; } = "";
        public string? COMPRESSED_YN { get; set; } = "";
        public string? COVERS { get; set; } = "";
        public string? FIN_DML_SEQ_NO { get; set; } = "";
        public string? RESV_DEPOSIT_ID { get; set; } = "";
        public string? REVISION_NO { get; set; } = "";
        public string? SOURCE_COMMISSION_NET_YN { get; set; } = "";
        public string? TAX_GENERATED_YN { get; set; } = "";
        public string? TA_COMMISSION_NET_YN { get; set; } = "";
        public string? POSTING_TYPE { get; set; } = "";
        public string? PARALLEL_GUEST_CREDIT { get; set; } = "";
        public string? PARALLEL_GUEST_DEBIT { get; set; } = "";
        public string? PARALLEL_CURRENCY { get; set; } = "";
        public string? EXCHANGE_DIFFERENCE_YN { get; set; } = "";
        public string? MEMBERSHIP_ID { get; set; } = "";
        public string? PARALLEL_NET_AMOUNT { get; set; } = "";
        public string? PARALLEL_GROSS_AMOUNT { get; set; } = "";
        public string? EXCHANGE_TYPE { get; set; } = "";
        public DateTime? EXCHANGE_DATE { get; set; }
        public string? INSTALLMENTS { get; set; } = "";
        public string? CONTRACT_GUEST_DEBIT { get; set; } = "";
        public string? CONTRACT_GUEST_CREDIT { get; set; } = "";
        public string? CONTRACT_NET_AMOUNT { get; set; } = "";
        public string? CONTRACT_GROSS_AMOUNT { get; set; } = "";
        public string? CONTRACT_CURRENCY { get; set; } = "";
        public string? CALC_POINTS_YN { get; set; } = "";
        public string? AR_CHARGE_TRANSFER_YN { get; set; } = "";
        public string? ASB_FLAG { get; set; } = "";
        public string? POSTIT_YN { get; set; } = "";
        public string? POSTIT_NO { get; set; } = "";
        public DateTime? ROUTING_DATE { get; set; }
        public string? PACKAGE_TRX_TYPE { get; set; } = "";
        public string? CC_TRX_FEE_AMOUNT { get; set; } = "";
        public string? CHANGE_DUE { get; set; } = "";
        public string? POSTING_SOURCE_NAME_ID { get; set; } = "";
        public string? AUTO_SETTLE_YN { get; set; } = "";
        public string? QUEUE_NAME { get; set; } = "";
        public string? DEP_TAX_TRANSFERED_YN { get; set; } = "";
        public string? ESIGNED_RECEIPT_NAME { get; set; } = "";
        public string? BONUS_CHECK_ID { get; set; } = "";
        public string? AUTO_CREDITBILL_YN { get; set; } = "";
        public string? POSTING_RHYTHM { get; set; } = "";
        public string? FBA_CERTIFICATE_NUMBER { get; set; } = "";
        public string? EXP_ORIGINAL_INVOICE { get; set; } = "";
        public string? EXP_INVOICE_TYPE { get; set; } = "";
        public string? ASB_TAX_FLAG { get; set; } = "";
        public string? ASB_ONLY_POST_TAXES_ONCE_YN { get; set; } = "";
        public string? ROUND_LINK_TRXNO { get; set; } = "";
        public string? ROUND_FACTOR_YN { get; set; } = "";
        public DateTime? SYSTEM_DATE { get; set; }
        public DateTime? JRNL_BUSINESS_DATE { get; set; }
        public string? JRNL_USER { get; set; } = "";
        public string? DEP_POSTING_FLAG { get; set; } = "";
        public DateTime? EFFECTIVE_DATE { get; set; }
        public string? PACKAGE_ARRANGEMENT_CODE { get; set; } = "";
        public string? CORRECTION_YN { get; set; } = "";
        public string? ROUTED_YN { get; set; } = "";
        public string? UPSELL_CHARGE_YN { get; set; } = "";
        public string? REVERSE_PAYMENT_TRX_NO { get; set; } = "";
        public string? ADVANCE_BILL_YN { get; set; } = "";
        public string? ADVANCE_BILL_REVERSED_YN { get; set; } = "";
        public string? ORG_POSTED_AMOUNT { get; set; } = "";
        public string? INC_TAX_DEDUCTED_YN { get; set; } = "";
        public string? ROOM_NTS_EFFECTIVE { get; set; } = "";
        public string? THRESHOLD_DIVERSION_ID { get; set; } = "";
        public string? THRESHOLD_ENTITY_TYPE { get; set; } = "";
        public string? THRESHOLD_ENTITY_QTY { get; set; } = "";
        public string? THRESHOLD_TREATMENT_FLAG { get; set; } = "";
        public string? EXCH_DIFF_TRX_NO { get; set; } = "";
        public string? DEPOSIT_TRANSACTION_ID { get; set; } = "";
        public string? ASSOCIATED_TRX_NO { get; set; } = "";
        public string? STAMP_DUTY_YN { get; set; } = "";
        public string? ASSOCIATED_RECPT_NO { get; set; } = "";
        public string? TAX_RATE { get; set; } = "";
        public string? TAX_RATE_TYPE { get; set; } = "";
        public string? VAT_OFFSET_YN { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
