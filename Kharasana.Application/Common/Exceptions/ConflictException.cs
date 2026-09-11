namespace Kharasana.Application.Common.Exceptions;

/// <summary>
/// استثناء يشير إلى تعارض في البيانات (مثلاً رقم هاتف مستخدم مسبقًا) — يُحوَّل تلقائياً إلى HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
