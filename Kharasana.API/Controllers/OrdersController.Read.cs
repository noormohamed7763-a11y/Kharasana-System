using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

// الجزء الخاص بالقراءة: القوائم والتفاصيل والعملاء.
// الحقول والبنّاء في OrdersController.cs.
public partial class OrdersController
{
    // ============================================================
    // 1. GET ALL
    // ============================================================
    /// <summary>جلب قائمة الطلبات مع ترقيم الصفحات وصفّ البيانات بِناءً على الدور.</summary>
    /// <remarks>
    /// - <b>Admin:</b> كل الطلبات (يمكن تضييق النطاق عبر FactoryId).
    /// - <b>FactoryEmployee:</b> طلبات مصنعه فقط.
    /// - <b>Client:</b> طلباته فقط.
    /// - <b>Driver:</b> الطلبات المسندة له فقط.
    /// </remarks>
    /// <param name="pagination">خيارات الترقيم (الصفحة والحجم والفلاتر الاختيارية).</param>
    /// <response code="200">تم جلب الطلبات بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح أو لا يوجد مصنع مرتبط.</response>
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClientOrDriver)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var caller = User.GetCallerContext();

        int? factoryId = null, clientId = null, driverId = null;

        switch (caller.Role)
        {
            case UserRole.FactoryEmployee:
                if (caller.FactoryId is null)
                    throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
                factoryId = caller.FactoryId;
                break;

            case UserRole.Client:
                clientId = caller.UserId;
                break;

            case UserRole.Driver:
                driverId = caller.UserId;
                break;

            case UserRole.Admin:
                // المدير يرى كل الطلبات، ويمكنه تضييق النطاق عبر فلتر المصنع في القائمة
                factoryId = pagination.FactoryId;
                break;
        }

        var result = await _orderService.GetPagedAsync(factoryId, clientId, driverId, caller, pagination);

        return Ok(new ApiResponse<PagedResult<OrderDto>>
        {
            Success = true,
            Message = Messages.OrdersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 2. GET BY DRIVER (تقرير سائق)
    // ============================================================
    /// <summary>جلب جميع طلبات سائق محدّد لطباعة تقرير — مرتبة تنازلياً بتاريخ الإنشاء.</summary>
    /// <remarks>
    /// - <b>Admin:</b> يرى طلبات أي سائق.
    /// - <b>FactoryEmployee:</b> يرى طلبات سائقي مصنعه فقط.
    /// </remarks>
    /// <param name="driverId">معرّف السائق.</param>
    /// <response code="200">تم جلب الطلبات بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">المتصل لا يملك صلاحية، أو السائق خارج نطاق مصنع موظف المصنع.</response>
    [HttpGet("by-driver/{driverId:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetByDriver(int driverId)
    {
        var caller = User.GetCallerContext();

        var orders = await _orderService.GetOrdersByDriverIdAsync(driverId, caller);

        return Ok(new ApiResponse<IEnumerable<OrderDto>>
        {
            Success = true,
            Message = Messages.OrdersRetrievedSuccessfully,
            Data = orders
        });
    }

    // ============================================================
    // 3. GET CUSTOMERS
    // ============================================================
    /// <summary>جلب قائمة العملاء الذين لديهم طلبات عند مصنع معيّن.</summary>
    /// <remarks>
    /// - <b>Admin:</b> يحدّد المصنع عبر factoryId (اختياري — كل المصانع بدونه).
    /// - <b>FactoryEmployee:</b> مصنعه تلقائياً ويُتجاهَل أي factoryId مرسَل.
    /// </remarks>
    /// <param name="factoryId">معرّف المصنع (اختياري للمدير).</param>
    /// <param name="pagination">خيارات الترقيم.</param>
    /// <response code="200">تم جلب العملاء بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet("customers")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetCustomers([FromQuery] int? factoryId, [FromQuery] PaginationParams pagination)
    {
        var caller = User.GetCallerContext();

        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            factoryId = caller.FactoryId;
        }

        var result = await _orderService.GetCustomersAsync(factoryId, pagination);

        return Ok(new ApiResponse<PagedResult<CustomerSummaryDto>>
        {
            Success = true,
            Message = Messages.CustomersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 3.b GET CUSTOMER (عميل واحد)
    // ============================================================
    /// <summary>جلب ملخص عميل واحد: إحصاءات طلباته وبيانات حسابه.</summary>
    /// <remarks>
    /// - <b>Admin:</b> يحدّد المصنع عبر factoryId (اختياري — كل المصانع بدونه).
    /// - <b>FactoryEmployee:</b> مصنعه تلقائياً ويُتجاهَل أي factoryId مرسل.
    /// </remarks>
    /// <param name="userId">معرّف العميل.</param>
    /// <param name="factoryId">معرّف المصنع (اختياري للمدير).</param>
    /// <response code="200">تم جلب ملخص العميل بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">لا توجد طلبات لهذا العميل في النطاق المطلوب.</response>
    [HttpGet("customers/{userId:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetCustomer(int userId, [FromQuery] int? factoryId)
    {
        // ✅ نفس عزل GetCustomers: الموظف محصور في مصنعه بلا استثناء
        var caller = User.GetCallerContext();

        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            factoryId = caller.FactoryId;
        }

        var customer = await _orderService.GetCustomerSummaryAsync(userId, factoryId);

        if (customer == null)
            throw new NotFoundException(Messages.CustomerNotFound);

        return Ok(new ApiResponse<CustomerSummaryDto>
        {
            Success = true,
            Message = Messages.CustomersRetrievedSuccessfully,
            Data = customer
        });
    }

    // ============================================================
    // 3. GET BY ID
    // ============================================================
    /// <summary>جلب تفاصيل طلب واحد مع التحقق من صلاحية المتصل لعرضه.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم جلب الطلب بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود أو لا يخص المتصل.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClientOrDriver)]
    public async Task<IActionResult> GetById(int id)
    {
        var caller = User.GetCallerContext();

        var order = await _orderService.GetByIdAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<OrderDetailsDto>
        {
            Success = true,
            Message = Messages.OrderRetrievedSuccessfully,
            Data = order
        });
    }
}
