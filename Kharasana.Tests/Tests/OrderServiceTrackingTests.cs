using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات تتبّع التغييرات في OrderService — تحرس قاعدة واحدة:
/// <b>تغيير حالة الطلب يجب أن يُنتج UPDATE واحداً على الطلب وحده</b>،
/// لا UPDATE على الكيانات المرتبطة به (العميل، المصنع، نوع الخرسانة، السائق).
///
/// السبب: كل تلك الكيانات تحمل <c>[Timestamp] RowVersion</c>، فإدراجها في UPDATE
/// غير مقصود يعني (١) كتابات زائدة على قاعدة البيانات مع كل تغيير حالة،
/// (٢) تضخّم RowVersion لكيانات لم تتغيّر فيفشل تعديل مشروع عليها بتعارض وهمي (409).
///
/// ⚠️ الاختباران يستخدمان سياقين على قاعدة InMemory واحدة:
/// سياق للتهيئة وسياق للخدمة — لأن السياق المُهيّئ يكون الكيان فيه مُتتبَّعاً،
/// فيُخفي مسار <c>SetValues</c> عيبَ إرفاق الرسم البياني كاملاً.
/// </summary>
public class OrderServiceTrackingTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly KharasanaDbContext _seedContext;
    private readonly KharasanaDbContext _context;
    private readonly OrderService _orderService;

    public OrderServiceTrackingTests()
    {
        _seedContext = TestDataSeeder.CreateContext(_dbName);
        _context = TestDataSeeder.CreateContext(_dbName);
        var passwordHasher = new PasswordHasher();
        var uow = new UnitOfWork(_context);
        var orderHelper = new OrderHelperService(uow);
        var orderQueryService = new OrderQueryService(uow, orderHelper);
        var orderCommandService = new OrderCommandService(uow, passwordHasher);
        var orderWorkflowService = new OrderWorkflowService(uow);
        _orderService = new OrderService(passwordHasher, uow, orderQueryService, orderCommandService, orderWorkflowService);
    }

    /// <summary>
    /// يُهيّئ مصنعاً وعميلاً ونوع خرسانة وطلباً في سياق التهيئة، ثم يُفرّغ
    /// متتبّع سياق الخدمة ليحاكي بداية طلب HTTP جديد.
    /// </summary>
    private async Task<Order> SeedOrderAsync(OrderStatus status, decimal unitPrice)
    {
        _seedContext.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع_تتبع"));
        _seedContext.Users.Add(TestDataSeeder.CreateUser(90, "عميل_تتبع", UserRole.Client, phone: "050000090"));
        _seedContext.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(1, 1, "C25", 25, 150m));

        var order = TestDataSeeder.CreateOrder(
            orderId: 0, clientId: 90, factoryId: 1, concreteTypeId: 1,
            quantity: 10, status: status, unitPrice: unitPrice);

        _seedContext.Orders.Add(order);
        await _seedContext.SaveChangesAsync();

        // سياق الخدمة لم يقرأ شيئاً بعد — متتبّعه فارغ تماماً كما في طلب حقيقي
        _context.ChangeTracker.Clear();

        return order;
    }

    /// <summary>
    /// يسجّل أنواع الكيانات بحالة Modified لحظة الحفظ — أي ما ستُرسله EF فعلاً كـ UPDATE.
    /// يُعدّل القائمة المُعادة في مكانها (Clear/AddRange) لا يستبدلها، وإلا بقي
    /// مُتغيّر الاختبار مشيراً إلى القائمة الفارغة الأصلية.
    /// </summary>
    private List<string> CaptureModifiedTypes()
    {
        var modified = new List<string>();

        _context.SavingChanges += (_, _) =>
        {
            modified.Clear();
            modified.AddRange(_context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .Select(e => e.Metadata.ClrType.Name)
                .OrderBy(name => name));
        };

        return modified;
    }

    [Fact]
    public async Task ApproveOrder_UpdatesOnlyTheOrder()
    {
        // Arrange
        var order = await SeedOrderAsync(OrderStatus.Pending, unitPrice: 150m);
        var modified = CaptureModifiedTypes();

        // Act
        await _orderService.ApproveOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — الطلب وحده
        modified.Should().Equal("Order");
    }

    [Fact]
    public async Task UpdateOrder_UpdatesOnlyTheOrder()
    {
        // Arrange
        var order = await SeedOrderAsync(OrderStatus.Pending, unitPrice: 150m);
        var modified = CaptureModifiedTypes();

        // Act — تعديل حقول الطلب دون تغيير نوع الخرسانة
        await _orderService.UpdateOrderAsync(
            order.OrderId,
            new UpdateOrderDto
            {
                ConcreteTypeId = 1,
                Quantity = 20m,
                ProjectName = "مشروع_معدَّل"
            },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        modified.Should().Equal("Order");

        var stored = await _context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == order.OrderId);
        stored.Quantity.Should().Be(20m);
        stored.TotalPrice.Should().Be(3000m);
    }

    [Fact]
    public async Task AssignDriver_UpdatesOrderAndTheTwoDriversOnly()
    {
        // Arrange — سائق حر وآخر مشغول بالطلب نفسه (لاختبار تحرير السابق)
        var order = await SeedOrderAsync(OrderStatus.Approved, unitPrice: 150m);

        _seedContext.Users.Add(TestDataSeeder.CreateDriver(210, "سائق_سابق", 1, status: DriverStatus.Busy));
        _seedContext.Users.Add(TestDataSeeder.CreateDriver(211, "سائق_جديد", 1, status: DriverStatus.Available));
        await _seedContext.SaveChangesAsync();

        order.DriverId = 210;
        await _seedContext.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var modified = CaptureModifiedTypes();

        // Act — إسناد الطلب لسائق آخر
        await _orderService.AssignDriverAsync(
            order.OrderId,
            new AssignDriverDto { DriverId = 211, TruckPlate = "أ ب ج 111" },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — الطلب والسائقان فقط؛ لا مصنع ولا عميل ولا نوع خرسانة
        modified.Should().Equal("Order", "User", "User");

        var previous = await _context.Users.AsNoTracking().SingleAsync(u => u.UserId == 210);
        var current = await _context.Users.AsNoTracking().SingleAsync(u => u.UserId == 211);
        previous.DriverStatus.Should().Be(DriverStatus.Available);
        current.DriverStatus.Should().Be(DriverStatus.Busy);
    }

    public void Dispose()
    {
        _context.Dispose();
        _seedContext.Dispose();
    }
}
