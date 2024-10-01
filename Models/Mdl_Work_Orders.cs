using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Work_Orders
    {
        public string? WO_NUMBER { get; set; }
        public string? ACT_TYPE { get; set; }
        public string? MASTER_SUB { get; set; }
        public DateTime? CREATED_DATE { get; set; }
        public string? CREATED_BY { get; set; }
        public string? PROBLEM_DESC { get; set; }
        public string? NOTES { get; set; }
        public string? ASSIGNED_BY { get; set; }
        public DateTime? ASSIGNED_ON_DATE { get; set; }
        public string? ASSIGNED_TO { get; set; }
        public string? TAKEN_BY { get; set; }
        public DateTime? TAKEN_DATE { get; set; }
        public string? RELEASED_BY { get; set; }
        public DateTime? RELEASED_DATE { get; set; }
        public string? COMPLETED_BY { get; set; }
        public DateTime? COMPLETED_DATE { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public DateTime? SHOW_ON { get; set; }
        public string? TOTAL_LABOR_COST { get; set; }
        public string? TOTAL_PARTS_COST { get; set; }
        public string? USER_EXT { get; set; }
        public string? DEPT_OF_ACTION { get; set; }
        public string? GUEST_ROOM_YN { get; set; }
        public string? PRIORITY_CHANGED_YN { get; set; }
        public string? EST_TIME_TO_COMPLETE { get; set; }
        public string? RESORT { get; set; }
        public string? CATEGORY_CODE { get; set; }
        public string? REASON_CODE { get; set; }
        public string? LOCATION_CODE { get; set; }
        public string? PRIORITY_CODE { get; set; }
        public string? PARENT_WO_NUMBER { get; set; }
        public string? STATUS_CODE { get; set; }
        public string? TASK_CODE { get; set; }
        public string? TASKITEM_NUMBER { get; set; }
        public string? TYPE_CODE { get; set; }
        public string? PLANT_ITEM_CODE { get; set; }
        public string? EST_UOT_CODE { get; set; }
        public string? DEPENDING_ON_WO_NUMBER { get; set; }
        public string? ROOM { get; set; }
        public string? GUEST_ORIGINATED_YN { get; set; }
        public DateTime? START_DATE { get; set; }
        public DateTime? END_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? PRIVATE_YN { get; set; }
        public string? FO_ROOM_STATUS { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? DOWNLOAD_RESORT { get; set; }
        public DateTime? DOWNLOAD_DATE { get; set; }
        public string? DOWNLOAD_SREP { get; set; }
        public DateTime? UPLOAD_DATE { get; set; }
        public string? LAPTOP_CHANGE { get; set; }
        public string? TRACECODE { get; set; }
        public string? SURVEY_ID { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? ATTENDEES { get; set; }
        public string? EXTERNAL_SYSTEM_ID { get; set; }
        public string? EXTERNAL_SYSTEM { get; set; }
        public string? SEND_METHOD { get; set; }
        public string? COMPLETED_YN { get; set; }
        public string? ACT_CLASS { get; set; }
        public string? AUTHOR { get; set; }
        public string? ATTACHMENT { get; set; }
        public string? GENERATED_BY_FREQ_ID { get; set; }
        public byte[]? BFILE_LOCATOR { get; set; }//###########
        public string? EST_RM_NIGHTS { get; set; }
        public string? EST_RM_REVENUE { get; set; }
        public string? EST_CAT_REVENUE { get; set; }
        public string? EST_OTHER_REVENUE { get; set; }
        public string? REQUEST_TEMPLATE_ID { get; set; }
        public string? REQUEST_TYPE_ID { get; set; }
        public string? CAMPAIGN_STATUS_CODE { get; set; }
        public string? GENERATED_BY_CAMPAIGN { get; set; }
        public string? RESULT { get; set; }
        public string? REQUEST_TYPE_TEMPLATES_ID { get; set; }
        public string? NOTIFIED_YN { get; set; }
        public string? DEPOSIT_AMOUNT { get; set; }
        public string? DEPOSIT_OWNER { get; set; }
        public string? ACTIVITY_AMOUNT { get; set; }
        public string? GUEST_TYPE { get; set; }
        public string? ATTACHMENT_LOCATION { get; set; }
        public string? INTERNAL_YN { get; set; }
        public string? DATABASE_ID { get; set; }
        public string? ATTACHMENT_OWNER { get; set; }
        public string? MINUTES_BEFORE_ALERT { get; set; }
        public string? GLOBAL_YN { get; set; }
        public string? TIMEZONE_CONVERTED_YN { get; set; }
        public string? ORIG_WO_NUMBER { get; set; }
        public DateTime? PROPOSAL_SENT_DATE { get; set; }
        public string? PROPOSAL_VIEW_TOKEN { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
