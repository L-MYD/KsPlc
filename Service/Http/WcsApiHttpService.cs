using KsPlc.Models;
using KsPlc.Models.wcs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KsPlc.Service.Http
{
    public class WcsApiHttpService
    {
        private static string apiBaseUrl = ConfigurationManager.AppSettings["WCSApiBaseUrl"];//Wcs-api-IP
        /// <summary>
        /// 释放站台（同步版本）
        /// </summary>
        /// <param name="station">站台信息字符串</param>
        public static void ReleaseStations(string station)
        {
            try
            {
                string url = $"{apiBaseUrl}/api/WCS/releaseStation";

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    // 创建JSON内容（将字符串作为JSON内容）
                    var content = new StringContent(station, Encoding.UTF8, "application/json");

                    // 发送同步POST请求
                    HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                    // 确保响应成功
                    response.EnsureSuccessStatusCode();

                    // 由于Java端返回void，我们不需要读取和处理响应数据
                }
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException)
            {
                throw new Exception($"HTTP请求失败: {ex.InnerException.Message}", ex.InnerException);
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                throw new Exception("请求超时", ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception($"释放站台请求失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 灭菌完成申请库位
        /// </summary>
        /// <param name="wmsTaskModel">站台信息</param>
        public static void plcAddTask(WmsTaskModel wmsTaskModel)
        {
            try
            {
                string url = $"{apiBaseUrl}/api/WCS/plcAddTask";

                // 序列化对象为JSON
                string jsonContent = JsonConvert.SerializeObject(wmsTaskModel);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    // 创建JSON内容
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // 发送同步POST请求
                    HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                    // 确保响应成功
                    response.EnsureSuccessStatusCode();

                    // 如果需要读取响应内容（即使Java端返回void，也可以读取空响应）
                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    // 可选：记录响应日志
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        System.Diagnostics.Trace.WriteLine($"响应内容: {responseContent}");
                    }
                }
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException httpEx)
            {
                throw new Exception($"HTTP请求失败: {httpEx.Message}", httpEx);
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                throw new Exception("请求超时", ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception($"释放站台请求失败: {ex.Message}", ex);
            }
        }
        public static void UnBindRcsSation(UnBind unbind)
        {
            try
            {
                string url = $"{apiBaseUrl}/api/WCS/releaseRcsStation";

                // 序列化对象为JSON
                string jsonContent = JsonConvert.SerializeObject(unbind);

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    // 创建JSON内容
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // 发送同步POST请求
                    HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                    // 确保响应成功
                    response.EnsureSuccessStatusCode();

                    // 如果需要读取响应内容（即使Java端返回void，也可以读取空响应）
                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    // 可选：记录响应日志
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        System.Diagnostics.Trace.WriteLine($"响应内容: {responseContent}");
                    }
                }
            }
            catch (AggregateException ex) when (ex.InnerException is HttpRequestException httpEx)
            {
                throw new Exception($"HTTP请求失败: {httpEx.Message}", httpEx);
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                throw new Exception("请求超时", ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception($"释放站台请求失败: {ex.Message}", ex);
            }
        }
    }
}