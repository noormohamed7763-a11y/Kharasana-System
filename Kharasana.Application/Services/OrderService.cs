using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// الأجزاء الأخرى من هذه الخدمة:
//   OrderService.Read.cs      — قراءة الطلبات وملخصات العملاء.
//   OrderService.Commands.cs  — الإنشاء والتعديل والحذف الناعم.
//   OrderService.Workflow.cs  — التسعير ودورة حياة الطلب وإسناد السائق.
public partial class OrderService : IOrderService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    // ============================================================
    // SHARED HELPERS
    // ============================================================

    private async Task<Order> GetOrderOrThrowAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        // ✅ قراءة مُتتبَّعة: كل مسارات هذا المُساعد إمّا تُعدّل الطلب أو تُحرّر سائقه.
        //    مع قراءة AsNoTracking كان Update(order) يُرفق الرسم البياني كاملاً
        //    (العميل، المصنع، نوع الخرسانة، السائق) بحالة Modified، فيُرسل UPDATE
        //    محروس بـ RowVersion على كلٍّ منها مع كل تغيير حالة — ويُرمى استثناء تتبّع
        //    عند إسناد سائق جديد لطلب له سائق سابق. لذا ندع EF يكشف التغييرات فعلياً:
        //    نداءات Update(order) أدناه صارت بلا أثر (كيان مُتتبَّع)، وبقاؤها مقصود
        //    كتوثيق للنية وكأمان لو عادت القراءة يوماً إلى AsNoTracking.
        var order = await _unitOfWork.Orders.GetByIdWithDetailsForUpdateAsync(id);
        if (order == null)
            throw new NotFoundException(Messages.OrderNotFound);

        switch (callerRole)
        {
            case UserRole.Admin:
                break;

            case UserRole.FactoryEmployee:
                if (!callerFactoryId.HasValue || order.FactoryId != callerFactoryId.Value)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            case UserRole.Client:
                if (order.ClientId != callerId)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            case UserRole.Driver:
                if (order.DriverId != callerId)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            default:
                throw new NotFoundException(Messages.OrderNotFound);
        }

        return order;
    }

    /// <summary>
    /// تحرير السائق المرتبط بالطلب (إعادته إلى «متاح»).
    /// لا تنفّذ أي إدخال/إخراج: تُعدّل الكيان المُتتبَّع فقط، والحفظ الفعلي
    /// مسؤولية SaveChangesAsync في المستدعي — لذلك ليست async.
    /// </summary>
    private void ReleaseDriverAsync(Order order)
    {
        if (!order.DriverId.HasValue)
            return;

        var driver = order.Driver;
        if (driver != null && driver.Role == UserRole.Driver)
        {
            driver.DriverStatus = DriverStatus.Available;
            driver.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(driver); // ✅ حفظ تحرير السائق فعلياً في قاعدة البيانات
        }
    }

    private static OrderDto MapToOrderDto(Order o, bool hidePricing)
    {
        return new OrderDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ClientName = o.Client?.FullName ?? string.Format(Messages.ClientFallback, o.ClientId),
            DriverId = o.DriverId,
            DriverName = o.Driver?.FullName ?? string.Empty,
            FactoryName = o.Factory?.FactoryName ?? string.Format(Messages.FactoryFallback, o.FactoryId),
            // اللقطة أولًا: الاسم كما كان وقت الإنشاء. وإن غابت (صف سابق للترحيل) نسقط للنوع الحالي.
            ConcreteTypeName = o.ConcreteTypeNameSnapshot ?? o.ConcreteType?.Name ?? string.Format(Messages.ConcreteTypeFallback, o.ConcreteTypeId),
            Quantity = o.Quantity,
            TotalPrice = hidePricing ? null : o.TotalPrice,
            TransportMethod = o.TransportMethod,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        };
    }

    private static OrderDetailsDto MapToDetailsDto(Order order, bool hidePricing, bool restrictDriverToActive = false)
    {
        // ✅ بيانات السائق لا تُعرض لطلب لم يُسند له سائق بعد؛
        //     وللعميل تُقصر على مرحلة التوصيل النشطة فقط (Approved / OnTheWay)
        var showDriver = order.DriverId.HasValue &&
            (!restrictDriverToActive || order.Status is OrderStatus.Approved or OrderStatus.OnTheWay);

        return new OrderDetailsDto
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            ClientName = order.Client?.FullName ?? string.Format(Messages.ClientFallback, order.ClientId),
            ClientPhone = order.Client?.Phone,
            ClientOrdersCount = order.Client?.ClientOrders?.Count ?? 0,
            FactoryId = order.FactoryId,
            FactoryName = order.Factory?.FactoryName ?? string.Format(Messages.FactoryFallback, order.FactoryId),
            ConcreteTypeId = order.ConcreteTypeId,
            // اللقطتان أولًا — القوة والاسم كما كانا وقت الإنشاء، والسقوط للنوع الحالي لصف قديم.
            ConcreteStrength = order.ConcreteTypeStrengthSnapshot ?? order.ConcreteType?.Strength ?? 0,
            ConcreteTypeName = order.ConcreteTypeNameSnapshot ?? order.ConcreteType?.Name ?? string.Format(Messages.ConcreteTypeFallback, order.ConcreteTypeId),
            ProjectName = order.ProjectName,
            ProjectOwnerName = order.ProjectOwnerName,
            SiteArea = order.SiteArea,
            SiteDescription = order.SiteDescription,
            SlabType = order.SlabType,
            Quantity = order.Quantity,
            NeedPump = order.NeedPump,
            FloorNumber = order.FloorNumber,
            UnitPrice = hidePricing ? null : order.UnitPrice,
            TotalPrice = hidePricing ? null : order.TotalPrice,
            PouringDate = order.PouringDate,
            TransportMethod = order.TransportMethod,
            DriverId = showDriver ? order.DriverId : null,
            DriverName = showDriver ? order.Driver?.FullName : null,
            DriverPhone = showDriver ? YemeniPhoneHelper.Normalize(order.Driver?.Phone) : null,
            TruckPlate = showDriver ? order.TruckPlate : null,
            Status = order.Status,
            Notes = order.Notes
        };
    }
}
