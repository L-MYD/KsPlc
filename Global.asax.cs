using KsPlc.App_Start;
using KsPlc.Controllers;
using KsPlc.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace KsPlc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        //protected void Application_Start()
        //{
        //    AreaRegistration.RegisterAllAreas();
        //    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        //    RouteConfig.RegisterRoutes(RouteTable.Routes);
        //    BundleConfig.RegisterBundles(BundleTable.Bundles);

        //}
        // PLC管理器实例
        // PLC管理器实例
        private static PlcManager _plcManager;

        // 定时任务实例
        private static PlcScheduledTask _plcScheduledTask;

        protected void Application_Start()
        {
            try
            {
                LogService.AddSystemLog("应用程序启动开始", "PLC系统启动", "Application_Start方法开始执行", "INFO", "Startup");

                // 1. 基础MVC配置
                AreaRegistration.RegisterAllAreas();
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                
                LogService.AddSystemLog("MVC配置完成", "PLC系统初始化", "所有基础配置完成", "INFO", "Startup");
                System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);
                // 2. 初始化PLC管理器
                LogService.AddSystemLog("开始初始化PLC管理器", "PLC启动", "创建PLC控制器并添加到管理器", "INFO", "PLC");
                InitializePlcManager();

                // 3. 启动定时任务
                LogService.AddSystemLog("开始启动PLC发送定时任务", "定时任务", "启动PLC数据发送定时任务", "INFO", "Task");
                StartPlcScheduledTask();

                LogService.AddSystemLog("应用程序启动完成", "PLC系统初始化", "所有初始化步骤执行完毕", "INFO", "Startup");
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("应用程序启动失败", "PLC系统初始化",
                    $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}",
                    "FATAL", "Startup");
                throw;
            }
        }

        /// <summary>
        /// 初始化PLC管理器
        /// </summary>
        private void InitializePlcManager()
        {
            try
            {
                // 获取PLC管理器单例
                _plcManager = PlcManager.Instance;

                // PLC1配置,立体库plcip
                var plc1 = new Plc1Controller(
                    ipAddress: "192.168.30.104",
                    rack: 0,
                    slot: 1
                );
                _plcManager.AddController(plc1);

                // PLC2配置.灭菌区plc
                var plc2 = new Plc2Controller(
                    ipAddress: "192.168.30.85",
                    rack: 0,
                    slot: 1
                );
                _plcManager.AddController(plc2);
                // PLC3配置,灭菌区提升机plc ip
                var plc3 = new Plc3Controller(
                    ipAddress: "192.168.30.80",
                    rack: 0,
                    slot: 1
               );
                _plcManager.AddController(plc3);
                // PLC5配置,发货区提升机plc ip
                var plc5 = new Plc5Controller(
                    ipAddress: "192.168.30.110",
                    rack: 0,
                    slot: 1
               );
                _plcManager.AddController(plc5);
                // PLC6配置,内销平库区牙叉式提升机plc ip
                var plc6 = new Plc6Controller(
                    ipAddress: "192.168.30.102",
                    rack: 0,
                    slot: 1
               );
                _plcManager.AddController(plc6);
               // // PLC7配置,内销平库区入库提升机plc ip
               // var plc7 = new Plc7Controller(
               //     ipAddress: "192.168.30.122",
               //     rack: 0,
               //     slot: 1
               //);
               // _plcManager.AddController(plc7);
                LogService.AddSystemLog("PLC控制器配置完成", "PLC初始化",
                    $"已配置 {_plcManager.GetAllControllers().Count} 个PLC控制器", "INFO", "PLC");

                // 启动所有PLC控制器
                _plcManager.StartAllControllers();
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("PLC管理器初始化失败", "PLC初始化",
                    $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}",
                    "ERROR", "PLC");
            }
        }

        /// <summary>
        /// 启动PLC定时任务
        /// </summary>
        private void StartPlcScheduledTask()
        {
            try
            {
                // 创建定时任务实例
                _plcScheduledTask = new PlcScheduledTask();

                // 启动任务，每5秒执行一次
                _plcScheduledTask.Start(5);

                //LogService.AddSystemLog("PLC定时任务已启动", "定时任务",
                //    "定时任务启动成功，间隔5秒执行", "INFO", "Task");
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("启动PLC定时任务失败", "定时任务",
                    $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}",
                    "ERROR", "Task");
            }
        }

        protected void Application_End()
        {
            try
            {
                LogService.AddSystemLog("应用程序开始关闭", "PLC系统关闭", "Application_End方法开始执行", "INFO", "Shutdown");

                // 停止定时任务
                if (_plcScheduledTask != null)
                {
                    _plcScheduledTask.Stop();
                    LogService.AddSystemLog("PLC定时任务已停止", "定时任务", "定时任务已停止", "INFO", "Task");
                }

                // 停止PLC管理器
                if (_plcManager != null)
                {
                    _plcManager.Dispose();
                    LogService.AddSystemLog("PLC管理器已释放", "PLC系统关闭", "所有PLC连接已断开", "INFO", "Shutdown");
                }

                LogService.AddSystemLog("应用程序关闭完成", "PLC系统关闭", "所有资源清理完毕", "INFO", "Shutdown");
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog("应用程序关闭过程中发生异常", "系统关闭",
                    $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}",
                    "ERROR", "Shutdown");
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception != null)
            {
                LogService.AddSystemLog("应用程序未处理异常", "全局异常",
                    $"异常类型: {exception.GetType().Name}, 消息: {exception.Message}",
                    "ERROR", "Global");
            }
        }
    }
}
