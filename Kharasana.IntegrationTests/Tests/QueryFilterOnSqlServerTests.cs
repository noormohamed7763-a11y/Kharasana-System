using FluentAssertions;
using Kharasana.IntegrationTests.Infrastructure;
using Kharasana.IntegrationTests.TestData;
using Kharasana.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.IntegrationTests.Tests;

/// <summary>
/// إثبات علّة P0.1 على SQL Server حقيقي — وهي العلّة التي لا يكفي فيها InMemory.
///
/// <para><b>العلّة:</b> <c>OrderRepository.OrdersWithDetails</c> تُحمّل مراجع
/// <b>إلزامية</b> (العميل، المصنع، نوع الخرسانة) بـ<c>Include</c>، فيترجمها
/// EF إلى <c>INNER JOIN</c>. وفلتر الحذف الناعم للكيان المرجعي كان يُطبَّق على
/// الانضمام، فيُسقط <b>صف الطلب نفسه</b> — أرشيف نوع خرسانة واحد يمحو طلباته
/// التاريخية بلا خطأ ظاهر.</para>
///
/// <para><b>لماذا هنا لا في اختبارات الوحدة:</b> اختبار الوحدة
/// (<c>OrderRepositoryQueryFilterTests</c>) يثبت <b>نية</b> الاستعلام عبر InMemory،
/// لكن InMemory ينفّذ LINQ في الذاكرة ولا يحوّله إلى SQL. هنا يُبنى الـ<c>INNER JOIN</c>
/// ويُترجم الفلتر ترجمةً حقيقية، فيثبت أن الإصلاح يعمل على المحرّك الذي يعمل به النظام.</para>
/// </summary>
public class QueryFilterOnSqlServerTests
{
    /// <summary>
    /// الرسم السليم كاملًا: مصنع 1، عميل 100، نوع خرسانة 1، سائق 200، طلب 1.
    /// </summary>
    private static async Task SeedOrderGraphAsync(SqlServerTestDatabase database)
    {
        await using var seed = database.CreateContext();

        seed.Factories.Add(IntegrationSeed.Factory(1, "مصنع_فلتر"));
        seed.Users.Add(IntegrationSeed.Client(100, "عميل_فلتر"));
        seed.Users.Add(IntegrationSeed.Driver(200, "سائق_فلتر", factoryId: 1));
        seed.ConcreteTypes.Add(IntegrationSeed.ConcreteType(1, 1, "C30"));
        seed.Orders.Add(IntegrationSeed.Order(1, 100, 1, 1, driverId: 200));

        await seed.SaveChangesAsync();
    }

    [SqlServerFact]
    public async Task Order_OfArchivedConcreteType_IsStillReturned()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await SeedOrderGraphAsync(database);

        // أرشفة نوع الخرسانة — هنا كان الطلب يختفي.
        await using (var archive = database.CreateContext())
        {
            var concreteType = await archive.ConcreteTypes.FirstAsync(c => c.ConcreteTypeId == 1);
            concreteType.IsDeleted = true;
            await archive.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var repository = new OrderRepository(context);

        var page = await repository.GetPagedAsync(
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();

        var order = page.Items.Single();
        order.ConcreteType.Should().NotBeNull("الطلب التاريخي يجب أن يبقى ظاهرًا بعد أرشفة نوعه");
        order.ConcreteType.Name.Should().Be("C30");
        order.Client.FullName.Should().Be("عميل_فلتر");
        order.Driver!.FullName.Should().Be("سائق_فلتر");
    }

    [SqlServerFact]
    public async Task Order_OfArchivedFactoryAndArchivedClient_IsStillReturned()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await SeedOrderGraphAsync(database);

        await using (var archive = database.CreateContext())
        {
            var factory = await archive.Factories.FirstAsync(f => f.FactoryId == 1);
            factory.IsDeleted = true;

            var client = await archive.Users.FirstAsync(u => u.UserId == 100);
            client.IsDeleted = true;

            await archive.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var repository = new OrderRepository(context);

        var page = await repository.GetPagedAsync(
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        page.TotalCount.Should().Be(1);
        page.Items.Single().Factory.FactoryName.Should().Be("مصنع_فلتر");
        page.Items.Single().Client.FullName.Should().Be("عميل_فلتر");
    }

    /// <summary>
    /// القاعدة المقابلة، وهي الأهم أمنيًا: تجاوز الفلتر في
    /// <c>OrdersWithDetails</c> أُضيف لأجل المراجع، فيجب ألّا يصير بابًا خلفيًا
    /// يعيد <b>الطلبات المحذوفة</b> إلى الواجهات.
    /// </summary>
    [SqlServerFact]
    public async Task SoftDeletedOrder_IsStillExcluded()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await SeedOrderGraphAsync(database);

        await using (var archive = database.CreateContext())
        {
            var order = await archive.Orders.FirstAsync(o => o.OrderId == 1);
            order.IsDeleted = true;
            await archive.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var repository = new OrderRepository(context);

        var page = await repository.GetPagedAsync(
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    /// <summary>
    /// مسار التعديل يقرأ بـ<c>IgnoreQueryFilters</c> أيضًا، فيلزم أن يبقى الطلب
    /// المحذوف ناعمًا مستبعدًا منه — وإلا أمكن تعديل طلب مؤرشف بمعرّفه.
    /// </summary>
    [SqlServerFact]
    public async Task SoftDeletedOrder_IsNotReadableForUpdate()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await SeedOrderGraphAsync(database);

        await using (var archive = database.CreateContext())
        {
            var order = await archive.Orders.FirstAsync(o => o.OrderId == 1);
            order.IsDeleted = true;
            await archive.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var repository = new OrderRepository(context);

        var forUpdate = await repository.GetByIdWithDetailsForUpdateAsync(1);
        var forRead = await repository.GetByIdWithDetailsAsync(1);

        forUpdate.Should().BeNull();
        forRead.Should().BeNull();
    }
}
