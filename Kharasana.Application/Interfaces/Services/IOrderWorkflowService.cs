using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// واجهة خدمات سير عمل الطلبات (Pricing, Workflow, Status).
/// </summary>
public interface IOrderWorkflowService
{
    Task<ServiceResult> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId);
}
