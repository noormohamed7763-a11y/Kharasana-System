namespace Kharasana.Domain.Validation;

/// <summary>
/// سياسة كلمة المرور الموحّدة — <b>المصدر الوحيد</b> للحد الأدنى للطول في النظام.
///
/// <para>قبل هذا الملف كان الحد الأدنى مكتوباً بالرقم في ستة مواضع متفرقة
/// (سمة <c>RegisterUserDto</c>، وقاعدة <c>ValidationRules.Password</c>،
/// وثلاثة ViewModels في الويب، ووسوم <c>minlength</c> وفحص JavaScript)،
/// وكانت قيمها متعارضة (6 في بعضها و8 في غيرها) بينما رسالة الخطأ
/// <c>Messages.PasswordMinLength</c> في طبقة Application تقول «8» دائماً —
/// فكان التسجيل العام يقبل كلمة مرور من 6 أحرف بينما التحقق في الخدمة يطلب 8.</para>
///
/// <para>لا تُعدّل هذا الرقم دون تحديث نص <c>Messages.PasswordMinLength</c>؛
/// يوجد اختبار يحرس تطابقهما (PasswordPolicyTests).</para>
/// </summary>
public static class PasswordPolicy
{
    /// <summary>الحد الأدنى لطول كلمة المرور — يُطبَّق على التسجيل العام وإنشاء المستخدمين معاً.</summary>
    public const int MinimumLength = 8;
}
