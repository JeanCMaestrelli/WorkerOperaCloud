using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerOperaCloud.Jobs;

namespace WorkerOperaCloud.Models
{
    public class Mdl_RoomsCombo
    {
        public string? RESORT { get; set; }
        public string? COMBO_ROOM { get; set; }
        public string? COMBO_ELEMENT { get; set; }
        public string? PERCENT_UTILIZED { get; set; }
        public string? AREA_UTILIZED { get; set; }
        public DateTime? RNA_INSERTDATE { get; set; } = null;
        public DateTime? RNA_UPDATEDATE { get; set; } = null;
        public string? DELETED_FLAG { get; set; } = "";
    }
}
