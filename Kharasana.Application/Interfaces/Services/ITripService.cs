using Kharasana.Application.DTOs.Trips;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface ITripService
{
    Task<TripDto> CreateAsync(int orderId, CreateTripDto dto);
    Task<IReadOnlyList<TripDto>> GetByOrderAsync(int orderId);
    Task<IReadOnlyList<TripDto>> CreateBatchAsync(int orderId, CreateTripsBatchDto dto);
    Task<TripDto> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto);
    Task SoftDeleteAsync(int tripId);
}
