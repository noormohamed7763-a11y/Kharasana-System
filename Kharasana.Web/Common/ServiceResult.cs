using System.Net.Http;
using Kharasana.Web.Localization;

namespace Kharasana.Web.Common;

/// <summary>
/// حامل نتيجة للعمليات على طبقة الخدمات.
/// بدلاً من إرجاع <c>null</c>/<c>false</c> وفقدان سبب الخطأ،
/// يمرّر هذا الكائن للـ Controller رسالة عربية دقيقة تُعرض مباشرةً للمستخدم،
/// مع فصله عن التفاصيل التقنية التي تبقى في السجلات فقط.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ServiceResult Ok(string? message = null)
        => new() { Succeeded = true, Message = message ?? AppMessages.Common.Ok };

    public static ServiceResult Fail(string message)
        => new() { Succeeded = false, Message = message };

    /// <summary>
    /// يبني نتيجة فشل من استثناء مع رسالة عربية احتياطية،
    /// مكتشفاً نوع الخطأ ليعطي المستخدم رسالة أدق:
    ///   - انقطاع اتصال/مهلة ← رسالة الشبكة
    ///   - أي خطأ آخر ← الرسالة الاحتياطية (مع تسجيل التفاصيل في السجل)
    /// </summary>
    public static ServiceResult Fail(Exception exception, string fallbackMessage)
        => exception is HttpRequestException or TaskCanceledException or TimeoutException
            ? Fail(AppMessages.Common.NetworkError)
            : Fail(fallbackMessage);
}

/// <summary>نسخة تحمل بيانات على النجاح.</summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data, string? message = null)
        => new() { Succeeded = true, Data = data, Message = message ?? AppMessages.Common.Ok };

    public static new ServiceResult<T> Fail(string message)
        => new() { Succeeded = false, Message = message };

    public static new ServiceResult<T> Fail(Exception exception, string fallbackMessage)
        => exception is HttpRequestException or TaskCanceledException or TimeoutException
            ? Fail(AppMessages.Common.NetworkError)
            : Fail(fallbackMessage);
}
