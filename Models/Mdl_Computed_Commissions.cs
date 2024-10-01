using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Computed_Commissions
    {
        public string? RESORT { get; set; } = "";
        public string? PAYMENT_ID { get; set; } = "";
        public string? TRAVEL_AGENT_ID { get; set; } = "";
        public string? RESV_NAME_ID { get; set; } = "";
        public string? COMMISSIONABLE_REVENUE { get; set; } = "";
        public string? GROSS_COMM_AMT { get; set; } = "";
        public string? PREPAID_COMM { get; set; } = "";
        public string? AR_AMOUNT { get; set; } = "";
        public string? VAT_AMOUNT { get; set; } = "";
        public string? COMM_STATUS { get; set; } = "";
        public string? COMMISSION_HOLD_CODE { get; set; } = "";
        public string? COMMISSION_HOLD_DESC { get; set; } = "";
        public string? INSERT_USER { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; } = null;
        public string? UPDATE_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; } = null;
        public string? COMM_TYPE { get; set; } = "";
        public string? TA_COMM_CODE { get; set; } = "";
        public string? MANUAL_EDIT_YN { get; set; } = "";
        public string? PAYEE_TYPE { get; set; } = "";
        public string? MANUAL_RESV_YN { get; set; } = "";
        public string? REMARKS { get; set; } = "";
        public string? PPD_REMARKS { get; set; } = "";
        public string? PPD_EDIT_YN { get; set; } = "";
        public string? DECIMAL_POSITIONS { get; set; } = "";
        public string? EXCHANGE_RATE { get; set; } = "";
        public DateTime? BUSINESS_DATE_CREATED { get; set; } = null;
        public string? COMM_CODE_DIFF_YN { get; set; } = "";
        public DateTime? DEPARTURE { get; set; } = null;
        public string? ADJUSTMENT_NOTE { get; set; } = "";
        public string? AR_YN { get; set; } = "";
        public string? TAX_FILE_STATUS { get; set; } = "";
        public DateTime? TAX_FILE_DATE { get; set; } = null;
        public string? OWNER_COMM_PROCESSED_YN { get; set; } = "";
        public string? COMMISSION_ID { get; set; } = "";
        public DateTime? BEGIN_DATE { get; set; } = null;
        public DateTime? END_DATE { get; set; } = null;
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
