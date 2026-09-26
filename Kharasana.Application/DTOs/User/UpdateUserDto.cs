using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.User;

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني — اختياري، وغيابه (null أو فراغ) يعني «أبقِ الحالي».
    ///
    /// <para>كان هذا الحقل غائباً عن الـ DTO بينما مسارات الويب ترسله (تعديل العميل
    /// والسائق والمستخدم)، فيُهمَل صامتاً: تُحفظ بقية الحقول وتظهر رسالة نجاح
    /// ولا يتغيّر البريد. ولأن البريد مُعرّف دخول (AuthService يقبل الدخول به)،
    /// فإنّ تغييره هنا يمرّ بفحص تفرّد يستثني المستخدم نفسه.</para>
    /// </summary>
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? ProfileImage { get; set; }
    public UserRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public DriverStatus? DriverStatus { get; set; }
    public int? FactoryId { get; set; }
    public bool IsActive { get; set; }
}