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
        int? factoryId, int? clientId, int? driverId, UserRole callerRole, PaginationParams pagination);

    Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination);

    // ============================================================
    // 2. CREATE
    // ============================================================
    Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null);

    Task<OrderDetailsDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId);

    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int userId, UserRole role, int? factoryId);

    // ============================================================
    // 3. PRICING & WORKFLOW
    // ============================================================
    Task<bool> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);

    Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId);
}