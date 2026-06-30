using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.wcs
{
    public class WmsTaskModel
    {
        public string WmsId { get; set; }
        public string Palno { get; set; }
        public string FromLocation { get; set; }
        public string ToLocation { get; set; }
        public string IfPicking { get; set; }
        public string Num { get; set; }
        public string TaskType { get; set; }

    }
}