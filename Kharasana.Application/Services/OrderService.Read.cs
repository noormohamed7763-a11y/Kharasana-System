using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// قراءة الطلبات: القائمة المُرقَّمة، تفاصيل طلب واحد، طلبات سائق (للتقارير)، وملخصات العملاء.
public partial class OrderService
{
    // ============================================================
    // GET CUSTOMERS
    // ============================================================
    public async Task<PagedResult<CustomerSummaryDto>> GetCustomersAsync(int? factoryId, PaginationParams pagination)
    {
        return await _unitOfWork.Orders.GetFactoryCustomersAsync(
            factoryId, pagination.Search, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, int? factoryId)
    {
        return await _unitOfWork.Orders.GetFactoryCustomerAsync(customerId, factoryId);
    }

    // ============================================================
    // GET BY ID
    // ============================================================
    public async Task<OrderDetailsDto> GetByIdAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        // ✅ العميل يرى بيانات السائق فقط أثناء التوصيل النشط (Approved / OnTheWay)
        //     ولا تُعرض لطلب لم يُسند له سائق بعد — باقي الأدوار تحتفظ بسلوكها الحالي
        var restrictDriverToActive = callerRole == UserRole.Client;

        return MapToDetailsDto(order, hidePricing: callerRole == UserRole.Driver,
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
            Items = result.Items.Select(o => MapToOrderDto(o, hidePricing)),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    // ============================================================
    // GET ORDERS BY DRIVER ID (for reports)
    // ============================================================
    public async Task<IEnumerable<OrderDto>> GetOrdersByDriverIdAsync(int driverId, int? callerFactoryId, UserRole callerRole)
    {
        // التحقق من الصلاحيات: مدير أو موظف مصنع فقط
        if (callerRole != UserRole.Admin && callerRole != UserRole.FactoryEmployee)
        {
            throw new ForbiddenException(Messages.NotAuthorizedToViewReport);
        }

        // موظف المصنع يرى سائقي مصنعه فقط.
        // ✅ fail-closed: موظف بلا مصنع مُسنَد يُرفض بدل أن يمرّ بلا فحص عزل إطلاقًا.
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

        // ملاحظة: نستدعي المخزن مباشرة بدل PaginationParams لأن PageSize فيه محدود بـ 100 —
        //     والتقرير يحتاج جميع طلبات السائق دفعة واحدة.
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
            .Select(o => MapToOrderDto(o, hidePricing: false));
    }
}
