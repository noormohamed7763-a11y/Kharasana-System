using System.Net;
using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;

namespace Kharasana.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex,
                "غير موجود: {Message} التتبّع={TraceId} المسار={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex,
                "ممنوع: {Message} التتبّع={TraceId} المسار={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex,
                "تعارض: {Message} التتبّع={TraceId} المسار={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Conflict);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex,
                "قاعدة عمل: {Message} التتبّع={TraceId} المسار={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex,
                "غير مُصرَّح: {Message} التتبّع={TraceId} المسار={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
        }
        catch (ValidationException ex)
        {
            // فحص FluentValidation المُشغَّل عبر ValidationFilter — يُردّ بخطأٍ واحد فقط
            // (الأول) ليطابق سلوك الخدمة القديم الذي يتوقف عند أول فحص فاشل
            var firstError = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message;

            _logger.LogWarning(ex,
                "فشل التحقق: {Message} التتبّع={TraceId} المسار={Path}",
                firstError, context.TraceIdentifier, context.Request.Path);

            await HandleMessageAsync(context, firstError, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "استثناء غير معالَج. التتبّع={TraceId} المسار={Path} المستخدم={User}",
                context.TraceIdentifier,
                context.Request.Path,
                context.User.Identity?.Name ?? "مجهول");
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        var message = statusCode == HttpStatusCode.InternalServerError
            ? Messages.UnexpectedError
            : exception.Message;

        await HandleMessageAsync(context, message, statusCode);
    }

    private async Task HandleMessageAsync(HttpContext context, string message, HttpStatusCode statusCode)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "بدأ إرسال الردّ بالفعل. تعذّر كتابة ردّ الخطأ. الحالة={StatusCode} التتبّع={TraceId}",
                (int)statusCode, context.TraceIdentifier);
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = ApiResponse.Fail(message);

        // ربط خطأ العميل بالسجل عبر TraceIdentifier — يُطابق نفس المعرف في رسائل الـ log أعلاه
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(response);
    }
}
