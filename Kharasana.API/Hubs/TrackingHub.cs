using Microsoft.AspNetCore.SignalR;
using Kharasana.Application.Interfaces.Services;

namespace Kharasana.API.Hubs;

public class TrackingHub : Hub
{
    private readonly ITrackingService _trackingService;

    public TrackingHub(ITrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    // يُستخدم من قبل السائق لتحديث موقعه
    public async Task UpdateDriverLocation(int orderId, double latitude, double longitude)
    {
        _trackingService.UpdateLocation(orderId, latitude, longitude);

        // بث الموقع الجديد للعميل المشترك في مجموعة هذا الطلب
        await Clients.Group(orderId.ToString()).SendAsync("ReceiveLocationUpdate", latitude, longitude);
    }

    // يُستخدم من قبل العميل للاشتراك في تتبع طلب معين
    public async Task JoinOrderTracking(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
    }

    // يُستخدم من قبل العميل لإلغاء الاشتراك
    public async Task LeaveOrderTracking(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId.ToString());
    }
}
