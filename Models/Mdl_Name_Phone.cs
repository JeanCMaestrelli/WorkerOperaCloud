using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Name_Phone
    {
        public string? PHONE_ID {get;set;}
        public string? NAME_ID {get;set;}
        public string? PHONE_TYPE {get;set;}
        public string? PHONE_ROLE {get;set;}
        public string? PHONE_NUMBER {get;set;}
        public DateTime? INSERT_DATE {get;set;}
        public string? INSERT_USER {get;set;}
        public DateTime? UPDATE_DATE {get;set;}
        public string? UPDATE_USER {get;set;}
        public DateTime? INACTIVE_DATE {get;set;}
        public DateTime? END_DATE {get;set;}
        public DateTime? BEGIN_DATE {get;set;}
        public string? ADDRESS_ID {get;set;}
        public string? PRIMARY_YN {get;set;}
        public string? DISPLAY_SEQ {get;set;}
        public string? LAPTOP_CHANGE {get;set;}
        public string? INDEX_PHONE {get;set;}
        public string? EXTENSION {get;set;}
        public string? EMAIL_FORMAT {get;set;}
        public string? SHARE_EMAIL_YN {get;set;}
        public string? DEFAULT_CONFIRMATION_YN {get;set;}
        public string? EMAIL_LANGUAGE {get;set;}
        public string? MOBILE_AUDIO_KEY_YN {get;set;}
        public string? COUNTRY_DIALING_CODE {get;set;}
        public string? VALID_YN {get;set;}
        public string? PHONE_COUNTRY_CODE {get;set;}
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
