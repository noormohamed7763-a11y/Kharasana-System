using System.Text.RegularExpressions;
using Kharasana.Domain.Enums;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حراسة توحيد خريطة حالة الطلب — مصدر واحد لكل تمثيل نصّي للحالة.
///
/// <para>كان في المشروع <b>ثلاث</b> خرائط متوازية للاسم العربي
/// (<c>OrderStatusHelper</c>، و<c>OrderStatusExtensions</c>، و<c>OrderDto.StatusArabic</c>)
/// و<b>ثلاث</b> خرائط للاحقة صنف CSS (<c>OrderStatusExtensions</c>،
/// و<c>DashboardApiService.GetStatusCssSuffix</c>، ودالة محلية في <c>Reports/Index.cshtml</c>).</para>
///
/// <para>الخريطة المكرّرة لا تتعارض لحظة كتابتها بل تنحرف لاحقاً: إضافة حالة جديدة
/// تُعدَّل في موضع وتُنسى في آخر، فيظهر الطلب نفسه بنصّين مختلفين في شاشة الطلبات
/// ولوحة المعلومات. الاختبارات هنا صنفان: دلالية على الخريطة الأساسية في Domain،
/// ومسح لمصدر المستودع يرفض ظهور خريطة عربية أو خريطة أصناف CSS في أي ملف آخر.</para>
///
/// <para><b>حدود المسح:</b> يكشف خرائط <c>switch</c> المكتوبة بصيغة
/// <c>OrderStatus.X =&gt; "نص"</c> — وهي الصيغة التي كُتبت بها الخرائط المكرّرة فعلاً.
/// ولذلك يُكمّله اختبار <see cref="DisplayPaths_DelegateToTheSingleSource"/> الذي يتحقق
/// من أن مسارات العرض تنادي المصدر الواحد بدل أن تحتفظ بخريطة خاصة من أي شكل.
/// المسح يقرأ الملفات من القرص فيلزم تشغيل الاختبارات من داخل المستودع.</para>
public class OrderStatusMapTests
{
    /// <summary>الملف الوحيد المسموح فيه بنصّ عربي لكل حالة (الاسم المجرّد).</summary>
    private const string ArabicMapFile = "Kharasana.Domain/Enums/OrderStatusHelper.cs";

    /// <summary>الملف الوحيد المسموح فيه بلواحق أصناف CSS للحالات (طبقة العرض).</summary>
    private const string CssMapFile = "Kharasana.Web/Helpers/OrderStatusExtensions.cs";

    /// <summary>سطر خريطة يُرجع نصاً عربياً لحالة، مثل <c>OrderStatus.Pending => "قيد الانتظار",</c>.</summary>
    private static readonly Regex ArabicMapLine =
        new(@"OrderStatus\.\w+\s*=>\s*""[^""]*\p{IsArabic}", RegexOptions.Compiled);

    /// <summary>
    /// سطر خريطة يُرجع صنف CSS للحالة — اللاحقة المجرّدة، أو الصيغة الكاملة <c>status-*</c>،
    /// أو صنف خلفية Bootstrap <c>bg-*</c> (خريطة شريط التقدّم في صفحة التفاصيل).
    /// </summary>
    private static readonly Regex CssMapLine =
        new(@"OrderStatus\.\w+\s*=>\s*""(status-[a-z]+|bg-[a-z]+|new|pending|approved|rejected|cancelled|ontheway|delivered|closed)""",
            RegexOptions.Compiled);

    /// <summary>سطر خريطة يُرجع نسبة تقدّم رقمية لحالة، مثل <c>OrderStatus.Pending => 25,</c>.</summary>
    private static readonly Regex ProgressMapLine =
        new(@"OrderStatus\.(\w+)\s*=>\s*(\d+)", RegexOptions.Compiled);

    /// <summary>
    /// المشاريع الإنتاجية التي يُفحص مصدرها. مشروع الاختبارات مستثنى عن قصد: الثابت
    /// المفروض هو تمثيل الحالة في ما يراه المستخدم، ووجود نصّ حالة في تأكيد اختبار
    /// (أو في مثال توثيقي كهذا الملف) لا يُنتج تناقضاً في الواجهة.
    /// </summary>
    private static readonly string[] ScannedProjects =
        ["Kharasana.Domain", "Kharasana.Application", "Kharasana.Infrastructure", "Kharasana.API", "Kharasana.Web"];

    /// <summary>مجلدات لا تُفحص: مخرجات البناء، ومستودع git، وأشجار عمل مهملة، وأصول غير مصدرية.</summary>
    private static readonly string[] IgnoredDirectories =
        ["bin", "obj", ".git", ".vs", ".kilo", "node_modules", "Documents"];

