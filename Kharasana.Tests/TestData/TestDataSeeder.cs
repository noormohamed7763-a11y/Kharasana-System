using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.TestData;

/// <summary>
/// مساعد لإنشاء بيانات اختبارية في قاعدة بيانات InMemory.
/// كل اسم حقل فريد (معرّف بـ suffix) لتجنّب تعارض الفهارس الفريدة.
/// </summary>
internal static class TestDataSeeder
{
    public static KharasanaDbContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<KharasanaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new KharasanaDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Factory CreateFactory(int id, string name, bool isActive = true, bool isDeleted = false)
        => new()
        {
            FactoryId = id,
            FactoryName = name,
            OwnerName = "المالك " + name,
            Phone = $"050{id:D6}",
            Area = "صنعاء",
            Address = $"شارع {name}",
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

    public static User CreateUser(
        int userId, string fullName, UserRole role,
        int? factoryId = null, bool isActive = true,
        string? phone = null, string? email = null)
        => new()
        {
            UserId = userId,
            FullName = fullName,
            Role = role,
            FactoryId = factoryId,
            IsActive = isActive,
            // نخزّن الرقم مُطبيعًا (كما يخزّنه RegisterAsync) حتى يتطابق مع عمليات البحث في AuthService
            Phone = YemeniPhoneHelper.Normalize(phone ?? $"77{userId:D7}"),
            Email = email ?? $"user{userId}@test.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            CreatedAt = DateTime.UtcNow
        };

    public static User CreateDriver(int userId, string fullName, int factoryId,
        bool isActive = true, DriverStatus status = DriverStatus.Available)
        => new()
        {
            UserId = userId,
            FullName = fullName,
            Role = UserRole.Driver,
            FactoryId = factoryId,
            IsActive = isActive,
            Phone = YemeniPhoneHelper.Normalize($"77{userId:D7}"),
            DriverStatus = status,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            CreatedAt = DateTime.UtcNow
        };

    public static ConcreteType CreateConcreteType(int id, int factoryId, string name,
        int strength = 25, decimal price = 150m, bool isActive = true)
        => new()
        {
            ConcreteTypeId = id,
            FactoryId = factoryId,
            Name = name,
            Strength = strength,
            UnitPrice = price,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static Order CreateOrder(int orderId, int clientId, int factoryId,
        int concreteTypeId, int quantity = 50,
        OrderStatus status = OrderStatus.Pending,
        int? driverId = null, decimal unitPrice = 150m)
        => new()
        {
            OrderId = orderId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{orderId:D8}",
            ClientId = clientId,
            FactoryId = factoryId,
            ConcreteTypeId = concreteTypeId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            Status = status,
            DriverId = driverId,
            SlabType = SlabType.Roof,
            TransportMethod = TransportMethod.FactoryTransport,
            CreatedAt = DateTime.UtcNow
        };
}