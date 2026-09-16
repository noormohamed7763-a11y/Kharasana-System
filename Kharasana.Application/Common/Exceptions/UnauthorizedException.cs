namespace Kharasana.Application.Common.Exceptions;

/// <summary>
/// استثناء يشير إلى جلسة منتهية أو هوية ناقصة أو طلب غير موثَّق — يُحوَّل لاحقاً إلى HTTP 401
/// عبر لصق ربطه في ExceptionMiddleware (الخطوة 4)، تماماً كما تُحوَّل بقية الاستثناءات.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}