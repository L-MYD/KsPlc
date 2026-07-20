using Dapper;
using KsPlc.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace KsPlc.Mapper
{
    public class LogMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串,mysql数据库，表名为wcs_systemlogs
                                                                                  // 插入日志

        public static bool Insert(LogModel log)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = @"
                INSERT INTO wcs_systemlogs 
                (usertype, logtype, level, message, module, operation, details, userId, ipaddress, createtime, isarchived) 
                VALUES 
                (@usertype, @logtype, @level, @message, @module, @operation, @details, @userId, @ipaddress, @createtime, @isarchived)";

                    int result = connection.Execute(sql, log);
                    return result > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"插入日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 批量插入日志
        public static bool InsertBatch(List<LogModel> logs)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = @"
                INSERT INTO wcs_systemlogs 
                (usertype, logtype, level, message, module, operation, details, userId, ipaddress, createtime, isarchived) 
                VALUES 
                (@usertype, @logtype, @level, @message, @module, @operation, @details, @userId, @ipaddress, @createtime, @isarchived)";

                    int result = connection.Execute(sql, logs);
                    return result == logs.Count;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"批量插入日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 根据ID查询日志
        public static LogModel GetById(int id)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "SELECT * FROM wcs_systemlogs WHERE id = @id";
                    return connection.QueryFirstOrDefault<LogModel>(sql, new { id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"查询日志失败: {ex.Message}");
                    return null;
                }
            }
        }

        // 查询所有日志
        public static List<LogModel> GetAll()
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "SELECT * FROM wcs_systemlogs ORDER BY createtime DESC";
                    return connection.Query<LogModel>(sql).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"查询所有日志失败: {ex.Message}");
                    return new List<LogModel>();
                }
            }
        }

        // 根据条件查询日志
        public static List<LogModel> GetByCondition(string level = null, string module = null, string userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "SELECT * FROM wcs_systemlogs WHERE 1=1";
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(level))
                    {
                        sql += " AND level = @level";
                        parameters.Add("level", level);
                    }

                    if (!string.IsNullOrEmpty(module))
                    {
                        sql += " AND module = @module";
                        parameters.Add("module", module);
                    }

                    if (!string.IsNullOrEmpty(userId))
                    {
                        sql += " AND userId = @userId";
                        parameters.Add("userId", userId);
                    }

                    if (startDate.HasValue)
                    {
                        sql += " AND createtime >= @startDate";
                        parameters.Add("startDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    if (endDate.HasValue)
                    {
                        sql += " AND createtime <= @endDate";
                        parameters.Add("endDate", endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    sql += " ORDER BY createtime DESC";

                    return connection.Query<LogModel>(sql, parameters).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"条件查询日志失败: {ex.Message}");
                    return new List<LogModel>();
                }
            }
        }

        // 分页查询日志
        public static (List<LogModel> Data, int TotalCount) GetByPage(int pageIndex, int pageSize, string level = null, string module = null)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string whereSql = "WHERE 1=1";
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(level))
                    {
                        whereSql += " AND level = @level";
                        parameters.Add("level", level);
                    }

                    if (!string.IsNullOrEmpty(module))
                    {
                        whereSql += " AND module = @module";
                        parameters.Add("module", module);
                    }

                    // 查询总数
                    string countSql = $"SELECT COUNT(1) FROM wcs_systemlogs {whereSql}";
                    int totalCount = connection.ExecuteScalar<int>(countSql, parameters);

                    // 查询分页数据 - MySQL分页语法
                    string dataSql = $@"
                SELECT * FROM wcs_systemlogs 
                {whereSql} 
                ORDER BY createtime DESC 
                LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";

                    var data = connection.Query<LogModel>(dataSql, parameters).ToList();

                    return (data, totalCount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"分页查询日志失败: {ex.Message}");
                    return (new List<LogModel>(), 0);
                }
            }
        }

        // 更新日志
        public static bool Update(LogModel log)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = @"
                UPDATE wcs_systemlogs SET 
                usertype = @usertype, 
                logtype = @logtype, 
                level = @level, 
                message = @message, 
                module = @module, 
                operation = @operation, 
                details = @details, 
                userId = @userId, 
                ipaddress = @ipaddress, 
                createtime = @createtime, 
                isarchived = @isarchived 
                WHERE id = @id";

                    int result = connection.Execute(sql, log);
                    return result > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"更新日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 根据ID删除日志
        public static bool Delete(int id)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "DELETE FROM wcs_systemlogs WHERE id = @id";
                    int result = connection.Execute(sql, new { id });
                    return result > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"删除日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 批量删除日志
        public static bool DeleteBatch(List<int> ids)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "DELETE FROM wcs_systemlogs WHERE id IN @ids";
                    int result = connection.Execute(sql, new { ids });
                    return result > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"批量删除日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 归档日志
        public static bool ArchiveLogs(DateTime beforeDate)
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = "UPDATE wcs_systemlogs SET isarchived = 'True' WHERE createtime <= @beforeDate AND isarchived = 'False'";
                    int result = connection.Execute(sql, new { beforeDate = beforeDate.ToString("yyyy-MM-dd HH:mm:ss") });
                    return result > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"归档日志失败: {ex.Message}");
                    return false;
                }
            }
        }

        // 获取日志统计信息
        public static dynamic GetLogStatistics()
        {
            using (IDbConnection connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    string sql = @"
                SELECT 
                    level,
                    COUNT(*) as count
                FROM wcs_systemlogs 
                GROUP BY level
                ORDER BY count DESC";

                    return connection.Query(sql).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"获取日志统计失败: {ex.Message}");
                    return null;
                }
            }
        }

        // 根据条件分页查询系统日志（usertype为system）
        //public static async Task<(List<LogModel> Data, int TotalCount)> GetSystemLogsByConditionAsync(LogQueryParams queryParams, int skipCount)
        //{
        //    using (IDbConnection connection = new MySqlConnection(SqlConn))
        //    {
        //        try
        //        {
        //            string whereSql = "WHERE usertype = 'system'";
        //            var parameters = new DynamicParameters();

        //            // 时间范围条件
        //            if (!string.IsNullOrEmpty(queryParams.startDate) && DateTime.TryParse(queryParams.startDate, out var startDate))
        //            {
        //                whereSql += " AND createtime >= @startDate";
        //                parameters.Add("startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
        //            }

        //            if (!string.IsNullOrEmpty(queryParams.endDate) && DateTime.TryParse(queryParams.endDate, out var endDate))
        //            {
        //                whereSql += " AND createtime <= @endDate";
        //                parameters.Add("endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        //            }

        //            // 日志级别条件
        //            if (!string.IsNullOrEmpty(queryParams.level))
        //            {
        //                whereSql += " AND level = @level";
        //                parameters.Add("level", queryParams.level);
        //            }

        //            // 查询总数
        //            string countSql = $"SELECT COUNT(1) FROM wcs_systemlogs {whereSql}";
        //            int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        //            // 查询分页数据 - MySQL分页语法
        //            string dataSql = $@"
        //        SELECT * FROM wcs_systemlogs 
        //        {whereSql} 
        //        ORDER BY createtime DESC 
        //        LIMIT {queryParams.pageSize} OFFSET {skipCount}";

        //            var data = (await connection.QueryAsync<LogModel>(dataSql, parameters)).ToList();

        //            return (data, totalCount);
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Trace.WriteLine($"分页查询系统日志失败: {ex.Message}");
        //            return (new List<LogModel>(), 0);
        //        }
        //    }
        //}

        // 获取WEB日志
        //public static async Task<(List<LogModel> Data, int TotalCount)> GetWebLogsByConditionAsync(LogQueryParams queryParams, int skipCount)
        //{
        //    return await GetLogsByUserTypeAsync(queryParams, skipCount, "web");
        //}

        //// 获取TES/RCS日志
        //public static async Task<(List<LogModel> Data, int TotalCount)> GetTesRcsLogsByConditionAsync(LogQueryParams queryParams, int skipCount)
        //{
        //    return await GetLogsByUserTypeAsync(queryParams, skipCount, new[] { "TES", "RCS" });
        //}

        //// 获取WCS-API日志
        //public static async Task<(List<LogModel> Data, int TotalCount)> GetWcsApiLogsByConditionAsync(LogQueryParams queryParams, int skipCount)
        //{
        //    return await GetLogsByUserTypeAsync(queryParams, skipCount, "api");
        //}

        //// 通用查询方法
        //private static async Task<(List<LogModel> Data, int TotalCount)> GetLogsByUserTypeAsync(LogQueryParams queryParams, int skipCount, string userType)
        //{
        //    return await GetLogsByUserTypeAsync(queryParams, skipCount, new[] { userType });
        //}

        //private static async Task<(List<LogModel> Data, int TotalCount)> GetLogsByUserTypeAsync(LogQueryParams queryParams, int skipCount, string[] userTypes)
        //{
        //    using (IDbConnection connection = new MySqlConnection(SqlConn))
        //    {
        //        try
        //        {
        //            string whereSql = "WHERE usertype IN @userTypes";
        //            var parameters = new DynamicParameters();
        //            parameters.Add("userTypes", userTypes);

        //            // 时间范围
        //            if (!string.IsNullOrEmpty(queryParams.startDate) && DateTime.TryParse(queryParams.startDate, out var startDate))
        //            {
        //                whereSql += " AND createtime >= @startDate";
        //                parameters.Add("startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
        //            }

        //            if (!string.IsNullOrEmpty(queryParams.endDate) && DateTime.TryParse(queryParams.endDate, out var endDate))
        //            {
        //                whereSql += " AND createtime <= @endDate";
        //                parameters.Add("endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        //            }

        //            // 日志级别
        //            if (!string.IsNullOrEmpty(queryParams.level))
        //            {
        //                whereSql += " AND level = @level";
        //                parameters.Add("level", queryParams.level);
        //            }

        //            // 总数
        //            string countSql = $"SELECT COUNT(1) FROM wcs_systemlogs {whereSql}";
        //            int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        //            // 分页数据 - MySQL分页语法
        //            string dataSql = $@"
        //        SELECT * FROM wcs_systemlogs 
        //        {whereSql} 
        //        ORDER BY createtime DESC 
        //        LIMIT {queryParams.pageSize} OFFSET {skipCount}";

        //            var data = (await connection.QueryAsync<LogModel>(dataSql, parameters)).ToList();

        //            return (data, totalCount);
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Trace.WriteLine($"查询日志失败: {ex.Message}");
        //            return (new List<LogModel>(), 0);
        //        }
        //    }
        //}
    }
}