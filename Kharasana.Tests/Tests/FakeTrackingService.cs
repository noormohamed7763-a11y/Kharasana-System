using Kharasana.Application.Interfaces.Services;

namespace Kharasana.Tests.Tests;

public class FakeTrackingService : ITrackingService
{
    public void UpdateLocation(int orderId, double latitude, double longitude) { }
    public (double Latitude, double Longitude)? GetLocation(int orderId) => null;
    public void RemoveOrder(int orderId) { }
}
