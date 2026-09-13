using System.Net;
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
                "NotFound: {Message} TraceId={TraceId} Path={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex,
                "Forbidden: {Message} TraceId={TraceId} Path={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex,
                "Conflict: {Message} TraceId={TraceId} Path={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Conflict);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex,
                "Business rule: {Message} TraceId={TraceId} Path={Path}",
                ex.Message, context.TraceIdentifier, context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception. TraceId={TraceId} Path={Path} User={User}",
                context.TraceIdentifier,
                context.Request.Path,
                context.User.Identity?.Name ?? "anonymous");
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response already started. Cannot write error response. StatusCode={StatusCode} TraceId={TraceId}",
                (int)statusCode, context.TraceIdentifier);
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var message = statusCode == HttpStatusCode.InternalServerError
            ? Messages.UnexpectedError
            : exception.Message;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        // ربط خطأ العميل بالسجل عبر TraceIdentifier — يُطابق نفس المعرف في رسائل الـ log أعلاه
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(response);
    }
}
