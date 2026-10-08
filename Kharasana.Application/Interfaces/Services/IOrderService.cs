using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Kharasana.Application.DTOs.Customer;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// واجهة خدمات الطلبات.
/// </summary>
public interface IOrderService
{
    // ============================================================
    // 1. READ
    // ============================================================
    Task<PagedResult<OrderDto>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, CallerContext caller, PaginationParams pagination);

    /// <summary>جلب جميع طلبات سائق محدد (للتقارير) — بدون ترقيم، مرتبة تنازلياً بالتاريخ.</summary>
    Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, CallerContext caller);

    Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination);

    /// <summary>ملخص عميل واحد — <c>null</c> إن لم تكن له طلبات في النطاق.</summary>
    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, int? factoryId);

    // ============================================================
    // 2. CREATE
    // ============================================================
    Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null);

    Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId);

    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int userId, UserRole role, int? factoryId);

    // ============================================================
    // 3. PRICING & WORKFLOW
    // ============================================================
    Task<ServiceResult> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
}