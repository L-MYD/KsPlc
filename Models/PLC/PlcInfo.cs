using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.PLC
{
    public class PlcInfo
    {
        public int id { get; set; }//主键id
        public string plccode { get; set; }//PLC设备编号
        public string plcname { get; set; }//PLC设备名称
        public string ipaddress { get; set; }//ip地址
        public string port { get; set; }//端口
        public string model { get; set; }//plc型号
        public string rack { get; set; }//机架号
        public string slot { get; set; }//插槽号
        public string isactive { get; set; }//是否在线
        public string lastheartbeat { get; set; }//最后心跳时间
    }
}