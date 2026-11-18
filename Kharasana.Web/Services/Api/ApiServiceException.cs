using System.Net;

namespace Kharasana.Web.Services.Api;

/// <summary>
/// استثناء مخصص يُرمى عند فشل استجابة API — يحمل رسالة المستخدم العربية ورمز الحالة.
/// يُستثنى من القاعدة العامة لـ ApiService ويعاد رميه ليتعامل معه Controller مباشرة
/// بدلاً من إرجاع null ثم تخمين سبب الفشل.
/// </summary>
public sealed class ApiServiceException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ApiError Error { get; }
    public object[]? MessageArgs { get; }
    public string? TraceId { get; }

    public ApiServiceException(
        HttpStatusCode statusCode,
        ApiError error,
        object[]? messageArgs = null,
        string? traceId = null,
        Exception? innerException = null)
        : base(error.FormatMessage(messageArgs ?? Array.Empty<object>()), innerException)
    {
        StatusCode = statusCode;
        Error = error;
        MessageArgs = messageArgs;
        TraceId = traceId;
    }

    // Constructor للتوافقية
    public ApiServiceException(
        HttpStatusCode statusCode,
        string userMessage,
        string? apiResponseMessage = null,
        string? traceId = null,
        Exception? innerException = null)
        : base(userMessage, null)
    {
        StatusCode = statusCode;
        Error = new ApiError("UNKNOWN_ERROR", userMessage, null, apiResponseMessage);
        TraceId = traceId;
        MessageArgs = null;
    }
}
