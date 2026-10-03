using Kharasana.Application.Common;

namespace Kharasana.Application.Common;

/// <summary>
/// حامل نتيجة للعمليات على طبقة الخدمات.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ServiceResult Ok(string? message = null)
        => new() { Succeeded = true, Message = message ?? "تم تنفيذ العملية بنجاح." };

    public static ServiceResult Fail(string message)
        => new() { Succeeded = false, Message = message };

    public static ServiceResult Fail(Exception exception, string fallbackMessage)
        => exception is HttpRequestException or TaskCanceledException or TimeoutException
            ? Fail("خطأ في الاتصال بالشبكة.")
            : Fail(fallbackMessage);
}

/// <summary>نسخة تحمل بيانات على النجاح.</summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static new ServiceResult<T> Ok(T data, string? message = null)
        => new() { Succeeded = true, Data = data, Message = message ?? "تم تنفيذ العملية بنجاح." };

    public static new ServiceResult<T> Fail(string message)
        => new() { Succeeded = false, Message = message };

    public static new ServiceResult<T> Fail(Exception exception, string fallbackMessage)
        => exception is HttpRequestException or TaskCanceledException or TimeoutException
            ? Fail("خطأ في الاتصال بالشبكة.")
            : Fail(fallbackMessage);
}
