using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class OrderWorkflowService : IOrderWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderWorkflowService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId)
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

        return ServiceResult.Ok(Messages.PriceSavedSuccessfully);
    }

    public async Task<ServiceResult> ApproveOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
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

        return ServiceResult.Ok(Messages.OrderApprovedSuccessfully);
    }

    public async Task<ServiceResult> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.OrderCannotBeRejectedInStatus);

        order.Status = OrderStatus.Rejected;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            order.Notes = (order.Notes + "\n" + string.Format(Messages.RejectionReasonPrefix, reason)).Trim();

        ReleaseDriver(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Messages.OrderRejectedSuccessfully);
    }

    public async Task<ServiceResult> CancelOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

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

        ReleaseDriver(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Messages.OrderCancelledSuccessfully);
    }

    public async Task<ServiceResult> AssignDriverAsync(int id, AssignDriverDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
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

        return ServiceResult.Ok(Messages.DriverAssignedSuccessfully);
    }

    public async Task<ServiceResult> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

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

        return ServiceResult.Ok(Messages.DeliveryStartedSuccessfully);
    }

    public async Task<ServiceResult> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (callerRole == UserRole.Driver && order.DriverId != callerId)
        {
            throw new ForbiddenException(Messages.OrderNotAssignedForDeliver);
        }

        if (order.Status != OrderStatus.OnTheWay)
            throw new BusinessException(Messages.CannotDeliverNonOnTheWayOrder);

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        ReleaseDriver(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Messages.OrderDeliveredSuccessfully);
    }

    public async Task<ServiceResult> CloseOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Delivered)
            throw new BusinessException(Messages.CannotCloseNonDeliveredOrder);

        order.Status = OrderStatus.Closed;
        order.ClosedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Messages.OrderClosedSuccessfully);
    }

    public async Task<ServiceResult> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (!OrderStatusHelper.CanTransitionTo(order.Status, dto.Status))
            throw new BusinessException(Messages.InvalidStatusTransition);

        if (dto.Status == OrderStatus.Approved && order.UnitPrice <= 0)
            throw new BusinessException(Messages.PriceRequiredBeforeApproval);

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Closed)
            order.ClosedAt = DateTime.UtcNow;

        if (dto.Status is OrderStatus.Delivered or OrderStatus.Rejected or OrderStatus.Cancelled)
            ReleaseDriver(order);

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Messages.OrderStatusUpdated);
    }

    private async Task<Order> GetOrderOrThrowAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
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

    private void ReleaseDriver(Order order)
    {
        if (!order.DriverId.HasValue)
            return;

        var driver = order.Driver;
        if (driver != null && driver.Role == UserRole.Driver)
        {
            driver.DriverStatus = DriverStatus.Available;
            driver.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(driver);
        }
    }
}
