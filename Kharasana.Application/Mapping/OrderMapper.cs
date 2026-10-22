using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Common;

namespace Kharasana.Application.Mapping;

public static class OrderMapper
{
    public static OrderDto MapToOrderDto(Order o, bool hidePricing)
    {
        return new OrderDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ClientName = o.Client?.FullName ?? string.Format(Messages.ClientFallback, o.ClientId),
            ClientPhone = o.Client?.Phone,
            DriverId = o.DriverId,
            DriverName = o.Driver?.FullName ?? string.Empty,
            FactoryName = o.Factory?.FactoryName ?? string.Format(Messages.FactoryFallback, o.FactoryId),
            ConcreteTypeName = o.ConcreteTypeNameSnapshot ?? o.ConcreteType?.Name ?? string.Format(Messages.ConcreteTypeFallback, o.ConcreteTypeId),
            Quantity = o.Quantity,
            TotalPrice = hidePricing ? null : o.TotalPrice,
            TotalDiscountAmount = o.TotalDiscountAmount,
            NetAmount = o.NetAmount,
            Payments = MapPayments(o.Payments),
            TransportMethod = o.TransportMethod,
            Status = o.Status,
            PouringDate = o.PouringDate,
            CreatedAt = o.CreatedAt
        };
    }

    /// <summary>
    /// تحويل دفعات الطلب إلى DTO — بترتيب زمني تصاعدي (الأقدم أولاً) لأن كشف
    /// الحساب يُقرأ زمنياً، والترتيب عشوائي بلا <c>Include</c> صريح.
    /// </summary>
    public static ICollection<OrderPaymentDto> MapPayments(IEnumerable<OrderPayment>? payments)
    {
        if (payments == null)
            return new List<OrderPaymentDto>();

        return payments
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.OrderPaymentId)
            .Select(p => new OrderPaymentDto
            {
                Id = p.OrderPaymentId,
                OrderId = p.OrderId,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNo = p.ReferenceNumber,
                Notes = p.Notes
            })
            .ToList();
    }

    public static OrderDetailsDto MapToDetailsDto(Order order, bool hidePricing, bool restrictDriverToActive = false)
    {
        var showDriver = order.DriverId.HasValue &&
            (!restrictDriverToActive || order.Status is Domain.Enums.OrderStatus.Approved or Domain.Enums.OrderStatus.OnTheWay);

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
            TotalDiscountAmount = hidePricing ? null : order.TotalDiscountAmount,
            NetAmount = hidePricing ? null : order.NetAmount,
            Payments = hidePricing ? new List<OrderPaymentDto>() : MapPayments(order.Payments),
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
