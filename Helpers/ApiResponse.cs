using System;

namespace StudentInformationSystem.Helpers
{
    /// <summary>
    /// 标准化的 API 响应包装，用于向小程序返回统一格式的数据。
    /// </summary>
    /// <typeparam name="T">返回数据类型。</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}
