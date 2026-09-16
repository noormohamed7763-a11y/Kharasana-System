namespace Kharasana.Domain.Enums;

/// <summary>
/// أدوار النظام.
/// <para>تنبيه أمني: لا تبدأ Admin من 0 — فالقيمة الافتراضية لأي صف جديد (default) كانت تُفسَّر كمدير.
/// القيمة 0 محجوزة الآن لـ None (لا دور)، والأدوار الفعلية تبدأ من 1.</para>
/// <para>تُخزَّن القيم الرقمية في قاعدة البيانات (HasConversion&lt;int&gt;) بينما يحمل JWT الاسم
/// (Role.ToString()) — لذا أي تغيير في هذه الأرقام يتطلب Migration لنقل الصفوف،
/// ولا يُبطل أي رمز وصول قائم.</para>
/// </summary>
public enum UserRole
{
    None = 0,
    Admin = 1,
    FactoryEmployee = 2,
    Driver = 3,
    Client = 4
}