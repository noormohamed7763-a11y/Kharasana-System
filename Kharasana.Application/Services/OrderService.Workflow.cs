using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// سير عمل الطلبات: المفوض إلى OrderWorkflowService.
public partial class OrderService
{
    // ============================================================
    // SET PRICE - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.SetPriceAsync(id, unitPrice, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // APPROVE - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.ApproveOrderAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // REJECT - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.RejectOrderAsync(id, reason, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // CANCEL - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.CancelOrderAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // ASSIGN DRIVER - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.AssignDriverAsync(id, dto, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // START DELIVERY - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.StartDeliveryAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // DELIVER - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.DeliverOrderAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // CLOSE - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.CloseOrderAsync(id, callerId, callerRole, callerFactoryId);
    }

    // ============================================================
    // UPDATE STATUS - Delegates to OrderWorkflowService
    // ============================================================
    public async Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        return await _orderWorkflowService.UpdateStatusAsync(id, dto, callerId, callerRole, callerFactoryId);
    }
}
