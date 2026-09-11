namespace Kharasana.Application.Common.Exceptions;

/// <summary>
/// استثناء يشير إلى عدم وجود السجل المطلوب — يُحوَّل تلقائياً إلى HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} غير موجود.") { }
}
