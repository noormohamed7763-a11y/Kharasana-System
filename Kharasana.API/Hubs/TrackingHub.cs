using Microsoft.AspNetCore.SignalR;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Kharasana.API.Hubs;

[Authorize] // ضمان أن المتصل مسجل دخول
public class TrackingHub : Hub
{
    private readonly ITrackingService _trackingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public TrackingHub(ITrackingService trackingService, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _trackingService = trackingService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task UpdateDriverLocation(int orderId, double latitude, double longitude)
    {
        // ✅ 1. تحقق صارم من الإحداثيات
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            return; // تجاهل الإحداثيات غير المنطقية
        }

        // ✅ 2. استخدام ICurrentUserService الموحد
        var userId = _currentUserService.UserId;
        if (userId == null) return;

        // ✅ 3. تحسين الأداء (يمكن لاحقاً إضافة Cache هنا)
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.DriverId.ToString() != userId)
        {
             return;
        }

        _trackingService.UpdateLocation(orderId, latitude, longitude);
        await Clients.Group(orderId.ToString()).SendAsync("ReceiveLocationUpdate", latitude, longitude);
    }
    public override async Task OnConnectedAsync()
    {
        // التحقق من أن السائق لديه طلبات قيد التنفيذ أو العميل لديه طلبات
        // ويمكن إضافة منطق لضم المستخدم لمجموعات الطلبات تلقائياً بناءً على الحالة
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // يُستخدم من قبل العميل للاشتراك في تتبع طلب معين
    public async Task JoinOrderTracking(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new HubException("الطلب غير موجود");
        }

        // ✅ 4. استخدام ICurrentUserService للتحقق من الصلاحيات
        var currentUserId = int.Parse(_currentUserService.UserId ?? "0");
        var currentFactoryId = _currentUserService.FactoryId;

        bool authorized = _currentUserService.IsInRole("Admin") ||
                         (_currentUserService.IsInRole("Client") && order.ClientId == currentUserId) ||
                         (_currentUserService.IsInRole("Driver") && order.DriverId == currentUserId) ||
                         (_currentUserService.IsInRole("FactoryEmployee") && order.FactoryId == currentFactoryId);

        if (!authorized)
        {
            throw new HubException("غير مصرح بتتبع هذا الطلب");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
    }

    // يُستخدم من قبل العميل لإلغاء الاشتراك
    public async Task LeaveOrderTracking(int orderId)
    {
        // يمكن تبسيط المغادرة دون تحقق ثقيل من قاعدة البيانات إذا لزم الأمر
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId.ToString());
    }
}
