using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Trips;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;

namespace Kharasana.Web.Services.Api;

public class TripApiService : ITripApiService
{
    private readonly ApiClient _apiClient;

    public TripApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<TripDto>> GetByOrderAsync(int orderId)
    {
        var response = await _apiClient.GetAsync<ApiResponse<IReadOnlyList<TripDto>>>($"orders/{orderId}/trips");
        return response?.Success == true ? (response.Data ?? new List<TripDto>()) : new List<TripDto>();
    }

    public async Task<TripDto?> CreateAsync(int orderId, CreateTripDto dto)
    {
        var response = await _apiClient.PostAsync<TripDto>($"orders/{orderId}/trips", dto);
        return response;
    }

    public async Task<IReadOnlyList<TripDto>> CreateBatchAsync(int orderId, CreateTripsBatchDto dto)
    {
        var response = await _apiClient.PostAsync<IReadOnlyList<TripDto>>($"orders/{orderId}/trips/batch", dto);
        return response ?? new List<TripDto>();
    }

    public async Task<TripDto?> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto)
    {
        // استخدام PutAsync بدلاً من PatchAsync لأن ApiClient لا يدعم Patch
        var response = await _apiClient.PutAsync<TripDto>($"trips/{tripId}/status", dto);
        return response;
    }

    public async Task<bool> SoftDeleteAsync(int tripId)
    {
        var response = await _apiClient.DeleteAsync<object>($"trips/{tripId}");
        return response != null;
    }
}