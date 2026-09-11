using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        // كل العدّادات تُنفّذ كـ COUNT في قاعدة البيانات، دون تحميل السجلات كاملة
        var totalFactories = await _unitOfWork.Factories.CountAsync();
        var totalClients = await _unitOfWork.Users.CountAsync(u => u.Role == UserRole.Client);
        var totalEmployees = await _unitOfWork.Users.CountAsync(u => u.Role == UserRole.FactoryEmployee);
        var totalDrivers = await _unitOfWork.Users.CountAsync(u => u.Role == UserRole.Driver);
        var totalOrders = await _unitOfWork.Orders.CountAsync();

        return new AdminDashboardDto
        {
            TotalFactories = totalFactories,
            TotalClients = totalClients,
            TotalEmployees = totalEmployees,
            TotalDrivers = totalDrivers,
            TotalOrders = totalOrders
        };
    }

    public async Task<FactoryDashboardDto> GetFactoryDashboardAsync(int factoryId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var newOrders = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId && o.Status == OrderStatus.New);
        var pendingOrders = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId && o.Status == OrderStatus.Pending);
        var approvedOrders = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId && o.Status == OrderStatus.Approved);
        var onTheWayOrders = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId && o.Status == OrderStatus.OnTheWay);
        var deliveredToday = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId &&
            o.Status == OrderStatus.Delivered &&
            o.DeliveredAt.HasValue &&
            o.DeliveredAt >= today &&
            o.DeliveredAt < tomorrow);

        var availableDrivers = await _unitOfWork.Users.CountAsync(u =>
            u.Role == UserRole.Driver &&
            u.FactoryId == factoryId &&
            u.IsActive &&
            u.DriverStatus == DriverStatus.Available);

        return new FactoryDashboardDto
        {
            NewOrders = newOrders,
            PendingOrders = pendingOrders,
            ApprovedOrders = approvedOrders,
            OnTheWayOrders = onTheWayOrders,
            DeliveredToday = deliveredToday,
            AvailableDrivers = availableDrivers
        };
    }
}