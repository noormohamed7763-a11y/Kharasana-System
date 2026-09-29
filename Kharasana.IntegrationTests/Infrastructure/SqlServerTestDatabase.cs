using Kharasana.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.IntegrationTests.Infrastructure;

/// <summary>
/// قاعدة بيانات SQL Server حقيقية <b>معزولة لاختبار واحد</b>: تُنشأ باسم فريد،
/// وتُطبَّق عليها <b>الترحيلات الفعلية</b> (<c>Database.MigrateAsync</c>) — لا
/// <c>EnsureCreated</c> — ثم تُحذف في النهاية.
///
/// <para><b>لماذا الترحيلات لا EnsureCreated:</b> الترحيلات هي ما ينشئه SQL Server
/// في الإنتاج فعلًا: الفهارس المُرَشَّحة وFK Restrict وRowVersion. و<c>EnsureCreated</c>
/// يبني المخطط من النموذج مباشرةً فيتجاوز الترحيلات، فنختبر مخططًا غير الذي يعمل به
/// النظام — وهي بالضبط الثغرة التي دفعنا إليها InMemory.</para>
///
/// <para><b>لماذا قاعدة لكل اختبار لا قاعدة مشتركة:</b> اختبارات القيود تعتمد على
/// اصطدام الفهارس الفريدة وتلوّث ChangeTracker بعد فشل حفظ؛ فالفصل التام يلغي أي
/// اعتماد على ترتيب التنفيذ أو تنظيف يدوي بين الاختبارات.</para>
/// </summary>
public sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private readonly string _connectionString;

    private SqlServerTestDatabase(string connectionString) => _connectionString = connectionString;

    /// <summary>
    /// ينشئ قاعدة معزولة ويطبّق الترحيلات. يرمي إن لم يكن الخادم مُهيَّأ — لكن
    /// لا يُنادى أصلًا في تلك الحالة لأن كل اختبار موسوم بـ<c>[SqlServerFact]</c>.
    /// </summary>
    public static async Task<SqlServerTestDatabase> CreateAsync()
    {
        var baseConnectionString = SqlServerTestEnvironment.ConnectionString
            ?? throw new InvalidOperationException(SqlServerTestEnvironment.SkipReason);

        var database = new SqlServerTestDatabase(
            BuildConnectionString(baseConnectionString, $"Kharasana_IT_{Guid.NewGuid():N}"));

        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        return database;
    }

    public KharasanaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KharasanaDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new KharasanaDbContext(options);
    }

    /// <summary>
    /// يستبدل اسم قاعدة البيانات في سلسلة الاتصال المزوَّدة، فيصير كل اختبار على
    /// قاعدة مستقلة بينما تبقى بقية الإعدادات (الخادم، المصادقة، الشهادة) كما هي.
    /// </summary>
    private static string BuildConnectionString(string baseConnectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
