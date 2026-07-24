using Dapper;
using KsPlc.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace KsPlc.Mapper
{
    public class LocationInfoMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串

        public static LocationInfoModel FindByLocationcode(string locationcode)
        {
            string sql = "SELECT * FROM wcs_locationinfo WHERE locationcode = @locationcode";
            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.QueryFirstOrDefault<LocationInfoModel>(sql, new { locationcode });
            }
        }
        /// <summary>
        /// 更新记录
        /// </summary>
        public static int Update(LocationInfoModel item)
        {
            string sql = @"
                UPDATE wcs_locationinfo
                SET status = @status              
                WHERE locationcode = @locationcode";
            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, item);
            }
        }
        /// <summary>
        /// 更新记录
        /// </summary>
        public static int UpdateCode(LocationInfoModel item)
        {
            string sql = @"
                UPDATE wcs_locationinfo
                SET  status = @status,
                    containercode = @containercode
                WHERE locationcode = @locationcode";
            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, item);
            }
        }
        /// <summary>
        /// 使用 NOT EXISTS 条件更新点位状态（无任务时才能清除），支持多个任务类型的精确匹配
        /// </summary>
        /// <param name="locationcode">点位编码</param>
        /// <param name="taskTypes">任务类型列表（至少一个）</param>
        /// <returns>影响行数（1表示清除成功，0表示未清除）</returns>
        public static int ClearIfNoTasks(string locationcode, IEnumerable<string> taskTypes)
        {
            if (taskTypes == null || !taskTypes.Any())
                throw new ArgumentException("至少提供一个任务类型", nameof(taskTypes));

            var typeList = taskTypes.ToList();
            // 构建参数化 IN 子句
            string placeholders = string.Join(",", typeList.Select((_, i) => $"@type{i}"));
            string sql = $@"
UPDATE wcs_locationinfo
SET status = 'available', containercode = NULL
WHERE locationcode = @locationcode
  AND NOT EXISTS (
      SELECT 1 FROM wcs_taskinfo
      WHERE TaskType IN ({placeholders})
        AND TaskStatus IN ('assigned', 'executing')
  )";

            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                var parameters = new DynamicParameters();
                parameters.Add("locationcode", locationcode);
                for (int i = 0; i < typeList.Count; i++)
                {
                    parameters.Add($"type{i}", typeList[i]);
                }
                return connection.Execute(sql, parameters);
            }
        }
    }
}