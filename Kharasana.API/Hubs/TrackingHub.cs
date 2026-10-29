using Microsoft.AspNetCore.SignalR;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Kharasana.API.Hubs;

[Authorize] // ضمان أن المتصل مسجل دخول
public class TrackingHub : Hub
{
    private readonly ITrackingService _trackingService;
    private readonly IUnitOfWork _unitOfWork;

    public TrackingHub(ITrackingService trackingService, IUnitOfWork unitOfWork)
    {
        _trackingService = trackingService;
        _unitOfWork = unitOfWork;
    }

    public async Task UpdateDriverLocation(int orderId, double latitude, double longitude)
    {
        // التحقق من هوية السائق
        var driverIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (driverIdClaim == null) return;

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.DriverId.ToString() != driverIdClaim)
        {
             // السائق ليس هو المعين لهذا الطلب
             return;
        }

        _trackingService.UpdateLocation(orderId, latitude, longitude);
        await Clients.Group(orderId.ToString()).SendAsync("ReceiveLocationUpdate", latitude, longitude);
    }
    // ... بقية الـ Hub

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
