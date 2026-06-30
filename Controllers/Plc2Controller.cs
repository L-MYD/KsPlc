//using KsPlc.Mapper;
//using KsPlc.Models;
//using KsPlc.Models.PLC;
//using KsPlc.Models.wcs;
//using KsPlc.Service;
//using KsPlc.Service.Http;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace KsPlc.Controllers
//{
//    public class Plc2Controller : PlcControllerBase
//    {
//        // PLC2配置 - 读取DB5和DB6
//        private const int DATA_BLOCK = 5;        // 主数据块DB5
//        private const int START_ADDRESS = 0;
//        private const int DATA_LENGTH = 76;
//        private const int STATUS_BLOCK = 6;      // 状态数据块DB6

//        public Plc2Controller(string ipAddress, short rack = 0, short slot = 1)
//            : base("PLC2", ipAddress, rack, slot)
//        {
//        }

//        // 实现抽象方法：ReadData
//        protected override byte[] ReadData()
//        {
//            // 读取DB5的数据
//            return base.ReadData(DATA_BLOCK, START_ADDRESS, DATA_LENGTH);
//        }

//        // 实现抽象方法：ParseData
//        protected override object ParseData(byte[] rawData)
//        {
//            if (rawData == null || rawData.Length < 76)
//                return null;

//            try
//            {
//                // 使用您原来的截取方式
//                var MessType_Bytes = rawData.Skip(0).Take(4).ToList();
//                var UnitID_Bytes = rawData.Skip(4).Take(32).ToList();
//                var FromLocation_Bytes = rawData.Skip(36).Take(6).ToList();
//                var ToLocation_Bytes = rawData.Skip(42).Take(6).ToList();
//                var UnitHigh_Bytes = rawData.Skip(48).Take(6).ToList();
//                var UnitWeigh_Bytes = rawData.Skip(54).Take(8).ToList();
//                var ReasonCode_Bytes = rawData.Skip(62).Take(10).ToList();
//                var CanWrite_Bytes = rawData.Skip(72).Take(4).ToList();

//                // 使用您原来的转换方法
//                var MessType = Encoding.ASCII.GetString(ClearNullChar(MessType_Bytes));
//                var UnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));
//                var FromLocation = Encoding.ASCII.GetString(ClearNullChar(FromLocation_Bytes)).PadLeft(4, '0');
//                var ToLocation = Encoding.ASCII.GetString(ClearNullChar(ToLocation_Bytes));
//                var UnitHigh = Encoding.ASCII.GetString(ClearNullChar(UnitHigh_Bytes));
//                var UnitWeigh = Encoding.ASCII.GetString(ClearNullChar(UnitWeigh_Bytes));
//                var ReasonCode = Encoding.ASCII.GetString(ClearNullChar(ReasonCode_Bytes));
//                var CanWrite = Encoding.ASCII.GetString(ClearNullChar(CanWrite_Bytes));

//                return new
//                {
//                    MessType,
//                    UnitID,
//                    FromLocation,
//                    ToLocation,
//                    UnitHigh,
//                    UnitWeigh,
//                    ReasonCode,
//                    CanWrite,
//                    Timestamp = DateTime.Now,
//                    SourceIP = this.IpAddress,
//                    SourceName = this.PlcName,
//                    DataBlock = DATA_BLOCK
//                };
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        // 读取DB6状态数据
//        public byte[] ReadStatusData(int start = 0, int length = 76)
//        {
//            return base.ReadData(STATUS_BLOCK, start, length);
//        }

//        // 解析DB6状态数据
//        public object ParseStatusData(byte[] rawData)
//        {
//            if (rawData == null || rawData.Length < 76)
//                return null;

//            try
//            {
//                // 使用相同的解析方式
//                var MessType_Bytes = rawData.Skip(0).Take(4).ToList();
//                var UnitID_Bytes = rawData.Skip(4).Take(32).ToList();
//                var FromLocation_Bytes = rawData.Skip(36).Take(6).ToList();
//                var ToLocation_Bytes = rawData.Skip(42).Take(6).ToList();
//                var UnitHigh_Bytes = rawData.Skip(48).Take(6).ToList();
//                var UnitWeigh_Bytes = rawData.Skip(54).Take(8).ToList();
//                var ReasonCode_Bytes = rawData.Skip(62).Take(10).ToList();
//                var CanWrite_Bytes = rawData.Skip(72).Take(4).ToList();

