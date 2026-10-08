using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;
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
        var orderHelper = new OrderHelperService(_unitOfWork);
        var orderQueryService = new OrderQueryService(_unitOfWork, orderHelper);
        var orderCommandService = new OrderCommandService(_unitOfWork, passwordHasher);
        var orderWorkflowService = new OrderWorkflowService(_unitOfWork);
        _orderService = new OrderService(
            passwordHasher, _unitOfWork, orderQueryService, orderCommandService, orderWorkflowService);
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
        result.Succeeded.Should().BeTrue();
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
    public async Task SetPrice_PendingOrder_Succeeds_AndUpdatesPrice()
    {
        // Arrange — OrderStatus.New لم تعد حالة إنشاء؛ الطلبات تُنشأ كـ Pending
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        // Act
        await _orderService.SetPriceAsync(
            order.OrderId, unitPrice: 150m, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — السعر محدّث والحالة تبقى Pending (لم نعد نستخدم New→Pending)
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

    [Fact]
    public async Task AssignDriver_CrossFactoryDriver_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);

        // Another factory and a driver belonging to it
        var otherFactory = TestDataSeeder.CreateFactory(900, "FactoryB");
        var otherDriver = TestDataSeeder.CreateDriver(105, "DriverOther", 900, status: DriverStatus.Available);
        _context.Factories.Add(otherFactory);
        _context.Users.Add(otherDriver);
        await _context.SaveChangesAsync();

        // Act — assign a driver from a different factory to this order
        var act = () => _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = 105,
            TruckPlate = "ABC"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.FactoryEmployeeFactoryMismatch);

        // Verify no partial mutation
        var updated = await _context.Users.ToListAsync();
        updated.Find(u => u.UserId == 105)!.DriverStatus.Should().Be(DriverStatus.Available);
        var orderAfter = await _context.Orders.ToListAsync();
        orderAfter.Find(o => o.OrderId == order.OrderId)!.DriverId.Should().BeNull();
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
            driverId: 200, caller: new CallerContext(10, UserRole.Admin, null));

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
            driverId: 201, caller: new CallerContext(20, UserRole.FactoryEmployee, 1));

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
            driverId: 202, caller: new CallerContext(20, UserRole.FactoryEmployee, 1));

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
            driverId: 300, caller: new CallerContext(20, UserRole.FactoryEmployee, 1));

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
            driverId: 203, caller: new CallerContext(90, UserRole.Client, null));

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
            driverId: 204, caller: new CallerContext(204, UserRole.Driver, 1));

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
            driverId: 9999, caller: new CallerContext(10, UserRole.Admin, null));

        // Assert
        orders.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // ⑧  OrderStatus.New — لم تعد حالة إنشاء؛ الطلبات تبدأ بـ Pending
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrder_PendingOrder_Succeeds()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        // Act
        var result = await _orderService.UpdateOrderAsync(
            order.OrderId, new UpdateOrderDto
            {
                ConcreteTypeId = order.ConcreteTypeId,
                Quantity = 75m,
                SlabType = order.SlabType,
                TransportMethod = order.TransportMethod,
                FloorNumber = 2,
                PouringDate = DateTime.UtcNow.AddDays(1)
            },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(75m);
    }

    [Fact]
    public async Task UpdateOrder_ApprovedOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);

        // Act
        var act = () => _orderService.UpdateOrderAsync(
            order.OrderId, new UpdateOrderDto
            {
                ConcreteTypeId = order.ConcreteTypeId,
                Quantity = 75m
            },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*يُسمح بالتعديل فقط للطلبات قيد الانتظار.*");
    }

    [Fact]
    public async Task RejectOrder_PendingOrder_Succeeds()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending);

        // Act
        var result = await _orderService.RejectOrderAsync(
            order.OrderId, "سبب الرفض",
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        result.Succeeded.Should().BeTrue();
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.Status.Should().Be(OrderStatus.Rejected);
    }

    [Fact]
    public async Task RejectOrder_ApprovedOrder_Throws()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Approved, unitPrice: 150m);

        // Act — طلب معتمد لا يمكن رفضه
        var act = () => _orderService.RejectOrderAsync(
            order.OrderId, "سبب",
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.OrderCannotBeRejectedInStatus);
    }

    // ─────────────────────────────────────────────
    // ⑩  مسارات لم تكن مغطّاة: إنشاء الطلب وحذفه
    //     (CreateAsync / CreatePhoneOrderAsync / DeleteOrderAsync)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_ClientRole_UsesCallerId_NotDtoClientId()
    {
        // Arrange — عميلان: المستدعي (90) وعميل آخر (91)
        await SeedOrderEnvironmentAsync();
        var otherClient = TestDataSeeder.CreateUser(91, "عميل_آخر", UserRole.Client, phone: "050000091");
        _context.Users.Add(otherClient);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto
        {
            ClientId = otherClient.UserId, // محاولة نسبة الطلب إلى عميل آخر
            FactoryId = 1,
            ConcreteTypeId = 1,
            Quantity = 10
        };

        // Act
        var created = await _orderService.CreateAsync(dto, currentUserId: 90, UserRole.Client);

        // Assert — الطلب يُنسب دائماً إلى المستدعي، وdto.ClientId مُتجاهَل تماماً لدور العميل
        created.ClientId.Should().Be(90);
    }

    [Fact]
    public async Task Create_DriverRole_ThrowsOrderCreationNotAllowed()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(200, "سائق_اختبار", 1);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto
        {
            ClientId = 90, FactoryId = 1, ConcreteTypeId = 1, Quantity = 10
        };

        // Act — السائق ليس من أدوار إنشاء الطلبات
        var act = () => _orderService.CreateAsync(dto, currentUserId: 200, UserRole.Driver);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.OrderCreationNotAllowed);
    }

    [Fact]
    public async Task Create_ConcreteTypeFromAnotherFactory_ThrowsFactoryMismatch()
    {
        // Arrange — نوع خرسانة تابع لمصنع آخر (بسعر قائمة مختلف تماماً)
        await SeedOrderEnvironmentAsync();
        var otherFactory = TestDataSeeder.CreateFactory(2, "مصنع_آخر");
        var foreignType = TestDataSeeder.CreateConcreteType(2, 2, "C40_آخر", 40, 900m);
        _context.Factories.Add(otherFactory);
        _context.ConcreteTypes.Add(foreignType);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto
        {
            ClientId = 90,
            FactoryId = 1,
            ConcreteTypeId = foreignType.ConcreteTypeId, // نوع يتبع مصنعاً آخر
            Quantity = 10
        };

        // Act
        var act = () => _orderService.CreateAsync(dto, currentUserId: 90, UserRole.Client);

        // Assert — لا يُنشأ طلب داخل مصنع بسعر قائمة مصنع آخر
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryMismatch);
    }

    [Fact]
    public async Task CreatePhoneOrder_PhoneBelongsToDriver_ThrowsPhoneLinkedToNonClientAccount()
    {
        // Arrange — الرقم مسجّل لحساب سائق (وليس عميلاً)
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateUser(
            200, "سائق_بمرقم", UserRole.Driver, factoryId: 1, phone: "771110004");
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var dto = new PhoneOrderDto
        {
            ClientPhone = "771110004",
            ClientFullName = "اسم مُقترح",
            FactoryId = 1,
            ConcreteTypeId = 1,
            Quantity = 10
        };

        // Act — لا يُنشأ عميل جديد بمرقم حساب موظف/سائق
        var act = () => _orderService.CreatePhoneOrderAsync(dto, employeeFactoryId: 1);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.PhoneLinkedToNonClientAccount);
    }

    [Fact]
    public async Task Delete_ClientCaller_ThrowsForbidden()
    {
        // Arrange — العميل يملك الطلب لكنه لا يملك حق حذفه/أرشفته
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(OrderStatus.Pending);

        // Act
        var act = () => _orderService.DeleteOrderAsync(
            order.OrderId, callerId: 90, UserRole.Client, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.Unauthorized);
    }

    // ─────────────────────────────────────────────
    // ⑪  إصلاحات منطق الطلب: تحرير السائق عند الحذف، شرط السعر عند الاعتماد،
    //     إعادة التسعير عند تغيير نوع الخرسانة، وإعادة تعيين السائق نفسه
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_OrderWithAssignedDriver_ReleasesDriver()
    {
        // Arrange — طلب معتمد مسند لسائق حالته Busy
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(210, "سائق_محجوز", 1, status: DriverStatus.Busy);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Approved, unitPrice: 150m);
        order.DriverId = driver.UserId;
        await _context.SaveChangesAsync();

        // Act
        await _orderService.DeleteOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — السائق عاد متاحًا؛ لولا ذلك بقي محجوزًا على طلب محذوف لا يمكن الوصول إليه
        var driverAfter = await _context.Users.AsNoTracking().FirstAsync(u => u.UserId == driver.UserId);
        driverAfter.DriverStatus.Should().Be(DriverStatus.Available);
    }

    [Fact]
    public async Task UpdateStatus_ToApproved_WithoutPrice_Throws()
    {
        // Arrange — طلب قيد الانتظار بلا سعر
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(OrderStatus.Pending, unitPrice: 0m);

        // Act — المسار المخصّص للمدير: Pending → Approved مباشرة
        var act = () => _orderService.UpdateStatusAsync(
            order.OrderId, new UpdateOrderStatusDto { Status = OrderStatus.Approved },
            callerId: 10, UserRole.Admin, callerFactoryId: null);

        // Assert — نفس قاعدة ApproveOrderAsync، فلا يُلتفّ على التسعير الإلزامي من هذا المسار
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.PriceRequiredBeforeApproval);
    }

    [Fact]
    public async Task UpdateStatus_ToApproved_WithPrice_Succeeds()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(OrderStatus.Pending, unitPrice: 150m);

        // Act
        await _orderService.UpdateStatusAsync(
            order.OrderId, new UpdateOrderStatusDto { Status = OrderStatus.Approved },
            callerId: 10, UserRole.Admin, callerFactoryId: null);

        // Assert
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.Status.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public async Task UpdateOrder_ChangingConcreteType_RepricesAndReturnsNewTypeName()
    {
        // Arrange — نوعان في المصنع نفسه بسعرين مختلفين، والطلب مسعَّر بسعر النوع الأول
        await SeedOrderEnvironmentAsync();
        var newType = TestDataSeeder.CreateConcreteType(2, 1, "C40", 40, 400m);
        _context.ConcreteTypes.Add(newType);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Pending, unitPrice: 150m, quantity: 10);

        // Act
        var result = await _orderService.UpdateOrderAsync(
            order.OrderId, new UpdateOrderDto
            {
                ConcreteTypeId = newType.ConcreteTypeId,
                Quantity = 10m,
                SlabType = order.SlabType,
                TransportMethod = order.TransportMethod
            },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — السعر يتبع النوع الجديد، والاستجابة تُعيد اسم النوع الجديد لا القديم
        result.ConcreteTypeName.Should().Be("C40");
        result.TotalPrice.Should().Be(4000m);

        var stored = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        stored!.ConcreteTypeId.Should().Be(newType.ConcreteTypeId);
        stored.UnitPrice.Should().Be(400m);
        stored.TotalPrice.Should().Be(4000m);
    }

    [Fact]
    public async Task UpdateOrder_ChangingConcreteType_KeepsManuallySetPrice()
    {
        // Arrange — سعر مُتفاوض عليه يدويًا (200) يخالف سعر النوع الحالي في القائمة (150)
        await SeedOrderEnvironmentAsync();
        var newType = TestDataSeeder.CreateConcreteType(2, 1, "C40", 40, 400m);
        _context.ConcreteTypes.Add(newType);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Pending, unitPrice: 200m, quantity: 10);

        // Act
        var result = await _orderService.UpdateOrderAsync(
            order.OrderId, new UpdateOrderDto
            {
                ConcreteTypeId = newType.ConcreteTypeId,
                Quantity = 10m,
                SlabType = order.SlabType,
                TransportMethod = order.TransportMethod
            },
            callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — النوع تغيّر لكن السعر المتفاوض عليه بقي كما هو
        result.ConcreteTypeName.Should().Be("C40");
        result.TotalPrice.Should().Be(2000m);
    }

    [Fact]
    public async Task AssignDriver_SameDriverAgain_UpdatesTruckPlate()
    {
        // Arrange — طلب معتمد مسند لسائق، فصار Busy بسبب هذا الطلب نفسه
        await SeedOrderEnvironmentAsync();
        var driver = TestDataSeeder.CreateDriver(211, "سائق_معاد", 1, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        var order = CreateAndSeedOrder(OrderStatus.Approved, unitPrice: 150m);

        await _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = driver.UserId,
            TruckPlate = "أ ب ج 111"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Act — إعادة تعيين السائق نفسه برقم شاحنة جديد
        await _orderService.AssignDriverAsync(order.OrderId, new AssignDriverDto
        {
            DriverId = driver.UserId,
            TruckPlate = "د هـ و 222"
        }, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — لم تفشل بـ DriverNotAvailable، ورقم الشاحنة تحدّث والسائق ما زال مشغولًا
        var updated = await _unitOfWork.Orders.GetByIdWithDetailsAsync(order.OrderId);
        updated!.TruckPlate.Should().Be("د هـ و 222");
        updated.DriverId.Should().Be(driver.UserId);

        var driverAfter = await _context.Users.AsNoTracking().FirstAsync(u => u.UserId == driver.UserId);
        driverAfter.DriverStatus.Should().Be(DriverStatus.Busy);
    }

    // ─────────────────────────────────────────────
    // ⑫  كلمة المرور المؤقتة لعميل الطلبات الهاتفية
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreatePhoneOrder_NewClient_ReturnsReadableTemporaryPassword()
    {
        // Arrange — رقم غير مسجَّل مسبقاً
        await SeedOrderEnvironmentAsync();

        var dto = new PhoneOrderDto
        {
            ClientPhone = "771110005",
            ClientFullName = "عميل_هاتفي",
            FactoryId = 1,
            ConcreteTypeId = 1,
            Quantity = 10
        };

        // Act
        var result = await _orderService.CreatePhoneOrderAsync(dto, employeeFactoryId: 1);

        // Assert — الحساب أُنشئ بكلمة مرور مقروءة (10 محارف بلا 0/O/1/l/I) تُعاد مرة واحدة
        result.NewClientTemporaryPassword.Should().NotBeNullOrWhiteSpace();
        result.NewClientTemporaryPassword.Should().MatchRegex("^[A-HJ-NP-Za-hi-kmnp-z2-9]{10}$");
        result.NewClientPhone.Should().Be("967771110005");

        // Assert — المخزَّن تجزئة فقط، والكلمة المعروضة تطابقها فعلاً فيستطيع العميل الدخول
        var stored = await _context.Users.AsNoTracking()
            .FirstAsync(u => u.Phone == "967771110005");
        stored.PasswordHash.Should().NotBe(result.NewClientTemporaryPassword);
        BCrypt.Net.BCrypt.Verify(result.NewClientTemporaryPassword, stored.PasswordHash).Should().BeTrue();

        // Assert — الطلب نفسه أُنشئ منسوباً للعميل الجديد
        result.Order.ClientId.Should().Be(stored.UserId);
    }

    [Fact]
    public async Task CreatePhoneOrder_ExistingClient_ReturnsNoTemporaryPassword()
    {
        // Arrange — الرقم مسجَّل لعميل قائم (الهاتف يُخزَّن مُطبَّعاً: 967771234567)
        await SeedOrderEnvironmentAsync();
        var existing = TestDataSeeder.CreateUser(92, "عميل_قائم", UserRole.Client, phone: "771234567");
        _context.Users.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new PhoneOrderDto
        {
            ClientPhone = "0771234567",
            ClientFullName = "اسم_لا_يُستخدم",
            FactoryId = 1,
            ConcreteTypeId = 1,
            Quantity = 10
        };

        // Act
        var result = await _orderService.CreatePhoneOrderAsync(dto, employeeFactoryId: 1);

        // Assert — لا كلمة مرور جديدة ولا تغيير لحساب قائم
        result.NewClientTemporaryPassword.Should().BeNull();
        result.NewClientPhone.Should().BeNull();
        result.Order.ClientId.Should().Be(existing.UserId);
    }

    // ─────────────────────────────────────────────
    // ⑧  ملخص عميل واحد (GetCustomerSummaryAsync)
    //
    // سبب الوجود: صفحة تفاصيل العميل كانت تجلب أول 100 عميل من قائمة مُصفّحة
    // ثم تبحث فيهم محلياً، فتُبلّغ «غير موجود» عن كل عميل يقع بعد الصفحة الأولى.
    // الآن هناك قراءة مباشرة لعميل واحد بنفس استعلام القائمة (ونفس عزل المصنع).
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerSummary_ReturnsAccountFieldsAndAggregates()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع_أ"));
        var client = TestDataSeeder.CreateUser(
            400, "عميل_ملخص", UserRole.Client,
            email: "summary@test.local", phone: "777400400");
        client.WhatsApp = YemeniPhoneHelper.Normalize("777400401");
        _context.Users.Add(client);
        _context.Orders.AddRange(
            TestDataSeeder.CreateOrder(1, 400, 1, 1, quantity: 10, unitPrice: 100m),
            TestDataSeeder.CreateOrder(2, 400, 1, 1, quantity: 5, unitPrice: 100m));
        await _context.SaveChangesAsync();

        // Act
        var summary = await _orderService.GetCustomerSummaryAsync(400, factoryId: null);

        // Assert — بيانات الحساب (كانت كلها غائبة عن الاستعلام: بريد/واتساب/حالة)
        summary.Should().NotBeNull();
        summary!.UserId.Should().Be(400);
        summary.FullName.Should().Be("عميل_ملخص");
        summary.Email.Should().Be("summary@test.local",
            "صفحة التفاصيل كانت تعرض «-» للبريد دائماً لأن الحقل لم يكن في المشروع");
        summary.WhatsApp.Should().Be(YemeniPhoneHelper.Normalize("777400401"));
        summary.IsActive.Should().BeTrue(
            "كانت تُقرأ false دائماً فتظهر حالة الحساب «موقوف» لعميل نشط");

        // Assert — التجميعات
        summary.OrdersCount.Should().Be(2);
        summary.TotalQuantity.Should().Be(15);
        summary.LastOrderDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomerSummary_HonoursFactoryScope()
    {
        // Arrange — للعميل طلب في المصنع ١ فقط
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(1, "مصنع_أ"),
            TestDataSeeder.CreateFactory(2, "مصنع_ب"));
        _context.Users.Add(TestDataSeeder.CreateUser(401, "عميل_ب", UserRole.Client));
        _context.Orders.Add(TestDataSeeder.CreateOrder(1, 401, 1, 1));
        await _context.SaveChangesAsync();

        // Act
        var insideOwnFactory = await _orderService.GetCustomerSummaryAsync(401, factoryId: 1);
        var insideOtherFactory = await _orderService.GetCustomerSummaryAsync(401, factoryId: 2);

        // Assert — نفس العزل الذي تفرضه قائمة العملاء (لا تسريب عميل مصنع آخر)
        insideOwnFactory.Should().NotBeNull();
        insideOtherFactory.Should().BeNull(
            "لا طلبات لهذا العميل في المصنع ٢، فلا يجوز أن يظهر لموظفه");
    }

    [Fact]
    public async Task GetCustomerSummary_CustomerWithoutOrders_ReturnsNull()
    {
        // Arrange — مستخدم بدور عميل بلا أي طلب (الحالة التي تُترجم إلى 404)
        _context.Users.Add(TestDataSeeder.CreateUser(402, "عميل_بلا_طلبات", UserRole.Client));
        await _context.SaveChangesAsync();

        // Act
        var summary = await _orderService.GetCustomerSummaryAsync(402, factoryId: null);

        // Assert
        summary.Should().BeNull("الملخص مبني على الطلبات، فلا وجود لعميل بلا طلبات");
    }

    // ─────────────────────────────────────────────
    // ⑨  معالجة تعارض التواريع (Concurrency)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ApproveOrder_ConcurrencyConflict_ThrowsConflictException()
    {
        // Arrange
        await SeedOrderEnvironmentAsync();
        var order = CreateAndSeedOrder(status: OrderStatus.Pending, unitPrice: 150m);

        // استخدام UnitOfWork الذي يرمي DbUpdateConcurrencyException لمحاكاة التعارض
        var concurrencyUoW = new ConcurrencyTestingUnitOfWork(_context);
        var passwordHasher = new PasswordHasher();
        var orderHelper = new OrderHelperService(concurrencyUoW);
        var orderQueryService = new OrderQueryService(concurrencyUoW, orderHelper);
        var orderCommandService = new OrderCommandService(concurrencyUoW, passwordHasher);
        var orderWorkflowService = new OrderWorkflowService(concurrencyUoW);
        var orderService = new OrderService(
            passwordHasher, concurrencyUoW, orderQueryService, orderCommandService, orderWorkflowService);

        // Act — محاكاة تعارض RowVersion عند حفظ الطلب
        var act = () => orderService.ApproveOrderAsync(
            order.OrderId, callerId: 20, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert — يُحوّل DbUpdateConcurrencyException إلى ConflictException (HTTP 409)
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.ConcurrencyConflict);
    }

    /// <summary>
    /// UnitOfWork اختباري يرمي DbUpdateConcurrencyException عند الحفظ الفعلي
    /// — يُستخدم لاختبار معالجة تعارض التواريع في UnitOfWork وتحويلها إلى ConflictException.
    /// </summary>
    private class ConcurrencyTestingUnitOfWork : UnitOfWork
    {
        public ConcurrencyTestingUnitOfWork(KharasanaDbContext context) : base(context)
        {
        }

        protected override Task<int> SaveChangesInternalAsync()
        {
            throw new DbUpdateConcurrencyException("RowVersion conflict simulated in test.");
        }
    }
}