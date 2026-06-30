using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models
{
    public class LogModel
    {
        public int id { get; set; }
        public string usertype { get; set; }      // 使用者类型: system/web/api/TES/RCS
        public string logtype { get; set; }       // 日志类型
        public string level { get; set; }         // 日志级别
        public string message { get; set; }       // 日志内容
        public string module { get; set; }        // 模块名称
        public string operation { get; set; }     // 操作描述
        public string details { get; set; }       // 详细内容
        public string userId { get; set; }        // 操作人员ID
        public string ipaddress { get; set; }     // IP地址
        public string createtime { get; set; }    // 创建时间
        public string isarchived { get; set; } = "False"; // 是否已归档
    }
}