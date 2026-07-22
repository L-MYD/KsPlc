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
    public class Plc6Controller : PlcControllerBase
    {
        // PLC6配置 - 读取DB5和DB6
        private const int DATA_BLOCK = 5;        // 主数据块DB5
        private const int STATUS_BLOCK = 6;      // 状态数据块DB6
        private const int START_ADDRESS = 0;
        private const int DATA_LENGTH = 76;
        private const int STATUS_LENGTH = 234;    // DB6长度为138字节

        public Plc6Controller(string ipAddress, short rack = 0, short slot = 1)
            : base("PLC6", ipAddress, rack, slot)
        {
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
            // 这里可以返回包含DB5和DB6数据的复合对象
            return ParseAllData();
        }

        // 读取并解析所有数据
        private object ParseAllData()
        {
            try
            {
                // 读取DB5数据
                byte[] db5Data = base.ReadData(DATA_BLOCK, START_ADDRESS, DATA_LENGTH);
                var db5Parsed = ParseDB5Data(db5Data);

                // 读取DB6数据
                byte[] db6Data = base.ReadData(STATUS_BLOCK, START_ADDRESS, STATUS_LENGTH);
                var db6Parsed = ParseDB6Data(db6Data);

                return new
                {
                    DB5 = db5Parsed,
                    DB6 = db6Parsed,
                    Timestamp = DateTime.Now,
                    SourceIP = this.IpAddress,
                    SourceName = this.PlcName
                };
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"解析PLC6数据异常", "数据解析",
                    $"PLC: {this.PlcName}, 错误: {ex.Message}", "ERROR", this.PlcName);
                return null;
            }
        }

        // 实现抽象方法：ProcessBusinessLogic
        protected override void ProcessBusinessLogic(object parsedData)
        {
            dynamic data = parsedData;

            if (data != null)
            {
                // 处理DB5数据
                if (data.DB5 != null)
                {
                    ProcessDB5BusinessLogic(data.DB5);
                }

                // 处理DB6数据
                if (data.DB6 != null)
                {
                    ProcessDB6BusinessLogic(data.DB6);
                }
            }
        }

        // 解析DB5数据
        private object ParseDB5Data(byte[] rawData)
        {
            if (rawData == null || rawData.Length < DATA_LENGTH)
                return null;

            try
            {
                var MessType_Bytes = rawData.Skip(0).Take(4).ToList();
                var UnitID_Bytes = rawData.Skip(4).Take(32).ToList();
                var FromLocation_Bytes = rawData.Skip(36).Take(6).ToList();
                var ToLocation_Bytes = rawData.Skip(42).Take(6).ToList();
                var UnitHigh_Bytes = rawData.Skip(48).Take(6).ToList();
                var UnitWeigh_Bytes = rawData.Skip(54).Take(8).ToList();
                var ReasonCode_Bytes = rawData.Skip(62).Take(10).ToList();
                var CanWrite_Bytes = rawData.Skip(72).Take(4).ToList();

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
                    Timestamp = DateTime.Now,
                    SourceIP = this.IpAddress,
                    SourceName = this.PlcName,
                    DataBlock = DATA_BLOCK
                };
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"解析DB5数据异常", "数据解析",
                    $"PLC: {this.PlcName}, 错误: {ex.Message}", "ERROR", this.PlcName);
                return null;
            }
        }

        // 解析DB6数据
        private object ParseDB6Data(byte[] rawData)
        {
            if (rawData == null || rawData.Length < STATUS_LENGTH)
            {
                LogService.AddSystemLog($"DB6数据长度不足", "数据解析",
                    $"PLC: {this.PlcName}, 期望: {STATUS_LENGTH}, 实际: {rawData?.Length ?? 0}", "WARN", this.PlcName);
                return null;
            }

            try
            {

                var Sation_Status1 = rawData.Skip(4).Take(38).ToList();    // 4-42
                var Sation_Status2 = rawData.Skip(44).Take(38).ToList();   // 44-82
                var Sation_Status3 = rawData.Skip(84).Take(38).ToList();   // 84-122
                var Sation_Status4 = rawData.Skip(124).Take(38).ToList();   // 122-162
                var Sation_Status5 = rawData.Skip(164).Take(38).ToList();   // 162-202

                var SationStatus1 = Encoding.ASCII.GetString(ClearNullChar(Sation_Status1));
                var SationStatus2 = Encoding.ASCII.GetString(ClearNullChar(Sation_Status2));
                var SationStatus3 = Encoding.ASCII.GetString(ClearNullChar(Sation_Status3));
                var SationStatus4 = Encoding.ASCII.GetString(ClearNullChar(Sation_Status4));
                var SationStatus5 = Encoding.ASCII.GetString(ClearNullChar(Sation_Status5));

                return new
                {
                    SationStatus1,
                    SationStatus2,
                    SationStatus3,
                    SationStatus4,
                    SationStatus5,
                    Timestamp = DateTime.Now,
                    DataBlock = STATUS_BLOCK,
                    SourceIP = this.IpAddress,
                    SourceName = this.PlcName,
                    DataType = "Status"
                };
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"解析DB6数据异常", "数据解析",
                    $"PLC: {this.PlcName}, 错误: {ex.Message}", "ERROR", this.PlcName);
                return null;
            }
        }

        // 处理DB5业务逻辑
        private void ProcessDB5BusinessLogic(object parsedData)
        {
            dynamic data = parsedData;


            // 根据CanWrite字段处理业务逻辑
            //if (data.CanWrite.Equals("01"))
            //{
            //    bool writeResult = WriteStringToPLC(DATA_BLOCK, 72, "10", 4);
            //    // 记录接收到的数据
            //    PLCMessageLog mes = new PLCMessageLog();
            //    mes.plcip = this.IpAddress;
            //    mes.direction = "Receive";
            //    mes.messagecontent = JsonConvert.SerializeObject(data);
            //    mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
            //    PLClogMapper.InsertMessageLog(mes);

            //    // 如果是MR消息类型
            //    if (data.MessType != null && data.MessType.Equals("MR"))
            //    {

            //        string unitId = data.UnitID; // 获取托盘号

            //        if (data.FromLocation.Equals("1201")) {
            //            LocationInfoModel locationInfo = new LocationInfoModel();
            //            locationInfo.locationcode = "FH-T1";
            //            locationInfo.status = "available";
            //            LocationInfoMapper.UpdateCode(locationInfo);
            //            UnBind unBind = new UnBind();
            //            unBind.carrierCode = unitId.Substring(0, 8);
            //            unBind.siteCode = "FH-T1";
            //            WcsApiHttpService.UnBindRcsSation(unBind);
            //        }
            //        if (data.ToLocation.Equals("1203")) {
            //            LocationInfoModel locationInfo2 = new LocationInfoModel();
            //            locationInfo2.locationcode = "FH-T2";
            //            locationInfo2.status = "occupied";
            //            LocationInfoMapper.UpdateCode(locationInfo2);
            //        }
            //    }
            //}
        }
        // 处理DB6业务逻辑
        private void ProcessDB6BusinessLogic(object parsedData)
        {
            dynamic data = parsedData;

            if (data != null)
            {
                try
                {
                    // 记录DB6状态数据到日志表
                    //PLCMessageLog mes = new PLCMessageLog();
                    //mes.plcip = this.IpAddress;
                    //mes.direction = "Receive(DB6)";
                    //mes.messagecontent = JsonConvert.SerializeObject(data);
                    //mes.messagetimestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
                    //PLClogMapper.InsertMessageLog(mes);

                    // 处理每个站点的状态
                    //LogService.AddSystemLog($"PLC6 DB6站点状态", "状态监控",
                    //    $"站点1: {data.SationStatus1}, 站点2: {data.SationStatus2}, 站点3: {data.SationStatus3}, 站点4: {data.SationStatus4}, 站点5: {data.SationStatus3}",
                    //    "INFO", this.PlcName);

                    // 检查站点状态
                    CheckStationStatus(data);
                }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"处理DB6业务逻辑异常", "业务逻辑",
                        $"PLC: {this.PlcName}, 错误: {ex.Message}", "ERROR", this.PlcName);
                }
            }
        }

        // 检查站点状态
        private void CheckStationStatus(dynamic data)
        {
            try
            {
                //站点2状态检查  4003需要----YK-T1
                if (!string.IsNullOrEmpty(data.SationStatus4))
                {
                    string SationCode = data.SationStatus1.Substring(0, 4);
                    string SationStatus = data.SationStatus1.Substring(6, 2);
                    string trayNumber = data.SationStatus1.Substring(8, 8);
                    if (SationCode.Equals("4003"))
                    {
                        if (SationStatus.Equals("10")) // 站点为空
                        {
                            LocationInfoModel la = LocationInfoMapper.FindByLocationcode("YK-T1");
                            LocationInfoModel la2 = LocationInfoMapper.FindByLocationcode("F4-RETURN-02");
                            if (la == null && la2 == null) return;

                            // 只有点位不是 available 状态时，才需要尝试清除（避免重复清除 available 点位）
                            if (la.status != "available")
                            {
                                // 原子操作：如果没有进行中的任务，则清除点位（状态设为 available，容器号清空）
                                int affected = LocationInfoMapper.ClearIfNoTasks("YK-T1", la.lanenumber);
                                if (affected > 0)
                                {
                                    // 清除成功，执行解绑
                                    UnBind unBind = new UnBind();
                                    unBind.carrierCode = la.containercode;
                                    unBind.siteCode = "YK-T1";
                                    WcsApiHttpService.UnBindRcsSation(unBind);
                                }
                                // 如果 affected == 0，说明有进行中的任务，不清除也不解绑
                            }
                            else if(la2.status != "available")
                            {
                                // 原子操作：如果没有进行中的任务，则清除点位（状态设为 available，容器号清空）
                                int affected = LocationInfoMapper.ClearIfNoTasks("F4-RETURN-02", la.lanenumber);
                                if (affected > 0)
                                {
                                    // 清除成功，执行解绑
                                    WcsApiHttpService.ReleaseStations(SationCode);
                                }
                                // 如果 affected == 0，说明有进行中的任务，不清除也不解绑
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"检查站点状态异常", "状态监控",
                    $"PLC: {this.PlcName}, 错误: {ex.Message}", "ERROR", this.PlcName);
            }
        }

        // 辅助方法：清除空字符
        private byte[] ClearNullChar(List<byte> RecvByteList)
        {
            IEnumerable<byte> bytelist = from t in RecvByteList where t > 31 && t < 126 select t;
            List<byte> ByteList = bytelist.ToList();
            return ByteList.ToArray();
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
                LogService.AddSystemLog($"PLC6写入字符串到PLC失败", "PLC通信",
                    $"异常: {ex.Message}, DB{dbBlock}.DBB{startAddress}, 值: {value}", "ERROR", "PLC6");
                return false;
            }

        }
    }
}