using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Common.Mapping;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class OrderQueryService : IOrderQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderHelperService _orderHelper;

    public OrderQueryService(IUnitOfWork unitOfWork, IOrderHelperService orderHelper)
    {
        _unitOfWork = unitOfWork;
        _orderHelper = orderHelper;
    }

    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination)
    {
        return await _unitOfWork.Orders.GetFactoryCustomersAsync(
            factoryId, pagination.Search, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, int? factoryId)
    {
        return await _unitOfWork.Orders.GetFactoryCustomerAsync(customerId, factoryId);
    }

    public async Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await _orderHelper.GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        var restrictDriverToActive = callerRole == UserRole.Client;

        return OrderMapper.MapToDetailsDto(order, hidePricing: callerRole == UserRole.Driver,
            restrictDriverToActive: restrictDriverToActive);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, CallerContext caller, PaginationParams pagination)
    {
        var result = await _unitOfWork.Orders.GetPagedAsync(
            caller, factoryId, clientId, driverId, pagination.Status, pagination.Search, pagination.PageNumber, pagination.PageSize);

        return result;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, CallerContext caller)
    {
        if (caller.Role != UserRole.Admin && caller.Role != UserRole.FactoryEmployee)
        {
            throw new ForbiddenException(Messages.NotAuthorizedToViewReport);
        }

        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (!caller.FactoryId.HasValue)
                throw new ForbiddenException(Messages.NotAuthorizedToViewReport);

            var driver = await _unitOfWork.Users.GetByIdAsync(driverId);
            if (driver == null || driver.FactoryId != caller.FactoryId.Value || driver.Role != UserRole.Driver)
            {
                throw new ForbiddenException(Messages.NotAuthorizedToViewReport);
            }
        }

        var result = await _unitOfWork.Orders.GetPagedAsync(
            caller,
            factoryId: caller.Role == UserRole.FactoryEmployee ? caller.FactoryId : null,
            clientId: null,
            driverId: driverId,
            status: null,
            search: null,
            pageNumber: 1,
            pageSize: 100_000);

        return result.Items
            .OrderByDescending(o => o.CreatedAt);
    }
}