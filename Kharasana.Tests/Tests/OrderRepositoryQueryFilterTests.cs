using Kharasana.Application.Common;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Tests.TestData;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حارس علّة P0 — «الطلب يختفي لأن كيانًا مرجعيًا حُذف ناعمًا».
///
/// كل قراءات الطلبات تمرّ بـ <c>OrderRepository.OrdersWithDetails</c>، وهي تُحمّل
/// مراجع <b>إلزامية</b> (العميل، المصنع، نوع الخرسانة) عبر <c>Include</c>. وفلتر الحذف
/// الناعم الخاص بالكيان المرجعي كان يُطبَّق على الانضمام فيتحوّل إلى INNER JOIN يُسقط
/// <b>صف الطلب نفسه</b> — لا صف الكيان المرجعي وحده. النتيجة: أرشيف نوع خرسانة واحد
/// يمحو طلباته التاريخية من القوائم والتفاصيل والتقارير، بلا أي رسالة خطأ.
///
/// والقاعدة المقابلة محفوظة أيضًا: الطلب المحذوف ناعمًا يبقى مستبعدًا، فلا يكون
/// تجاوز الفلتر بابًا خلفيًا يعيد الطلبات المحذوفة إلى الواجهات.
/// </summary>
public class OrderRepositoryQueryFilterTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryQueryFilterTests()
    {
        _context = TestDataSeeder.CreateContext();
        _repository = new OrderRepository(_context);
    }

    /// <summary>
    /// مصنع وعميل ونوع خرسانة وطلب مكتمل الأركان — الحالة السليمة التي تُقاس عليها
    /// حالات الحذف الناعم، فيكون الفرق الوحيد هو ما حُذف ناعمًا.
    /// </summary>
    private async Task SeedOrderGraphAsync(
        bool concreteTypeDeleted = false,
        bool factoryDeleted = false,
        bool clientDeleted = false,
        bool orderDeleted = false,
        bool driverDeleted = false)
    {
        _context.Factories.Add(
            TestDataSeeder.CreateFactory(1, "مصنع_فلتر", isDeleted: factoryDeleted));

        var client = TestDataSeeder.CreateUser(100, "عميل_فلتر", UserRole.Client, phone: "050000100");
        client.IsDeleted = clientDeleted;
        _context.Users.Add(client);

        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(1, 1, "C30", isActive: !concreteTypeDeleted, isDeleted: concreteTypeDeleted));

        var driver = TestDataSeeder.CreateDriver(200, "سائق_فلتر", factoryId: 1);
        driver.IsDeleted = driverDeleted;
        _context.Users.Add(driver);

        var order = TestDataSeeder.CreateOrder(1, 100, 1, 1, quantity: 20, driverId: 200);
        order.IsDeleted = orderDeleted;
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();
    }

    // ============================================================
    // الطلب يبقى ظاهرًا عندما يُحذف الكيان المرجعي ناعمًا
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_OrderOfSoftDeletedConcreteType_IsStillReturned()
    {
        await SeedOrderGraphAsync(concreteTypeDeleted: true);

        var result = await _repository.GetPagedAsync(
            new CallerContext(1, UserRole.Admin, null),
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.Single().ConcreteTypeName.Should().Be("C30");
    }

    [Fact]
    public async Task GetPagedAsync_OrderOfSoftDeletedFactory_IsStillReturned()
    {
        await SeedOrderGraphAsync(factoryDeleted: true);

        var result = await _repository.GetPagedAsync(
            new CallerContext(1, UserRole.Admin, null),
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().FactoryName.Should().Be("مصنع_فلتر");
    }

    [Fact]
    public async Task GetPagedAsync_OrderOfSoftDeletedClient_IsStillReturned()
    {
        await SeedOrderGraphAsync(clientDeleted: true);

        var result = await _repository.GetPagedAsync(
            new CallerContext(1, UserRole.Admin, null),
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().ClientName.Should().Be("عميل_فلتر");
    }

    [Fact]
    public async Task GetPagedAsync_OrderOfSoftDeletedDriver_IsStillReturned()
    {
        await SeedOrderGraphAsync(driverDeleted: true);

        var result = await _repository.GetPagedAsync(
            new CallerContext(1, UserRole.Admin, null),
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().DriverName.Should().Be("سائق_فلتر");
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_OrderOfSoftDeletedConcreteType_IsStillReturned()
    {
        await SeedOrderGraphAsync(concreteTypeDeleted: true);

        var order = await _repository.GetByIdWithDetailsAsync(1);

        order.Should().NotBeNull();
        order!.ConcreteType.Should().NotBeNull();
        order.ConcreteType.Name.Should().Be("C30");
    }

    // ============================================================
    // القاعدة المقابلة: الطلب المحذوف ناعمًا يبقى مستبعدًا
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_SoftDeletedOrder_IsStillExcluded()
    {
        await SeedOrderGraphAsync(orderDeleted: true);

        var result = await _repository.GetPagedAsync(
            new CallerContext(1, UserRole.Admin, null),
            factoryId: null, clientId: null, driverId: null, status: null,
            search: null, pageNumber: 1, pageSize: 10);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ============================================================
    // الأثر من طرف الواجهة: الاسم يصل ولا يسقط إلى نص الاحتياط
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ThroughService_KeepsConcreteTypeNameOfSoftDeletedType()
    {
        await SeedOrderGraphAsync(concreteTypeDeleted: true);

        var uow = new UnitOfWork(_context);
        var passwordHasher = new PasswordHasher();
        var orderHelper = new OrderHelperService(uow);
        IOrderService service = new OrderService(
            passwordHasher,
            uow,
            new OrderQueryService(uow, orderHelper),
            new OrderCommandService(uow, passwordHasher),
            new OrderWorkflowService(uow));

        var result = await service.GetPagedAsync(
            factoryId: null, clientId: null, driverId: null,
            caller: new CallerContext(1, UserRole.Admin, null),
            pagination: new PaginationParams { PageNumber = 1, PageSize = 10 });

        var dto = result.Items.Should().ContainSingle().Subject;
        dto.ConcreteTypeName.Should().Be("C30");
        dto.ClientName.Should().Be("عميل_فلتر");
    }

    public void Dispose() => _context.Dispose();
}
