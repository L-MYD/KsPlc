
using KsPlc.Mapper;
using KsPlc.Models.PLC;
using Newtonsoft.Json;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace KsPlc.Service
{
    public class PlcScheduledTask
    {
        private Timer _timer;
        private bool _isRunning = false;
        private readonly object _lockObj = new object();

        /// <summary>
        /// 启动定时任务
        /// </summary>
        public void Start(int intervalSeconds = 5)
        {
            System.Diagnostics.Trace.WriteLine($"启动PLC发送定时任务，间隔: {intervalSeconds}秒");
            _timer = new Timer(SendDataToPlc, null, 0, intervalSeconds * 1000);
        }

        /// <summary>
        /// 停止定时任务
        /// </summary>
        public void Stop()
        {
            _timer?.Dispose();
            System.Diagnostics.Trace.WriteLine("停止PLC发送定时任务");
        }

        /// <summary>
        /// 发送数据到PLC
        /// </summary>
        private void SendDataToPlc(object state)
        {
            if (_isRunning) return;

            lock (_lockObj)
            {
                _isRunning = true;

                try
                {
                    // 1. 从数据库查询需要发送的数据
                    var dataList = PlcSendMesMapper.GetAll();

                    if (dataList != null && dataList.Count > 0)
                    {
                        System.Diagnostics.Trace.WriteLine($"发现 {dataList.Count} 条待发送数据");

                        // 2. 循环处理每条数据
                        foreach (var data in dataList)
                        {
                            ProcessSingleData(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"PLC发送任务异常: {ex.Message}");
                }
                finally
                {
                    _isRunning = false;
                }
            }
        }

        /// <summary>
        /// 处理单个数据
        /// </summary>
        private void ProcessSingleData(KsPlc.Models.PLC.PlcSendMes data)
        {
            try
            {
                // 在发送前检查PLC状态
                int dbBlock = ParseDbNumber(data.DbData);

                // 检查PLC是否可写
                bool canWrite = CheckPlcCanWrite(data.PlcIp, dbBlock);

                if (!canWrite)
                {
                    System.Diagnostics.Trace.WriteLine($"PLC {data.PlcIp} DB{dbBlock} 尚未读完上一条数据，跳过发送");
                    return; // 直接返回，不发送
                }

                // 3. 构建字节数组（76字节）
                byte[] dataBytes = BuildDataBytes(data);

                // 创建日志记录
                PLCMessageLog mes = new PLCMessageLog();

                // 从数据库记录中获取PLC IP地址，而不是使用 this.IpAddress
                mes.plcip = data.PlcIp;

                mes.direction = "Send";
                mes.messagecontent = JsonConvert.SerializeObject(data);
                mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");

                // 插入日志
                PLClogMapper.InsertMessageLog(mes);

                // 4. 写入到PLC
                bool success = WriteToPlc(data.PlcIp, dbBlock, 0, dataBytes);

                if (success)
                {
                    System.Diagnostics.Trace.WriteLine($"发送成功: PLC={data.PlcIp}, DB={dbBlock}, 单元={data.UnitID}");

                    // 6. 发送成功后删除数据库记录
                    KsPlc.Mapper.PlcSendMesMapper.Delete(data.id);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"发送失败: PLC={data.PlcIp}, DB={dbBlock}, 单元={data.UnitID}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"处理数据 {data.id} 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查PLC是否可写（CanWrite是否为"10"）
        /// </summary>
        private bool CheckPlcCanWrite(string ip, int dbBlock)
        {
            try
            {
                using (var plc = new Plc(CpuType.S71200, ip, 0, 1))
                {
                    plc.Open();

                    if (plc.IsConnected)
                    {
                        // 读取DB块中的CanWrite字段
                        // 根据您的数据格式，CanWrite位于偏移72位置，长度4字节
                        byte[] canWriteBytes = plc.ReadBytes(DataType.DataBlock, dbBlock, 72, 4);

                        // 解析CanWrite值
                        // S7字符串格式：第一个字节是最大长度，第二个字节是实际长度
                        if (canWriteBytes.Length >= 2)
                        {
                            int actualLength = canWriteBytes[1];
                            if (actualLength >= 2)
                            {
                                string canWriteValue = System.Text.Encoding.ASCII.GetString(canWriteBytes, 2, 2);
                                System.Diagnostics.Trace.WriteLine($"PLC {ip} DB{dbBlock} CanWrite状态: {canWriteValue}");
                                return canWriteValue == "10";
                            }
                        }
                        return false;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"PLC连接失败，无法检查状态: IP={ip}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"检查PLC {ip} DB{dbBlock} 状态异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 构建76字节的数据包（S7字符串格式）
        /// 每个字段都是西门子S7字符串格式（带2个长度字节）
        /// </summary>
        private byte[] BuildDataBytes(KsPlc.Models.PLC.PlcSendMes data)
        {
            byte[] dataBytes = new byte[76];
            int currentIndex = 0;

            // MessType: 总长度4字节 = 2长度字节 + 最多2字符
            WriteS7StringField(dataBytes, ref currentIndex, 4, data.MessType);

            // UnitID: 总长度32字节 = 2长度字节 + 最多30字符
            WriteS7StringField(dataBytes, ref currentIndex, 32, data.UnitID);

            // FromLocation: 总长度6字节 = 2长度字节 + 最多4字符
            WriteS7StringField(dataBytes, ref currentIndex, 6, data.FromLocation);

            // ToLocation: 总长度6字节 = 2长度字节 + 最多4字符
            WriteS7StringField(dataBytes, ref currentIndex, 6, data.ToLocation);

            // UnitHigh: 总长度6字节 = 2长度字节 + 最多4字符
            WriteS7StringField(dataBytes, ref currentIndex, 6, data.UnitHigh);

            // UnitWeigh: 总长度8字节 = 2长度字节 + 最多6字符
            WriteS7StringField(dataBytes, ref currentIndex, 8, data.UnitWeigh);

            // ReasonCode: 总长度10字节 = 2长度字节 + 最多8字符
            WriteS7StringField(dataBytes, ref currentIndex, 10, data.ReasonCode);

            // CanWrite: 总长度4字节 = 2长度字节 + 最多2字符
            WriteS7StringField(dataBytes, ref currentIndex, 4, data.CanWrite);

            return dataBytes;
        }

        /// <summary>
        /// 写入单个S7字符串字段
        /// totalLength: 字段总字节数（包含2个长度字节）
        /// </summary>
        private void WriteS7StringField(byte[] data, ref int startIndex, int totalLength, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            // 计算最大字符串长度（总长度减去2个长度字节）
            int maxStringLength = totalLength - 2;

            // 实际字符串长度（不能超过最大字符串长度）
            int actualLength = Math.Min(value.Length, maxStringLength);

            // 第一个字节：最大长度（字符串最大字符数）
            data[startIndex] = (byte)maxStringLength;

            // 第二个字节：实际长度（字符串实际字符数）
            data[startIndex + 1] = (byte)actualLength;

            // 从第三个字节开始写入字符串字符
            if (actualLength > 0)
            {
                byte[] stringBytes = System.Text.Encoding.ASCII.GetBytes(value);
                int copyLength = Math.Min(stringBytes.Length, maxStringLength);
                Array.Copy(stringBytes, 0, data, startIndex + 2, copyLength);
            }

            // 剩余部分填充为0
            for (int i = startIndex + 2 + actualLength; i < startIndex + totalLength; i++)
            {
                data[i] = 0;
            }

            // 移动到下一个字段的起始位置
            startIndex += totalLength;
        }

        /// <summary>
        /// 解析DbData字段获取DB块号
        /// </summary>
        private int ParseDbNumber(string dbData)
        {
            if (string.IsNullOrEmpty(dbData))
            {
                System.Diagnostics.Trace.WriteLine("DbData字段为空，使用默认DB101");
                return 101;
            }

            try
            {
                dbData = dbData.Trim();

                // 如果包含"DB"前缀，去掉
                if (dbData.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
                {
                    dbData = dbData.Substring(2);
                }

                if (int.TryParse(dbData, out int dbNumber))
                {
                    return dbNumber;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"无法解析DbData字段: {dbData}，使用默认DB101");
                    return 101;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"解析DbData字段异常: {ex.Message}，使用默认DB101");
                return 101;
            }
        }

        /// <summary>
        /// 写入数据到PLC（S7-1200）
        /// </summary>
        private bool WriteToPlc(string ip, int dbBlock, int startAddress, byte[] data)
        {
            try
            {
                using (var plc = new Plc(CpuType.S71200, ip, 0, 1))
                {
                    plc.Open();

                    if (plc.IsConnected)
                    {
                        plc.WriteBytes(DataType.DataBlock, dbBlock, startAddress, data);
                        System.Diagnostics.Trace.WriteLine($"写入PLC成功: IP={ip}, DB={dbBlock}, Address={startAddress}, 数据长度={data.Length}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"PLC连接失败: IP={ip}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"写入PLC {ip} 失败: {ex.Message}");
                return false;
            }
        }

    }
}