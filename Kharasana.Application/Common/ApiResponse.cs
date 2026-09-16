namespace Kharasana.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

/// <summary>
/// مُنشؤات لردود <see cref="ApiResponse{T}"/> الموحّدة للنجاح والفشل — تبني
/// <see cref="ApiResponse{Object}"/> بشكلٍ متساوٍ وتُبقي الرسائل وحدها حقلاً مقدّماً.
/// </summary>
public static class ApiResponse
{
    /// <summary>رد نجاح بدون بيانات (Success=true، Data=null).</summary>
    public static ApiResponse<object> Ok(string message) => new()
    {
        Success = true,
        Message = message,
        Data = null
    };

    /// <summary>رد نجاح يحمل بيانات (Success=true).</summary>
    public static ApiResponse<object> Ok(object data, string message) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    /// <summary>رد فشل بدون بيانات (Success=false، Data=null).</summary>
    public static ApiResponse<object> Fail(string message) => new()
    {
        Success = false,
        Message = message,
        Data = null
    };
}