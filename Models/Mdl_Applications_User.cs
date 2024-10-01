using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Applications_User
    {
        public string? APP_USER_ID { get; set; } = "";
        public string? APP_USER { get; set; } = "";
        public string? APP_PASSWORD { get; set; } = "";
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; } = "";
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; } = "";
        public string? ORACLE_UID { get; set; } = "";
        public string? ORACLE_USER { get; set; } = "";
        public string? ORACLE_PASSWORD { get; set; } = "";
        public DateTime? INACTIVE_DATE { get; set; }
        public string? TITLE { get; set; } = "";
        public string? DEFAULT_FORM { get; set; } = "";
        public string? NAME { get; set; } = "";
        public string? APP_USER_TYPE { get; set; } = "";
        public string? LAST_LOGGED_RESORT { get; set; } = "";
        public string? DEF_CASHIER_ID { get; set; } = "";
        public string? APP_USER_DESCRIPTION { get; set; } = "";
        public string? PERSON_NAME_ID { get; set; } = "";
        public DateTime? DISABLED_UNTIL { get; set; }
        public DateTime? EXPIRES_ON { get; set; }
        public DateTime? LAST_LOGGED_TIMESTAMP { get; set; }
        public string? IS_SUPERUSER { get; set; } = "";
        public string? EMPLOYEE_NUMBER { get; set; } = "";
        public string? GENERAL_FILEPATH { get; set; } = "";
        public string? USER_FILEPATH { get; set; } = "";
        public string? DEFAULT_RESORT { get; set; } = "";
        public string? MAX_USER_SESSIONS { get; set; } = "";
        public string? INTERNAL_YN { get; set; } = "";
        public string? MAX_CHECKOUT_DAYS { get; set; } = "";
        public string? DEFAULT_TERMINAL { get; set; } = "";
        public string? DEFAULT_LANGUAGE { get; set; } = "";
        public string? DEPT_ID { get; set; } = "";
        public string? MALE_FEMALE { get; set; } = "";
        public string? USER_PBX_ID { get; set; } = "";
        public DateTime? DATE_HIRED { get; set; }
        public string? WORK_PERMIT_NO { get; set; } = "";
        public DateTime? WORK_PERMIT_EXPDATE { get; set; }
        public string? RATE_TYPE { get; set; } = "";
        public string? SALARY_INTERVAL { get; set; } = "";
        public string? HOURLY_RATE { get; set; } = "";
        public string? WEEKLY_SALARY { get; set; } = "";
        public string? OT_MULTIPLIER { get; set; } = "";
        public string? HIRE_TYPE { get; set; } = "";
        public string? REHIRE_YN { get; set; } = "";
        public string? EMP_EXTENSION { get; set; } = "";
        public string? EMP_PAGER { get; set; } = "";
        public string? TERM_REASON { get; set; } = "";
        public DateTime? TERMINATED_DATE { get; set; }
        public string? INACTIVE_REASON_CODE { get; set; } = "";
        public DateTime? INACTIVE_FROM { get; set; }
        public DateTime? INACTIVE_TO { get; set; }
        public string? WEEK_MIN { get; set; } = "";
        public string? WEEK_MAX { get; set; } = "";
        public string? MONDAY_MIN { get; set; } = "";
        public string? MONDAY_MAX { get; set; } = "";
        public string? TUESDAY_MIN { get; set; } = "";
        public string? TUESDAY_MAX { get; set; } = "";
        public string? WEDNESDAY_MIN { get; set; } = "";
        public string? WEDNESDAY_MAX { get; set; } = "";
        public string? THURSDAY_MIN { get; set; } = "";
        public string? THURSDAY_MAX { get; set; } = "";
        public string? FRIDAY_MIN { get; set; } = "";
        public string? FRIDAY_MAX { get; set; } = "";
        public string? SATURDAY_MIN { get; set; } = "";
        public string? SATURDAY_MAX { get; set; } = "";
        public string? SUNDAY_MIN { get; set; } = "";
        public string? SUNDAY_MAX { get; set; } = "";
        public string? COMMENTS { get; set; } = "";
        public string? LEAD_ADDRESS { get; set; } = "";
        public string? LEAD_COMM { get; set; } = "";
        public string? LEAD_ADDRESS_DET { get; set; } = "";
        public string? LAPTOP_ID { get; set; } = "";
        public string? HOURS_PER_WEEK { get; set; } = "";
        public string? EMP_STATUS { get; set; } = "";
        public DateTime? PASSWORD_LAST_CHANGE { get; set; }
        public string? PASSWORD_CHANGE_DAYS { get; set; } = "";
        public string? GRACE_LOGIN { get; set; } = "";
        public string? SREP_GROUP { get; set; } = "";
        public string? DEFAULT_REPORTGROUP { get; set; } = "";
        public string? AUTHORIZER_YN { get; set; } = "";
        public DateTime? AUTHORIZER_INACTIVE_DATE { get; set; }
        public string? SFA_NAME { get; set; } = "";
        public string? LOGIN_CRO { get; set; } = "";
        public string? AUTHORIZER_RATE_CODE { get; set; } = "";
        public string? LOGIN_DOMAIN { get; set; } = "";
        public string? RECEIVE_BROADCAST_MSG { get; set; } = "";
        public string? DEFAULT_MFN_RESORT { get; set; } = "";
        public string? MFN_USER_TYPE { get; set; } = "";
        public string? FORCE_PASSWORD_CHANGE_YN { get; set; } = "";
        public string? ACCOUNT_LOCKED_OUT_YN { get; set; } = "";
        public string? PREVENT_ACCOUNT_LOCKOUT { get; set; } = "";
        public DateTime? LOCKOUT_DATE { get; set; }
        public string? ACCESS_PMS { get; set; } = "";
        public string? ACCESS_SC { get; set; } = "";
        public string? ACCESS_CONFIG { get; set; } = "";
        public string? ACCESS_EOD { get; set; } = "";
        public string? ACCESS_UTIL { get; set; } = "";
        public string? ACCESS_ORS { get; set; } = "";
        public string? ACCESS_SFA { get; set; } = "";
        public string? ACCESS_OCIS { get; set; } = "";
        public string? ACCESS_OCM { get; set; } = "";
        public string? ACCESS_OXI { get; set; } = "";
        public string? ACCESS_OXIHUB { get; set; } = "";
        public string? CHAIN_CODE { get; set; } = "";
        public string? APP_USER_UNIQ { get; set; } = "";
        public string? MAX_DAYS_AFTER_CO { get; set; } = "";
        public string? USER_GROUP_ADMIN { get; set; } = "";
        public string? ACCESS_ORMS { get; set; } = "";
        public string? ACCESS_OBI { get; set; } = "";
        public string? SREP_CODE { get; set; } = "";
        public string? LOGIN_ATTEMPTS { get; set; } = "";
        public string? PROPERTY_ACCESS_YN { get; set; } = "";
        public string? ACCESS_SCBI { get; set; } = "";
        public string? TIMEZONE_REGION { get; set; } = "";
        public string? ACCESS_OCRM { get; set; } = "";
        public string? EMPLOYEE_INCENTIVE_NUMBER { get; set; } = "";
        public string? SERVICE_REQUEST_ALERTS_YN { get; set; } = "";
        public string? MOBILE_ALERTS_YN { get; set; } = "";
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
