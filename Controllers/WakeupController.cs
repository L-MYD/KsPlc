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
        [System.Web.Mvc.Route("plcapi")]
        public async System.Threading.Tasks.Task<JsonResult> PlcServiceApi([FromBody] PlcApiModel model)
        {
            string location = "1061";
            string podid = model.PodId;
            // PLC4配置,缓存区短程提升机plc ip
            // 尝试从 PlcManager 获取已注册的 PLC4 实例，若不存在则创建并注册
            var plc4 = PlcManager.Instance.GetController("PLC4") as Plc4Controller;
            if (plc4 == null)
            {
                plc4 = new Plc4Controller(ipAddress: "192.168.30.124", rack: 0, slot: 1);
                PlcManager.Instance.AddController(plc4);
                plc4.Connect();
            }
            else if (!plc4.IsConnected)
            {
                // 若未连接，尝试异步连接并等待短时间
                await System.Threading.Tasks.Task.Run(() => plc4.Connect()).ConfigureAwait(false);
            }
            bool isError = plc4.GetPLC_ERROR();      // 是否故障
            bool isLow = plc4.GetLOW_DOWN();          // 是否低位状态
            bool isUp = plc4.GetUP_DOWN();            // 是否高位状态

            if (isError == false)
            {
                if (isLow == true)
                {
                    PlcSendMes plcSendMes6 = new PlcSendMes();
                    plcSendMes6.PlcIp = "192.168.30.124";
                    plcSendMes6.DbData = "100";
                    plcSendMes6.MessType = "TO";
                    plcSendMes6.UnitID = podid + "************";
                    plcSendMes6.FromLocation = "1601";
                    plcSendMes6.ToLocation = "1601";
                    plcSendMes6.CanWrite = "01";
                    plcSendMes6.UnitHigh = "0000";
                    plcSendMes6.UnitWeigh = "000000";
                    plcSendMes6.ReasonCode = "00000000";
                    PlcSendMesMapper.Insert(plcSendMes6);
                    // ---- 轮询等待提升机到达高位（异步非阻塞） ----
                    int maxRetries = 30;          // 最多尝试30次
                    int retryInterval = 1000;     // 间隔1秒（毫秒）

                    for (int i = 0; i < maxRetries; i++)
                    {
                        await System.Threading.Tasks.Task.Delay(retryInterval).ConfigureAwait(false);

                        // 在后台线程读取 PLC 状态，避免阻塞请求线程
                        isUp = await System.Threading.Tasks.Task.Run(() => plc4.GetUP_DOWN()).ConfigureAwait(false);
                        if (isUp == true)
                        {
                            LocationInfoModel locationInfo = new LocationInfoModel();
                            locationInfo.locationcode = "YR-T2";
                            locationInfo.status = "occupied";
                            locationInfo.containercode = podid;
                            LocationInfoMapper.UpdateCode(locationInfo);
                            // 保持长连接，避免每次请求断开
                            // 到达高位，成功
                            return Json(ApiResponse<string>.Success("成功"));
                        }
                    }
                    // 保持长连接，避免每次请求断开
                    // 超时未到位，返回失败
                    return Json(ApiResponse<string>.Error("失败"));
                }
            }
            System.Diagnostics.Trace.WriteLine($"故障: {isError}, 低位: {isLow}, 高位: {isUp}");

            // 保持长连接，避免每次请求断开
            // 返回 JSON 格式的成功响应
            return Json(ApiResponse<string>.Success("失败"));
        }

        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.Route("plcapiDB")]
        public async System.Threading.Tasks.Task<JsonResult> PlcServiceApiDB([FromBody] PlcApiModel model)
        {
            string location = "1061";
            string podid = model.PodId;
            // PLC4配置,缓存区短程提升机plc ip
            var plc4 = PlcManager.Instance.GetController("PLC4") as Plc4Controller;
            if (plc4 == null)
            {
                plc4 = new Plc4Controller(ipAddress: "192.168.30.124", rack: 0, slot: 1);
                PlcManager.Instance.AddController(plc4);
                plc4.Connect();
            }
            else if (!plc4.IsConnected)
            {
                await System.Threading.Tasks.Task.Run(() => plc4.Connect()).ConfigureAwait(false);
            }
            bool isError = plc4.GetPLC_ERROR();      // 是否故障
            bool isLow = plc4.GetLOW_DOWN();          // 是否低位状态
            bool isUp = plc4.GetUP_DOWN();            // 是否高位状态

            if (isError == false)
            {
                if (isLow == true)
                {
                    PlcSendMes plcSendMes6 = new PlcSendMes();
                    plcSendMes6.PlcIp = "192.168.30.124";
                    plcSendMes6.DbData = "100";
                    plcSendMes6.MessType = "TO";
                    plcSendMes6.UnitID = podid + "************";
                    plcSendMes6.FromLocation = "1601";
                    plcSendMes6.ToLocation = "1601";
                    plcSendMes6.CanWrite = "01";
                    plcSendMes6.UnitHigh = "0000";
                    plcSendMes6.UnitWeigh = "000000";
                    plcSendMes6.ReasonCode = "00000000";
                    PlcSendMesMapper.Insert(plcSendMes6);
                    // ---- 轮询等待提升机到达高位 ----
                    int maxRetries = 30;          // 最多尝试30次
                    int retryInterval = 1000;     // 间隔1秒（毫秒）

                    for (int i = 0; i < maxRetries; i++)
                    {
                        await System.Threading.Tasks.Task.Delay(retryInterval).ConfigureAwait(false);
                        isUp = await System.Threading.Tasks.Task.Run(() => plc4.GetUP_DOWN()).ConfigureAwait(false);
                        if (isUp == true)
                        {
                            LocationInfoModel locationInfo = new LocationInfoModel();
                            locationInfo.locationcode = "YR-T2";
                            locationInfo.status = "occupied";
                            locationInfo.containercode = podid;
                            LocationInfoMapper.UpdateCode(locationInfo);
                            // 保持长连接，避免每次请求断开
                            // 到达高位，成功
                            return Json(ApiResponse<string>.Success("成功"));
                        }
                    }
                    // 保持长连接，避免每次请求断开
                    // 超时未到位，返回失败
                    return Json(ApiResponse<string>.Error("失败"));
                }
            }
            System.Diagnostics.Trace.WriteLine($"故障: {isError}, 低位: {isLow}, 高位: {isUp}");

            // 保持长连接，避免每次请求断开
            // 返回 JSON 格式的成功响应
            return Json(ApiResponse<string>.Success("失败"));
        }
    }
}
