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
    public string? ApiResponseMessage { get; }
    public string? TraceId { get; }

    public ApiServiceException(
        HttpStatusCode statusCode,
        string userMessage,
        string? apiResponseMessage = null,
        string? traceId = null,
        Exception? innerException = null)
        : base(userMessage, innerException)
    {
        StatusCode = statusCode;
        ApiResponseMessage = apiResponseMessage;
        TraceId = traceId;
    }
}
