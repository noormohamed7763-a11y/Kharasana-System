using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Moq;
using Xunit;

namespace Kharasana.Tests.Tests;

public class FinanceServiceTests
{
    private readonly KharasanaDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FinanceService _financeService;

    public FinanceServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        var unitOfWork = new UnitOfWork(_context);
        
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        var idempotencyServiceMock = new Mock<IIdempotencyService>();
        
        _financeService = new FinanceService(unitOfWork, _currentUserServiceMock.Object, idempotencyServiceMock.Object);
    }

    [Fact]
    public async Task GetPendingPaymentsAsync_AdminCanAccessAnyFactory()
    {
        // Arrange
        _currentUserServiceMock.Setup(c => c.IsInRole("Admin")).Returns(true);
        
        // Act & Assert
        // This shouldn't throw UnauthorizedException
        await _financeService.GetPendingPaymentsAsync(1); // Assuming factory 1 exists in test data
    }

    [Fact]
    public async Task GetPendingPaymentsAsync_EmployeeCanAccessOwnFactory()
    {
        // Arrange
        int factoryId = 1;
        _currentUserServiceMock.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUserServiceMock.Setup(c => c.FactoryId).Returns(factoryId);
        
        // Act & Assert
        await _financeService.GetPendingPaymentsAsync(factoryId);
    }

    [Fact]
    public async Task GetPendingPaymentsAsync_EmployeeCannotAccessOtherFactory()
    {
        // Arrange
        int ownFactoryId = 1;
        int otherFactoryId = 2;
        _currentUserServiceMock.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUserServiceMock.Setup(c => c.FactoryId).Returns(ownFactoryId);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _financeService.GetPendingPaymentsAsync(otherFactoryId));
    }
}
