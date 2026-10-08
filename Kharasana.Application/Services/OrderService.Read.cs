using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// قراءة الطلبات: المفوض إلى OrderQueryService.
public partial class OrderService
{
    // ============================================================
    // GET CUSTOMERS - Delegates to OrderQueryService
    // ============================================================
    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination)
    {
        return await _orderQueryService.GetCustomersAsync(factoryId, pagination);
    }

    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, int? factoryId)
    {
        return await _orderQueryService.GetCustomerSummaryAsync(customerId, factoryId);
    }

    // ============================================================
    // GET BY ID - Delegates to OrderQueryService
    // ============================================================
    public async Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderQueryService.GetByIdAsync(id, callerId, callerRole, callerFactoryId);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, CallerContext caller, PaginationParams pagination)
    {
        return await _orderQueryService.GetPagedAsync(factoryId, clientId, driverId, caller, pagination);
    }

    // ============================================================
    // GET ORDERS BY DRIVER ID (for reports) - Delegates to OrderQueryService
    // ============================================================
    public async Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, CallerContext caller)
    {
        return await _orderQueryService.GetOrdersByDriverIdAsync(driverId, caller);
    }
}
