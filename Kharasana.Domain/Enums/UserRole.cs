namespace Kharasana.Domain.Enums;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// أدوار النظام.
/// <para>تنبيه أمني: لا تبدأ Admin من 0 — فالقيمة الافتراضية لأي صف جديد (default) كانت تُفسَّر كمدير.
/// القيمة 0 محجوزة الآن لـ None (لا دور)، والأدوار الفعلية تبدأ من 1.</para>
/// <para>تُخزَّن القيم الرقمية في قاعدة البيانات (HasConversion&lt;int&gt;) بينما يحمل JWT الاسم
/// (Role.ToString()) — لذا أي تغيير في هذه الأرقام يتطلب Migration لنقل الصفوف،
/// ولا يُبطل أي رمز وصول قائم.</para>
/// <para><c>[Display]</c> هنا هو المصدر الوحيد للاسم العربي المعروض (كما في
/// <see cref="TransportMethod"/>) — يقرأه <c>Html.GetEnumSelectList</c> في النماذج
/// و<c>EnumHelper.GetDisplayName</c> في الواجهات. لا يؤثر في <c>ToString()</c>
/// الذي يحمله JWT ولا في التخزين الرقمي.</para>
/// </summary>
public enum UserRole
{
    /// <summary>بلا دور — قيمة حارسة لا يُنشأ بها مستخدم، ولا تُعرض في قوائم الأدوار.</summary>
    [Display(Name = "بلا دور")]
    None = 0,

    [Display(Name = "مدير النظام")]
    Admin = 1,

    [Display(Name = "مدير مصنع")]
    FactoryAdmin = 5,

    [Display(Name = "موظف مصنع")]
    FactoryEmployee = 2,

    [Display(Name = "سائق")]
    Driver = 3,

    [Display(Name = "عميل")]
    Client = 4
}