//                var MessType = Encoding.ASCII.GetString(ClearNullChar(MessType_Bytes));
//                var UnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));
//                var FromLocation = Encoding.ASCII.GetString(ClearNullChar(FromLocation_Bytes)).PadLeft(4, '0');
//                var ToLocation = Encoding.ASCII.GetString(ClearNullChar(ToLocation_Bytes));
//                var UnitHigh = Encoding.ASCII.GetString(ClearNullChar(UnitHigh_Bytes));
//                var UnitWeigh = Encoding.ASCII.GetString(ClearNullChar(UnitWeigh_Bytes));
//                var ReasonCode = Encoding.ASCII.GetString(ClearNullChar(ReasonCode_Bytes));
//                var CanWrite = Encoding.ASCII.GetString(ClearNullChar(CanWrite_Bytes));

//                return new
//                {
//                    MessType,
//                    UnitID,
//                    FromLocation,
//                    ToLocation,
//                    UnitHigh,
//                    UnitWeigh,
//                    ReasonCode,
//                    CanWrite,
//                    Timestamp = DateTime.Now,
//                    DataBlock = STATUS_BLOCK,
//                    SourceIP = this.IpAddress,
//                    SourceName = this.PlcName,
//                    DataType = "Status"
//                };
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        // DB5的写操作
//        public bool WriteDB5(int start, byte[] data)
//        {
//            return WriteData(DATA_BLOCK, start, data);
//        }

//        // DB6的写操作
//        public bool WriteDB6(int start, byte[] data)
//        {
//            return WriteData(STATUS_BLOCK, start, data);
//        }

//        // 实现抽象方法：ProcessBusinessLogic
//        protected override void ProcessBusinessLogic(object parsedData)
//        {
//            bool writeResult = WriteStringToPLC(DATA_BLOCK, 72, "10", 4);
//            dynamic data = parsedData;
//                // 根据CanWrite字段处理业务逻辑
//                if (data.CanWrite.Equals("01"))
//                  {
//                // 记录接收到的数据
//                PLCMessageLog mes = new PLCMessageLog();
//                mes.plcip = this.IpAddress;
//                mes.direction = "Receive";
//                mes.messagecontent = JsonConvert.SerializeObject(data);
//                mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
//                // 插入到数据库
//                PLClogMapper.InsertMessageLog(mes);
//                // 如果是MR消息类型
//                if (data.MessType != null && data.MessType.Equals("MR"))
//                    {

//                        string unitId = data.UnitID;//获取托盘号
//                    string sation = data.ToLocation;//获取站点编号，再根据站点编号去
//                                                        //1014--MJ-1
//                                                        //1018--MJ-2
//                                                        //1020--MJ-3

//                    WmsTaskModel wmsTaskModel = new WmsTaskModel();
//                        wmsTaskModel.Palno = unitId;
//                        switch (sation)
//                        {
//                            case "1014":
//                                wmsTaskModel.FromLocation = "MJ-1";
//                            wmsTaskModel.TaskType = "04";
//                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
//                            wmsTaskModel.IfPicking = "0";
//                            wmsTaskModel.Num = "3";
//                            //参数组装好了过后，去发送请求
//                            WcsApiHttpService.plcAddTask(wmsTaskModel);
//                            break;
//                            case "1018":
//                                wmsTaskModel.FromLocation = "MJ-2";
//                            wmsTaskModel.TaskType = "04";
//                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
//                            wmsTaskModel.IfPicking = "0";
//                            wmsTaskModel.Num = "3";
//                            //参数组装好了过后，去发送请求
//                            WcsApiHttpService.plcAddTask(wmsTaskModel);
//                            break;
//                            case "1020":
//                                wmsTaskModel.FromLocation = "MJ-3";
//                            wmsTaskModel.TaskType = "04";
//                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
//                            wmsTaskModel.IfPicking = "0";
//                            wmsTaskModel.Num = "3";
//                            //参数组装好了过后，去发送请求
//                            WcsApiHttpService.plcAddTask(wmsTaskModel);
//                            break;
//                            default:
//                                break;
//                        }
//                        //wmsTaskModel.TaskType = "04";
//                        //wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
//                        //wmsTaskModel.IfPicking = "0";
//                        //wmsTaskModel.Num = "3";
//                        ////参数组装好了过后，去发送请求
//                        //WcsApiHttpService.plcAddTask(wmsTaskModel);

