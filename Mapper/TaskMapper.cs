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
    public class TaskMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串
        /// <summary>
        /// 查询指定任务类型下，状态为 assigned 或 executing 的任务列表
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>符合条件的任务集合</returns>
        public static List<TaskModel> GetTasksByTypeInProgress(string taskType)
        {
            string sql = @"
            SELECT *
            FROM wcs_taskinfo
            WHERE TaskType = @TaskType
              AND TaskStatus IN ('assigned', 'executing')";
            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Query<TaskModel>(sql, new { TaskType = taskType }).ToList();
            }
        }
    }
}