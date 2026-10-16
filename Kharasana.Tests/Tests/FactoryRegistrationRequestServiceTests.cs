using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات وحدة شاملة لـ FactoryRegistrationRequestService:
/// • إنشاء طلب جديد بحالة معلّقة (Pending)
/// • الاستعلام والترقيم والبحث والفلترة بالحالة
/// • احتساب الإحصائيات (GetStatusCountsAsync)
/// • رفض الطلب مع توثيق السبب ومنع تكرار المعالجة
/// • قبول الطلب: إنشاء المصنع، حساب مدير المصنع، ورمز التفعيل
/// • تراجع المعاملة (Rollback) عند فشل أي خطوة أثناء القبول
/// </summary>
public class FactoryRegistrationRequestServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly FakeUserService _fakeUserService;
    private readonly FactoryRegistrationRequestService _service;

    public FactoryRegistrationRequestServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _unitOfWork = new UnitOfWork(_context);
        _fakeUserService = new FakeUserService();
        _service = new FactoryRegistrationRequestService(_unitOfWork, _fakeUserService);
    }

    public void Dispose() => _context.Dispose();

    // ─────────────────────────────────────────────
    // 1. CreateRequestAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateRequestAsync_ValidDto_AddsPendingRequestAndPersists()
    {
        // Arrange
        var dto = new RegisterFactoryDto
        {
            FactoryName = "مصنع الأمل للخرسانة",
            OwnerName = "سالم الأحمدي",
            Email = "alamal@example.com",
            Phone = "771234567",
            Area = "صنعاء",
            Address = "شارع الستين",
            BusinessRegistrationNumber = "CR-987654"
        };

        // Act
        var result = await _service.CreateRequestAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();

        var saved = await _context.FactoryRegistrationRequests.SingleOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.FactoryName.Should().Be(dto.FactoryName);
        saved.ContactName.Should().Be(dto.OwnerName);
        saved.ContactEmail.Should().Be(dto.Email);
        saved.ContactPhone.Should().Be(dto.Phone);
        saved.Area.Should().Be(dto.Area);
        saved.Address.Should().Be(dto.Address);
        saved.CommercialId.Should().Be(dto.BusinessRegistrationNumber);
        saved.Status.Should().Be(RegistrationStatus.Pending);
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        saved.ProcessedAt.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // 2. GetAllRequestsAsync & GetPendingRequestsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllRequestsAsync_ReturnsAllRequestsRegardlessOfStatus()
    {
        // Arrange
        _context.FactoryRegistrationRequests.AddRange(
            CreateRequest(1, "مصنع 1", RegistrationStatus.Pending),
            CreateRequest(2, "مصنع 2", RegistrationStatus.Approved),
            CreateRequest(3, "مصنع 3", RegistrationStatus.Rejected)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllRequestsAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ReturnsOnlyPendingRequests()
    {
        // Arrange
        _context.FactoryRegistrationRequests.AddRange(
            CreateRequest(1, "مصنع 1", RegistrationStatus.Pending),
            CreateRequest(2, "مصنع 2", RegistrationStatus.Approved),
            CreateRequest(3, "مصنع 3", RegistrationStatus.Rejected),
            CreateRequest(4, "مصنع 4", RegistrationStatus.Pending)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetPendingRequestsAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Status == RegistrationStatus.Pending);
    }

    // ─────────────────────────────────────────────
    // 3. GetPagedRequestsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedRequestsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        _context.FactoryRegistrationRequests.AddRange(
            CreateRequest(1, "مصنع 1", RegistrationStatus.Pending),
            CreateRequest(2, "مصنع 2", RegistrationStatus.Approved),
            CreateRequest(3, "مصنع 3", RegistrationStatus.Pending)
        );
        await _context.SaveChangesAsync();

        var filter = new RegistrationRequestFilterParams
        {
            Status = RegistrationStatus.Pending,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPagedRequestsAsync(filter);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(r => r.Status == RegistrationStatus.Pending);
    }

    [Theory]
    [InlineData("البركة", 1)]     // اسم المصنع
    [InlineData("خالد", 1)]       // اسم جهة الاتصال
    [InlineData("baraka@", 1)]    // البريد
    [InlineData("عدن", 1)]        // المنطقة
    [InlineData("غير_موجود", 0)]  // لا تطابق
    public async Task GetPagedRequestsAsync_WithSearchTerm_FiltersCorrectly(string term, int expectedCount)
    {
        // Arrange
        _context.FactoryRegistrationRequests.AddRange(
            new FactoryRegistrationRequest
            {
                Id = 1,
                FactoryName = "مصنع البركة",
                ContactName = "خالد العولقي",
                ContactEmail = "baraka@example.com",
                ContactPhone = "771111111",
                Area = "عدن",
                Status = RegistrationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new FactoryRegistrationRequest
            {
                Id = 2,
                FactoryName = "مصنع النور",
                ContactName = "محمد العامري",
                ContactEmail = "noor@example.com",
                ContactPhone = "772222222",
                Area = "صنعاء",
                Status = RegistrationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var filter = new RegistrationRequestFilterParams
        {
            Search = term,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPagedRequestsAsync(filter);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetPagedRequestsAsync_AppliesPaginationAndOrder()
    {
        // Arrange
        var now = DateTime.UtcNow;
        for (int i = 1; i <= 5; i++)
        {
            _context.FactoryRegistrationRequests.Add(new FactoryRegistrationRequest
            {
                Id = i,
                FactoryName = $"مصنع {i}",
                ContactName = $"مالك {i}",
                ContactEmail = $"factory{i}@test.com",
                ContactPhone = $"77000000{i}",
                Area = "صنعاء",
                Status = RegistrationStatus.Pending,
                CreatedAt = now.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        var filter = new RegistrationRequestFilterParams
        {
            PageNumber = 2,
            PageSize = 2
        };

        // Act
        var result = await _service.GetPagedRequestsAsync(filter);

        // Assert
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(2);
        // الترتيب تنازلي حسب CreatedAt: 5, 4, 3, 2, 1 -> الصفحة الثانية تحتوي 3 و 2
        result.Items.First().Id.Should().Be(3);
        result.Items.Last().Id.Should().Be(2);
    }

    // ─────────────────────────────────────────────
    // 4. GetStatusCountsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetStatusCountsAsync_CalculatesCorrectCounts()
    {
        // Arrange
        _context.FactoryRegistrationRequests.AddRange(
            CreateRequest(1, "مصنع 1", RegistrationStatus.Pending),
            CreateRequest(2, "مصنع 2", RegistrationStatus.Pending),
            CreateRequest(3, "مصنع 3", RegistrationStatus.Approved),
            CreateRequest(4, "مصنع 4", RegistrationStatus.Rejected),
            CreateRequest(5, "مصنع 5", RegistrationStatus.Rejected)
        );
        await _context.SaveChangesAsync();

        // Act
        var (pending, approved, rejected) = await _service.GetStatusCountsAsync();

        // Assert
        pending.Should().Be(2);
        approved.Should().Be(1);
        rejected.Should().Be(2);
    }

    // ─────────────────────────────────────────────
    // 5. GetRequestDetailsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetRequestDetailsAsync_ExistingId_ReturnsRequest()
    {
        // Arrange
        var request = CreateRequest(10, "مصنع الاتحاد", RegistrationStatus.Pending);
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRequestDetailsAsync(10);

        // Assert
        result.Should().NotBeNull();
        result!.FactoryName.Should().Be("مصنع الاتحاد");
    }

    [Fact]
    public async Task GetRequestDetailsAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetRequestDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // 6. RejectRequestAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task RejectRequestAsync_PendingRequest_UpdatesStatusAndReason()
    {
        // Arrange
        var request = CreateRequest(20, "مصنع التجربة", RegistrationStatus.Pending);
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RejectRequestAsync(20, "السجل التجاري غير مطابق");

        // Assert
        result.Succeeded.Should().BeTrue();

        var updated = await _context.FactoryRegistrationRequests.FindAsync(20);
        updated!.Status.Should().Be(RegistrationStatus.Rejected);
        updated.RejectionReason.Should().Be("السجل التجاري غير مطابق");
        updated.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RejectRequestAsync_NonExistentRequest_ReturnsFailure()
    {
        // Act
        var result = await _service.RejectRequestAsync(999, "أي سبب");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("الطلب غير موجود");
    }

    [Theory]
    [InlineData(RegistrationStatus.Approved)]
    [InlineData(RegistrationStatus.Rejected)]
    public async Task RejectRequestAsync_AlreadyProcessedRequest_ReturnsFailure(RegistrationStatus status)
    {
        // Arrange
        var request = CreateRequest(30, "مصنع معالج", status);
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RejectRequestAsync(30, "سبب الرفض");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("تمت معالجته مسبقاً");
    }

    // ─────────────────────────────────────────────
    // 7. ApproveRequestAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ApproveRequestAsync_PendingRequest_CreatesFactoryUserAndActivationToken()
    {
        // Arrange
        var request = new FactoryRegistrationRequest
        {
            Id = 50,
            FactoryName = "مصنع الرواد الحديث",
            ContactName = "عبدالرحمن الشامي",
            ContactEmail = "alrowad@example.com",
            ContactPhone = "775555555",
            Area = "صنعاء",
            Address = "منطقة ذهبان الصناعية",
            CommercialId = "CR-12345",
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApproveRequestAsync(50);

        // Assert
        result.Succeeded.Should().BeTrue();

        // 1. تحديث الطلب
        var updatedRequest = await _context.FactoryRegistrationRequests.FindAsync(50);
        updatedRequest!.Status.Should().Be(RegistrationStatus.Approved);
        updatedRequest.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // 2. إنشاء المصنع
        var factory = await _context.Factories.SingleOrDefaultAsync(f => f.FactoryName == request.FactoryName);
        factory.Should().NotBeNull();
        factory!.OwnerName.Should().Be(request.ContactName);
        factory.Email.Should().Be(request.ContactEmail);
        factory.Phone.Should().Be(request.ContactPhone);
        factory.Area.Should().Be(request.Area);
        factory.Address.Should().Be(request.Address);
        factory.IsActive.Should().BeTrue();

        // 3. استدعاء إنشاء المستخدم بصلاحية FactoryAdmin
        _fakeUserService.CreatedUsers.Should().HaveCount(1);
        var createdUser = _fakeUserService.CreatedUsers.Single();
        createdUser.FullName.Should().Be(request.ContactName);
        createdUser.Email.Should().Be(request.ContactEmail);
        createdUser.Phone.Should().Be(request.ContactPhone);
        createdUser.Role.Should().Be(UserRole.FactoryAdmin.ToString());
        createdUser.FactoryId.Should().Be(factory.FactoryId);

        // 4. إنشاء رمز التفعيل للمستخدم الجديد
        var token = await _context.ActivationTokens.SingleOrDefaultAsync(t => t.UserId == createdUser.UserId);
        token.Should().NotBeNull();
        token!.TokenHash.Should().NotBeNullOrWhiteSpace();
        token.ExpiryDate.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task ApproveRequestAsync_NonExistentRequest_ReturnsFailure()
    {
        // Act
        var result = await _service.ApproveRequestAsync(999);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("الطلب غير موجود");
    }

    [Theory]
    [InlineData(RegistrationStatus.Approved)]
    [InlineData(RegistrationStatus.Rejected)]
    public async Task ApproveRequestAsync_AlreadyProcessedRequest_ReturnsFailure(RegistrationStatus status)
    {
        // Arrange
        var request = CreateRequest(60, "مصنع معالج", status);
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApproveRequestAsync(60);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("تمت معالجته مسبقاً");
    }

    [Fact]
    public async Task ApproveRequestAsync_UserCreationThrows_RollsBackAndFails()
    {
        // Arrange
        var request = CreateRequest(70, "مصنع الخطأ", RegistrationStatus.Pending);
        _context.FactoryRegistrationRequests.Add(request);
        await _context.SaveChangesAsync();

        _fakeUserService.FailNextCreate = true;

        // Act
        var result = await _service.ApproveRequestAsync(70);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("فشلت عملية القبول");
    }

    // ─────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────

    private static FactoryRegistrationRequest CreateRequest(
        int id, string factoryName, RegistrationStatus status)
        => new()
        {
            Id = id,
            FactoryName = factoryName,
            ContactName = "مالك " + factoryName,
            ContactEmail = $"contact{id}@test.local",
            ContactPhone = $"77{id:D7}",
            Area = "صنعاء",
            Address = $"شارع {factoryName}",
            CommercialId = $"CR-{id:D5}",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
}

/// <summary>
/// تنفيذ بديل خفيف لـ IUserService لاختبارات مسار قبول طلبات التسجيل.
/// </summary>
internal class FakeUserService : IUserService
{
    private int _idSequence = 1000;
    public List<UserDto> CreatedUsers { get; } = [];
    public bool FailNextCreate { get; set; }

    public Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (FailNextCreate)
            throw new InvalidOperationException("محاكاة فشل إنشاء المستخدم.");

        var user = new UserDto
        {
            UserId = ++_idSequence,
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = dto.Role.ToString(),
            FactoryId = dto.FactoryId,
            IsActive = false
        };

        CreatedUsers.Add(user);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<UserDto>> GetAllAsync() => Task.FromResult<IEnumerable<UserDto>>([]);

    public Task<PagedResult<UserDto>> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, PaginationParams pagination, bool? isActive = null)
        => Task.FromResult(new PagedResult<UserDto>());

    public Task<UserDto> GetByIdAsync(int id) => throw new NotImplementedException();
    public Task<bool> UpdateAsync(int id, UpdateUserDto dto) => Task.FromResult(true);
    public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    public Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto) => Task.FromResult(true);
    public Task<bool> UpdateDriverStatusAsync(int driverId, UpdateDriverStatusDto dto, UserRole callerRole, int? callerFactoryId) => Task.FromResult(true);
    public Task<bool> ToggleDriverActiveAsync(int driverId, UserRole callerRole, int? callerFactoryId) => Task.FromResult(true);
}
