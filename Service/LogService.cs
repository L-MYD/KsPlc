using KsPlc.Mapper;
using KsPlc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Service
{
    public class LogService
    {
        // System系统日志
        public static bool AddSystemLog(string message, string operation, string details, string level = "INFO", string module = "System", string userId = "system")
        {
            var log = new LogModel
            {
                usertype = "system",
                logtype = "系统日志",
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = GetLocalIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // Web应用日志
        public static bool AddWebLog(string message, string operation, string details, string userId, string level = "INFO", string module = "Web")
        {
            var log = new LogModel
            {
                usertype = "web",
                logtype = "操作日志",
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = GetClientIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // API接口日志
        public static bool AddApiLog(string message, string operation, string details, string userId = "api", string level = "INFO", string module = "API")
        {
            var log = new LogModel
            {
                usertype = "api",
                logtype = "接口日志",
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = GetClientIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // TES系统日志
        public static bool AddTesLog(string message, string operation, string details, string userId = "tes", string level = "INFO", string module = "TES")
        {
            var log = new LogModel
            {
                usertype = "TES",
                logtype = "TES日志",
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = GetLocalIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // RCS系统日志
        public static bool AddRcsLog(string message, string operation, string details, string userId = "rcs", string level = "INFO", string module = "RCS")
        {
            var log = new LogModel
            {
                usertype = "RCS",
                logtype = "RCS日志",
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = GetLocalIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // 通用方法 - 如果需要更灵活的日志记录
        public static bool AddCustomLog(string userType, string logType, string level, string message, string module, string operation, string details, string userId, string ipAddress = null)
        {
            var log = new LogModel
            {
                usertype = userType,
                logtype = logType,
                level = level,
                message = message,
                module = module,
                operation = operation,
                details = details,
                userId = userId,
                ipaddress = ipAddress ?? GetLocalIpAddress(),
                createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return LogMapper.Insert(log);
        }

        // 获取本地IP地址
        private static string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // 获取客户端IP地址（用于Web和API）
        private static string GetClientIpAddress()
        {
            try
            {
                // 如果在Web环境中
                if (System.Web.HttpContext.Current != null)
                {
                    var request = System.Web.HttpContext.Current.Request;
                    string ip = request.Headers["X-Forwarded-For"] ??
                               request.Headers["X-Real-IP"] ??
                               request.UserHostAddress;

                    if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown")
                        ip = request.UserHostAddress;

                    return ip;
                }
            }
            catch
            {
                // 忽略异常
            }

            return GetLocalIpAddress();
        }
    }
}