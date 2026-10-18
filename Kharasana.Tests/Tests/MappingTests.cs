using FluentAssertions;
using Kharasana.Application.Mapping;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Xunit;

namespace Kharasana.Tests.Tests;

public class MappingTests
{
    [Fact]
    public void UserMapper_MapsUserToUserDto_Correctly()
    {
        var user = new User
        {
            UserId = 10,
            FullName = "أحمد محمد",
            Email = "ahmed@example.com",
            Phone = "+967771234567",
            WhatsApp = "+967771234567",
            Role = UserRole.FactoryEmployee,
            FactoryId = 5,
            Factory = new Factory { FactoryId = 5, FactoryName = "مصنع الأمانة" },
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var dto = UserMapper.ToDto(user);

        dto.UserId.Should().Be(10);
        dto.FullName.Should().Be("أحمد محمد");
        dto.Email.Should().Be("ahmed@example.com");
        dto.Role.Should().Be("FactoryEmployee");
        dto.FactoryId.Should().Be(5);
        dto.FactoryName.Should().Be("مصنع الأمانة");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UserMapper_WhenFactoryIsNull_FactoryNameIsNull()
    {
        var user = new User
        {
            UserId = 20,
            FullName = "علي خالد",
            Role = UserRole.Client,
            Factory = null
        };

        var dto = UserMapper.ToDto(user);

        dto.FactoryName.Should().BeNull();
        dto.Role.Should().Be("Client");
    }

    [Fact]
    public void ConcreteTypeMapper_MapsProperties_AndHandlesNullFactoryGracefully()
    {
        var concreteTypeWithoutFactory = new ConcreteType
        {
            ConcreteTypeId = 1,
            FactoryId = 2,
            Name = "C30",
            Strength = 30,
            UnitPrice = 200m,
            IsActive = true,
            Factory = null!
        };

        var dto = ConcreteTypeMapper.ToDto(concreteTypeWithoutFactory);

        dto.ConcreteTypeId.Should().Be(1);
        dto.FactoryId.Should().Be(2);
        dto.Name.Should().Be("C30");
        dto.Strength.Should().Be(30);
        dto.UnitPrice.Should().Be(200m);
        dto.FactoryName.Should().Be(string.Empty);
    }

    [Fact]
    public void ConcreteTypeMapper_WhenFactoryIsLoaded_MapsFactoryName()
    {
        var concreteTypeWithFactory = new ConcreteType
        {
            ConcreteTypeId = 2,
            FactoryId = 3,
            Name = "C35",
            Factory = new Factory { FactoryId = 3, FactoryName = "مصنع النور" }
        };

        var dto = ConcreteTypeMapper.ToDto(concreteTypeWithFactory);

        dto.FactoryName.Should().Be("مصنع النور");
    }

    [Fact]
    public void FactoryMapper_MapsFactoryToFactoryDto_Correctly()
    {
        var factory = new Factory
        {
            FactoryId = 7,
            FactoryName = "مصنع الوحدة",
            OwnerName = "سالم",
            Phone = "+967770000000",
            Area = "صنعاء",
            Address = "شارع الستين",
            IsActive = true
        };

        var dto = FactoryMapper.ToDto(factory);

        dto.FactoryId.Should().Be(7);
        dto.FactoryName.Should().Be("مصنع الوحدة");
        dto.OwnerName.Should().Be("سالم");
        dto.Area.Should().Be("صنعاء");
        dto.Address.Should().Be("شارع الستين");
        dto.IsActive.Should().BeTrue();
    }
}
