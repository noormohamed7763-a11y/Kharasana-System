namespace Kharasana.Application.Interfaces.Services;

public interface ITrackingService
{
    void UpdateLocation(int orderId, double latitude, double longitude);
    (double Latitude, double Longitude)? GetLocation(int orderId);
    void RemoveOrder(int orderId);
}
