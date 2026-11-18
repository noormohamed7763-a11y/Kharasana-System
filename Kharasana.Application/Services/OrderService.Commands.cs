using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Kharasana.Application.Common;

namespace Kharasana.Application.Services;

// إنشاء الطلبات وتعديلها وحذفها: المفوض إلى OrderCommandService.
public partial class OrderService
{
    // ============================================================
    // CREATE ORDER - Delegates to OrderCommandService
    // ============================================================
    public async Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, string? idempotencyKey = null, int? currentUserFactoryId = null)
    {
        return await _orderCommandService.CreateAsync(dto, currentUserId, currentRole, idempotencyKey, currentUserFactoryId);
    }

    // ============================================================
    // CREATE PHONE ORDER - Delegates to OrderCommandService
    // ============================================================
    public async Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId, string? idempotencyKey = null, int currentUserId = 0)
    {
        return await _orderCommandService.CreatePhoneOrderAsync(dto, employeeFactoryId, idempotencyKey, currentUserId);
    }

    // ============================================================
    // UPDATE ORDER - Delegates to OrderCommandService
    // ============================================================
    public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderCommandService.UpdateOrderAsync(id, dto, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // DELETE - Delegates to OrderCommandService
    // ============================================================
    public async Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderCommandService.DeleteOrderAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // ADD PAYMENT - Delegates to OrderCommandService
    // ============================================================
    public async Task AddPaymentAsync(int orderId, AddPaymentDto dto, int callerId, UserRole callerRole, string? idempotencyKey = null, int? callerFactoryId = null)
    {
        await _orderCommandService.AddPaymentAsync(orderId, dto, callerId, callerRole, idempotencyKey, callerFactoryId);
    }
}
