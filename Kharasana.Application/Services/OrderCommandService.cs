using System.Security.Cryptography;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Common.Mapping;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class OrderCommandService : IOrderCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public OrderCommandService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null)
    {
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
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
            var client = await _unitOfWork.Users.GetByIdAsync(currentUserId);
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

        var order = BuildOrder(dto, clientId, concreteType);

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return OrderMapper.MapToDetailsDto(order, hidePricing: false);
    }

    public async Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId)
    {
        var normalizedPhone = YemeniPhoneHelper.Normalize(dto.ClientPhone);
        if (normalizedPhone == null)
            throw new BusinessException(Messages.InvalidYemeniPhone);

        if (dto.FactoryId != employeeFactoryId)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        if (concreteType.FactoryId != dto.FactoryId)
            throw new BusinessException(Messages.FactoryMismatch);

        if (!concreteType.IsActive)
            throw new BusinessException(Messages.ConcreteTypeInactive);

        var client = await _unitOfWork.Users.GetByPhoneAsync(normalizedPhone);

        string? temporaryPassword = null;

        if (client == null)
        {
            if (string.IsNullOrWhiteSpace(dto.ClientFullName))
                throw new BusinessException(Messages.ClientFullNameRequiredForNewClient);

            temporaryPassword = GenerateTemporaryPassword();

            client = new User
            {
                FullName = dto.ClientFullName,
                Phone = normalizedPhone,
                Email = null,
                PasswordHash = _passwordHasher.Hash(temporaryPassword),
                Role = UserRole.Client,
                FactoryId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(client);
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

        var order = BuildOrder(createDto, client.UserId, concreteType);

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return new PhoneOrderResultDto
        {
            Order = OrderMapper.MapToDetailsDto(order, hidePricing: false),
            NewClientTemporaryPassword = temporaryPassword,
            NewClientPhone = temporaryPassword == null ? null : normalizedPhone
        };
    }

    public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Pending)
        {
            var statusName = OrderStatusHelper.GetArabicName(order.Status);
            throw new BusinessException(string.Format(Messages.OrderCannotBeUpdatedInStatus, statusName));
        }

        if (order.ConcreteTypeId != dto.ConcreteTypeId)
        {
            var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
            if (concreteType == null)
                throw new BusinessException(string.Format(Messages.ConcreteTypeNotFoundById, dto.ConcreteTypeId));

            if (concreteType.FactoryId != order.FactoryId)
                throw new BusinessException(Messages.ConcreteTypeNotBelongsToFactory);

            if (!concreteType.IsActive)
                throw new BusinessException(string.Format(Messages.ConcreteTypeInactiveForOrder, concreteType.Name));

            var previousUnitPrice = order.ConcreteType?.UnitPrice;
            var wasManuallyPriced = previousUnitPrice.HasValue && order.UnitPrice != previousUnitPrice.Value;

            order.ConcreteTypeId = dto.ConcreteTypeId;
            order.ConcreteType = concreteType;
            order.ConcreteTypeNameSnapshot = concreteType.Name;
            order.ConcreteTypeStrengthSnapshot = concreteType.Strength;

            if (!wasManuallyPriced)
                order.UnitPrice = concreteType.UnitPrice;
        }

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

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return OrderMapper.MapToOrderDto(order, false);
    }

    public async Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (callerRole != UserRole.Admin && callerRole != UserRole.FactoryEmployee)
            throw new ForbiddenException(Messages.Unauthorized);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Closed)
            throw new BusinessException(Messages.CannotCancelDeliveredOrClosedOrder);

        ReleaseDriverAsync(order);

        order.IsDeleted = true;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
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

    private void ReleaseDriverAsync(Order order)
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

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const int length = 10;

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

        return new string(chars);
    }

    private async Task<int> ResolveClientIdForStaffOrderAsync(int clientId)
    {
        if (clientId <= 0)
            throw new BusinessException(Messages.ClientRequiredForAdminOrder);

        var client = await _unitOfWork.Users.GetByIdAsync(clientId);
        if (client == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (client.Role != UserRole.Client)
            throw new BusinessException(Messages.InvalidClient);

        if (!client.IsActive)
            throw new BusinessException(Messages.UserInactive);

        return clientId;
    }

    private static Order BuildOrder(CreateOrderDto dto, int clientId, ConcreteType concreteType)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var totalPrice = dto.Quantity * concreteType.UnitPrice;

        return new Order
        {
            OrderNumber = orderNumber,
            ClientId = clientId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            ConcreteTypeNameSnapshot = concreteType.Name,
            ConcreteTypeStrengthSnapshot = concreteType.Strength,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            UnitPrice = concreteType.UnitPrice,
            TotalPrice = totalPrice,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Status = OrderStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
