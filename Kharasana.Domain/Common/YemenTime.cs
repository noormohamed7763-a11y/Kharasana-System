namespace Kharasana.Domain.Common;

/// <summary>
/// توقيت اليمن كنقطة مرجعية واحدة لِحدود اليوم.
///
/// <para>كل الطوابع الزمنية في النظام تُخزَّن وتُقارَن بـ UTC
/// (<c>DateTime.UtcNow</c>)، واليمن UTC+3 بلا توقيت صيفي. احتساب «اليوم» بـ
/// <c>DateTime.UtcNow.Date</c> كان يجعل الطلبات المُنشأة بين 00:00 و03:00 بتوقيت
/// صنعاء تُحتسب ضمن «أمس» — فتظهر لوحة المصنع وتقارير «اليوم» ناقصة بلا أي خطأ
/// ظاهر ولا استثناء.</para>
///
/// <para>الاستخدام الصحيح: <see cref="TodayUtcRange"/> لتُمرَّر حدوده إلى استعلام
/// على عمود مخزَّن UTC، و<see cref="Today"/> لعرض تاريخ اليوم للمستخدم.</para>
/// </summary>
public static class YemenTime
{
    /// <summary>إزاحة اليمن عن UTC — ثابتة طول العام (لا توقيت صيفي).</summary>
    public static readonly TimeSpan Offset = TimeSpan.FromHours(3);

    /// <summary>تاريخ اليوم الحالي بتوقيت اليمن.</summary>
    public static DateTime Today => (DateTime.UtcNow + Offset).Date;

    /// <summary>
    /// حدود اليوم المحلي (بدايته ونهايته غير الشاملة) معبَّراً عنهما بـ UTC.
    /// هذان الحدّان هما ما يُقارَن به عمود مخزَّن بـ UTC مثل <c>Order.CreatedAt</c>.
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) TodayUtcRange()
    {
        var startUtc = Today - Offset;
        return (startUtc, startUtc.AddDays(1));
    }
}
