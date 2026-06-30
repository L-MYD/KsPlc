using KsPlc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Service
{
    //此为管理所有plc的管理器
    public class PlcManager : IDisposable
    {
        // 存储所有PLC控制器的字典
        private readonly Dictionary<string, PlcControllerBase> _plcControllers = new Dictionary<string, PlcControllerBase>();

        // 单例实例
        private static PlcManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取PLC管理器单例
        /// </summary>
        public static PlcManager Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ?? (_instance = new PlcManager());
                }
            }
        }

        // 私有构造函数（单例模式）
        private PlcManager()
        {
            LogService.AddSystemLog("PLC管理器初始化", "系统启动", "创建PLC管理器单例", "INFO", "PLC");
        }

        /// <summary>
        /// 添加PLC控制器
        /// </summary>
        /// <param name="controller">PLC控制器</param>
        public void AddController(PlcControllerBase controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (_plcControllers.ContainsKey(controller.PlcName))
            {
                LogService.AddSystemLog($"PLC控制器已存在", "PLC管理",
                    $"PLC名称: {controller.PlcName}", "WARN", "PLC");
                return;
            }

            _plcControllers[controller.PlcName] = controller;

            // 订阅事件
            controller.OnConnectionStatusChanged += OnPlcConnectionStatusChanged;
            controller.OnDataReceived += OnPlcDataReceived;

            LogService.AddSystemLog($"添加PLC控制器", "PLC管理",
                $"PLC名称: {controller.PlcName}, IP: {controller.IpAddress}, 类型: {controller.PlcType}",
                "INFO", "PLC");
        }

        /// <summary>
        /// 启动所有PLC控制器
        /// </summary>
        public void StartAllControllers()
        {
            LogService.AddSystemLog("开始启动所有PLC控制器", "PLC管理",
                $"PLC数量: {_plcControllers.Count}", "INFO", "PLC");

            foreach (var controller in _plcControllers.Values)
            {
                try
                {
                    bool success = controller.Connect();

                    LogService.AddSystemLog($"PLC控制器启动结果", "PLC管理",
                        $"PLC名称: {controller.PlcName}, 连接状态: {(success ? "成功" : "失败")}",
                        success ? "INFO" : "WARN", "PLC");
                }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"PLC控制器启动异常", "PLC管理",
                        $"PLC名称: {controller.PlcName}, 错误: {ex.Message}", "ERROR", "PLC");
                }
            }

            LogService.AddSystemLog("所有PLC控制器启动完成", "PLC管理",
                $"总计PLC数量: {_plcControllers.Count}", "INFO", "PLC");
        }

        /// <summary>
        /// 停止所有PLC控制器
        /// </summary>
        public void StopAllControllers()
        {
            LogService.AddSystemLog("开始停止所有PLC控制器", "PLC管理",
                $"PLC数量: {_plcControllers.Count}", "INFO", "PLC");

            foreach (var controller in _plcControllers.Values)
            {
                try
                {
                    controller.Disconnect();
                    LogService.AddSystemLog($"PLC控制器已停止", "PLC管理",
                        $"PLC名称: {controller.PlcName}", "INFO", "PLC");
                }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"PLC控制器停止异常", "PLC管理",
                        $"PLC名称: {controller.PlcName}, 错误: {ex.Message}", "ERROR", "PLC");
                }
            }
        }

        /// <summary>
        /// 获取指定PLC控制器
        /// </summary>
        public PlcControllerBase GetController(string plcName)
        {
            return _plcControllers.TryGetValue(plcName, out var controller) ? controller : null;
        }

        /// <summary>
        /// 获取所有PLC控制器
        /// </summary>
        public List<PlcControllerBase> GetAllControllers()
        {
            return _plcControllers.Values.ToList();
        }

        /// <summary>
        /// 获取所有PLC状态
        /// </summary>
        public Dictionary<string, bool> GetAllPlcStatus()
        {
            return _plcControllers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.IsConnected
            );
        }

        /// <summary>
        /// PLC连接状态变化事件处理
        /// </summary>
        private void OnPlcConnectionStatusChanged(string plcName, bool isConnected)
        {
            LogService.AddSystemLog($"PLC连接状态变化", "PLC事件",
                $"PLC名称: {plcName}, 状态: {(isConnected ? "已连接" : "已断开")}", "INFO", "PLC");
        }

        /// <summary>
        /// PLC数据接收事件处理
        /// </summary>
        private void OnPlcDataReceived(string plcName, object data)
        {
            // 这里可以统一处理所有PLC的数据，比如记录日志或触发全局事件
            //LogService.AddSystemLog($"收到PLC数据", "PLC事件",
            //    $"PLC名称: {plcName}, 数据类型: {data?.GetType().Name}", "DEBUG", "PLC");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopAllControllers();

            foreach (var controller in _plcControllers.Values)
            {
                try
                {
                    controller.Dispose();
                }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"释放PLC控制器异常", "PLC管理",
                        $"错误: {ex.Message}", "ERROR", "PLC");
                }
            }

            _plcControllers.Clear();

            LogService.AddSystemLog("PLC管理器已释放", "资源释放", "所有资源已清理", "INFO", "PLC");
        }
    }
}