using KsPlc.Mapper;
using KsPlc.Models;
using KsPlc.Models.PLC;
using KsPlc.Models.wcs;
using KsPlc.Service;
using KsPlc.Service.Http;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace KsPlc.Controllers
{
    [System.Web.Mvc.RoutePrefix("api/Wakeup")]
    public class WakeupController : Controller
    {
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.Route("plc")]
        public JsonResult PlcServiceWakeUp()
        {
            // 添加系统日志
            LogService.AddSystemLog("PlcService已启动(唤醒服务成功执行)", null,
                                     null, null, null, null);

            // 返回 JSON 格式的成功响应
            return Json(ApiResponse<string>.Success("成功"));
        }

        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.Route("plc")]
        public JsonResult PlcServiceApi([FromBody] string podid)
        {
            string location = "1061";
            // PLC4配置,缓存区短程提升机plc ip
            var plc4 = new Plc4Controller(
                ipAddress: "192.168.30.124",
                rack: 0,
                slot: 1
           );
            bool isError = plc4.GetPLC_ERROR();      // 是否故障
            bool isLow = plc4.GetLOW_DOWN();          // 是否低位状态
            bool isUp = plc4.GetUP_DOWN();            // 是否高位状态

            if (isError == false)
            {
                if (isLow == true)
                {
                    PlcSendMes plcSendMes6 = new PlcSendMes();
                    plcSendMes6.PlcIp = "192.168.30.124";
                    plcSendMes6.DbData = "4";
                    plcSendMes6.MessType = "TO";
                    plcSendMes6.UnitID = podid + "************";
                    plcSendMes6.FromLocation = "1301";
                    plcSendMes6.ToLocation = "1304";
                    plcSendMes6.CanWrite = "01";
                    plcSendMes6.UnitHigh = "0000";
                    plcSendMes6.UnitWeigh = "000000";
                    plcSendMes6.ReasonCode = "00000000";
                    PlcSendMesMapper.Insert(plcSendMes6);
                }
                while (isUp)
                {
                    if (isLow == true)
                    {
                        WmsTaskModel wmsTaskModel = new WmsTaskModel();
                        wmsTaskModel.Palno = podid;
                        wmsTaskModel.FromLocation = "DCTSJ-1";
                        wmsTaskModel.TaskType = "04";
                        wmsTaskModel.WmsId = $"I{DateTime.Now.Ticks % 10000000:0000000}";
                        wmsTaskModel.IfPicking = "0";
                        wmsTaskModel.Num = "3";
                        WcsApiHttpService.plcAddTask(wmsTaskModel);

                        PlcSendMes plcSendMes6 = new PlcSendMes();
                        plcSendMes6.PlcIp = "192.168.30.124";
                        plcSendMes6.DbData = "4";
                        plcSendMes6.MessType = "CL";
                        plcSendMes6.UnitID = podid + "************";
                        plcSendMes6.FromLocation = "1301";
                        plcSendMes6.ToLocation = "1304";
                        plcSendMes6.CanWrite = "01";
                        plcSendMes6.UnitHigh = "0000";
                        plcSendMes6.UnitWeigh = "000000";
                        plcSendMes6.ReasonCode = "00000000";
                        PlcSendMesMapper.Insert(plcSendMes6);
                        break;
                    }
                }
            }
            Console.WriteLine($"故障: {isError}, 低位: {isLow}, 高位: {isUp}");


            // 返回 JSON 格式的成功响应
            return Json(ApiResponse<string>.Success("成功"));
        }
    }
}
