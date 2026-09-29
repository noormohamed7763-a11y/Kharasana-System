namespace Kharasana.Application.Common.Exceptions;

/// <summary>
/// استثناء يشير إلى جلسة منتهية أو هوية ناقصة أو طلب غير موثَّق — يُحوَّل إلى HTTP 401
/// في <c>ExceptionMiddleware</c> (فرع <c>UnauthorizedException</c>)، تماماً كما تُحوَّل
/// بقية الاستثناءات.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}