using System.Net;

namespace Course_management.Dto
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public string Status { get; set; }
        public int Code { get; set; }
        public T Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Request successful", int code = 200)
        {
            return new ApiResponse<T>
            {
                Message = message,
                Status = "success",
                Code = code,
                Data = data
            };
        }

        public static ApiResponse<T> Error(string message = "An error occurred", int code = 400, T? data = default)
        {
            return new ApiResponse<T>
            {
                Message = message,
                Status = "error",
                Code = code,
                Data = data
            };
        }
    }

    // For responses without data
    public class ApiResponse
    {
        public string Message { get; set; }
        public string Status { get; set; }
        public int Code { get; set; }

        public static ApiResponse Success(string message = "Request successful", int code = 200)
        {
            return new ApiResponse
            {
                Message = message,
                Status = "success",
                Code = code
            };
        }

        public static ApiResponse Error(string message = "An error occurred", int code = 400)
        {
            return new ApiResponse
            {
                Message = message,
                Status = "error",
                Code = code
            };
        }
    }
}