using Kharasana.Application.DTOs.Trips;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface ITripService
{
    Task<TripDto> CreateAsync(int orderId, CreateTripDto dto, int callerId, UserRole role, int? factoryId);
    Task<IReadOnlyList<TripDto>> GetByOrderAsync(int orderId, int callerId, UserRole role, int? factoryId);
    Task<IReadOnlyList<TripDto>> CreateBatchAsync(int orderId, CreateTripsBatchDto dto, int callerId, UserRole role, int? factoryId);
    Task<TripDto> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto, int callerId, UserRole role, int? factoryId);
    Task SoftDeleteAsync(int tripId, int callerId, UserRole role, int? factoryId);
}
