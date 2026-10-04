using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.IntegrationTests.TestData;

/// <summary>
/// بيانات اختبارية لمشروع التكامل — نسخة مستقلّة عن باذر مشروع الوحدة
/// (<c>Kharasana.Tests.TestData.TestDataSeeder</c>) لأنه <c>internal</c> هناك،
/// ولا يصحّ ربط مشروع اختبار بمشروع اختبار آخر.
///
/// <para>القيم مطابقة لِما ينجح في اختبارات الوحدة (نفس الرسم: مصنع 1، عميل 100،
/// نوع خرسانة 1، سائق 200، طلب 1) فلا نفترق في المعنى بين المشروعين. وكل هاتف
/// مختلف عن الآخر لأن فهرس <c>Users.Phone</c> فريد مُرشَّح.</para>
/// </summary>
internal static class IntegrationSeed
{
    public static Factory Factory(string name, bool isDeleted = false)
        => new()
        {
            // FactoryId يُترك فارغاً ليُنشئه SQL Server تلقائياً
            FactoryName = name,
            OwnerName = "المالك " + name,
            Phone = $"050{Guid.NewGuid().ToString("N").Substring(0, 6)}", // رقم عشوائي
            Area = "صنعاء",
            Address = $"شارع {name}",
            IsActive = true,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

    public static User Client(string fullName, bool isDeleted = false)
        => new()
        {
            // UserId يُترك فارغاً ليُنشئه SQL Server تلقائياً
            FullName = fullName,
            Role = UserRole.Client,
            Phone = $"077{Guid.NewGuid().ToString("N").Substring(0, 7)}", // رقم عشوائي
            Email = $"client{Guid.NewGuid().ToString("N").Substring(0, 5)}@integration.test",
            PasswordHash = "integration-test-hash",
            IsActive = true,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

    public static User Driver(string fullName, int factoryId, bool isDeleted = false)
        => new()
        {
            // UserId يُترك فارغاً ليُنشئه SQL Server تلقائياً
            FullName = fullName,
            Role = UserRole.Driver,
            FactoryId = factoryId,
            Phone = $"077{Guid.NewGuid().ToString("N").Substring(0, 7)}",
            PasswordHash = "integration-test-hash",
            IsActive = true,
            IsDeleted = isDeleted,
            DriverStatus = DriverStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

    public static ConcreteType ConcreteType(
        int factoryId, string name, bool isDeleted = false)
        => new()
        {
            // ConcreteTypeId يُترك فارغاً
            FactoryId = factoryId,
            Name = name,
            Strength = 25,
            UnitPrice = 150m,
            IsActive = !isDeleted,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

    public static Order Order(
        int clientId, int factoryId, int concreteTypeId, int? driverId = null)
        => new()
        {
            // OrderId يُترك فارغاً
            OrderNumber = $"ORD-IT-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
            ClientId = clientId,
            FactoryId = factoryId,
            ConcreteTypeId = concreteTypeId,
            Quantity = 20,
            UnitPrice = 150m,
            TotalPrice = 3000m,
            Status = OrderStatus.Pending,
            DriverId = driverId,
            SlabType = SlabType.Roof,
            TransportMethod = TransportMethod.FactoryTransport,
            CreatedAt = DateTime.UtcNow
        };
}
