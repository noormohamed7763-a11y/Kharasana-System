using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// واجهة خدمات أوامر الطلبات (Create, Update, Delete).
/// </summary>
public interface IOrderCommandService
{
    Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, Domain.Enums.UserRole currentRole, int? currentUserFactoryId = null);

    Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId);

    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int userId, Domain.Enums.UserRole role, int? factoryId);

    Task<bool> DeleteOrderAsync(int id, int callerId, Domain.Enums.UserRole callerRole, int? callerFactoryId);
}
