using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Services;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حارس لقطتَي نوع الخرسانة على الطلب — الاسم والمقاومة كما كانا <b>وقت الإنشاء</b>.
///
/// <para><b>العلّة التي يمنعها:</b> الطلب كان يعرض اسم نوعه ومقاومته بالقراءة الحيّة من
/// الكتالوج (<c>order.ConcreteType.Name</c>). فتعديل المصنع لاسم النوع أو مقاومته
/// (<c>ConcreteTypeService.UpdateAsync</c> يسمح بذلك) كان <b>يُعيد كتابة عرض كل طلب
/// قديم</b>: طلب سُعِّر وبِيع باسم «C25» يظهر لاحقًا باسم النوع الجديد. وهي لقطة تاريخية
/// تجارية على نمط <c>Order.UnitPrice</c> القائم أصلًا.</para>
///
/// <para><b>ولماذا اختبار InMemory يكفي هنا:</b> اللقطة منطق خدمة لا قيد قاعدة بيانات —
/// لا فهرس ولا FK ولا RowVersion. وهذه بالضبط الحدود التي يوثّقها
/// <c>SQL_SERVER_INTEGRATION_TESTING.md</c>.</para>
/// </summary>
public class OrderConcreteTypeSnapshotTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly OrderService _orderService;

    public OrderConcreteTypeSnapshotTests()
    {
        _context = TestDataSeeder.CreateContext();
        _unitOfWork = new UnitOfWork(_context);
        var passwordHasher = new PasswordHasher();
        var orderHelper = new OrderHelperService(_unitOfWork);
        var orderQueryService = new OrderQueryService(_unitOfWork, orderHelper);
        var orderCommandService = new OrderCommandService(_unitOfWork, passwordHasher);
        var orderWorkflowService = new OrderWorkflowService(_unitOfWork);
        _orderService = new OrderService(passwordHasher, _unitOfWork, orderQueryService, orderCommandService, orderWorkflowService);
    }

    public void Dispose() => _context.Dispose();

    /// <summary>مصنع 1، عميل 90، ونوعان: «C25/25» و«C35/35».</summary>
    private async Task SeedAsync()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع_لقطة"));
        _context.Users.Add(TestDataSeeder.CreateUser(90, "عميل_لقطة", UserRole.Client, phone: "050000090"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(1, 1, "C25", strength: 25, price: 150m));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(2, 1, "C35", strength: 35, price: 200m));

        await _context.SaveChangesAsync();
    }

    private static CreateOrderDto NewOrderDto(int concreteTypeId = 1) => new()
    {
        ClientId = 90,
        FactoryId = 1,
        ConcreteTypeId = concreteTypeId,
        Quantity = 10,
        SlabType = SlabType.Roof,
        TransportMethod = TransportMethod.FactoryTransport
    };

    [Fact]
    public async Task Create_CapturesTheNameAndStrengthOfTheChosenType()
    {
        await SeedAsync();

        var created = await _orderService.CreateAsync(NewOrderDto(), currentUserId: 90, UserRole.Client);

        created.ConcreteTypeName.Should().Be("C25");
        created.ConcreteStrength.Should().Be(25);

        var stored = await _context.Orders.AsNoTracking().SingleAsync();
        stored.ConcreteTypeNameSnapshot.Should().Be("C25");
        stored.ConcreteTypeStrengthSnapshot.Should().Be(25);
    }

    /// <summary>جوهر اللقطة: إعادة تسمية النوع لا تمسّ طلبًا بِيع قبلها.</summary>
    [Fact]
    public async Task RenamingTheType_DoesNotChangeAnExistingOrder()
    {
        await SeedAsync();
        var created = await _orderService.CreateAsync(NewOrderDto(), currentUserId: 90, UserRole.Client);

        // المصنع يُعدّل الكتالوج — هنا كان الطلب القديم يتبدّل اسمه ومقاومته.
        var concreteType = await _context.ConcreteTypes.FirstAsync(c => c.ConcreteTypeId == 1);
        concreteType.Name = "C25-معدّل";
        concreteType.Strength = 40;
        await _context.SaveChangesAsync();

        var reread = await _orderService.GetByIdAsync(
            created.OrderId, callerId: 90, callerRole: UserRole.Client, callerFactoryId: null);

        reread.ConcreteTypeName.Should().Be("C25",
            "الطلب يبقى معبِّرًا عن النوع الذي بِيع به فعلًا لا عن اسمه الحالي في الكتالوج");
        reread.ConcreteStrength.Should().Be(25);
    }

    /// <summary>
    /// صف سابق للترحيل (لقطتاه فارغتان) — كحال صفٍّ لم تجد له التعبئة الخلفية نوعًا.
    /// يسقط العرض إلى النوع الحالي بدل أن يُظهر فراغًا.
    /// </summary>
    [Fact]
    public async Task AnOrderWithoutSnapshots_FallsBackToTheCurrentType()
    {
        await SeedAsync();

        _context.Orders.Add(TestDataSeeder.CreateOrder(7, clientId: 90, factoryId: 1, concreteTypeId: 1));
        await _context.SaveChangesAsync();

        var dto = await _orderService.GetByIdAsync(
            7, callerId: 90, callerRole: UserRole.Client, callerFactoryId: null);

        dto.ConcreteTypeName.Should().Be("C25");
        dto.ConcreteStrength.Should().Be(25);
    }

    /// <summary>تغيير نوع الطلب يحدّث اللقطتين معًا، وإلا بقي الطلب معلقًا على نوعه القديم.</summary>
    [Fact]
    public async Task ChangingTheOrderType_RefreshesTheSnapshots()
    {
        await SeedAsync();
        var created = await _orderService.CreateAsync(NewOrderDto(concreteTypeId: 1), 90, UserRole.Client);

        var updated = await _orderService.UpdateOrderAsync(
            created.OrderId,
            new UpdateOrderDto
            {
                ConcreteTypeId = 2,
                Quantity = 10,
                SlabType = SlabType.Roof,
                TransportMethod = TransportMethod.FactoryTransport
            },
            callerId: 90, callerRole: UserRole.Client, callerFactoryId: null);

        updated.ConcreteTypeName.Should().Be("C35");
        updated.TotalPrice.Should().Be(2000m, "10 م³ × 200 للسعر الجديد");

        var stored = await _context.Orders.AsNoTracking().SingleAsync();
        stored.ConcreteTypeNameSnapshot.Should().Be("C35");
        stored.ConcreteTypeStrengthSnapshot.Should().Be(35);
    }
}
