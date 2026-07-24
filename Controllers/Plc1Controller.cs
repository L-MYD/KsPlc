using KsPlc.Mapper;
using KsPlc.Models;
using KsPlc.Models.PLC;
using KsPlc.Models.wcs; // MODIFIED: 引入 UnBind 模型用于解绑调用
using KsPlc.Service;
using KsPlc.Service.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
// MODIFIED: 为了添加DB102位监控，新增Threading引用
using System.Threading;

namespace KsPlc.Controllers
{
    public class Plc1Controller : PlcControllerBase
    {
        // PLC1特定的配置
        private const int DATA_BLOCK = 101;
        private const int START_ADDRESS = 0;
        private const int DATA_LENGTH = 76;
        //写的DB块
        private const int DB102_BLOCK = 100;
        private const int DB102= 102;
        // MODIFIED: 新增用于轮询DB102位的定时器
        private Timer _db102MonitorTimer;
        // MODIFIED: 防止重复解绑的标志
        private bool _db102UnbindPerformed = false;
        public Plc1Controller(string ipAddress, short rack = 0, short slot = 1)
            : base("PLC1", ipAddress, rack, slot)
        {
            // MODIFIED: 自动启动 DB102 位轮询监控（按要求自动启用）
            StartDb102Monitor();
        }

        // 实现抽象方法：ReadData
        protected override byte[] ReadData()
        {
            // 调用基类的带参数方法进行实际读取
            return base.ReadData(DATA_BLOCK, START_ADDRESS, DATA_LENGTH);
        }

        // 实现抽象方法：ParseData
        protected override object ParseData(byte[] rawData)
        {
            if (rawData == null || rawData.Length < 76)
                return null;

            try
            {
                // 使用您原来的截取方式
                var MessType_Bytes = rawData.Skip(0).Take(4).ToList();
                var UnitID_Bytes = rawData.Skip(4).Take(32).ToList();
                var FromLocation_Bytes = rawData.Skip(36).Take(6).ToList();
                var ToLocation_Bytes = rawData.Skip(42).Take(6).ToList();
                var UnitHigh_Bytes = rawData.Skip(48).Take(6).ToList();
                var UnitWeigh_Bytes = rawData.Skip(54).Take(8).ToList();
                var ReasonCode_Bytes = rawData.Skip(62).Take(10).ToList();
                var CanWrite_Bytes = rawData.Skip(72).Take(4).ToList();

                // 使用您原来的转换方法
                var MessType = Encoding.ASCII.GetString(ClearNullChar(MessType_Bytes));
                // 原代码
                // var UnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));

                // 新代码
                var rawUnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));
                // 移除尾部 * 或其他填充符（只保留字母和数字）
                var UnitID = new string(rawUnitID.TakeWhile(c => char.IsLetterOrDigit(c)).ToArray());
                var FromLocation = Encoding.ASCII.GetString(ClearNullChar(FromLocation_Bytes)).PadLeft(4, '0');
                var ToLocation = Encoding.ASCII.GetString(ClearNullChar(ToLocation_Bytes));
                var UnitHigh = Encoding.ASCII.GetString(ClearNullChar(UnitHigh_Bytes));
                var UnitWeigh = Encoding.ASCII.GetString(ClearNullChar(UnitWeigh_Bytes));
                var ReasonCode = Encoding.ASCII.GetString(ClearNullChar(ReasonCode_Bytes));
                var CanWrite = Encoding.ASCII.GetString(ClearNullChar(CanWrite_Bytes));

