namespace Kharasana.Web.Models.Components;

/// <summary>
/// خلية «الشخص» في الجداول: دائرة الحرف الأول + الاسم + سطر ثانوي اختياري.
///
/// <para>كانت هذه الكتلة مكرّرة في جداول المستخدمين والعملاء والسائقين، وكل نسخة
/// تعيد حساب الحرف الأول بنفسها. المنطق هنا واحد: الاسم الفارغ يعطي «؟» بدل
/// انهيار <c>Trim()[0]</c> على نص فارغ.</para>
/// </summary>
public class UserCellViewModel
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>سطر ثانوي تحت الاسم (البريد الإلكتروني مثلاً).</summary>
    public string? SubText { get; set; }

    /// <summary>كلاس لون الدائرة: avatar-admin / avatar-employee / avatar-driver / avatar-client.</summary>
    public string AvatarCssClass { get; set; } = "avatar-default";
}
