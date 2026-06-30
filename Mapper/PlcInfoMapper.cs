using Dapper;
using KsPlc.Models.PLC;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace KsPlc.Mapper
{
    public class PlcInfoMapper
    {
        public static string SqlConn = ConfigurationManager.AppSettings["WCSSQL"];//获取数据库连接字符串,表名为wcs_plcinfo
        /// <summary>
        /// 获取指定数量的PLC信息列表
        /// </summary>
        /// <param name="count">要获取的记录数量</param>
        /// <returns>PLC信息列表</returns>
        /// <summary>
        /// 获取指定数量的PLC信息列表
        /// </summary>
        /// <param name="count">要获取的记录数量</param>
        /// <returns>PLC信息列表</returns>
        public static List<PlcInfo> GetPlclist(int count)
        {
            string sql = $@"
        SELECT * FROM wcs_plcinfo 
        ORDER BY id DESC
        LIMIT {count}";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    return connection.Query<PlcInfo>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查询PLC信息失败: {ex.Message}");
                return new List<PlcInfo>();
            }
        }

        /// <summary>
        /// 获取PLC信息总数（用于分页）
        /// </summary>
        /// <returns>总记录数</returns>
        public static int GetPlcTotalCount()
        {
            string sql = "SELECT COUNT(1) FROM wcs_plcinfo";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    return connection.ExecuteScalar<int>(sql);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取PLC总数失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 根据状态获取PLC数量
        /// </summary>
        /// <param name="status">状态：online/offline</param>
        /// <returns>数量</returns>
        public static int GetPlcCountByStatus(string status)
        {
            string sql = "SELECT COUNT(1) FROM wcs_plcinfo WHERE isactive = @status";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    return connection.ExecuteScalar<int>(sql, new { status });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"根据状态查询PLC数量失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 根据IP更新PLC状态
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="status">状态</param>
        /// <returns>影响的行数</returns>
        public static int UpdatePlcStatusByIp(string ipAddress, string status)
        {
            string sql = @"
            UPDATE wcs_plcinfo SET 
                isactive = @status,
                lastheartbeat = @lastheartbeat
            WHERE ipaddress = @ipaddress";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();

                    var parameters = new
                    {
                        ipaddress = ipAddress,
                        status = status,
                        lastheartbeat = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    return connection.Execute(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"根据IP更新PLC状态失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 根据IP地址获取PLC信息（异步版本）
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>PLC信息对象，如果未找到返回null</returns>
        public static async Task<PlcInfo> GetPlcInfoByIpAsync(string ipAddress)
        {
            string sql = "SELECT * FROM wcs_plcinfo WHERE ipaddress = @ipaddress";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    await connection.OpenAsync();
                    return await connection.QueryFirstOrDefaultAsync<PlcInfo>(sql, new { ipaddress = ipAddress });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"根据IP查询PLC信息失败: {ex.Message}");
                return null;
            }
        }

        // 以下是新增的常用方法

        /// <summary>
        /// 获取所有在线的PLC设备
        /// </summary>
        /// <returns>在线PLC列表</returns>
        public static List<PlcInfo> GetOnlinePlcs()
        {
            string sql = "SELECT * FROM wcs_plcinfo WHERE isactive = 'online' ORDER BY id";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    return connection.Query<PlcInfo>(sql).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查询在线PLC失败: {ex.Message}");
                return new List<PlcInfo>();
            }
        }

        /// <summary>
        /// 根据PLC编号获取PLC信息
        /// </summary>
        /// <param name="plcCode">PLC设备编号</param>
        /// <returns>PLC信息</returns>
        public static PlcInfo GetPlcByCode(string plcCode)
        {
            string sql = "SELECT * FROM wcs_plcinfo WHERE plccode = @plccode";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    return connection.QueryFirstOrDefault<PlcInfo>(sql, new { plccode = plcCode });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"根据编号查询PLC失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 插入新的PLC设备信息
        /// </summary>
        /// <param name="plcInfo">PLC信息对象</param>
        /// <returns>是否成功</returns>
        public static bool InsertPlc(PlcInfo plcInfo)
        {
            string sql = @"
            INSERT INTO wcs_plcinfo 
            (plccode, plcname, ipaddress, port, model, rack, slot, isactive, lastheartbeat) 
            VALUES 
            (@plccode, @plcname, @ipaddress, @port, @model, @rack, @slot, @isactive, @lastheartbeat)";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    int result = connection.Execute(sql, plcInfo);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"插入PLC信息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新PLC设备信息
        /// </summary>
        /// <param name="plcInfo">PLC信息对象</param>
        /// <returns>是否成功</returns>
        public static bool UpdatePlc(PlcInfo plcInfo)
        {
            string sql = @"
            UPDATE wcs_plcinfo SET 
                plccode = @plccode,
                plcname = @plcname,
                ipaddress = @ipaddress,
                port = @port,
                model = @model,
                rack = @rack,
                slot = @slot,
                isactive = @isactive,
                lastheartbeat = @lastheartbeat
            WHERE id = @id";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    int result = connection.Execute(sql, plcInfo);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新PLC信息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据ID删除PLC设备
        /// </summary>
        /// <param name="id">PLC ID</param>
        /// <returns>是否成功</returns>
        public static bool DeletePlc(int id)
        {
            string sql = "DELETE FROM wcs_plcinfo WHERE id = @id";

            try
            {
                using (var connection = new MySqlConnection(SqlConn))
                {
                    connection.Open();
                    int result = connection.Execute(sql, new { id });
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除PLC失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 分页查询PLC信息
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="status">状态筛选</param>
        /// <returns>分页数据和总数</returns>
        public static (List<PlcInfo> Data, int TotalCount) GetPlcsByPage(int pageIndex, int pageSize, string status = null)
        {
            using (var connection = new MySqlConnection(SqlConn))
            {
                try
                {
                    connection.Open();

                    string whereSql = "WHERE 1=1";
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(status))
                    {
                        whereSql += " AND isactive = @status";
                        parameters.Add("status", status);
                    }

                    // 查询总数
                    string countSql = $"SELECT COUNT(1) FROM wcs_plcinfo {whereSql}";
                    int totalCount = connection.ExecuteScalar<int>(countSql, parameters);

                    // 查询分页数据
                    string dataSql = $@"
                    SELECT * FROM wcs_plcinfo 
                    {whereSql} 
                    ORDER BY id DESC 
                    LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";

                    var data = connection.Query<PlcInfo>(dataSql, parameters).ToList();

                    return (data, totalCount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"分页查询PLC失败: {ex.Message}");
                    return (new List<PlcInfo>(), 0);
                }
            }
        }

    }
}