//                    }
//            }
//        }

//        /// <summary>
//        /// 写入字符串到PLC
//        /// </summary>
//        private bool WriteStringToPLC(int dbBlock, int startAddress, string value, int maxLength)
//        {
//            try
//            {
//                int actualLength = Math.Min(value.Length, maxLength - 2);
//                byte[] dataBytes = new byte[maxLength];
//                dataBytes[0] = (byte)(maxLength - 2);
//                dataBytes[1] = (byte)actualLength;

//                byte[] stringBytes = Encoding.ASCII.GetBytes(value);
//                int copyLength = Math.Min(stringBytes.Length, maxLength - 2);
//                Array.Copy(stringBytes, 0, dataBytes, 2, copyLength);

//                for (int i = 2 + copyLength; i < maxLength; i++)
//                {
//                    dataBytes[i] = 0;
//                }

//                return WriteData(dbBlock, startAddress, dataBytes);
//            }
//            catch (Exception ex)
//            {
//                LogService.AddSystemLog($"PLC2写入字符串到PLC失败", "PLC通信",
//                    $"异常: {ex.Message}, DB{dbBlock}.DBB{startAddress}, 值: {value}", "ERROR", "PLC2");
//                return false;
//            }
//        }

//        // PLC2特定的命令写入方法
//        public bool WriteCommand(string command)
//        {
//            byte[] commandBytes = Encoding.ASCII.GetBytes(command.PadRight(20, '\0'));
//            return WriteData(DATA_BLOCK, 80, commandBytes);
//        }

