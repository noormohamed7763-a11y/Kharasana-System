using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Application.DTOs.Customer;

namespace Kharasana.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IConcreteTypeRepository _concreteTypeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        IConcreteTypeRepository concreteTypeRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _concreteTypeRepository = concreteTypeRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    // ============================================================
    // GET CUSTOMERS
    // ============================================================
    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination)
    {
        var (items, totalCount) = await _unitOfWork.Orders.GetFactoryCustomersAsync(
            factoryId, pagination.Search, pagination.PageNumber, pagination.PageSize);

        return new PagedResult<CustomerSummaryDto>
        {
            Items = items,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    // ============================================================
    // CREATE ORDER
    // ============================================================
    public async Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null)
    {
        // ✅ تشمل المؤرشف: لا تُنشأ طلبات على مصنع مؤرشف — خطأ واضح بدل "غير موجود"
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _concreteTypeRepository.GetByIdAsync(dto.ConcreteTypeId);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        if (concreteType.FactoryId != dto.FactoryId)
            throw new BusinessException(Messages.FactoryMismatch);

        if (!concreteType.IsActive)
            throw new BusinessException(Messages.ConcreteTypeInactive);

        int clientId;

        if (currentRole == UserRole.Admin)
        {
            clientId = await ResolveClientIdForStaffOrderAsync(dto.ClientId);
        }
        else if (currentRole == UserRole.FactoryEmployee)
        {
            if (!currentUserFactoryId.HasValue || dto.FactoryId != currentUserFactoryId.Value)
                throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

            clientId = await ResolveClientIdForStaffOrderAsync(dto.ClientId);
        }
        else if (currentRole == UserRole.Client)
        {
            var client = await _userRepository.GetByIdAsync(currentUserId);
            if (client == null)
                throw new NotFoundException(Messages.UserNotFound);

            if (!client.IsActive)
                throw new BusinessException(Messages.UserInactive);

            clientId = currentUserId;
        }
        else
        {
            throw new ForbiddenException(Messages.OrderCreationNotAllowed);
        }

        var order = BuildOrder(dto, clientId, concreteType.UnitPrice);

        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDetailsDto(order, hidePricing: false);
    }

    // ============================================================
    // CREATE PHONE ORDER
    // ============================================================
    public async Task<OrderDetailsDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId)
    {
        var normalizedPhone = YemeniPhoneHelper.Normalize(dto.ClientPhone);
        if (normalizedPhone == null)
            throw new BusinessException(Messages.InvalidYemeniPhone);

        if (dto.FactoryId != employeeFactoryId)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        // ✅ تشمل المؤرشف: لا تُعدَّل طلبات تابعة لمصنع مؤرشف
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _concreteTypeRepository.GetByIdAsync(dto.ConcreteTypeId);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        if (concreteType.FactoryId != dto.FactoryId)
            throw new BusinessException(Messages.FactoryMismatch);

        if (!concreteType.IsActive)
            throw new BusinessException(Messages.ConcreteTypeInactive);

        var client = await _userRepository.GetByPhoneAsync(normalizedPhone);

        if (client == null)
        {
            if (string.IsNullOrWhiteSpace(dto.ClientFullName))
                throw new BusinessException(Messages.ClientFullNameRequiredForNewClient);

            client = new User
            {
                FullName = dto.ClientFullName,
                Phone = normalizedPhone,
                Email = null,
                PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                Role = UserRole.Client,
                FactoryId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(client);
            await _unitOfWork.SaveChangesAsync();
        }
        else if (client.Role == UserRole.Client)
        {
            if (!client.IsActive)
                throw new BusinessException(Messages.UserInactive);
        }
        else
        {
            throw new BusinessException(Messages.PhoneLinkedToNonClientAccount);
        }

        var createDto = new CreateOrderDto
        {
            ClientId = client.UserId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Notes = dto.Notes
        };

        var order = BuildOrder(createDto, client.UserId, concreteType.UnitPrice);

        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDetailsDto(order, hidePricing: false);
    }

    // ============================================================
    // UPDATE ORDER
    // ============================================================
    public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending)
        {
            var statusName = OrderStatusHelper.GetArabicName(order.Status);
            throw new BusinessException(string.Format(Messages.OrderCannotBeUpdatedInStatus, statusName));
        }

        if (order.ConcreteTypeId != dto.ConcreteTypeId)
        {
            var concreteType = await _concreteTypeRepository.GetByIdAsync(dto.ConcreteTypeId);
            if (concreteType == null)
                throw new BusinessException(string.Format(Messages.ConcreteTypeNotFoundById, dto.ConcreteTypeId));

            if (concreteType.FactoryId != order.FactoryId)
                throw new BusinessException(Messages.ConcreteTypeNotBelongsToFactory);

            if (!concreteType.IsActive)
                throw new BusinessException(string.Format(Messages.ConcreteTypeInactiveForOrder, concreteType.Name));

            order.ConcreteTypeId = dto.ConcreteTypeId;
        }

        if (dto.Quantity <= 0)
            throw new BusinessException(Messages.QuantityMustBePositive);

        if (dto.Quantity > 1000)
            throw new BusinessException(Messages.QuantityTooLarge);

        if (dto.PouringDate.HasValue && dto.PouringDate.Value.Date < DateTime.UtcNow.Date)
            throw new BusinessException(Messages.PouringDateCannotBeInPast);

        if (dto.NeedPump && !dto.FloorNumber.HasValue)
            throw new BusinessException(Messages.PumpRequiresFloorNumber);

        if (dto.NeedPump && dto.FloorNumber.HasValue && dto.FloorNumber.Value < 0)
            throw new BusinessException(Messages.FloorNumberMustBeNonNegative);

        order.ProjectName = dto.ProjectName;
        order.ProjectOwnerName = dto.ProjectOwnerName;
        order.SiteArea = dto.SiteArea;
        order.SiteDescription = dto.SiteDescription;
        order.SlabType = dto.SlabType;
        order.Quantity = dto.Quantity;
        order.NeedPump = dto.NeedPump;
        order.FloorNumber = dto.FloorNumber;
        order.PouringDate = dto.PouringDate;
        order.TransportMethod = dto.TransportMethod;
        order.Notes = dto.Notes;
        order.UpdatedAt = DateTime.UtcNow;

        order.TotalPrice = order.Quantity * order.UnitPrice;

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToOrderDto(order, false);
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);
        return MapToDetailsDto(order, hidePricing: callerRole == UserRole.Driver);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, UserRole callerRole, PaginationParams pagination)
    {
        var (items, totalCount) = await _orderRepository.GetPagedAsync(
            factoryId, clientId, driverId, pagination.Status, pagination.Search, pagination.PageNumber, pagination.PageSize);

        var hidePricing = callerRole == UserRole.Driver;

        return new PagedResult<OrderDto>
        {
            Items = items.Select(o => MapToOrderDto(o, hidePricing)),
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    // ============================================================
    // SET PRICE
    // ============================================================
    public async Task<bool> SetPriceAsync(int id, decimal unitPrice, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        if (unitPrice <= 0)
            throw new BusinessException(Messages.UnitPriceMustBePositive);

        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.CannotUpdatePriceAtThisStage);

        order.UnitPrice = unitPrice;
        order.TotalPrice = unitPrice * order.Quantity;
        order.UpdatedAt = DateTime.UtcNow;

        if (order.Status == OrderStatus.New)
            order.Status = OrderStatus.Pending;

        _orderRepository.Update(order);
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

        _orderRepository.Update(order);
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

        _orderRepository.Update(order);
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

        await ReleaseDriverAsync(order);

        _orderRepository.Update(order);
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

        _orderRepository.Update(order);
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

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        if (dto.Status == OrderStatus.Closed)
            order.ClosedAt = DateTime.UtcNow;

        if (dto.Status is OrderStatus.Delivered or OrderStatus.Rejected or OrderStatus.Cancelled)
            await ReleaseDriverAsync(order);

        _orderRepository.Update(order);
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

        var driver = await _userRepository.GetByIdAsync(dto.DriverId);
        if (driver == null)
            throw new NotFoundException(Messages.DriverNotFound);

        if (driver.Role != UserRole.Driver)
            throw new BusinessException(Messages.InvalidDriver);

        if (!driver.IsActive)
            throw new BusinessException(Messages.UserInactive);

        if (driver.DriverStatus != DriverStatus.Available)
            throw new BusinessException(Messages.DriverNotAvailable);

        if (order.DriverId.HasValue && order.DriverId.Value != dto.DriverId)
        {
            var previousDriver = await _userRepository.GetByIdAsync(order.DriverId.Value);
            if (previousDriver != null && previousDriver.Role == UserRole.Driver)
            {
                previousDriver.DriverStatus = DriverStatus.Available;
                previousDriver.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(previousDriver);
            }
        }

        order.DriverId = dto.DriverId;
        order.TruckPlate = dto.TruckPlate;
        order.UpdatedAt = DateTime.UtcNow;

        driver.DriverStatus = DriverStatus.Busy;
        driver.UpdatedAt = DateTime.UtcNow;

        _orderRepository.Update(order);
        _userRepository.Update(driver);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // REJECT
    // ============================================================
    public async Task<bool> RejectOrderAsync(int id, string? reason, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending)
            throw new BusinessException(Messages.OrderCannotBeRejectedInStatus);

        order.Status = OrderStatus.Rejected;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            order.Notes = (order.Notes + "\n" + string.Format(Messages.RejectionReasonPrefix, reason)).Trim();

        await ReleaseDriverAsync(order);

        _orderRepository.Update(order);
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

        await ReleaseDriverAsync(order);

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private async Task<Order> GetOrderOrThrowAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(id);
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

    private async Task ReleaseDriverAsync(Order order)
    {
        if (!order.DriverId.HasValue)
            return;

        var driver = order.Driver;
        if (driver != null && driver.Role == UserRole.Driver)
        {
            driver.DriverStatus = DriverStatus.Available;
            driver.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(driver); // ✅ حفظ تحرير السائق فعلياً في قاعدة البيانات
        }
    }

    private async Task<int> ResolveClientIdForStaffOrderAsync(int clientId)
    {
        if (clientId <= 0)
            throw new BusinessException(Messages.ClientRequiredForAdminOrder);

        var client = await _userRepository.GetByIdAsync(clientId);
        if (client == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (client.Role != UserRole.Client)
            throw new BusinessException(Messages.InvalidClient);

        if (!client.IsActive)
            throw new BusinessException(Messages.UserInactive);

        return clientId;
    }

    private static Order BuildOrder(CreateOrderDto dto, int clientId, decimal unitPrice)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var totalPrice = dto.Quantity * unitPrice;

        return new Order
        {
            OrderNumber = orderNumber,
            ClientId = clientId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            UnitPrice = unitPrice,
            TotalPrice = totalPrice,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Status = OrderStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static OrderDto MapToOrderDto(Order o, bool hidePricing)
    {
        return new OrderDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ClientName = o.Client?.FullName ?? string.Format(Messages.ClientFallback, o.ClientId),
            DriverName = o.Driver?.FullName ?? string.Empty,
            FactoryName = o.Factory?.FactoryName ?? string.Format(Messages.FactoryFallback, o.FactoryId),
            ConcreteTypeName = o.ConcreteType?.Name ?? string.Format(Messages.ConcreteTypeFallback, o.ConcreteTypeId),
            Quantity = o.Quantity,
            TotalPrice = hidePricing ? null : o.TotalPrice,
            TransportMethod = o.TransportMethod,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        };
    }

    private static OrderDetailsDto MapToDetailsDto(Order order, bool hidePricing)
    {
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
            ConcreteTypeName = order.ConcreteType?.Name ?? string.Format(Messages.ConcreteTypeFallback, order.ConcreteTypeId),
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
            DriverId = order.DriverId,
            DriverName = order.Driver?.FullName,
            TruckPlate = order.TruckPlate,
            Status = order.Status,
            Notes = order.Notes
        };
    }
}