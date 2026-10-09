using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.Web.Filters;

/// <summary>
/// فلتر مخصص لالتقاط ApiServiceException في أفعال AJAX (مثل OrderWorkflowController).
/// يقلل من تكرار try-catch في المتحكمات.
/// </summary>
public class ApiExceptionHandlerFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionHandlerFilter> _logger;

    public ApiExceptionHandlerFilter(ILogger<ApiExceptionHandlerFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ApiServiceException apiEx)
        {
            context.ExceptionHandled = true;
            _logger.LogWarning(
                "التقط الفلتر ApiServiceException: الحالة={StatusCode} التتبّع={TraceId}",
                (int)apiEx.StatusCode,
                apiEx.TraceId);

            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = apiEx.Error.FormatMessage(apiEx.MessageArgs ?? Array.Empty<object>()),
                traceId = apiEx.TraceId
            });
        }
        else if (context.Exception is Exception ex)
        {
            // استثناءات غير متوقعة
            context.ExceptionHandled = true;
            _logger.LogError(ex, "استثناء غير متوقع في طلب AJAX. التتبّع={TraceId}", context.HttpContext.TraceIdentifier);
            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = AppMessages.Common.OperationFailed
            });
        }
    }
}
