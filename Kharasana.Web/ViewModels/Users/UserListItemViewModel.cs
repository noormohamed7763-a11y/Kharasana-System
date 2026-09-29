namespace Kharasana.Web.ViewModels.Users;

public class UserListItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? FactoryId { get; set; }
    /// <summary>اسم المصنع — يُملأ في مسار التفاصيل وحده (لا تعرضه القائمة).</summary>
    public string? FactoryName { get; set; }
    public bool IsActive { get; set; }
    public string? LicenseNumber { get; set; }
    public string? DriverStatus { get; set; }
    /// <summary>يُملأ في مسار التفاصيل وحده — تعرضه صفحة تفاصيل المستخدم.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>يُملأ في مسار التفاصيل وحده، ويبقى فارغاً لمن لم يُعدَّل حسابه.</summary>
    public DateTime? UpdatedAt { get; set; }
}