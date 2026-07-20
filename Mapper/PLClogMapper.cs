using Dapper;
using KsPlc.Models.PLC;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KsPlc.Mapper
{
    public class PLClogMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串
        /// <summary>
        /// 插入PLC通讯日志记录
        /// </summary>
        /// <param name="log">日志记录对象</param>
        /// <returns>受影响的行数</returns>
        public static int InsertMessageLog(PLCMessageLog log)
        {
            string sql = @"
        INSERT INTO wcs_plcmessagelog (
            plcid, plcip, direction, messagetype, messagecontent,
            rawdata, messagetimestamp, processedtime, responsetime,
            statuscode, errorcode, errormessage, retrycount, isacknowledged
        ) VALUES (
            @plcid, @plcip, @direction, @messagetype, @messagecontent,
            @rawdata, @messagetimestamp, @processedtime, @responsetime,
            @statuscode, @errorcode, @errormessage, @retrycount, @isacknowledged
        )";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    // 改为同步打开连接
                    connection.Open();
                    // 改为同步执行
                    return connection.Execute(sql, log);
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志
                System.Diagnostics.Trace.WriteLine($"插入PLC通讯日志失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 记录发送给PLC的消息
        /// </summary>
        /// <param name="plcId">PLC编号</param>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageContent">消息内容</param>
        /// <param name="rawData">原始数据（可选）</param>
        /// <returns>是否记录成功</returns>
        //public static async Task<bool> LogSendMessageAsync(string plcId, string plcIp, string messageType,
        //    string messageContent, string rawData = "")
        //{
        //    var log = new PLCMessageLog
        //    {
        //        plcid = plcId,
        //        plcip = plcIp,
        //        direction = PLCMessageLog.MessageDirections.Send, // 使用类内定义的常量
        //        messagetype = messageType,
        //        messagecontent = messageContent,
        //        rawdata = rawData,
        //        messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"), // 使用您指定的时间格式
        //        processedtime = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"),
        //        statuscode = PLCMessageLog.StatusCodes.Success // 使用类内定义的常量
        //    };

        //    int result = await InsertMessageLogAsync(log);
        //    return result > 0;
        //}

        /// <summary>
        /// 记录从PLC接收的消息
        /// </summary>
        /// <param name="plcId">PLC编号</param>
        /// <param name="plcIp">PLC IP地址</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageContent">消息内容</param>
        /// <param name="rawData">原始数据（可选）</param>
        /// <param name="responseTime">响应时间（毫秒）</param>
        /// <param name="statusCode">状态码</param>
        /// <returns>是否记录成功</returns>
        //public static async Task<bool> LogReceiveMessageAsync(string plcId, string plcIp, string messageType,
        //    string messageContent, string rawData = "", int responseTime = 0,
        //    string statusCode = PLCMessageLog.StatusCodes.Success) // 使用类内定义的常量
        //{
        //    var log = new PLCMessageLog
        //    {
        //        plcid = plcId,
        //        plcip = plcIp,
        //        direction = PLCMessageLog.MessageDirections.Receive, // 使用类内定义的常量
        //        messagetype = messageType,
        //        messagecontent = messageContent,
        //        rawdata = rawData,
        //        messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"), // 使用您指定的时间格式
        //        processedtime = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"),
        //        responsetime = responseTime,
        //        statuscode = statusCode
        //    };

        //    int result = await InsertMessageLogAsync(log);
        //    return result > 0;
        //}

        /// <summary>
        /// 查询PLC通讯日志
        /// </summary>
        /// <param name="plcId">PLC编号（可选）</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <param name="messageType">消息类型（可选）</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>日志记录列表</returns>
        public static async Task<List<PLCMessageLog>> GetMessageLogsAsync(
            string plcId = null,
            string startTime = null,
            string endTime = null,
            string messageType = null,
            int page = 1,
            int pageSize = 100)
        {
            var whereBuilder = new StringBuilder(" WHERE 1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(plcId))
            {
                whereBuilder.Append(" AND plcid = @plcid");
                parameters.Add("plcid", plcId);
            }

            if (!string.IsNullOrEmpty(startTime))
            {
                whereBuilder.Append(" AND messagetimestamp >= @starttime");
                parameters.Add("starttime", startTime);
            }

            if (!string.IsNullOrEmpty(endTime))
            {
                whereBuilder.Append(" AND messagetimestamp <= @endtime");
                parameters.Add("endtime", endTime);
            }

            if (!string.IsNullOrEmpty(messageType))
            {
                whereBuilder.Append(" AND messagetype = @messagetype");
                parameters.Add("messagetype", messageType);
            }

            string sql = $@"
        SELECT * FROM wcs_plcmessagelog 
        {whereBuilder}
        ORDER BY id DESC
        LIMIT @pagesize OFFSET @offset";

            parameters.Add("offset", (page - 1) * pageSize);
            parameters.Add("pagesize", pageSize);

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return (await connection.QueryAsync<PLCMessageLog>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"查询PLC通讯日志失败: {ex.Message}");
                return new List<PLCMessageLog>();
            }
        }

        /// <summary>
        /// 获取PLC日志列表（分页）
        /// </summary>
        /// <param name="queryParams">查询参数</param>
        /// <returns>数据列表和总数</returns>
        //public static async Task<(List<PLCMessageLog> data, int totalCount)> GetPlcLogListAsync(PlcLogQueryParams queryParams)
        //{
        //    var whereBuilder = new StringBuilder(" WHERE 1=1");
        //    var parameters = new DynamicParameters();

        //    // 时间范围查询
        //    if (!string.IsNullOrEmpty(queryParams.startDate) && DateTime.TryParse(queryParams.startDate, out DateTime startDate))
        //    {
        //        // 使用您指定的时间格式
        //        string startDateStr = startDate.ToString("yyyy:MM:dd HH:mm:ss");
        //        whereBuilder.Append(" AND messagetimestamp >= @startdate");
        //        parameters.Add("startdate", startDateStr);
        //    }

        //    if (!string.IsNullOrEmpty(queryParams.endDate) && DateTime.TryParse(queryParams.endDate, out DateTime endDate))
        //    {
        //        // 结束时间设置为当天的最后一秒，使用您指定的时间格式
        //        string endDateStr = endDate.ToString("yyyy:MM:dd 23:59:59");
        //        whereBuilder.Append(" AND messagetimestamp <= @enddate");
        //        parameters.Add("enddate", endDateStr);
        //    }

        //    // PLC名称/级别查询（模糊查询）
        //    if (!string.IsNullOrEmpty(queryParams.level))
        //    {
        //        whereBuilder.Append(" AND (plcid LIKE CONCAT('%', @level, '%') OR messagetype LIKE CONCAT('%', @level, '%'))");
        //        parameters.Add("level", queryParams.level);
        //    }

        //    // 状态查询（精确匹配）
        //    if (!string.IsNullOrEmpty(queryParams.status))
        //    {
        //        whereBuilder.Append(" AND direction = @status");
        //        parameters.Add("status", queryParams.status);
        //    }

        //    try
        //    {
        //        using (var connection = new MySqlConnection(SqlConn))
        //        {
        //            await connection.OpenAsync();

        //            // 查询总记录数
        //            string countSql = $"SELECT COUNT(1) FROM wcs_plcmessagelog {whereBuilder}";
        //            int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        //            // 查询分页数据 - MySQL分页语法
        //            string dataSql = $@"
        //        SELECT * FROM wcs_plcmessagelog 
        //        {whereBuilder}
        //        ORDER BY id DESC
        //        LIMIT @pagesize OFFSET @offset";

        //            parameters.Add("offset", (queryParams.page - 1) * queryParams.pageSize);
        //            parameters.Add("pagesize", queryParams.pageSize);

        //            var data = (await connection.QueryAsync<PLCMessageLog>(dataSql, parameters)).ToList();

        //            return (data, totalCount);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"查询PLC日志列表失败: {ex.Message}");
        //        return (new List<PLCMessageLog>(), 0);
        //    }
        //}

        // 以下是新增的常用方法

        /// <summary>
        /// 根据ID获取单个PLC日志记录
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>PLC日志记录</returns>
        public static async Task<PLCMessageLog> GetLogByIdAsync(int id)
        {
            string sql = "SELECT * FROM wcs_plcmessagelog WHERE id = @id";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return await connection.QueryFirstOrDefaultAsync<PLCMessageLog>(sql, new { id });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"根据ID查询PLC日志失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定PLC的最新日志记录
        /// </summary>
        /// <param name="plcId">PLC编号</param>
        /// <param name="count">记录数量</param>
        /// <returns>日志记录列表</returns>
        public static async Task<List<PLCMessageLog>> GetRecentLogsByPlcIdAsync(string plcId, int count = 50)
        {
            string sql = @"
        SELECT * FROM wcs_plcmessagelog 
        WHERE plcid = @plcid 
        ORDER BY id DESC 
        LIMIT @count";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return (await connection.QueryAsync<PLCMessageLog>(sql, new { plcid = plcId, count })).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"查询PLC最近日志失败: {ex.Message}");
                return new List<PLCMessageLog>();
            }
        }

        /// <summary>
        /// 更新日志确认状态
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <param name="isAcknowledged">是否已确认</param>
        /// <returns>是否成功</returns>
        public static async Task<bool> UpdateAcknowledgedStatusAsync(int id, int isAcknowledged)
        {
            string sql = "UPDATE wcs_plcmessagelog SET isacknowledged = @isacknowledged WHERE id = @id";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    int result = await connection.ExecuteAsync(sql, new { id, isacknowledged = isAcknowledged });
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"更新日志确认状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除旧的PLC日志记录（用于日志清理）
        /// </summary>
        /// <param name="beforeDate">删除此日期之前的记录</param>
        /// <returns>删除的记录数</returns>
        public static async Task<int> DeleteOldLogsAsync(DateTime beforeDate)
        {
            string sql = "DELETE FROM wcs_plcmessagelog WHERE messagetimestamp < @beforeDate";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return await connection.ExecuteAsync(sql, new { beforeDate = beforeDate.ToString("yyyy:MM:dd HH:mm:ss") });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"删除旧日志失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 获取错误日志统计
        /// </summary>
        /// <param name="days">统计天数</param>
        /// <returns>错误统计信息</returns>
        public static async Task<dynamic> GetErrorStatisticsAsync(int days = 7)
        {
            string sql = @"
        SELECT 
            plcid,
            statuscode,
            COUNT(*) as count
        FROM wcs_plcmessagelog 
        WHERE messagetimestamp >= @startDate
        GROUP BY plcid, statuscode
        ORDER BY count DESC";

            var startDate = DateTime.Now.AddDays(-days).ToString("yyyy:MM:dd HH:mm:ss");

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return (await connection.QueryAsync(sql, new { startDate })).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"获取错误统计失败: {ex.Message}");
                return null;
            }
        }

        ///// <summary>
        ///// 记录错误消息
        ///// </summary>
        ///// <param name="plcId">PLC编号</param>
        ///// <param name="plcIp">PLC IP地址</param>
        ///// <param name="messageType">消息类型</param>
        ///// <param name="messageContent">消息内容</param>
        ///// <param name="errorCode">错误代码</param>
        ///// <param name="errorMessage">错误描述</param>
        ///// <param name="retryCount">重试次数</param>
        ///// <returns>是否记录成功</returns>
        //public static async Task<bool> LogErrorMessageAsync(string plcId, string plcIp, string messageType,
        //    string messageContent, string errorCode, string errorMessage, int retryCount = 0)
        //{
        //    var log = new PLCMessageLog
        //    {
        //        plcid = plcId,
        //        plcip = plcIp,
        //        direction = PLCMessageLog.MessageDirections.Send,
        //        messagetype = messageType,
        //        messagecontent = messageContent,
        //        messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"),
        //        processedtime = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss"),
        //        statuscode = PLCMessageLog.StatusCodes.Error,
        //        errorcode = errorCode,
        //        errormessage = errorMessage,
        //        retrycount = retryCount
        //    };

        //    int result = await InsertMessageLogAsync(log);
        //    return result > 0;
        //}

    }
}