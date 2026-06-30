using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.PLC
{
    public class PlcSendMes
    {
        public int id { get; set; }//主键id
        public string PlcIp { get; set; }//PLC设备编号
        public string DbData { get; set; }//PLC设备名称
        public string MessType { get; set; }//PLC设备名称
        public string UnitID { get; set; }//PLC设备名称
        public string FromLocation { get; set; }//PLC设备名称
        public string ToLocation { get; set; }//PLC设备名称
        public string UnitHigh { get; set; }//PLC设备名称
        public string UnitWeigh { get; set; }//PLC设备名称
        public string ReasonCode { get; set; }//PLC设备名称
        public string CanWrite { get; set; }//PLC设备名称
    }
}