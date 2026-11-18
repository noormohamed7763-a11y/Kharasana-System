namespace Kharasana.Web.Services.Api;

/// <summary>
/// يمثل تفاصيل الخطأ المعياري للـ API.
/// يدعم القوالب (Templates) باستخدام {0}, {1} لتمرير سياق الخطأ.
/// </summary>
public sealed record ApiError(
    string ErrorCode,
    string UserMessageTemplate,
    string? UserSolution = null,
    string? DeveloperReason = null)
{
    // خاصية للتوافقية مع الكود القديم
    public string UserMessage => UserMessageTemplate;

    public string FormatMessage(params object[] args) =>
        args.Length > 0 ? string.Format(UserMessageTemplate, args) : UserMessageTemplate;
}
