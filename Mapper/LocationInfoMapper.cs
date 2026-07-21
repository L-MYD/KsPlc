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
        /// 使用 NOT EXISTS 条件更新点位状态（无任务时才能清除），taskType 模糊匹配
        /// </summary>
        public static int ClearIfNoTasks(string locationcode, string taskType)
        {
            string sql = @"
UPDATE wcs_locationinfo
SET status = 'available', containercode = NULL
WHERE locationcode = @locationcode
  AND NOT EXISTS (
      SELECT 1 FROM wcs_taskinfo
      WHERE TaskType LIKE CONCAT('%', @taskType, '%')
        AND TaskStatus IN ('assigned', 'executing')
  )";
            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, new { locationcode, taskType });
            }
        }
    }
}