//        // 辅助方法：清除空字符
//        private byte[] ClearNullChar(List<byte> RecvByteList)
//        {
//            IEnumerable<byte> bytelist = from t in RecvByteList where t > 31 && t < 126 select t;
//            List<byte> ByteList = bytelist.ToList();
//            return ByteList.ToArray();
//        }
//    }
//}
using KsPlc.Mapper;
using KsPlc.Models;
using KsPlc.Models.PLC;
using KsPlc.Models.wcs;
using KsPlc.Service;
using KsPlc.Service.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace KsPlc.Controllers
{
    public class Plc2Controller : PlcControllerBase, IDisposable
    {
        // PLC2配置 - 读取DB5和DB6
        private const int DATA_BLOCK = 5;        // 主数据块DB5
        private const int STATUS_BLOCK = 6;      // 状态数据块DB6
        private const int START_ADDRESS = 0;
        private const int DATA_LENGTH = 76;
        private const int STATUS_LENGTH = 960;    // 假设DB6也是76字节

        // DB6状态轮询定时器
        private Timer _statusPollingTimer;
        private const int STATUS_POLLING_INTERVAL = 2000; // DB6轮询间隔2秒

        public Plc2Controller(string ipAddress, short rack = 0, short slot = 1)
            : base("PLC2", ipAddress, rack, slot)
        {
            // 初始化DB6状态轮询定时器（但先不启动，等连接成功后再启动）
        }

        // 实现抽象方法：ReadData
        protected override byte[] ReadData()
        {
            // 读取DB5的数据
            return base.ReadData(DATA_BLOCK, START_ADDRESS, DATA_LENGTH);
        }

        // 实现抽象方法：ParseData
        protected override object ParseData(byte[] rawData)
        {
            return ParseDB5Data(rawData);
        }

        // 重写连接方法，在连接成功后启动DB6轮询
        public new bool Connect()
        {
            bool result = base.Connect();
            if (result && _isConnected)
            {
                StartStatusPolling();
            }
            return result;
        }

        // 启动DB6状态轮询
        private void StartStatusPolling()
        {
            try
            {
                StopStatusPolling();

                _statusPollingTimer = new Timer(
                    StatusPollCallback,
                    null,
                    1000, // 首次延迟1秒
                    STATUS_POLLING_INTERVAL);

                LogService.AddSystemLog($"启动DB6状态轮询", "PLC轮询",
                    $"PLC名称: {_plcName}, 间隔: {STATUS_POLLING_INTERVAL}ms", "INFO", _plcName);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"启动DB6状态轮询失败", "PLC轮询",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // 停止DB6状态轮询
        private void StopStatusPolling()
        {
            try
            {
                _statusPollingTimer?.Dispose();
                _statusPollingTimer = null;
            }
            catch { }
        }

        // DB6轮询回调
        private void StatusPollCallback(object state)
        {
            if (!_isConnected) return;

            try
            {
                // 读取DB6数据
                byte[] statusData = ReadStatusData();

                if (statusData != null && statusData.Length >= STATUS_LENGTH)
                {
                    // 解析DB6数据
                    object parsedStatusData = ParseDB6Data(statusData);

                    if (parsedStatusData != null)
                    {
                        // 处理DB6业务逻辑
                        ProcessDB6BusinessLogic(parsedStatusData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"DB6状态轮询异常", "PLC轮询",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // 解析DB5数据
        private object ParseDB5Data(byte[] rawData)
        {
            if (rawData == null || rawData.Length < DATA_LENGTH)
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
                var UnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));
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
                    Timestamp = DateTime.Now,
                    SourceIP = this.IpAddress,
                    SourceName = this.PlcName,
                    DataBlock = DATA_BLOCK
                };
            }
            catch
            {
                return null;
            }
        }

        // 解析DB6数据
        private object ParseDB6Data(byte[] rawData)
        {
            if (rawData == null || rawData.Length < STATUS_LENGTH)
                return null;

            try
            {
                // 使用相同的解析方式（假设DB6数据结构与DB5相同）
                var MessType_Bytes = rawData.Skip(0).Take(4).ToList();
                var UnitID_Bytes = rawData.Skip(4).Take(32).ToList();
                var FromLocation_Bytes = rawData.Skip(36).Take(6).ToList();
                var ToLocation_Bytes = rawData.Skip(42).Take(6).ToList();
                var UnitHigh_Bytes = rawData.Skip(48).Take(6).ToList();
                var UnitWeigh_Bytes = rawData.Skip(54).Take(8).ToList();
                var ReasonCode_Bytes = rawData.Skip(62).Take(10).ToList();
                var CanWrite_Bytes = rawData.Skip(72).Take(4).ToList();

                var MessType = Encoding.ASCII.GetString(ClearNullChar(MessType_Bytes));
                var UnitID = Encoding.ASCII.GetString(ClearNullChar(UnitID_Bytes));
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
                    Timestamp = DateTime.Now,
                    DataBlock = STATUS_BLOCK,
                    SourceIP = this.IpAddress,
                    SourceName = this.PlcName,
                };
            }
            catch
            {
                return null;
            }
        }

        // 读取DB6状态数据
        public byte[] ReadStatusData(int start = 0, int length = 76)
        {
            return base.ReadData(STATUS_BLOCK, start, length);
        }

        // DB5的写操作
        public bool WriteDB5(int start, byte[] data)
        {
            return WriteData(DATA_BLOCK, start, data);
        }

        // DB6的写操作
        public bool WriteDB6(int start, byte[] data)
        {
            return WriteData(STATUS_BLOCK, start, data);
        }

        // 实现抽象方法：ProcessBusinessLogic - 处理DB5业务逻辑
        protected override void ProcessBusinessLogic(object parsedData)
        {

            dynamic data = parsedData;

            // 根据CanWrite字段处理业务逻辑
            if (data.CanWrite.Equals("01"))
            {
                bool writeResult = WriteStringToPLC(DATA_BLOCK, 72, "10", 4);
                // 记录接收到的数据
                PLCMessageLog mes = new PLCMessageLog();
                mes.plcip = this.IpAddress;
                mes.direction = "Receive";
                mes.messagecontent = JsonConvert.SerializeObject(data);
                mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
                PLClogMapper.InsertMessageLog(mes);

                // 如果是MR消息类型
                if (data.MessType != null && data.MessType.Equals("MR"))
                {
                    string unitId = data.UnitID; // 获取托盘号
                    string station = data.ToLocation; // 获取站点编号

                    WmsTaskModel wmsTaskModel = new WmsTaskModel();
                    wmsTaskModel.Palno = unitId;

                    switch (station)
                    {
                        case "1014":
                            wmsTaskModel.FromLocation = "MJ-1";
                            wmsTaskModel.TaskType = "04";
                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
                            wmsTaskModel.IfPicking = "0";
                            wmsTaskModel.Num = "3";
                            WcsApiHttpService.plcAddTask(wmsTaskModel);
                            break;
                        case "1018":
                            wmsTaskModel.FromLocation = "MJ-2";
                            wmsTaskModel.TaskType = "04";
                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
                            wmsTaskModel.IfPicking = "0";
                            wmsTaskModel.Num = "3";
                            WcsApiHttpService.plcAddTask(wmsTaskModel);
                            break;
                        case "1020":
                            wmsTaskModel.FromLocation = "MJ-3";
                            wmsTaskModel.TaskType = "04";
                            wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
                            wmsTaskModel.IfPicking = "0";
                            wmsTaskModel.Num = "3";
                            WcsApiHttpService.plcAddTask(wmsTaskModel);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // 处理DB6业务逻辑
        private void ProcessDB6BusinessLogic(object parsedData)
        {
            dynamic data = parsedData;

            if (data != null)
            {
                // 记录DB6状态数据
                LogService.AddSystemLog($"DB6状态数据", "状态监控",
                    $"消息类型: {data.MessType}, 托盘号: {data.UnitID}, 来源位置: {data.FromLocation}, 目标位置: {data.ToLocation}",
                    "INFO", _plcName);

                // 根据CanWrite字段处理DB6业务逻辑
                if (data.CanWrite != null && data.CanWrite.Equals("01"))
                {
                    // 写入确认（如果需要）
                    bool writeResult = WriteStringToPLC(STATUS_BLOCK, 72, "10", 4);

                    if (writeResult)
                    {
                        LogService.AddSystemLog($"PLC2写入DB6 CanWrite成功", "PLC通信",
                            $"DB{STATUS_BLOCK}.DBB72写入值: 10", "DEBUG", "PLC2");
                    }

                    // 处理DB6的特定消息类型
                    if (data.MessType != null)
                    {
                        switch (data.MessType)
                        {
                            case "AL": // 报警消息
                                LogService.AddSystemLog($"PLC2 DB6报警消息", "状态监控",
                                    $"报警代码: {data.ReasonCode}, 位置: {data.FromLocation}", "WARN", "PLC2");
                                break;
                            case "ST": // 状态消息
                                LogService.AddSystemLog($"PLC2 DB6状态更新", "状态监控",
                                    $"设备状态: {data.UnitID}, 当前位置: {data.FromLocation}", "INFO", "PLC2");
                                break;
                            default:
                                break;
                        }
                    }
                }

                // 记录DB6数据到日志表
                PLCMessageLog mes = new PLCMessageLog();
                mes.plcip = this.IpAddress;
                mes.direction = "Receive(DB6)";
                mes.messagecontent = JsonConvert.SerializeObject(data);
                mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
                PLClogMapper.InsertMessageLog(mes);
            }
        }

        /// <summary>
        /// 写入字符串到PLC
        /// </summary>
        private bool WriteStringToPLC(int dbBlock, int startAddress, string value, int maxLength)
        {
            try
            {
                int actualLength = Math.Min(value.Length, maxLength - 2);
                byte[] dataBytes = new byte[maxLength];
                dataBytes[0] = (byte)(maxLength - 2);
                dataBytes[1] = (byte)actualLength;

                byte[] stringBytes = Encoding.ASCII.GetBytes(value);
                int copyLength = Math.Min(stringBytes.Length, maxLength - 2);
                Array.Copy(stringBytes, 0, dataBytes, 2, copyLength);

                for (int i = 2 + copyLength; i < maxLength; i++)
                {
                    dataBytes[i] = 0;
                }

                return WriteData(dbBlock, startAddress, dataBytes);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"PLC2写入字符串到PLC失败", "PLC通信",
                    $"异常: {ex.Message}, DB{dbBlock}.DBB{startAddress}, 值: {value}", "ERROR", "PLC2");
                return false;
            }
        }

        // PLC2特定的命令写入方法
        public bool WriteCommand(string command)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command.PadRight(20, '\0'));
            return WriteData(DATA_BLOCK, 80, commandBytes);
        }

        // 辅助方法：清除空字符
        private byte[] ClearNullChar(List<byte> RecvByteList)
        {
            IEnumerable<byte> bytelist = from t in RecvByteList where t > 31 && t < 126 select t;
            List<byte> ByteList = bytelist.ToList();
            return ByteList.ToArray();
        }

        // 重写Dispose方法以释放状态轮询定时器
        public new void Dispose()
        {
            StopStatusPolling();
            base.Dispose();
        }
    }
}