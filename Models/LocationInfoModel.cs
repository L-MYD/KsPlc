using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models
{
    public class LocationInfoModel
    {
        public int id { get; set; }
        public string locationcode { get; set; }      // 使用者类型: system/web/api/TES/RCS
        public string locationtype { get; set; }       // 日志类型
        public string status { get; set; }       // 日志类型
        public string containercode { get; set; }       // 日志类型
        public string storagetime { get; set; }       // 日志类型
        public string lanenumber { get; set; }       // 日志类型
        public int laneid { get; set; }       // 日志类型
        public int lanesequence { get; set; }       // 日志类型
        public string backup { get; set; }       // 日志类型

    }
}