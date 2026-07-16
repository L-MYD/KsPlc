using KsPlc.Mapper;
using KsPlc.Service;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace KsPlc.Controllers
{
    public abstract class PlcControllerBase : IDisposable
    {
        // ========== 受保护的字段 ==========
        protected Plc _plc;
        protected Timer _pollingTimer;
        protected Timer _healthCheckTimer;
        protected readonly string _plcName;
        protected readonly string _ipAddress;
        protected readonly short _rack;
        protected readonly short _slot;
        protected bool _isConnected = false;
        protected DateTime _lastSuccessTime = DateTime.MinValue;
        protected readonly object _connectionLock = new object();
        private int _reconnectCount = 0;
        private const int MAX_RECONNECT_RETRY = 50; // 最大重连次数（限制避免无限重试）
        private CancellationTokenSource _reconnectCts;
        private readonly SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _reconnectPending = false;

        // ========== 常量 ==========
        protected const int DEFAULT_POLLING_INTERVAL = 1000;      // 默认轮询间隔1秒
        protected const int DEFAULT_HEALTH_CHECK_INTERVAL = 5000; // 默认健康检查5秒
        protected const int INITIAL_RECONNECT_DELAY = 5000;       // 初始重连延迟5秒

        // ========== 属性 ==========
        public string PlcName => _plcName;
        public string IpAddress => _ipAddress;
        public bool IsConnected => _isConnected;
        public DateTime LastSuccessTime => _lastSuccessTime;
        public int ReconnectCount => _reconnectCount;

        // ========== 事件 ==========
        public event Action<string, bool> OnConnectionStatusChanged;
        public event Action<string, object> OnDataReceived;

        // ========== 构造函数 ==========
        protected PlcControllerBase(string plcName, string ipAddress, short rack, short slot)
        {
            _plcName = plcName;
            _ipAddress = ipAddress;
            _rack = rack;
            _slot = slot;

            // 立即创建PLC对象，但先不连接
            CreatePlcInstance();

            // 启动健康检查（无论连接状态如何）
            StartHealthCheck();

            LogService.AddSystemLog($"PLC控制器创建", "PLC初始化",
                $"PLC名称: {_plcName}, IP: {_ipAddress}, Rack: {_rack}, Slot: {_slot}",
                "INFO", _plcName);
        }

        // ========== 抽象方法 ==========
        protected abstract byte[] ReadData();
        protected abstract object ParseData(byte[] rawData);
        protected abstract void ProcessBusinessLogic(object parsedData);

        // ========== 虚方法 ==========
        public virtual string PlcType => "Unknown";
        protected virtual int PollingInterval => DEFAULT_POLLING_INTERVAL;
        protected virtual int HealthCheckInterval => DEFAULT_HEALTH_CHECK_INTERVAL;

        // ========== 公共方法 ==========

        /// <summary>
        /// 连接到PLC（首次连接）
        /// </summary>
        public bool Connect()
        {
            lock (_connectionLock)
            {
                try
                {
                    //LogService.AddSystemLog($"开始连接PLC", "PLC连接",
                    //    $"PLC名称: {_plcName}", "INFO", _plcName);

                    // 如果已经连接，直接返回
                    if (_isConnected && _plc != null && _plc.IsConnected)
                    {
                        //LogService.AddSystemLog($"PLC已连接，无需重复连接", "PLC连接",
                        //    $"PLC名称: {_plcName}", "DEBUG", _plcName);
                        return true;
                    }

                    // 关闭已有连接
                    if (_plc != null && _plc.IsConnected)
                    {
                        try
                        {
                            _plc.Close();
                        }
                        catch { /* 忽略关闭异常 */ }
                    }

                    // 创建新的PLC实例
                    CreatePlcInstance();

                    // 建立连接
                    _plc.Open();
                    _isConnected = true;
                    _lastSuccessTime = DateTime.Now;
                    _reconnectCount = 0; // 重置重连计数
                    // 连接成功后取消任何待处理的重连任务
                    CancelPendingReconnect();
                     // 连接成功：更新数据库状态为在线
                    PlcInfoMapper.UpdatePlcStatusByIp(_ipAddress, "online");
                    // 启动数据轮询
                    StartPolling();

                    //LogService.AddSystemLog($"PLC连接成功", "PLC连接",
                    //    $"PLC名称: {_plcName}, IP: {_ipAddress}", "INFO", _plcName);

                    // 触发连接状态变化事件
                    OnConnectionStatusChanged?.Invoke(_plcName, true);

                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    // 连接失败：更新数据库状态为离线
                    PlcInfoMapper.UpdatePlcStatusByIp(_ipAddress, "offline");
                    LogService.AddSystemLog($"PLC连接失败", "PLC连接",
                        $"PLC名称: {_plcName}, 错误: {ex}", "ERROR", _plcName);

                    // 触发连接状态变化事件
                    OnConnectionStatusChanged?.Invoke(_plcName, false);

                    // 启动首次重连（延迟5秒）
                    StartReconnectDelayed(INITIAL_RECONNECT_DELAY);

                    return false;
                }
            }
        }

        /// <summary>
        /// 断开PLC连接
        /// </summary>
        public void Disconnect()
        {
            lock (_connectionLock)
            {
                try
                {
                    //LogService.AddSystemLog($"断开PLC连接", "PLC断开",
                    //    $"PLC名称: {_plcName}", "INFO", _plcName);

                    // 停止轮询
                    StopPolling();

                    // 关闭PLC连接
                    if (_plc != null && _plc.IsConnected)
                    {
                        _plc.Close();
                    }

                    _isConnected = false;

                    //LogService.AddSystemLog($"PLC连接已断开", "PLC断开",
                    //    $"PLC名称: {_plcName}", "INFO", _plcName);

                    // 触发连接状态变化事件
                    OnConnectionStatusChanged?.Invoke(_plcName, false);

                    // 启动重连（延迟10秒）
                    StartReconnectDelayed(10000);
                }
                catch (Exception ex)
                {
                    //LogService.AddSystemLog($"断开PLC连接时出错", "PLC断开",
                    //    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
                }
            }
        }

        /// <summary>
        /// 强制重连
        /// </summary>
        public void ForceReconnect()
        {
            lock (_connectionLock)
            {
                //LogService.AddSystemLog($"强制重连PLC", "PLC重连",
                //    $"PLC名称: {_plcName}, 当前状态: {_isConnected}", "INFO", _plcName);

                // 先断开
                if (_plc != null && _plc.IsConnected)
                {
                    try
                    {
                        _plc.Close();
                    }
                    catch { /* 忽略 */ }
                }

                _isConnected = false;

                // 取消任何待处理重连并立即尝试连接（异步）
                CancelPendingReconnect();
                Task.Run(() => StartReconnectDelayed(0));
            }
        }

        // ========== 受保护的方法 ==========

        /// <summary>
        /// 创建PLC实例
        /// </summary>
        protected void CreatePlcInstance()
        {
            try
            {
                if (_plc != null)
                {
                    try
                    {
                        if (_plc.IsConnected)
                        {
                            _plc.Close();
                        }
                    }
                    catch { /* 忽略 */ }
                }

                _plc = new Plc(CpuType.S71200, _ipAddress, _rack, _slot);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"创建PLC实例失败", "PLC创建",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
                throw;
            }
        }

        /// <summary>
        /// 从PLC读取数据（通用方法）
        /// </summary>
        protected byte[] ReadData(int dataBlock, int startAddress, int length)
        {
            lock (_connectionLock)
            {
                if (!_isConnected || _plc == null || !_plc.IsConnected)
                {
                    // 标记为未连接
                    if (_isConnected)
                    {
                        _isConnected = false;
                        //LogService.AddSystemLog($"检测到PLC连接已断开", "数据读取",
                        //    $"PLC名称: {_plcName}", "WARN", _plcName);
                    }
                    return null;
                }

                try
                {
                    byte[] data = _plc.ReadBytes(DataType.DataBlock, dataBlock, startAddress, length);
                    _lastSuccessTime = DateTime.Now; // 更新最后成功时间
                    return data;
                }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"读取PLC数据失败", "数据读取",
                        $"PLC名称: {_plcName}, DB{dataBlock}, 地址: {startAddress}, 错误: {ex}",
                        "ERROR", _plcName);

                    // 标记为断开连接并触发事件
                    _isConnected = false;
                    OnConnectionStatusChanged?.Invoke(_plcName, false);

                    // 启动重连
                    StartReconnectDelayed(5000);

                    return null;
                }
            }
        }

        /// <summary>
        /// 向PLC写入数据（通用方法）
        /// </summary>
        protected bool WriteData(int dataBlock, int startAddress, byte[] data)
        {
            lock (_connectionLock)
            {
                if (!_isConnected || _plc == null || !_plc.IsConnected)
                {
                    //LogService.AddSystemLog($"无法写入数据，PLC未连接", "数据写入",
                    //    $"PLC名称: {_plcName}", "WARN", _plcName);
                    return false;
                }

                try
                {
                    _plc.WriteBytes(DataType.DataBlock, dataBlock, startAddress, data);
                    _lastSuccessTime = DateTime.Now;

                    //LogService.AddSystemLog($"写入PLC数据成功", "数据写入",
                    //    $"PLC名称: {_plcName}, DB{dataBlock}, 地址: {startAddress}+数据：{data}, 长度: {data.Length}",
                    //    "DEBUG", _plcName);

                    return true;
                }
                catch (Exception ex)
                {
                    //LogService.AddSystemLog($"写入PLC数据失败", "数据写入",
                    //    $"PLC名称: {_plcName}, DB{dataBlock}, 地址: {startAddress}, 错误: {ex.Message}",
                    //    "ERROR", _plcName);

                    // 标记为断开连接
                    _isConnected = false;

                    // 触发连接状态变化事件
                    OnConnectionStatusChanged?.Invoke(_plcName, false);

                    // 启动重连
                    StartReconnectDelayed(5000);

                    return false;
                }
            }
        }

        // ========== 定时器管理 ==========

        /// <summary>
        /// 启动数据轮询
        /// </summary>
        private void StartPolling()
        {
            try
            {
                StopPolling();

                _pollingTimer = new Timer(PollDataCallback, null, 0, PollingInterval);

                //LogService.AddSystemLog($"启动数据轮询", "PLC轮询",
                //    $"PLC名称: {_plcName}, 间隔: {PollingInterval}ms", "INFO", _plcName);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"启动数据轮询失败", "PLC轮询",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        /// <summary>
        /// 停止数据轮询
        /// </summary>
        private void StopPolling()
        {
            try
            {
                _pollingTimer?.Dispose();
                _pollingTimer = null;
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"停止数据轮询失败", "PLC轮询",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        /// <summary>
        /// 启动健康检查
        /// </summary>
        private void StartHealthCheck()
        {
            try
            {
                _healthCheckTimer?.Dispose();

                _healthCheckTimer = new Timer(HealthCheckCallback, null, HealthCheckInterval, HealthCheckInterval);

                //LogService.AddSystemLog($"启动健康检查", "PLC健康检查",
                //    $"PLC名称: {_plcName}, 间隔: {HealthCheckInterval}ms", "DEBUG", _plcName);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"启动健康检查失败", "PLC健康检查",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // ========== 重连逻辑 ==========

        /// <summary>
        /// 启动带延迟的重连（单实例）
        /// </summary>
        private void StartReconnectDelayed(int delayMilliseconds)
        {
            // 如果已经有重连在进行则不重复启动
            if (_reconnectPending) return;

            _reconnectPending = true;
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    if (delayMilliseconds > 0)
                        await Task.Delay(delayMilliseconds, token).ConfigureAwait(false);

                    await ReconnectLoopAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* 取消即可 */ }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"重连任务异常", "PLC重连", $"PLC名称: {_plcName}, 错误: {ex}", "ERROR", _plcName);
                }
                finally
                {
                    _reconnectPending = false;
                }
            });
        }

        private void CancelPendingReconnect()
        {
            try
            {
                _reconnectCts?.Cancel();
                _reconnectCts?.Dispose();
                _reconnectCts = null;
                _reconnectPending = false;
            }
            catch { }
        }

        private async Task ReconnectLoopAsync(CancellationToken token)
        {
            int attempt = 0;

            while (!token.IsCancellationRequested && attempt < MAX_RECONNECT_RETRY && !_isConnected)
            {
                attempt++;
                _reconnectCount = attempt;

                try
                {
                    // 保证只有一个重连尝试在进行
                    await _reconnectSemaphore.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        if (token.IsCancellationRequested) break;

                        // 同步调用 Connect（内部有锁），避免并发冲突
                        var success = false;
                        try
                        {
                            success = Connect();
                        }
                        catch (Exception ex)
                        {
                            LogService.AddSystemLog($"重连尝试异常", "PLC重连", $"PLC名称: {_plcName}, 错误: {ex}", "WARN", _plcName);
                            success = false;
                        }

                        if (success)
                        {
                            // 连接成功，退出循环
                            return;
                        }
                    }
                    finally
                    {
                        _reconnectSemaphore.Release();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogService.AddSystemLog($"重连循环异常", "PLC重连", $"PLC名称: {_plcName}, 错误: {ex}", "ERROR", _plcName);
                }

                // 计算下次重连延迟（指数退避 + 随机抖动）
                int delay = CalculateReconnectDelay(INITIAL_RECONNECT_DELAY, attempt);
                var jitter = new Random().Next(0, Math.Min(1000, delay / 4));
                delay = Math.Min(60000, delay + jitter);

                try
                {
                    await Task.Delay(delay, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
            }

            LogService.AddSystemLog($"停止重连循环", "PLC重连", $"PLC名称: {_plcName}, 尝试次数: {_reconnectCount}", "INFO", _plcName);
        }

        /// <summary>
        /// 计算重连延迟（指数退避算法）
        /// </summary>
        private int CalculateReconnectDelay(int baseDelay, int attemptCount)
        {
            // 指数退避：5s, 10s, 20s, 40s, 60s, 60s...
            int delay = baseDelay * (int)Math.Pow(2, Math.Min(attemptCount - 1, 4));

            // 最大不超过60秒
            return Math.Min(delay, 60000);
        }

        // ========== 定时器回调 ==========

        /// <summary>
        /// 数据轮询回调
        /// </summary>
        private void PollDataCallback(object state)
        {
            if (!_isConnected) return;

            try
            {
                // 调用抽象方法读取数据
                byte[] rawData = ReadData();

                if (rawData != null && rawData.Length > 0)
                {
                    // 调用抽象方法解析数据
                    object parsedData = ParseData(rawData);

                    if (parsedData != null)
                    {
                        // 触发数据到达事件
                        OnDataReceived?.Invoke(_plcName, parsedData);

                        // 处理业务逻辑
                        ProcessBusinessLogic(parsedData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"数据轮询回调异常", "PLC轮询",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        /// <summary>
        /// 健康检查回调
        /// </summary>
        private void HealthCheckCallback(object state)
        {
            try
            {
                // 如果已连接，检查最后通信时间
                if (_isConnected)
                {
                    // 检查通信超时（30秒无通信认为断开）
                    if ((DateTime.Now - _lastSuccessTime).TotalSeconds > 30)
                    {
                        //LogService.AddSystemLog($"PLC通讯超时", "PLC健康检查",
                        //    $"PLC名称: {_plcName}, 最后成功时间: {_lastSuccessTime}", "WARN", _plcName);

                        lock (_connectionLock)
                        {
                            _isConnected = false;

                            // 触发连接状态变化事件
                            OnConnectionStatusChanged?.Invoke(_plcName, false);

                            // 启动重连
                            StartReconnectDelayed(5000);
                        }
                    }
                }
                else
                {
                    // 如果未连接，检查是否应该重连
                    if (_reconnectCount < MAX_RECONNECT_RETRY)
                    {
                        // 每分钟检查一次，确保重连机制正常工作
                        if (DateTime.Now.Second == 0) // 每分钟的0秒
                        {
                            //LogService.AddSystemLog($"PLC未连接，健康检查触发重连检查", "PLC健康检查",
                            //    $"PLC名称: {_plcName}, 重连次数: {_reconnectCount}", "DEBUG", _plcName);

                            // 检查是否有重连计划，如果没有则计划一个
                            StartReconnectDelayed(5000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //LogService.AddSystemLog($"健康检查异常", "PLC健康检查",
                //    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }

        // ========== 释放资源 ==========
        public void Dispose()
        {
            try
            {
                LogService.AddSystemLog($"释放PLC控制器资源", "资源释放",
                    $"PLC名称: {_plcName}", "INFO", _plcName);

                // 停止所有定时器
                StopPolling();
                _healthCheckTimer?.Dispose();

                // 取消任何待处理的重连任务
                CancelPendingReconnect();

                // 断开连接
                lock (_connectionLock)
                {
                    if (_plc != null)
                    {
                        try
                        {
                            if (_plc.IsConnected)
                            {
                                _plc.Close();
                            }
                            _plc = null;
                        }
                        catch (Exception ex)
                        {
                            LogService.AddSystemLog($"关闭PLC连接时出错", "资源释放",
                                $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
                        }
                    }
                }

                LogService.AddSystemLog($"PLC控制器资源已释放", "资源释放",
                    $"PLC名称: {_plcName}", "INFO", _plcName);
            }
            catch (Exception ex)
            {
                LogService.AddSystemLog($"释放PLC控制器资源时出错", "资源释放",
                    $"PLC名称: {_plcName}, 错误: {ex.Message}", "ERROR", _plcName);
            }
        }
    }
}