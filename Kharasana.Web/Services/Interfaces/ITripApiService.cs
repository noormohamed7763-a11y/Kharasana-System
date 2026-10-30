using Kharasana.Application.DTOs.Trips;

namespace Kharasana.Web.Services.Interfaces;

public interface ITripApiService
{
    Task<IReadOnlyList<TripDto>> GetByOrderAsync(int orderId);
    Task<TripDto> CreateAsync(int orderId, CreateTripDto dto);
    Task<IReadOnlyList<TripDto>> CreateBatchAsync(int orderId, CreateTripsBatchDto dto);
    Task<TripDto> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto);
    Task<bool> SoftDeleteAsync(int tripId);
}
