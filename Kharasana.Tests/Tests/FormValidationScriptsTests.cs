namespace Kharasana.Tests.Tests;

/// <summary>
/// حراسة تحميل سكربتات التحقق في كل صفحة فيها نموذج إدخال.
///
/// <para>صفحات الـ MVC تعتمد على <c>jquery.validate</c> (عبر
/// <c>_ValidationScriptsPartial</c>) لتحويل قيود الـ DataAnnotations إلى تحقق فوري
/// في المتصفح. غياب الجزئية لا يُفشل شيئاً في الخادم: الصفحة تعمل، لكن كل خطأ إدخال
/// يستلزم إرسال النموذج كاملاً وانتظار إعادة تحميل، وتُفقد الحقول المُعبّأة عند
/// فشل التحقق على الخادم. ولهذا كان الغياب يمرّ بلا أن يشتكي أحد.</para>
///
/// <para><b>القاعدة أ:</b> كل صفحة (غير جزئية) فيها <c>&lt;form</c> و<c>asp-for</c>
/// يجب أن تستدعي <c>_ValidationScriptsPartial</c>. هذا هو ما كشف غيابه عن
/// <c>Orders/Create.cshtml</c> وحدها بين نماذج الطلبات.</para>
///
/// <para><b>القاعدة ب:</b> الجزئية التي تحتوي <c>asp-for</c> (مثل
/// <c>ConcreteTypes/_CreateForm.cshtml</c>) تُحمَّل داخل صفحة أب، والسكربت يُحمَّل
/// مرة واحدة للصفحة كلها — فالمسؤولية على الأب لا على الجزئية. القاعدة تتحقق من أن
/// كل أب يعرض تلك الجزئية يحمّل السكربت، حتى لا يفلت النموذج إذا انتقلت الجزئية
/// إلى صفحة جديدة.</para>
///
/// <para>يقرأ الملفات من القرص، فيلزم تشغيل الاختبارات من داخل المستودع
/// (نفس أسلوب <see cref="OrderStatusMapTests"/>).</para>
/// </summary>
public class FormValidationScriptsTests
{
    private const string ViewsRoot = "Kharasana.Web/Views";

    /// <summary>الجزئية الوحيدة المسموح بها كمصدر لسكربتات التحقق.</summary>
    private const string ValidationPartial = "_ValidationScriptsPartial";

    /// <summary>وسم بداية أي نموذج إدخال.</summary>
    private const string FormTag = "<form";

    /// <summary>وسم مساعد يُولّد حقول الإدخال مع قيود التحقق.</summary>
    private const string InputTagHelper = "asp-for";

    // ─────────────────────────────────────────────
    // 1. صفحات النماذج تحمّل السكربت
    // ─────────────────────────────────────────────

    [Fact]
    public void FormViews_LoadTheValidationScriptsPartial()
    {
        var views = ViewFiles().ToList();

        // حارس على المسح نفسه: لو تغيّر المسار أو الامتداد لعاد المجموع صفراً
        // ونجح الاختبار بلا أن يفحص شيئاً.
        views.Should().NotBeEmpty($"{ViewsRoot} يجب أن يحتوي ملفات .cshtml");

        var formViews = views
            .Where(file => !IsPartial(file))
            .Where(file => File.ReadAllText(file) is var text
                           && text.Contains(FormTag, StringComparison.Ordinal)
                           && text.Contains(InputTagHelper, StringComparison.Ordinal))
            .ToList();

        formViews.Should().NotBeEmpty("يوجد في المشروع نماذج إدخال فعلية");

        formViews.Should().Contain(
            file => Relative(file).EndsWith("Orders/Create.cshtml", StringComparison.Ordinal),
            "صفحة إنشاء الطلب نموذج إدخال معروف، وغياب السكربت عنها هو العطل الأصلي");

        var missing = formViews
            .Where(file => !File.ReadAllText(file).Contains(ValidationPartial, StringComparison.Ordinal))
            .Select(Relative)
            .ToList();

        missing.Should().BeEmpty(
            "كل صفحة فيها نموذج و asp-for يجب أن تحمّل {0}، وإلا صار التحقق خادمياً فقط "
            + "وLost الحقول عند كل خطأ إدخال",
            ValidationPartial);
    }

    // ─────────────────────────────────────────────
    // 2. أبناء الجزئيات يحمّلون السكربت
    // ─────────────────────────────────────────────

    [Fact]
    public void ViewsRenderingAFormPartial_LoadTheValidationScriptsPartial()
    {
        var views = ViewFiles().ToList();

        var formPartials = views
            .Where(IsPartial)
            .Where(file => File.ReadAllText(file).Contains(InputTagHelper, StringComparison.Ordinal))
            .ToList();

        formPartials.Should().NotBeEmpty("توجد جزئيات نماذج مُشتركة (ConcreteTypes/Factories)");

        var offenders = new List<string>();

        foreach (var partial in formPartials)
        {
            // الاسم المرجعي في <partial name="X" /> أو @await Html.PartialAsync("X")
            var name = Path.GetFileNameWithoutExtension(partial);
            var folder = Path.GetDirectoryName(partial)!;

            var parents = Directory
                .EnumerateFiles(folder, "*.cshtml")
                .Where(file => !IsPartial(file))
                .Where(file => File.ReadAllText(file).Contains(name, StringComparison.Ordinal))
                .ToList();

            if (parents.Count == 0)
            {
                offenders.Add($"{Relative(partial)} (لا أب يعرضه — الجزئية ميتة أو مُسماة باسم آخر)");
                continue;
            }

            offenders.AddRange(parents
                .Where(parent => !File.ReadAllText(parent).Contains(ValidationPartial, StringComparison.Ordinal))
                .Select(Relative));
        }

        offenders.Should().BeEmpty(
            "الصفحة التي تعرض جزئية فيها asp-for هي المسؤولة عن تحميل {0}",
            ValidationPartial);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    /// <summary>جزئية عرض (اسمها يبدأ بـ <c>_</c>) — تُحمَّل داخل صفحة أب لا مستقلة.</summary>
    private static bool IsPartial(string path) => Path.GetFileName(path).StartsWith('_');

    private static IEnumerable<string> ViewFiles()
        => Directory.EnumerateFiles(
            Path.Combine(RepoRoot(), ViewsRoot.Replace('/', Path.DirectorySeparatorChar)),
            "*.cshtml",
            SearchOption.AllDirectories);

    private static string Relative(string path)
        => Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Kharasana.slnx")))
            dir = dir.Parent;

        dir.Should().NotBeNull(
            $"تعذّر العثور على Kharasana.slnx صعوداً من {AppContext.BaseDirectory} — شغّل الاختبارات من داخل المستودع");

        return dir!.FullName;
    }
}
