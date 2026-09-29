using FluentAssertions;
using Kharasana.IntegrationTests.Infrastructure;
using Kharasana.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.IntegrationTests.Tests;

/// <summary>
/// قيود المخطط التي <b>لا يفرضها</b> مزوّد InMemory، فيثبتها SQL Server وحده:
/// الفهرس الفريد المُرَشَّح، وFK Restrict، وRowVersion.
///
/// <para>هذه ليست اختبارات منطق خدمات (تلك في <c>Kharasana.Tests</c>) بل اختبارات
/// <b>عقد قاعدة البيانات</b>: لو حُذف <c>HasFilter</c> من
/// <c>ConcreteTypeConfiguration</c>، أو صار <c>OnDelete</c> شيئًا غير
/// <c>Restrict</c>، أو نُزع <c>IsRowVersion</c> — تسقط هذه الاختبارات وحدها.</para>
/// </summary>
public class SchemaConstraintTests
{
    [SqlServerFact]
    public async Task ConcreteType_DuplicateNameInSameFactory_IsRejected()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();

        await using var context = database.CreateContext();
        context.Factories.Add(IntegrationSeed.Factory(1, "مصنع_فهرس"));
        context.ConcreteTypes.Add(IntegrationSeed.ConcreteType(1, 1, "C30"));
        await context.SaveChangesAsync();

        // النوع الثاني يحمل الاسم نفسه في المصنع نفسه وهو غير محذوف ⇒ يصطدم بالفهرس.
        context.ConcreteTypes.Add(IntegrationSeed.ConcreteType(2, 1, "C30"));
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [SqlServerFact]
    public async Task ConcreteType_ArchivedName_IsReleasedForReuse()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();

        await using var context = database.CreateContext();
        context.Factories.Add(IntegrationSeed.Factory(1, "مصنع_تحرير"));
        context.ConcreteTypes.Add(IntegrationSeed.ConcreteType(1, 1, "C30"));
        await context.SaveChangesAsync();

        // أرشفة النوع ⇒ يخرج من نطاق الفهرس المُرَشَّح (WHERE IsDeleted = 0) فيتحرّر الاسم.
        var archived = await context.ConcreteTypes.FirstAsync(c => c.ConcreteTypeId == 1);
        archived.IsDeleted = true;
        await context.SaveChangesAsync();

        context.ConcreteTypes.Add(IntegrationSeed.ConcreteType(3, 1, "C30"));
        var act = async () => await context.SaveChangesAsync();

        // ✅ هذا هو جوهر «خيار B»: إعادة استخدام الأسماء المحرَّرة — لا يفرضه إلا فهرس مُرشَّح.
        await act.Should().NotThrowAsync();

        // وفلتر الاستعلام العام يُخفي المؤرشف، فيبقى الظاهر واحدًا.
        (await context.ConcreteTypes.CountAsync()).Should().Be(1);
        (await context.ConcreteTypes.IgnoreQueryFilters().CountAsync()).Should().Be(2);
    }

    [SqlServerFact]
    public async Task Factory_WithConcreteTypes_CannotBePhysicallyDeleted()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();

        await using var context = database.CreateContext();
        context.Factories.Add(IntegrationSeed.Factory(1, "مصنع_قيد"));
        context.ConcreteTypes.Add(IntegrationSeed.ConcreteType(1, 1, "C30"));
        await context.SaveChangesAsync();

        // حذف صلب للمصنع بينما أنواعه قائمة ⇒ FK Restrict يرفضه SQL Server.
        var factory = await context.Factories.FirstAsync(f => f.FactoryId == 1);
        context.Factories.Remove(factory);

        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [SqlServerFact]
    public async Task Order_RowVersion_DetectsConcurrentUpdate()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();

        await using (var seed = database.CreateContext())
        {
            seed.Factories.Add(IntegrationSeed.Factory(1, "مصنع_تزامن"));
            seed.Users.Add(IntegrationSeed.Client(100, "عميل_تزامن"));
            seed.ConcreteTypes.Add(IntegrationSeed.ConcreteType(1, 1, "C30"));
            seed.Orders.Add(IntegrationSeed.Order(1, 100, 1, 1));
            await seed.SaveChangesAsync();
        }

        await using var firstWriter = database.CreateContext();
        await using var secondWriter = database.CreateContext();

        // قراءتان لنفس الصف قبل أي كتابة ⇒ نسختان بنفس قيمة RowVersion.
        var fromFirst = await firstWriter.Orders.FirstAsync(o => o.OrderId == 1);
        var fromSecond = await secondWriter.Orders.FirstAsync(o => o.OrderId == 1);

        fromFirst.Quantity = 10;
        await firstWriter.SaveChangesAsync();

        // الكتابة الثانية تحمل RowVersion قديمًا ⇒ SQL Server يردّ صفر صفوف متأثرة.
        fromSecond.Quantity = 20;
        var act = async () => await secondWriter.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