    // ─────────────────────────────────────────────
    // 1. الخريطة الأساسية في Domain — دلالياً
    // ─────────────────────────────────────────────

    /// <summary>كل حالة معرّفة لها اسم فريد، ولا حالة تسقط إلى «غير معروف».</summary>
    [Fact]
    public void CanonicalArabicNames_CoverEveryStatusDistinctly()
    {
        var statuses = Enum.GetValues<OrderStatus>();
        var names = statuses.Select(OrderStatusHelper.GetArabicName).ToList();

        statuses.Should().HaveCount(8);
        names.Should().OnlyContain(n => !string.IsNullOrWhiteSpace(n) && n != "غير معروف");
        names.Should().OnlyHaveUniqueItems(
            "حالتان بنفس النص تجعل الشاشة عاجزة عن التمييز بينهما");
    }

    /// <summary>
    /// الاسم المجرّد بلا إيموجي — الإيموجي مسؤولية طبقة العرض وحدها.
    /// هذا ما يجعل <c>OrderStatusHelper</c> صالحاً للاستخدام في رسائل الخطأ أيضاً
    /// (مثل <c>Messages.OrderCannotBeUpdatedInStatus</c>) حيث لا مكان لزخرفة.
    /// </summary>
    [Fact]
    public void CanonicalArabicNames_CarryNoDisplayDecoration()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var name = OrderStatusHelper.GetArabicName(status);

            // U+2190–U+2BFF يغطي الرموز أحادية المحرف (⏳ ✅ ❌)، والبدائل الزوجية
            // (surrogate pairs) تغطي الإيموجي خارج BMP مثل 🚚 📦 🔒.
            var hasDecoration = name.Any(c => char.IsSurrogate(c) || (c >= 0x2190 && c <= 0x2BFF));

