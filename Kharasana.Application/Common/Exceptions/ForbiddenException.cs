namespace Kharasana.Application.Common.Exceptions;

/// <summary>
/// استثناء يشير إلى عدم وجود صلاحية الكائن للعملية — يُحوَّل تلقائياً إلى HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
