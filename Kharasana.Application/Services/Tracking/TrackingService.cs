using System.Collections.Concurrent;
using Kharasana.Application.Interfaces.Services;

namespace Kharasana.Application.Services.Tracking;

public class TrackingService : ITrackingService
{
    private readonly ConcurrentDictionary<int, (double Latitude, double Longitude)> _locations = new();

    public void UpdateLocation(int orderId, double latitude, double longitude)
    {
        _locations[orderId] = (latitude, longitude);
    }

    public (double Latitude, double Longitude)? GetLocation(int orderId)
    {
        return _locations.TryGetValue(orderId, out var location) ? location : null;
    }

    public void RemoveOrder(int orderId)
    {
        _locations.TryRemove(orderId, out _);
    }
}
