using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Name_Adress
    {
        public string? ADDRESS_ID {get;set;}
        public string? NAME_ID {get;set;}
        public string? ADDRESS_TYPE {get;set;}
        public DateTime? INSERT_DATE {get;set;}
        public string? INSERT_USER {get;set;}
        public DateTime? UPDATE_DATE {get;set;}
        public string? UPDATE_USER {get;set;}
        public DateTime? BEGIN_DATE {get;set;}
        public DateTime? END_DATE {get;set;}
        public string? ADDRESS1 {get;set;}
        public string? ADDRESS2 {get;set;}
        public string? ADDRESS3 {get;set;}
        public string? ADDRESS4 {get;set;}
        public string? CITY {get;set;}
        public string? COUNTRY {get;set;}
        public string? PROVINCE {get;set;}
        public string? STATE {get;set;}
        public string? ZIP_CODE {get;set;}
        public DateTime? INACTIVE_DATE {get;set;}
        public string? PRIMARY_YN {get;set;}
        public string? FOREIGN_COUNTRY {get;set;}
        public string? IN_CARE_OF {get;set;}
        public string? CITY_EXT {get;set;}
        public string? LAPTOP_CHANGE {get;set;}
        public string? LANGUAGE_CODE {get;set;}
        public string? CLEANSED_STATUS {get;set;}
        public DateTime? CLEANSED_DATETIME {get;set;}
        public string? CLEANSED_ERRORMSG {get;set;}
        public string? CLEANSED_VALIDATIONSTATUS {get;set;}
        public string? CLEANSED_MATCHSTATUS {get;set;}
        public string? BARCODE {get;set;} 
        public string? LAST_UPDATED_RESORT {get;set;}
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