            hasDecoration.Should().BeFalse(
                $"الاسم المجرّد للحالة {status} يجب أن يكون نصاً عربياً بلا إيموجي");
        }
    }

    // ─────────────────────────────────────────────
    // 2. مسح المصدر — لا خريطة ثانية في أي ملف
    // ─────────────────────────────────────────────

    /// <summary>
    /// خريطة الاسم العربي موجودة في ملف واحد فقط، وخريطة صنف CSS في ملف واحد فقط.
    /// أي <c>switch</c> جديد يُرجع نصاً لكل حالة هو خريطة موازية ستنحرف عن المصدر بصمت.
    /// </summary>
    [Fact]
    public void StatusTextMaps_ExistInExactlyOneFilePerConcern()
    {
        var root = RepoRoot();
        var arabicHits = new Dictionary<string, int>();
        var cssHits = new Dictionary<string, int>();

        foreach (var file in ScannedProjects
                     .SelectMany(project => SourceFiles(Path.Combine(root, project), "*.cs")
                         .Concat(SourceFiles(Path.Combine(root, project), "*.cshtml"))))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var text = File.ReadAllText(file);

            var arabic = ArabicMapLine.Matches(text).Count;
            if (arabic > 0) arabicHits[relative] = arabic;

            var css = CssMapLine.Matches(text).Count;
            if (css > 0) cssHits[relative] = css;
        }

        arabicHits.Keys.Should().BeEquivalentTo(
            [ArabicMapFile],
            "خريطة الاسم العربي تُكتب في {0} وحده؛ أي ملف آخر فيه سطر OrderStatus.X => \"نص عربي\" هو نسخة ثانية",
            ArabicMapFile);

        cssHits.Keys.Should().BeEquivalentTo(
            [CssMapFile],
            "أصناف CSS للحالة (لواحق status- وأصناف bg- لشريط التقدّم) تُكتب في {0} وحده؛ أي ملف آخر فيه OrderStatus.X => \"ontheway\" أو \"bg-warning\" هو نسخة ثانية مكرّرة",
            CssMapFile);

        // حماية من النجاح الكاذب: لو تغيّر نمط السطر أو أُعيدت تسمية الملف يتوقف
        // المسح عن إيجاد أي شيء ويمرّ الاختبار بلا معنى. هذان الحدّان يجعلانه يفشل بصوت عالٍ.
        arabicHits[ArabicMapFile].Should().BeGreaterThanOrEqualTo(8, "الحالات الثماني لها ثمانية أسماء");
        cssHits[CssMapFile].Should().BeGreaterThanOrEqualTo(8, "الحالات الثماني لها ثماني لواحق CSS وأصناف تقدّم");
    }

    /// <summary>
    /// مسارات العرض تنادي المصدر الواحد بدل الاحتفاظ بخريطة خاصة.
    /// ضروري لأن المسح أعلاه يتعرّف على صيغة <c>switch</c> فقط؛ هذا الاختبار يمنع
    /// استبدال الاستدعاء بخريطة من شكل آخر (قاموس أو دالة محلية).
    /// </summary>
    [Theory]
    [InlineData("Kharasana.Web/ViewModels/Orders/OrderDto.cs", "GetArabicName()")]
    [InlineData("Kharasana.Web/Services/Api/DashboardApiService.cs", "GetCssSuffix()")]
    [InlineData("Kharasana.Web/Views/Reports/Index.cshtml", "GetCssSuffix()")]
    [InlineData("Kharasana.Web/Views/Orders/Details.cshtml", "GetProgressPercent()")]
    [InlineData("Kharasana.Web/Views/Orders/Details.cshtml", "GetProgressMotionClasses()")]
    public void DisplayPaths_DelegateToTheSingleSource(string relativePath, string expectedCall)
    {
        var file = Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        File.Exists(file).Should().BeTrue($"{relativePath} يجب أن يبقى موجوداً");

        File.ReadAllText(file).Should().Contain(
            expectedCall,
            $"{relativePath} يجب أن يستدعي {expectedCall} بدل بناء خريطة الحالة بنفسه");
    }

    // ─────────────────────────────────────────────
    // 3. اللواحق تطابق أنماط CSS المعرّفة فعلاً
    // ─────────────────────────────────────────────

    /// <summary>
    /// كل لاحقة في الخريطة لها صنف معرّف في <c>components.css</c>.
    /// صنف مفقود يعني شارة بلا لون تُعرض بشكل افتراضي بلا أن يشتكي أحد.
    /// </summary>
    [Fact]
    public void CssSuffixes_AllHaveMatchingStylesInComponentsCss()
    {
        var root = RepoRoot();
        var cssMapSource = File.ReadAllText(
            Path.Combine(root, CssMapFile.Replace('/', Path.DirectorySeparatorChar)));
        var componentsCss = File.ReadAllText(Path.Combine(
            root, "Kharasana.Web", "wwwroot", "css", "components.css"));

        var suffixes = Regex.Matches(cssMapSource, @"OrderStatus\.(\w+)\s*=>\s*""([a-z]+)""")
            .Select(m => (Status: m.Groups[1].Value, Suffix: m.Groups[2].Value))
            .ToList();

        suffixes.Should().HaveCount(8, "كل حالة من الحالات الثماني لها لاحقة CSS");
        suffixes.Select(s => s.Status).Should().BeEquivalentTo(Enum.GetNames<OrderStatus>());
        suffixes.Select(s => s.Suffix).Should().OnlyHaveUniqueItems();

        foreach (var (status, suffix) in suffixes)
        {
            componentsCss.Should().Contain(
                $".status-{suffix}",
                $"الحالة {status} تُعرض بالصنف status-{suffix} فيجب أن يكون معرّفاً في components.css");
        }
    }

    // ─────────────────────────────────────────────
    // 4. خرائط شريط التقدّم — المدى والاتجاه
    // ─────────────────────────────────────────────

    /// <summary>
    /// نِسَب شريط التقدّم: واحدة لكل حالة، وداخل المدى المعروض (0–100)،
    /// وأصناف الخلفية أصناف Bootstrap، والمسار السليم غير تنازلي.
    ///
    /// <para>كانت الخريطتان محليّتين في <c>Views/Orders/Details.cshtml</c> — خارج
    /// نطاق المسح (فالمسح يتعرّف على النصوص العربية ولواحق <c>status-</c> فقط،
    /// ولا الخريطة الرقمية ولا <c>bg-*</c> تظهر فيهما) فأي تعديل عليهما كان يمرّ
    /// بلا حارس. نُقلتا إلى المصدر الواحد ووُسِّع المسح ليغطّي <c>bg-*</c>،
    /// ويبقى هذا الاختبار يتحقق من المدى والاتجاه — وهما ما لا يلتقطه المسح.</para>
    /// </summary>
    [Fact]
    public void ProgressMaps_CoverEveryStatusWithinRangeAndAscend()
    {
        var source = File.ReadAllText(
            Path.Combine(RepoRoot(), CssMapFile.Replace('/', Path.DirectorySeparatorChar)));

        // النسب: OrderStatus.X => <رقم>
        var percents = ProgressMapLine.Matches(source)
            .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));

        // أصناف الخلفية: OrderStatus.X => "bg-..."
        var bgClasses = Regex.Matches(source, @"OrderStatus\.(\w+)\s*=>\s*""(bg-[\w-]+)""")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        var expected = Enum.GetNames<OrderStatus>();

        percents.Keys.Should().BeEquivalentTo(expected, "لكل حالة نسبة تقدّم واحدة");
        bgClasses.Keys.Should().BeEquivalentTo(expected, "لكل حالة صنف خلفية واحد");

        percents.Values.Should().OnlyContain(p => p >= 0 && p <= 100,
            "شريط Bootstrap يقصّ ما خرج عن 0–100، فيظهر الشريط ممتلئاً أو فارغاً بغير الحقيقة");

        bgClasses.Values.Should().OnlyContain(c => c.StartsWith("bg-"),
            "الصنف يوضع في class=\"progress-bar {الصنف} …\" فيجب أن يكون صنف خلفية Bootstrap");

        // المسار السليم تصاعدي: تراجعه يعني شريطاً ينقص كلما تقدّم الطلب
        string[] happyPath =
            [nameof(OrderStatus.New), nameof(OrderStatus.Pending), nameof(OrderStatus.Approved),
             nameof(OrderStatus.OnTheWay), nameof(OrderStatus.Delivered), nameof(OrderStatus.Closed)];

        happyPath.Select(s => percents[s]).Should().BeInAscendingOrder(
            "تقدّم الطلب لا يتراجع في المسار السليم");
    }

    // ─────────────────────────────────────────────
    // 5. الحالات الجارية — مؤشّر الحركة
    // ─────────────────────────────────────────────

    /// <summary>
    /// مؤشّر الحركة (شريط التقدّم المتحرك) يُمنح للحالات التي فيها عمل جارٍ فقط.
    /// الشريط المتحرك المخطَّط يقول «يجري الآن»، ومنحه لحالة انتهى فيها العمل يجعل
    /// صفحة التفاصيل تناقض شريطها الزمني: مراحل الطلب المرفوض كلها فارغة (○) بينما
    /// شريط التقدّم ينبض.
    ///
    /// <para><b>القيمة الحقيقية لهذا الاختبار</b> في سطره الأخير: يثبّت أن «جارية»
    /// و«نهائية» علاقتان مختلفتان عمداً. «تم التسليم» تنتقل إلى «مغلق» فليست نهائية،
    /// ومع ذلك لا عمل جارٍ فيها. فلو أضاف أحدهم حالة نهائية جديدة ونسي تحديث
    /// <c>IsInFlight</c>، أو «بسّط» الدالة باشتقاقها من جدول الانتقالات، يفشل هنا.</para>
    /// </summary>
    [Fact]
    public void IsInFlight_CoversExactlyTheStatusesWithWorkUnderway()
    {
        var statuses = Enum.GetValues<OrderStatus>();

        statuses.Where(OrderStatusHelper.IsInFlight).Should().BeEquivalentTo(
            [OrderStatus.New, OrderStatus.Pending, OrderStatus.Approved, OrderStatus.OnTheWay],
            "هذه الأربع وحدها ينتظر فيها الطلب إجراءً");

        // كل حالة نهائية (بلا انتقالات مسموحة) لا يمكن أن تكون جارية
        statuses
            .Where(s => OrderStatusHelper.GetAllowedTransitions(s).Length == 0)
            .Should().OnlyContain(s => !OrderStatusHelper.IsInFlight(s),
                "الحالة النهائية لا عمل فيها، فلا يُمنح شريطها مؤشّر حركة");

        // «تم التسليم» ليست نهائية لكنها ليست جارية — برهان أن القاعدتين مختلفتان
        OrderStatusHelper.GetAllowedTransitions(OrderStatus.Delivered).Should().NotBeEmpty(
            "«تم التسليم» تنتقل إلى «مغلق» فهي ليست نهائية في جدول الانتقالات");
        OrderStatusHelper.IsInFlight(OrderStatus.Delivered).Should().BeFalse(
            "لا عمل جارٍ في طلب سُلِّم — هو في انتظار الإغلاق فقط");
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    /// <summary>جذر المستودع — يُصعد من مجلد الاختبار حتى يُعثر على ملف الحل.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Kharasana.slnx")))
            dir = dir.Parent;

        dir.Should().NotBeNull(
            $"تعذّر العثور على Kharasana.slnx صعوداً من {AppContext.BaseDirectory} — شغّل الاختبارات من داخل المستودع");

        return dir!.FullName;
    }

    /// <summary>كل ملفات النمط المطلوب تحت الجذر، مع تخطّي مجلدات البناء والأصول غير المصدرية.</summary>
    private static IEnumerable<string> SourceFiles(string root, string searchPattern)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            foreach (var file in Directory.EnumerateFiles(current, searchPattern))
                yield return file;

            foreach (var sub in Directory.EnumerateDirectories(current))
            {
                var name = Path.GetFileName(sub);
                if (!IgnoredDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
                    pending.Push(sub);
            }
        }
    }
}
