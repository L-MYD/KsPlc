using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models.PLC
{
    public class PLCMessageLog
    {
        /// <summary>
        /// 日志记录ID（自动生成的全数字ID）
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// PLC设备编号
        /// </summary>
        public string plcid { get; set; } = string.Empty;

        /// <summary>
        /// PLC设备IP地址
        /// </summary>
        public string plcip { get; set; } = string.Empty;

        /// <summary>
        /// 消息方向：Send-发送 Receive-接收
        /// </summary>
        public string direction { get; set; } = string.Empty;

        /// <summary>
        /// 消息类型（如：EquipmentControl, SensorStatus, Heartbeat等）
        /// </summary>
        public string messagetype { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容（JSON格式或自定义格式）
        /// </summary>
        public string messagecontent { get; set; } = string.Empty;

        /// <summary>
        /// 原始数据（Base64编码的字符串）
        /// </summary>
        public string rawdata { get; set; } = string.Empty;

        /// <summary>
        /// 消息时间戳（格式：yyyy:MM:dd HH:mm:ss）
        /// </summary>
        public string messagetimestamp { get; set; } = string.Empty;

        /// <summary>
        /// WCS处理时间（格式：yyyy:MM:dd HH:mm:ss）
        /// </summary>
        public string processedtime { get; set; } = string.Empty;

        /// <summary>
        /// 响应时间（毫秒）
        /// </summary>
        public int responsetime { get; set; }

        /// <summary>
        /// 状态码：Success-成功 Warning-警告 Error-错误 Timeout-超时
        /// </summary>
        public string statuscode { get; set; } = string.Empty;

        /// <summary>
        /// 错误代码
        /// </summary>
        public string errorcode { get; set; } = string.Empty;

        /// <summary>
        /// 错误描述信息
        /// </summary>
        public string errormessage { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int retrycount { get; set; }

        /// <summary>
        /// 是否已确认
        /// </summary>
        public int isacknowledged { get; set; }

        /// <summary>
        /// 生成唯一数字ID（基于时间戳+序列号）
        /// </summary>
        /// <returns>全数字的唯一ID</returns>

        /// <summary>
        /// PLC消息方向常量
        /// </summary>
        public static class MessageDirections
        {
            public const string Send = "Send";      // WCS发送给PLC
            public const string Receive = "Receive"; // WCS从PLC接收
        }

        /// <summary>
        /// 状态码常量
        /// </summary>
        public static class StatusCodes
        {
            public const string Success = "Success";  // 成功
            public const string Warning = "Warning";  // 警告
            public const string Error = "Error";      // 错误
            public const string Timeout = "Timeout";  // 超时
        }
    }
}