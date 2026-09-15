using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.Extensions.Options;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات OrderService — التركيز على:
/// • تدفقات تغيير الحالة (Status Transitions) — صحيح/خاطئ
/// • صلاحية الوصول حسب الدور (Admin / FactoryEmployee / Client / Driver)
/// • حساب السعر (SetPrice)
/// • تعيين السائق (AssignDriver) — صلاحيات السائق وحالة الطلب
/// • تدفق التوصيل الكامل (Approve → StartDelivery → Deliver → Close)
/// </summary>
public class OrderServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _unitOfWork = new UnitOfWork(_context);

        // نحتاج IPasswordHasher لـ CreatePhoneOrder — نستخدم BCrypt الحقيقي
        var passwordHasher = new PasswordHasher();
        _orderService = new OrderService(
            _unitOfWork.Orders, _unitOfWork.ConcreteTypes,
            _unitOfWork.Users, passwordHasher, _unitOfWork);
    }

    // ─────────────────────────────────────────────
    // ①  تدفقات تغيير الحالة
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Approve_PendingOrder_Succeeds()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending, unitPrice: 150m);

        // Act — موظف المصنع يعتمد الطلب (الحالة: Pending → Approved)
        var result = await _orderService.ApproveOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        result.Should().BeTrue();
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.Status.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public async Task Approve_NonPendingOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.New);

        // Act — محاولة اعتماد طلب جديد (الحالة: New → Approved غير مسموح)
        var act = () => _orderService.ApproveOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("لا يمكن اعتماد الطلب إلا وهو في حالة قيد الانتظار.");
    }

    [Fact]
    public async Task SetPrice_ZeroPrice_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.New);

        // Act
        var act = () => _orderService.SetPriceAsync(
            order.OrderId, unitPrice: 0, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("سعر المتر يجب أن يكون أكبر من صفر.");
    }

    [Fact]
    public async Task SetPrice_OnApprovedOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);

        // Act — لا يمكن تعديل السعر لطلب معتمد
        var act = () => _orderService.SetPriceAsync(
            order.OrderId, unitPrice: 200m, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("لا يمكن تعديل السعر في هذه المرحلة من الطلب.");
    }

    [Fact]
    public async Task SetPrice_NewOrder_ChangesStatusToPending()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.New);

        // Act — تحديد السعر على طلب جديد يحوّله إلى Pending تلقائياً
        await _orderService.SetPriceAsync(
            order.OrderId, unitPrice: 150m, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.Status.Should().Be(OrderStatus.Pending);
        updated.UnitPrice.Should().Be(150m);
        updated.TotalPrice.Should().Be(150m * order.Quantity);
    }

    // ─────────────────────────────────────────────
    // ②  تدفق التوصيل الكامل: Pending → Approved → OnTheWay → Delivered → Closed
    // ─────────────────────────────────────────────

    [Fact]
    public async Task FullDeliveryFlow_PendingToClosed()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        // 1. تعيين السعر (New → Pending معредел) — سبق له
        await _orderService.SetPriceAsync(
            order.OrderId, 150m, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // 2. اعتماد الطلب (Pending → Approved)
        await _orderService.ApproveOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // 3. تعيين السائق
        var driver = TestDataSeeder.CreateDriver(100, "سائق التوصيل", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        await _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = 100,
            TruckPlate = "أ ب ج 123"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // 4. بدء التوصيل (Approved → OnTheWay)
        await _orderService.StartDeliveryAsync(
            order.OrderId, callerId: 100, UserRole.Driver, callerFactoryId: 1);

        // 5. تأكيد التسليم (OnTheWay → Delivered)
        await _orderService.DeliverOrderAsync(
            order.OrderId, callerId: 100, UserRole.Driver, callerFactoryId: 1);

        // 6. إغلاق الطلب (Delivered → Closed)
        await _orderService.CloseOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        var final = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        final!.Status.Should().Be(OrderStatus.Closed);
        final.ClosedAt.Should().NotBeNull();

        // التحقق من تحرير السائق بعد التسليم
        var driverAfter = await _context.Users.FindAsync(100);
        driverAfter!.DriverStatus.Should().Be(DriverStatus.Available);
    }

    // ─────────────────────────────────────────────
    // ③  صلاحية الوصول حسب الدور
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrder_ClientCanOnlySeeOwnOrder()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        // Act — العميل يحاول رؤية طلب ينتمي لعميل آخر
        var act = () => _orderService.GetByIdAsync(
            order.OrderId, callerId: 999, UserRole.Client, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.OrderNotFound);
    }

    [Fact]
    public async Task GetOrder_FactoryEmployeeCanOnlySeeSameFactoryOrder()
    {
        // Arrange — طلب ينتمي للمصنع 1، موظف ينتمي للمصنع 2
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        var otherFactory = TestDataSeeder.CreateFactory(800, "مصنع_أخرى");
        _context.Factories.Add(otherFactory);
        await _context.SaveChangesAsync();

        // Act — موظف المصنع 2 يحاول رؤية طلب المصنع 1
        var act = () => _orderService.GetByIdAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 800);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.OrderNotFound);
    }

    // ─────────────────────────────────────────────
    // ④  تعيين السائق — شروط وصلاحيات
    // ─────────────────────────────────────────────

    [Fact]
    public async Task AssignDriver_NonApprovedOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        var driver = TestDataSeeder.CreateDriver(101, "سائق_قافل", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — محاولة تعيين سائق لطلب قيد الانتظار
        var act = () => _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = 101,
            TruckPlate = "أ ب ج"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.DriverAssignmentOnlyForApprovedOrders);
    }

    [Fact]
    public async Task AssignDriver_BusyDriver_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);
        var busyDriver = TestDataSeeder.CreateDriver(102, "سائق_مشغول", 1, status: DriverStatus.Busy);
        _context.Users.Add(busyDriver);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = 102,
            TruckPlate = "أ ب ج"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.DriverNotAvailable);
    }

    // ─────────────────────────────────────────────
    // ⑤  إلغاء الطلب
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Cancel_DeliveredOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Delivered);

        // Act
        var act = () => _orderService.CancelOrderAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("لا يمكن إلغاء طلب تم تسليمه أو إغلاقه.");
    }

    [Fact]
    public async Task Cancel_NewOrder_Succeeds()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.New);

        // Act
        await _orderService.CancelOrderAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.Status.Should().Be(OrderStatus.Cancelled);
    }

    // ─────────────────────────────────────────────
    // ⑥  بيانات السائق للعميل — تُعرض فقط أثناء التوصيل النشط
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrder_ClientSeesDriverInfo_WhenApproved()
    {
        // Arrange — طلب معتمد ومسند لسائق
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(103, "سائق_نشط", 1, status: DriverStatus.Busy);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);
        order.DriverId = driver.UserId;
        order.TruckPlate = "أ ب ج 123";
        await _context.SaveChangesAsync();

        // Act — العميل صاحب الطلب
        var details = await _orderService.GetByIdAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert — الاسم والهاتف (مُطبيع) ورقم الشاحنة ظاهرة
        details.DriverId.Should().Be(driver.UserId);
        details.DriverName.Should().Be("سائق_نشط");
        details.DriverPhone.Should().Be(YemeniPhoneHelper.Normalize(driver.Phone));
        details.TruckPlate.Should().Be("أ ب ج 123");
    }

    [Fact]
    public async Task GetOrder_ClientDoesNotSeeDriverInfo_WhenOrderNotActive()
    {
        // Arrange — طلب مُسلم (Delivered) لكن السائق ما زال معينا
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(104, "سائق_منتهي", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(status: OrderStatus.Delivered, unitPrice: 150m);
        order.DriverId = driver.UserId;
        order.TruckPlate = "أ ب ج 123";
        await _context.SaveChangesAsync();

        // Act — العميل يرى طلبه المسلَّم
        var details = await _orderService.GetByIdAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert — بيانات السائق مخفية خارج Approved/OnTheWay
        details.DriverId.Should().BeNull();
        details.DriverName.Should().BeNull();
        details.DriverPhone.Should().BeNull();
        details.TruckPlate.Should().BeNull();
    }

    [Fact]
    public async Task GetOrder_ClientDoesNotSeeDriverInfo_WhenNoDriverAssigned()
    {
        // Arrange — طلب Approved لكن لم يُسند له سائق بعد
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);

        // Act
        var details = await _orderService.GetByIdAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert
        details.DriverId.Should().BeNull();
        details.DriverName.Should().BeNull();
        details.DriverPhone.Should().BeNull();
        details.TruckPlate.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// تجهيز المصنع، العميل، ونوع الخرسانة للاختبارات.
    /// </summary>
    private async Task SeedOrderEnvironmentAsync()
    {
        var factory = TestDataSeeder.CreateFactory(1, "مصنع_اختبار");
        var client = TestDataSeeder.CreateUser(90, "عميل_اختبار", UserRole.Client, phone: "050000090");
        var concreteType = TestDataSeeder.CreateConcreteType(1, 1, "C25", 25, 150m);

        _context.Factories.Add(factory);
        _context.Users.Add(client);
        _context.ConcreteTypes.Add(concreteType);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// إنشاء وحفظ طلب اختباري بحالة معينة.
    /// </summary>
    private Domain.Entities.Order CreateAndSeedOrder(
        OrderStatus status, decimal unitPrice = 100m, int quantity = 50)
    {
        var order = TestDataSeeder.CreateOrder(
            orderId: 0, // InMemory يولّد ID تلقائيًا
            clientId: 90, factoryId: 1, concreteTypeId: 1,
            quantity: quantity, status: status, unitPrice: unitPrice);

        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }

    public void Dispose() => _context.Dispose();

    // ─────────────────────────────────────────────
    // ⑦  GetOrdersByDriverIdAsync — تقرير السائق
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersByDriverId_Admin_ReturnsAllOrdersDescending()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(200, "سائق_اختبار", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Create 3 orders for the driver with different dates
        var order1 = CreateAndSeedOrder(OrderStatus.Approved, 100m, 10);
        order1.DriverId = 200;
        order1.CreatedAt = new DateTime(2024, 1, 1);
        await _context.SaveChangesAsync();

        var order2 = CreateAndSeedOrder(OrderStatus.Delivered, 150m, 20);
        order2.DriverId = 200;
        order2.CreatedAt = new DateTime(2024, 1, 5);
        await _context.SaveChangesAsync();

        var order3 = CreateAndSeedOrder(OrderStatus.New, 200m, 30);
        order3.DriverId = 200;
        order3.CreatedAt = new DateTime(2024, 1, 3);
        await _context.SaveChangesAsync();

        // Act — Admin يرى جميع الطلبات مرتبة تنازلياً
        var orders = await _orderService.GetOrdersByDriverIdAsync(
            driverId: 200, callerFactoryId: null, callerRole: UserRole.Admin);

        // Assert
        orders.Should().HaveCount(3);
        orders.Select(o => o.CreatedAt).Should().BeInDescendingOrder();
        orders.First().CreatedAt.Should().Be(new DateTime(2024, 1, 5));
        orders.Last().CreatedAt.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task GetOrdersByDriverId_FactoryEmployee_SameFactory_ReturnsOrders()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(201, "سائق_مصنع_1", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Approved, 100m);
        order.DriverId = 201;
        order.CreatedAt = new DateTime(2024, 2, 1);
        await _context.SaveChangesAsync();

        // Act — موظف المصنع 1 يرى طلبات سائق المصنع 1
        var orders = await _orderService.GetOrdersByDriverIdAsync(
            driverId: 201, callerFactoryId: 1, callerRole: UserRole.FactoryEmployee);

        // Assert
        orders.Should().HaveCount(1);
        orders.First().OrderId.Should().Be(order.OrderId);
    }

    [Fact]
    public async Task GetOrdersByDriverId_FactoryEmployee_DifferentFactory_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var otherFactory = TestDataSeeder.CreateFactory(900, "مصنع_أخرى");
        var driver = TestDataSeeder.CreateDriver(202, "سائق_مصنع_أخرى", 900, status: DriverStatus.Available);
        _context.Factories.Add(otherFactory);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Approved, 100m);
        order.DriverId = 202;
        order.FactoryId = 900;
        await _context.SaveChangesAsync();

        // Act — موظف المصنع 1 يحاول رؤية طلبات سائق المصنع 900
        var act = () => _orderService.GetOrdersByDriverIdAsync(
            driverId: 202, callerFactoryId: 1, callerRole: UserRole.FactoryEmployee);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.NotAuthorizedToViewReport);
    }

    [Fact]
    public async Task GetOrdersByDriverId_FactoryEmployee_NonDriverUser_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var client = TestDataSeeder.CreateUser(300, "عميل_ليس_سائق", UserRole.Client, phone: "050000300");
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        // Act — موظف المصنع يحاول إنشاء تقرير لمستخدم ليس سائقاً
        var act = () => _orderService.GetOrdersByDriverIdAsync(
            driverId: 300, callerFactoryId: 1, callerRole: UserRole.FactoryEmployee);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.NotAuthorizedToViewReport);
    }

    [Fact]
    public async Task GetOrdersByDriverId_Client_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(203, "سائق_اختبار", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — العميل يحاول رؤية تقرير السائق
        var act = () => _orderService.GetOrdersByDriverIdAsync(
            driverId: 203, callerFactoryId: null, callerRole: UserRole.Client);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.NotAuthorizedToViewReport);
    }

    [Fact]
    public async Task GetOrdersByDriverId_Driver_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(204, "سائق_آخر", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — سائق يحاول رؤية تقرير سائق آخر
        var act = () => _orderService.GetOrdersByDriverIdAsync(
            driverId: 204, callerFactoryId: 1, callerRole: UserRole.Driver);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.NotAuthorizedToViewReport);
    }

    [Fact]
    public async Task GetOrdersByDriverId_NonExistentDriver_ReturnsEmpty()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();

        // Act — سائق غير موجود
        var orders = await _orderService.GetOrdersByDriverIdAsync(
            driverId: 9999, callerFactoryId: null, callerRole: UserRole.Admin);

        // Assert
        orders.Should().BeEmpty();
    }
}