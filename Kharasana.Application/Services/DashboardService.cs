using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Common;
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

    /// <summary>
    /// مؤشرات لوحة المصنع.
    ///
    /// <para><b>نطاقان زمنيان مختلفان عمداً — لا تُجمع عدّاداتهما:</b>
    /// <see cref="FactoryDashboardDto.TodayOrders"/> و<see cref="FactoryDashboardDto.DeliveredToday"/>
    /// محدودان باليوم المحلي (توقيت اليمن)، بينما PendingOrders/ApprovedOrders/OnTheWayOrders
    /// عدّادات تراكمية «كم طلباً ما زال في هذه الحالة الآن» بلا حدّ زمني. جمع
    /// <c>TodayOrders</c> معها يُحصي مرتين كل طلب أُنشئ اليوم وما زال مفتوحاً.</para>
    /// </summary>
    public async Task<FactoryDashboardDto> GetFactoryDashboardAsync(int factoryId)
    {
        // ✅ حدود اليوم بتوقيت اليمن مُحوَّلة إلى UTC — العمود CreatedAt مخزَّن UTC.
        //    كان DateTime.UtcNow.Date يُسقط أول ثلاث ساعات من اليوم المحلي في «أمس».
        var (today, tomorrow) = YemenTime.TodayUtcRange();

        // ✅ «طلبات اليوم» = الطلبات المُنشأة اليوم، وليس OrderStatus.New.
        //    الحالة New لا تُسنَد في أي مسار إنشاء (CreateAsync وCreatePhoneOrderAsync
        //    تُنشئان بحالة Pending)، فاحتسابها كان يُعطي صفرًا دائمًا.
        var todayOrders = await _unitOfWork.Orders.CountAsync(o =>
            o.FactoryId == factoryId &&
            o.CreatedAt >= today &&
            o.CreatedAt < tomorrow);

        // عدّادات الحالات الحالية — تراكمية بلا حدّ زمني (حجم العمل المفتوح الآن)
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
            TodayOrders = todayOrders,
            PendingOrders = pendingOrders,
            ApprovedOrders = approvedOrders,
            OnTheWayOrders = onTheWayOrders,
            DeliveredToday = deliveredToday,
            AvailableDrivers = availableDrivers
        };
    }
}