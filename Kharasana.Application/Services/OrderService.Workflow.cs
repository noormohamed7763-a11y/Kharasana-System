using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// دورة حياة الطلب: التسعير، الموافقة، الرفض، الإلغاء، بدء التوصيل، التسليم، الإغلاق،
// تغيير الحالة، وإسناد السائق.
public partial class OrderService
{
    // ============================================================
    // SET PRICE
    // ============================================================
    public async Task<bool> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        if (unitPrice <= 0)
            throw new BusinessException(Messages.UnitPriceMustBePositive);

        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.CannotUpdatePriceAtThisStage);

        order.UnitPrice = unitPrice;
        order.TotalPrice = unitPrice * order.Quantity;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // APPROVE
    // ============================================================
    public async Task<bool> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (callerRole != UserRole.FactoryEmployee && callerRole != UserRole.Admin)
            throw new ForbiddenException(Messages.NotAuthorizedToApproveOrder);

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.CannotApproveNonPendingOrder);

        if (order.UnitPrice <= 0)
            throw new BusinessException(Messages.PriceRequiredBeforeApproval);

        order.Status = OrderStatus.Approved;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // ✅ START DELIVERY - تم التعديل للسماح للسائق
    // ============================================================
    public async Task<bool> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        // ✅ السماح للسائق ببدء التوصيل إذا كان الطلب مسنداً إليه
        if (callerRole == UserRole.Driver && order.DriverId != callerId)
        {
            throw new ForbiddenException(Messages.OrderNotAssignedForStartDelivery);
        }

        if (order.Status != OrderStatus.Approved)
            throw new BusinessException(Messages.CannotStartDeliveryForNonApprovedOrder);

        if (order.TransportMethod == TransportMethod.FactoryTransport && !order.DriverId.HasValue)
            throw new BusinessException(Messages.DriverRequiredForDelivery);

        order.Status = OrderStatus.OnTheWay;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // ✅ DELIVER - تم التعديل للسماح للسائق
    // ============================================================
    public async Task<bool> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        // ✅ السماح للسائق بتأكيد التسليم إذا كان الطلب مسنداً إليه
        if (callerRole == UserRole.Driver && order.DriverId != callerId)
        {
            throw new ForbiddenException(Messages.OrderNotAssignedForDeliver);
        }

        if (order.Status != OrderStatus.OnTheWay)
            throw new BusinessException(Messages.CannotDeliverNonOnTheWayOrder);

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        ReleaseDriverAsync(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // CLOSE
    // ============================================================
    public async Task<bool> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Delivered)
            throw new BusinessException(Messages.CannotCloseNonDeliveredOrder);

        order.Status = OrderStatus.Closed;
        order.ClosedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // UPDATE STATUS
    // ============================================================
    public async Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (!OrderStatusHelper.CanTransitionTo(order.Status, dto.Status))
            throw new BusinessException(Messages.InvalidStatusTransition);

        // ✅ نفس شرط ApproveOrderAsync حرفيًا: بدون هذا الفحص يتيح مسار تغيير الحالة
        //    الانتقال Pending → Approved بسعر صفر، فيلتفّ على قاعدة التسعير الإلزامي.
        if (dto.Status == OrderStatus.Approved && order.UnitPrice <= 0)
            throw new BusinessException(Messages.PriceRequiredBeforeApproval);

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Closed)
            order.ClosedAt = DateTime.UtcNow;

        if (dto.Status is OrderStatus.Delivered or OrderStatus.Rejected or OrderStatus.Cancelled)
            ReleaseDriverAsync(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // ASSIGN DRIVER
    // ============================================================
    public async Task<bool> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Approved)
            throw new BusinessException(Messages.DriverAssignmentOnlyForApprovedOrders);

        if (order.TransportMethod == TransportMethod.ClientOwnTransport)
            throw new BusinessException(Messages.CannotAssignDriverForClientTransport);

        var driver = await _unitOfWork.Users.GetByIdAsync(dto.DriverId);
        if (driver == null)
            throw new NotFoundException(Messages.DriverNotFound);

        if (driver.Role != UserRole.Driver)
            throw new BusinessException(Messages.InvalidDriver);

        if (!driver.IsActive)
            throw new BusinessException(Messages.UserInactive);

        // ✅ إعادة تعيين السائق نفسه بلا أثر (idempotent): حالته Busy لأن هذا الطلب ذاته مسند
        //    إليه، فاشتراط Available هنا كان يرفض مجرد تعديل رقم الشاحنة على الطلب نفسه.
        var isSameDriver = order.DriverId.HasValue && order.DriverId.Value == dto.DriverId;

        if (!isSameDriver && driver.DriverStatus != DriverStatus.Available)
            throw new BusinessException(Messages.DriverNotAvailable);

        if (driver.FactoryId != order.FactoryId)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        if (order.DriverId.HasValue && order.DriverId.Value != dto.DriverId)
        {
            var previousDriver = await _unitOfWork.Users.GetByIdAsync(order.DriverId.Value);
            if (previousDriver != null && previousDriver.Role == UserRole.Driver)
            {
                previousDriver.DriverStatus = DriverStatus.Available;
                previousDriver.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(previousDriver);
            }
        }

        order.DriverId = dto.DriverId;
        order.TruckPlate = dto.TruckPlate;
        order.UpdatedAt = DateTime.UtcNow;

        driver.DriverStatus = DriverStatus.Busy;
        driver.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        _unitOfWork.Users.Update(driver);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // REJECT
    // ============================================================
    public async Task<bool> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.OrderCannotBeRejectedInStatus);

        order.Status = OrderStatus.Rejected;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            order.Notes = (order.Notes + "\n" + string.Format(Messages.RejectionReasonPrefix, reason)).Trim();

        ReleaseDriverAsync(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // ✅ CANCEL - تم التعديل للسماح للسائق
    // ============================================================
    public async Task<bool> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        // ✅ السماح للسائق بإلغاء الطلب إذا كان مسنداً إليه
        if (callerRole == UserRole.Driver && order.DriverId != callerId)
        {
            throw new ForbiddenException(Messages.OrderNotAssignedForCancel);
        }

        if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Closed)
            throw new BusinessException(Messages.CannotCancelDeliveredOrClosedOrder);

        if (order.Status == OrderStatus.Rejected || order.Status == OrderStatus.Cancelled)
            throw new BusinessException(Messages.OrderAlreadyRejectedOrCancelled);

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        ReleaseDriverAsync(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
