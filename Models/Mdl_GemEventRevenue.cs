using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_GemEventRevenue
    {
        public string? EVENT_ID { get; set; }
        public string? RESORT { get; set; }
        public string? BOOK_ID { get; set; }
        public string? REV_TYPE { get; set; }
        public string? REV_GROUP { get; set; }
        public string? ORDER_BY { get; set; }
        public string? FORECAST_REVENUE { get; set; }
        public string? EXPECTED_REVENUE { get; set; }
        public string? GUARANTEED_REVENUE { get; set; }
        public string? ACTUAL_REVENUE { get; set; }
        public string? BILLED_REVENUE { get; set; }
        public string? IGNORE_FORECAST_YN { get; set; }
        public string? FLAT_YN { get; set; }
        public string? CUSTOM_YN { get; set; }
        public string? EXPECTED_COST { get; set; }
        public string? GUARANTEED_COST { get; set; }
        public string? ACTUAL_COST { get; set; }
        public string? BILLED_COST { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public string? PKG_REVENUE_YN { get; set; }
        public string? FORECAST_EDITED_YN { get; set; }
        public string? MINIMUM_REVENUE_YN { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
