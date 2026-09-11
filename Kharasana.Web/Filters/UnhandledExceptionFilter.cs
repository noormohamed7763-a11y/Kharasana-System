using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.Web.Filters;

/// <summary>
/// فلتر عالمي لالتقاط ApiServiceException الهاربة من أي controller action.
/// يضمن أن المستخدم لا يرى صفحة خطأ 500 فارغة على أخطاء API المتوقعة.
/// - AJAX/fetch → JSON { success=false, message }
/// - GET → إعادة توجيه للصفحة السابقة مع TempData error
/// - POST → إعادة توجيه للصفحة الرئيسية مع TempData error
/// - 401 → إعادة توجيه لصفحة تسجيل الدخول
/// </summary>
public sealed class UnhandledExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<UnhandledExceptionFilter> _logger;

    public UnhandledExceptionFilter(ILogger<UnhandledExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiServiceException apiEx)
            return Task.CompletedTask; // أخطاء غير ApiService تنتقل لـ UseExceptionHandler (500)

        context.ExceptionHandled = true;

        _logger.LogWarning(
            "Unhandled ApiServiceException intercepted by filter: StatusCode={StatusCode} Message={Message} TraceId={TraceId}",
            (int)apiEx.StatusCode,
            apiEx.Message,
            apiEx.TraceId);

        // ── AJAX / fetch ──
        if (IsAjaxRequest(context.HttpContext))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                message = apiEx.Message,
                traceId = apiEx.TraceId
            })
            { StatusCode = (int)apiEx.StatusCode };
            return Task.CompletedTask;
        }

        // ── 401 Unauthorized → تسجيل الدخول ──
        if (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return Task.CompletedTask;
        }

        // ── الباقي: إعادة توجيه مع رسالة الخطأ ──
        var httpContext = context.HttpContext;
        httpContext.Items["ErrorMessage"] = apiEx.Message;

        if (!string.IsNullOrEmpty(apiEx.TraceId))
            httpContext.Items["ErrorTraceId"] = apiEx.TraceId;

        var referrer = httpContext.Request.Headers["Referer"].FirstOrDefault();

        // لا نعيد التوجيه لنفس المسار (حلقة لا نهائية)
        var safeRedirect = "/"
            + (httpContext.User?.Identity?.IsAuthenticated == true ? "Dashboard" : "Account/Login");

        if (!string.IsNullOrWhiteSpace(referrer)
            && referrer.StartsWith("/", StringComparison.Ordinal)
            && !referrer.StartsWith(httpContext.Request.Path, StringComparison.OrdinalIgnoreCase))
        {
            safeRedirect = referrer;
        }

        context.Result = new RedirectResult(safeRedirect);
        return Task.CompletedTask;
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        if (string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var accept = context.Request.Headers.Accept.ToString();
        if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }
}
