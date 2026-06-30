using Dapper;
using KsPlc.Models.PLC;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace KsPlc.Mapper
{
    public class PlcSendMesMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串
        /// <summary>
        /// 添加记录
        /// </summary>
        public static int Insert(PlcSendMes item)
        {
            string sql = @"
                INSERT INTO `wcs_plc-sendmes`
                (PlcIp, DbData, MessType, UnitID, FromLocation, 
                 ToLocation, UnitHigh, UnitWeigh, ReasonCode, CanWrite) 
                VALUES 
                (@PlcIp, @DbData, @MessType, @UnitID, @FromLocation, 
                 @ToLocation, @UnitHigh, @UnitWeigh, @ReasonCode, @CanWrite)";

            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, item);
            }
        }

        /// <summary>
        /// 查询所有记录
        /// </summary>
        public static List<PlcSendMes> GetAll()
        {
            string sql = "SELECT * FROM `wcs_plc-sendmes`";

            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Query<PlcSendMes>(sql).ToList();
            }
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public static int Update(PlcSendMes item)
        {
            string sql = @"
                UPDATE `wcs_plc-sendmes`
                SET PlcIp = @PlcIp, 
                    DbData = @DbData, 
                    MessType = @MessType, 
                    UnitID = @UnitID, 
                    FromLocation = @FromLocation, 
                    ToLocation = @ToLocation, 
                    UnitHigh = @UnitHigh, 
                    UnitWeigh = @UnitWeigh, 
                    ReasonCode = @ReasonCode, 
                    CanWrite = @CanWrite
                WHERE id = @id";

            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, item);
            }
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public static int Delete(int id)
        {
            string sql = "DELETE FROM `wcs_plc-sendmes` WHERE id = @id";

            using (var connection = new MySqlConnection(SqlConn))
            {
                connection.Open();
                return connection.Execute(sql, new { id });
            }
        }
    }
}