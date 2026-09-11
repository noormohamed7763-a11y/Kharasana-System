namespace Kharasana.Application.DTOs.Auth;

public class LoginResponseDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? FactoryId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }

    /// <summary>
    /// تحذير يُعرض بعد تسجيل الدخول (مثل: المصنع غير نشط) — null عند عدم وجود تحذير.
    /// </summary>
    public string? Notification { get; set; }

    /// <summary>
    /// حالة المصنع (نشط/موقوف) لحسابات موظفي المصنع والسائقين — null للتسجيلات غير المرتبطة بمصنع.
    /// تُستخدم في الواجهة لإخفاء الإجراءات غير المسموحة وعرض بانر دائم عند التعطيل.
    /// </summary>
    public bool? FactoryIsActive { get; set; }
}