                return new
                {
                    MessType,
                    UnitID,
                    FromLocation,
                    ToLocation,
                    UnitHigh,
                    UnitWeigh,
                    ReasonCode,
                    CanWrite,
                    Timestamp = DateTime.Now
                };
            }
            catch
            {
                return null;
            }
        }
        //DB100的读
        public byte[] ReadDB100(int start, int length)
        {
            return base.ReadData(DB102_BLOCK, start, length);
        }
        //DB100的写
        public bool WriteDB100(int start, byte[] data)
        {
            return WriteData(DB102_BLOCK, start, data);
        }
        // 实现抽象方法：ProcessBusinessLogic
        //这个方法为逻辑方法。实现逻辑
        protected override void ProcessBusinessLogic(object parsedData)
        {
            dynamic data = parsedData;
            if (data.CanWrite.Equals("01"))
            {
                bool writeResult = WriteStringToPLC(DATA_BLOCK, 72, "10", 4);
                PLCMessageLog mes = new PLCMessageLog();
                mes.plcip = this.IpAddress;
                mes.direction = "Receive";
                mes.messagecontent = JsonConvert.SerializeObject(data);
                mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
                PLClogMapper.InsertMessageLog(mes);
                //执行完成逻辑后，将CanWrite改为01发送给plc,当前为DB101.
                //逻辑.............
                if (data.MessType.Equals("ET")) {
                 var station= data.ToLocation;
                    switch (station)
                    {
                        case "3002":
                            WcsApiHttpService.ReleaseStations("3002");
                            break;
                        case "3004":
                            WcsApiHttpService.ReleaseStations("3004");
                            break;
                        case "3006":
                            WcsApiHttpService.ReleaseStations("3006");
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// 写入西门子S7格式字符串到PLC
        /// </summary>
        /// <param name="dbBlock">DB块号</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="value">要写入的字符串值</param>
        /// <param name="maxLength">字符串最大长度（包括长度字节，例如4）</param>
        /// <returns>写入是否成功</returns>
        private bool WriteStringToPLC(int dbBlock, int startAddress, string value, int maxLength)
        {
            try
            {
                // 计算实际长度（不能超过最大长度-2，因为前两个字节是长度信息）
                int actualLength = Math.Min(value.Length, maxLength - 2);

                // 创建字节数组，总长度为maxLength
                byte[] dataBytes = new byte[maxLength];

                // 第一个字节：最大长度（不包括前两个长度字节）
                dataBytes[0] = (byte)(maxLength - 2);

                // 第二个字节：实际长度
                dataBytes[1] = (byte)actualLength;

                // 从第三个字节开始写入字符串字符
                byte[] stringBytes = Encoding.ASCII.GetBytes(value);
                int copyLength = Math.Min(stringBytes.Length, maxLength - 2);
                Array.Copy(stringBytes, 0, dataBytes, 2, copyLength);

                // 剩余部分填充为0（可选，西门子字符串通常不需要）
                for (int i = 2 + copyLength; i < maxLength; i++)
                {
                    dataBytes[i] = 0;
                }

                // 写入到PLC
                return WriteData(dbBlock, startAddress, dataBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"写入字符串到PLC失败: {ex.Message}");
                LogService.AddSystemLog($"写入字符串到PLC失败", "PLC通信",
                    $"异常: {ex.Message}, DB{dbBlock}.DBB{startAddress}, 值: {value}", "ERROR", "PLC1");
                return false;
            }
        }

        // PLC1特定的方法
        public bool WriteCommand(string command)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command.PadRight(20, '\0'));

            return WriteData(DATA_BLOCK, 80, commandBytes);
        }

        // MODIFIED: 启动/停止 DB102 位的监控，读取 DB102.DBX0.1
        private void StartDb102Monitor(int intervalMs = 2000)
        {
            try
            {
                StopDb102Monitor();
                _db102MonitorTimer = new Timer(Db102MonitorCallback, null, 0, intervalMs);
                LogService.AddSystemLog("启动DB102位监控", "PLC监控", $"PLC: {_plcName}, 间隔: {intervalMs}ms", "INFO", _plcName);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("启动DB102位监控失败", "PLC监控", $"错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        private void StopDb102Monitor()
        {
            try
            {
                _db102MonitorTimer?.Dispose();
                _db102MonitorTimer = null;
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("停止DB102位监控失败", "PLC监控", $"错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // 轮询回调：读取 DB102.DBX0.1 位，若为 false 并且 AGV-OUT-01 状态为 available，则执行解绑
        private void Db102MonitorCallback(object state)
        {
            try
            {
                // 读取 DB102 第0字节
                byte[] data = base.ReadData(DB102, 0, 1);
                if (data == null || data.Length == 0) return;

                // DBX0.1 -> byte0 的 bit1 (从0开始计数)
                bool bitValue = ((data[0] >> 1) & 0x1) == 1;

                if (!bitValue)
                {
                    // 只有在尚未执行解绑的情况下才进行操作，防止重复解绑
                    if (_db102UnbindPerformed) return;

                    var la = LocationInfoMapper.FindByLocationcode("AGV-OUT-01");
                    if (la != null && la.status != "available")
                    {
                        try
                        {
                            // 拆分任务类型（支持中文逗号、英文逗号，并去除空格）
                            var separators = new[] { '，', ',' };
                            var taskTypes = la.lanenumber
                                              .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(t => t.Trim())
                                              .ToList();

                            // 如果 la.lanenumber 为空或拆分后为空，需根据业务决定是否直接清除（但通常应有值）
                            if (!taskTypes.Any())
                            {
                                System.Diagnostics.Trace.WriteLine($"点位 AGV-OUT-01 的 lanenumber 为空，无法检查任务，不清除点位");
                                return;  // 退出 CheckStationStatus 方法，或只退出当前站点的处理
                            }
                            // 原子操作：如果没有进行中的任务，则清除点位（状态设为 available，容器号清空）
                            int affected = LocationInfoMapper.ClearIfNoTasks("AGV-OUT-01", taskTypes);
                            if (affected > 0)
                            {
                                // 清除成功，执行解绑
                                UnBind unBind = new UnBind();
                                unBind.carrierCode = la.containercode;
                                unBind.siteCode = "AGV-OUT-01";
                                WcsApiHttpService.UnBindRcsSation(unBind);// 调用AGV解绑接口
                                WcsApiHttpService.ReleaseStations("AGV-OUT-01");// 调用WCS释放站台接口
                            }

                            // 标记已解绑，避免重复请求
                            _db102UnbindPerformed = true;

                            LogService.AddSystemLog("DB102位触发解绑", "PLC监控",
                                $"PLC: {_plcName}, DB102.DBX0.1=false, 已对 AGV-OUT-01 解绑: {la.containercode}", "INFO", _plcName);
                        }
                        catch (Exception ex)
                        {
                            LogService.AddSystemLog("执行解绑失败", "PLC监控",
                                $"错误: {ex.Message}", "ERROR", _plcName);
                        }
                    }
                }
                else
                {
                    // 当位恢复为 true 时，清除已解绑标志，以便后续再次触发解绑
                    if (_db102UnbindPerformed)
                    {
                        _db102UnbindPerformed = false;
                        LogService.AddSystemLog("DB102位恢复，清除解绑标志", "PLC监控", $"PLC: {_plcName}", "DEBUG", _plcName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("DB102位监控异常", "PLC监控", $"错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // 辅助方法
        //private string GetStringFromBytes(byte[] data, int start, int length)
        //{
        //    if (data == null || data.Length < start + length)
        //        return string.Empty;

        //    var bytes = data.Skip(start).Take(length).Where(b => b > 31 && b < 126).ToArray();
        //    return Encoding.ASCII.GetString(bytes);
        //}
        private byte[] ClearNullChar(List<byte> RecvByteList)
        {
            IEnumerable<byte> bytelist = from t in RecvByteList where t > 31 && t < 126 select t;
            List<byte> ByteList = bytelist.ToList();
            return ByteList.ToArray();
        }
        // MODIFIED: 当控制器释放时停止DB102监控
        public new void Dispose()
        {
            StopDb102Monitor();
            base.Dispose();
        }
    }
}