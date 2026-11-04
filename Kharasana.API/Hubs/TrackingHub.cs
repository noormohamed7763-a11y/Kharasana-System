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
        var caller = GetCallerContext();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new HubException("الطلب غير موجود");
        }

        // التحقق من الصلاحية: Admin أو صاحب الطلب أو السائق المسند أو موظف المصنع
        bool authorized = caller.Role == UserRole.Admin ||
                         caller.Role == UserRole.Client && order.ClientId == caller.UserId ||
                         caller.Role == UserRole.Driver && order.DriverId == caller.UserId ||
                         caller.Role == UserRole.FactoryEmployee && order.FactoryId == caller.FactoryId;

        if (!authorized)
        {
            throw new HubException("غير مصرح بتتبع هذا الطلب");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, orderId.ToString());
    }

    // يُستخدم من قبل العميل لإلغاء الاشتراك
    public async Task LeaveOrderTracking(int orderId)
    {
        var caller = GetCallerContext();

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new HubException("الطلب غير موجود");
        }

        // التحقق من الصلاحية
        bool authorized = caller.Role == UserRole.Admin ||
                         caller.Role == UserRole.Client && order.ClientId == caller.UserId ||
                         caller.Role == UserRole.Driver && order.DriverId == caller.UserId ||
                         caller.Role == UserRole.FactoryEmployee && order.FactoryId == caller.FactoryId;

        if (!authorized)
        {
            throw new HubException("غير مصرح بمغادرة تتبع هذا الطلب");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId.ToString());
    }

    private CallerContext GetCallerContext()
    {
        var user = Context.User;
        if (user == null)
            throw new HubException("مستخدم غير مصادق");

        var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var roleClaim = user.FindFirst(System.Security.Claims.ClaimTypes.Role);
        var factoryIdClaim = user.FindFirst("factoryId");

        if (userIdClaim == null || roleClaim == null)
            throw new HubException("مطالبات توكن غير مكتملة");

        return new CallerContext(
            int.Parse(userIdClaim.Value),
            Enum.Parse<UserRole>(roleClaim.Value),
            factoryIdClaim != null ? int.Parse(factoryIdClaim.Value) : null
        );
    }
}
