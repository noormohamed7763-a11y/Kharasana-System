using System.Globalization;

namespace Kharasana.Application.Common;

/// <summary>
/// تثبيت ثقافة العرض في نقطة واحدة — يُنادى مرة واحدة من <c>Program.cs</c> في الـ API والويب.
///
/// <para>العلّة التي يمنعها: لم يكن في المشروع أي <c>CultureInfo</c> إطلاقًا، فكل تنسيق
/// (<c>ToString("yyyy-MM-dd")</c>، <c>"{price:N0}"</c>) وكل <b>ربط نموذج</b> لِـ
/// <c>&lt;input type="date"&gt;</c> كان يتبع ثقافة الجهاز. وعلى جهاز بثقافة
/// <c>ar-SA</c> يصير التقويم أم القرى: يُعرض التاريخ هجريًا، ويُقرأ
/// <c>"2026-09-28</c> القادم من المتصفح كسنة هجرية 2026 فيفشل الربط ويرى المستخدم
/// «التاريخ غير صالح» — بلا أي خطأ في السجلات.</para>
///
/// <para>الثقافة المبنية هنا مبنية على <see cref="CultureInfo.InvariantCulture"/>
/// لا على <c>ar-YE</c> عمدًا: التقويم الميلادي والأرقام والفواصل اللاتينية مضمونة
/// <b>بحكم البناء</b> لا بحكم بيانات ثقافة قد تختلف بين الأجهزة أو تغيب مع
/// <c>InvariantGlobalization</c>، ثم تُضاف أسماء عربية لليوم والشهر للعرض وحده.</para>
///
/// <para>المكسب الصافي: مخرجات التاريخ والرقم صارت واحدة على كل جهاز. الموضع الوحيد
/// الذي يعرض اسم يوم/شهر عربيًا هو سطر التاريخ في لوحة المدير؛ وباقي العروض تستخدم
/// تنسيقات رقمية صريحة فلا تتأثر إلا بالتقويم والفواصل.</para>
/// </summary>
public static class AppCulture
{
    /// <summary>
    /// الثقافة العربية الميلادية — تُبنى مرة واحدة وتُعاد للقراءة فقط.
    /// </summary>
    public static CultureInfo ArabicGregorian { get; } = BuildArabicGregorian();

    /// <summary>
    /// يثبّتها ثقافةً للعملية وللخيوط الجديدة. تُنادى في أول <c>Program.cs</c>
    /// قبل بناء أي خدمة، لأن ربط النموذج يقرأ الثقافة عند معالجة الطلب لا عند الإقلاع.
    /// </summary>
    public static void Configure()
    {
        CultureInfo.DefaultThreadCurrentCulture = ArabicGregorian;
        CultureInfo.DefaultThreadCurrentUICulture = ArabicGregorian;
        CultureInfo.CurrentCulture = ArabicGregorian;
        CultureInfo.CurrentUICulture = ArabicGregorian;
    }

    private static CultureInfo BuildArabicGregorian()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

        // الفهرس 12 مخصّص للتقاويم ذات الثلاثة عشر شهرًا — يبقى فارغًا كما في كل ثقافة
        culture.DateTimeFormat.MonthNames =
        [
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", ""
        ];
        culture.DateTimeFormat.AbbreviatedMonthNames = culture.DateTimeFormat.MonthNames;
        // «d MMMM» يستعمل الصيغة الجرّية (genitive) لا الاسمية — والعربية لا تفرّق بينهما هنا
        culture.DateTimeFormat.MonthGenitiveNames = culture.DateTimeFormat.MonthNames;
        culture.DateTimeFormat.AbbreviatedMonthGenitiveNames = culture.DateTimeFormat.MonthNames;

        // DayNames مفهرسة بـ DayOfWeek: الأحد = 0 … السبت = 6
        culture.DateTimeFormat.DayNames =
            ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];
        culture.DateTimeFormat.AbbreviatedDayNames =
            ["أحد", "اثنين", "ثلاثاء", "أربعاء", "خميس", "جمعة", "سبت"];

        culture.DateTimeFormat.AMDesignator = "ص";
        culture.DateTimeFormat.PMDesignator = "م";

        // منع أي تعديل لاحق بالخطأ على ثقافة مشتركة بين كل الخيوط
        return CultureInfo.ReadOnly(culture);
    }
}
