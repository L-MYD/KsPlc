using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KsPlc.Models
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T data { get; set; }

        public ApiResponse(int code, string message, T dat)
        {
            Code = code;
            Message = message;
            data = dat;
        }

        // 成功响应快捷方法
        public static ApiResponse<T> Success(T data)
        {
            return new ApiResponse<T>(200, "Success", data);
        }
        // 错误响应快捷方法
        public static ApiResponse<T> Error(string message, int code = 500)
        {
            return new ApiResponse<T>(code, message, default(T));
        }
    }
}