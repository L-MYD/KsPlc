using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.PLC
{
    public class PlcMessageModel
    {
        public string MessType { get; set; }//PLC设备编号
        public string UnitID { get; set; }//PLC设备名称
        public string FromLocation { get; set; }//PLC设备名称
        public string ToLocation { get; set; }//PLC设备名称
        public string UnitHigh { get; set; }//PLC设备名称
        public string UnitWeigh { get; set; }//PLC设备名称
        public string ReasonCode { get; set; }//PLC设备名称
        public string CanWrite { get; set; }//PLC设备名称
        public string Timestamp { get; set; }//PLC设备名称
        public string DataBlock { get; set; }//PLC设备名称
        public string SourceIP { get; set; }//PLC设备名称
        public string SourceName { get; set; }//PLC设备名称
        public string DataType { get; set; }//PLC设备名称

    }
}