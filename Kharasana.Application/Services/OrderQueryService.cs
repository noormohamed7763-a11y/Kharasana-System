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
        int? factoryId, int? clientId, int? driverId, UserRole callerRole, PaginationParams pagination)
    {
        var result = await _unitOfWork.Orders.GetPagedAsync(
            factoryId, clientId, driverId, pagination.Status, pagination.Search, pagination.PageNumber, pagination.PageSize);

        var hidePricing = callerRole == UserRole.Driver;

        return new PagedResult<OrderDto>
        {
            Items = result.Items.Select(o => OrderMapper.MapToOrderDto(o, hidePricing)),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, int? callerFactoryId, UserRole callerRole)
    {
        if (callerRole != UserRole.Admin && callerRole != UserRole.FactoryEmployee)
        {
            throw new ForbiddenException(Messages.NotAuthorizedToViewReport);
        }

        if (callerRole == UserRole.FactoryEmployee)
        {
            if (!callerFactoryId.HasValue)
                throw new ForbiddenException(Messages.NotAuthorizedToViewReport);

            var driver = await _unitOfWork.Users.GetByIdAsync(driverId);
            if (driver == null || driver.FactoryId != callerFactoryId.Value || driver.Role != UserRole.Driver)
            {
                throw new ForbiddenException(Messages.NotAuthorizedToViewReport);
            }
        }

        var result = await _unitOfWork.Orders.GetPagedAsync(
            factoryId: callerRole == UserRole.FactoryEmployee ? callerFactoryId : null,
            clientId: null,
            driverId: driverId,
            status: null,
            search: null,
            pageNumber: 1,
            pageSize: 100_000);

        return result.Items
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => OrderMapper.MapToOrderDto(o, hidePricing: false));
    }
}