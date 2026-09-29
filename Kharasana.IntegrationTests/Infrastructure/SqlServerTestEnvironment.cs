namespace Kharasana.IntegrationTests.Infrastructure;

/// <summary>
/// بيئة اختبارات التكامل — المصدر الوحيد لسلسلة الاتصال بخادم SQL Server حقيقي.
///
/// <para><b>لماذا SQL Server حقيقي ولا InMemory:</b> مزوّد InMemory لا ينفّذ SQL
/// Server، فلا يُثبت شيئًا ممّا نختبره هنا: الفهارس <b>المُرَشَّحة</b>
/// (<c>WHERE IsDeleted = 0</c>) لا تمييز لها عنده، و<b>FK Restrict</b> لا يُفرَض،
/// و<b>RowVersion</b> لا يتغيّر، و<b>ترتيب</b> الحروف (collation) غير محاكى —
/// وهو تحديدًا سبب بقاء بند <c>ToLower()</c> بلا إصلاح.</para>
///
/// <para><b>التهيئة:</b> يُقرأ متغيّر البيئة <see cref="ConnectionStringVariable"/>.
/// وإن غاب تُتخطّى اختبارات هذا المشروع كلها بهدوء (Skip) فلا تُسقط بناء CI لمن لا
/// خادم لديه، ولا تُوهم أحدًا بنجاح لم يقع. مثال (Windows، PowerShell):</para>
/// <code>
/// $env:KHARASANA_TEST_SQLSERVER = "Server=localhost;Database=master;User Id=sa;Password=…;TrustServerCertificate=True"
/// dotnet test Kharasana.IntegrationTests
/// </code>
/// </summary>
public static class SqlServerTestEnvironment
{
    /// <summary>اسم متغيّر البيئة الحامل لسلسلة الاتصال. يُبنى منه اسم قاعدة معزولة لكل اختبار.</summary>
    public const string ConnectionStringVariable = "KHARASANA_TEST_SQLSERVER";

    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable);

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>سبب التخطّي — يظهر في تقرير الاختبار فيعرف القارئ لماذا لم يُنفَّذ.</summary>
    public static string SkipReason =>
        $"اختبار تكامل يحتاج SQL Server حقيقيًا: عيّن سلسلة الاتصال في متغيّر البيئة " +
        $"'{ConnectionStringVariable}' ثم أعد التشغيل. (InMemory لا يثبت الفهارس المُرَشَّحة " +
        $"ولا FK Restrict ولا RowVersion ولا ترتيب SQL Server.)";
}
