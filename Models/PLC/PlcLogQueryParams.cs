using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.PLC
{
    public class PlcLogQueryParams
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public string startDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string endDate { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int page { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int pageSize { get; set; } = 10;

        /// <summary>
        /// PLC名称/级别
        /// </summary>
        public string level { get; set; }

        /// <summary>
        /// 状态（读/写）
        /// </summary>
        public string status { get; set; }
    }
}