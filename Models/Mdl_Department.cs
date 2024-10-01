using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerOperaCloud.Models
{
    public class Mdl_Department
    {
        public string? RESORT { get; set; }
        public string? DEPT_ID { get; set; }
        public string? DEPT_NAME { get; set; }
        public string? DEPT_MANAGER_PAGER { get; set; }
        public DateTime? INACTIVE_DATE { get; set; }
        public string? INSERT_USER { get; set; }
        public DateTime? INSERT_DATE { get; set; }
        public string? UPDATE_USER { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? CAN_DELETE_YN { get; set; }
        public string? ORDER_BY { get; set; }
        public string? MESSAGE_TEXT { get; set; }
        public string? DEPT_EMAIL { get; set; }
        public string? ALLOW_ON_TASK_SHEET_YN { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";

    }
}
