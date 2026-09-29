using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.User;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? ProfileImage { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }

    /// <summary>
    /// اسم المصنع — يُملأ في قراءة المستخدم الواحد (<c>UserService.GetByIdAsync</c>) لأنها
    /// القراءة الوحيدة التي تُحمّل كيان المصنع؛ قائمة المستخدمين لا تُحمّله فتبقى فارغة هنا.
    /// تعرضه صفحة تفاصيل المستخدم بدل معرّف المصنع الرقمي.
    /// </summary>
    public string? FactoryName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>تاريخ الإنشاء — تعرضه صفحة تفاصيل المستخدم في الويب.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>آخر تحديث — تعرضه صفحة تفاصيل المستخدم في الويب، وتبقى فارغة لمن لم يُعدَّل.</summary>
    public DateTime? UpdatedAt { get; set; }
}