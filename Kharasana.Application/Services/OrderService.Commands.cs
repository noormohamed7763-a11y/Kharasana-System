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
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null)
    {
        return await _orderCommandService.CreateAsync(dto, currentUserId, currentRole, currentUserFactoryId);
    }

    // ============================================================
    // CREATE PHONE ORDER - Delegates to OrderCommandService
    // ============================================================
    public async Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId)
    {
        return await _orderCommandService.CreatePhoneOrderAsync(dto, employeeFactoryId);
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
}
