using KsPlc.Mapper;
using KsPlc.Models;
using KsPlc.Models.PLC;
using KsPlc.Service;
using KsPlc.Service.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

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
        public Plc1Controller(string ipAddress, short rack = 0, short slot = 1)
            : base("PLC1", ipAddress, rack, slot)
        {
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
                Console.WriteLine($"写入字符串到PLC失败: {ex.Message}");
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
    }
}