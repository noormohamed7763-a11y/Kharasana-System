using System.Globalization;
using Kharasana.Application.Common;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حرّاس ثقافة العرض (<see cref="AppCulture"/>).
///
/// العلّة: لم يكن في المشروع أي <c>CultureInfo</c>، فكان التقويم والأرقام يتبعان جهاز
/// التشغيل — وعلى جهاز بثقافة أم القرى يُعرض <c>2026-09-28</c> هجريًا، ويُقرأ ما يرسله
/// <c>&lt;input type="date"&gt;</c> كسنة هجرية فيفشل الربط.
///
/// <para>اختبار <see cref="AppCulture.Configure"/> وحده يستدعيها — لأنها تُغيّر ثقافة
/// العملية كلها فتسرّب إلى اختبارات تعمل بالتوازي على خيوط أخرى. لذلك يُعيد كل قيمة
/// إلى أصلها في <c>finally</c>، فيبقى الأثر محصورًا في مدّة الاختبار. أمّا حقيقة أن
/// <c>Program.cs</c> يُناديها فعلًا في أول <c>Main</c> فلا يغطّيها اختبار — تُرى
/// بالعين في الملفّين.</para>
/// </summary>
public class AppCultureTests
{
    private static readonly DateTime Sample = new(2026, 9, 28, 14, 30, 0);

    /// <summary>
    /// إقلاع التطبيقين يمرّ من هنا. كان هذا الموضع بلا أي تغطية: اختبارات الثقافة
    /// تفحص الثقافة المبنيّة ولا تفحص <c>Configure()</c> نفسها — فلو أُسندت لِخاصية
    /// خاطئة (مثلاً UI بدل العادية) لما كشف ذلك أي اختبار، ولظهر الأثر في الإقلاع وحده.
    /// </summary>
    [Fact]
    public void Configure_InstallsTheCultureAndRestoresCleanly()
    {
        // حفظ الأصل ليكون الأثر محدودًا بمدّة هذا الاختبار (انظر تعليق الصنف)
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        var originalDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            AppCulture.Configure();

            CultureInfo.CurrentCulture.Should().BeSameAs(AppCulture.ArabicGregorian);
            CultureInfo.CurrentUICulture.Should().BeSameAs(AppCulture.ArabicGregorian);
            CultureInfo.DefaultThreadCurrentCulture.Should().BeSameAs(AppCulture.ArabicGregorian);
            CultureInfo.DefaultThreadCurrentUICulture.Should().BeSameAs(AppCulture.ArabicGregorian);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUi;
        }
    }

    [Fact]
    public void Culture_UsesGregorianCalendar()
    {
        AppCulture.ArabicGregorian.DateTimeFormat.Calendar
            .Should().BeOfType<GregorianCalendar>();
    }

    /// <summary>
    /// الجوهر: التاريخ يُنسَّق ويُقرأ ميلاديًا. هذه هي الحالة التي ينكسر فيها الربط
    /// على جهاز بتقويم أم القرى — والاختبار يوثّق الانكسار صراحةً قبل التحقق من السلامة.
    /// </summary>
    [Fact]
    public void Date_RoundTripsAsGregorianIso()
    {
        // الانكسار الذي نمنعه: نفس اللحظة تحمل رقم سنة مختلفًا في تقويم أم القرى،
        // فلو نسّقها الخادم به لما طابق النصّ ما يرسله المتصفح ولا ما يقرأه منه.
        new UmAlQuraCalendar().GetYear(Sample).Should().NotBe(2026);

        // الثقافة المثبَّتة: تنسيقًا وقراءةً
        var culture = AppCulture.ArabicGregorian;

        Sample.ToString("yyyy-MM-dd", culture).Should().Be("2026-09-28");

        DateTime.TryParse("2026-09-28", culture, DateTimeStyles.None, out var parsed)
            .Should().BeTrue();
        parsed.Year.Should().Be(2026);
        parsed.Month.Should().Be(9);
        parsed.Day.Should().Be(28);
    }

    [Fact]
    public void Numbers_UseLatinSeparators()
    {
        // "N0" هو ما تستخدمه عروض الأسعار (FormatPrice / TotalQuantity)
        1234567m.ToString("N0", AppCulture.ArabicGregorian).Should().Be("1,234,567");
        1234.5m.ToString("N2", AppCulture.ArabicGregorian).Should().Be("1,234.50");
    }

    /// <summary>أسماء اليوم والشهر تبقى عربية — الموضع الوحيد الذي يعتمد عليها اسمًا هو لوحة المدير.</summary>
    [Fact]
    public void DayAndMonthNames_AreArabic()
    {
        Sample.ToString("MMMM", AppCulture.ArabicGregorian).Should().Be("سبتمبر");
        Sample.ToString("dddd", AppCulture.ArabicGregorian).Should().Be("الاثنين");

        // كل الأسماء مضبوطة الطول: مصفوفة التقويم تتطلب 7 أيام و13 شهرًا
        AppCulture.ArabicGregorian.DateTimeFormat.DayNames.Should().HaveCount(7);
        AppCulture.ArabicGregorian.DateTimeFormat.MonthNames.Should().HaveCount(13);
        AppCulture.ArabicGregorian.DateTimeFormat.MonthGenitiveNames.Should().HaveCount(13);
    }

    [Fact]
    public void Culture_IsReadOnly()
    {
        AppCulture.ArabicGregorian.IsReadOnly.Should().BeTrue();
    }
}
