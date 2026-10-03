using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Kharasana.Application.DTOs.Customer;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// واجهة خدمات استعلام الطلبات.
/// </summary>
public interface IOrderQueryService
{
    Task<PagedResult<OrderDto>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, UserRole callerRole, PaginationParams pagination);

    /// <summary>جلب جميع طلبات سائق محدد (للتقارير) — بدون ترقيم، مرتبة تنازلياً بالتاريخ.</summary>
    Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, int? callerFactoryId, UserRole callerRole);

    Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination);

    /// <summary>ملخص عميل واحد — <c>null</c> إن لم تكن له طلبات في النطاق.</summary>
    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, int? factoryId);
}
