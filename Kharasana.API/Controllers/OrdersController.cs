using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// إدارة طلبات الخرسانة بدورة حياتها الكاملة: إنشاء، تسعير، موافقة عميل،
/// إسناد سائق، توصيل، إغلاق، رفض وإلغاء — مع عزل البيانات بحسب دور المتصل.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

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

        var result = await _orderService.GetPagedAsync(factoryId, clientId, driverId, caller.Role, pagination);

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

        var orders = await _orderService.GetOrdersByDriverIdAsync(driverId, caller.FactoryId, caller.Role);

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

    // ============================================================
    // 4. CREATE
    // ============================================================
    /// <summary>إنشاء طلب جديد (يُنشئه العميل بنفسه، أو المصنع/المدير بالنيابة).</summary>
    /// <param name="dto">بيانات الطلب الجديد.</param>
    /// <response code="201">تم إنشاء الطلب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClient)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var caller = User.GetCallerContext();

        var result = await _orderService.CreateAsync(dto, caller.UserId, caller.Role, caller.FactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.OrderId },
            new ApiResponse<OrderDetailsDto>
            {
                Success = true,
                Message = Messages.OrderCreatedSuccess,
                Data = result
            });
    }

    // ============================================================
    // 5. UPDATE
    // ============================================================
    /// <summary>تعديل بيانات طلب موجود.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="dto">البيانات الجديدة للطلب.</param>
    /// <response code="200">تم تحديث الطلب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<UpdateOrderDto>))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        var caller = User.GetCallerContext();

        var result = await _orderService.UpdateOrderAsync(id, dto, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<OrderDto>
        {
            Success = true,
            Message = Messages.OrderUpdatedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 6. CREATE PHONE ORDER
    // ============================================================
    /// <summary>إنشاء طلب هاتفي بالنيابة عن عميل (يستخدمه المصنع/المدير).</summary>
    /// <remarks>موظف المصنع يُنشئ الطلب لمصنعه تلقائياً؛ المدير يحدّد المصنع في الحمولة.</remarks>
    /// <param name="dto">بيانات الطلب الهاتفي.</param>
    /// <response code="201">تم إنشاء الطلب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpPost("phone-order")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> CreatePhoneOrder([FromBody] PhoneOrderDto dto)
    {
        var caller = User.GetCallerContext();

        int employeeFactoryId;

        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            employeeFactoryId = caller.FactoryId.Value;
        }
        else
        {
            employeeFactoryId = dto.FactoryId;
        }

        var result = await _orderService.CreatePhoneOrderAsync(dto, employeeFactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.OrderId },
            new ApiResponse<OrderDetailsDto>
            {
                Success = true,
                Message = Messages.OrderCreatedSuccess,
                Data = result
            });
    }

    // ============================================================
    // 7. SET PRICE
    // ============================================================
    /// <summary>حفظ/تحديث سعر الطلب.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="dto">بيانات السعر.</param>
    /// <response code="200">تم حفظ السعر بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}/price")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> SetPrice(int id, [FromBody] SetPriceDto dto)
    {
        var caller = User.GetCallerContext();

        await _orderService.SetPriceAsync(id, dto.UnitPrice, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.PriceSavedSuccessfully));
    }

    // ============================================================
    // 8. APPROVE
    // ============================================================
    /// <summary>الموافقة على طلب (اعتماد السعر من جهة المصنع).</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تمت الموافقة بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Approve(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.ApproveOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderApprovedSuccessfully));
    }

    // ============================================================
    // 9. UPDATE STATUS
    // ============================================================
    /// <summary>تحديث حالة الطلب يدوياً (للمدير فقط).</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="dto">الحالة الجديدة للطلب.</param>
    /// <response code="200">تم تحديث الحالة بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var caller = User.GetCallerContext();

        await _orderService.UpdateStatusAsync(id, dto, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderStatusUpdated));
    }

    // ============================================================
    // 10. ASSIGN DRIVER ✅
    // ============================================================
    /// <summary>إسناد طلب إلى سائق معيّن.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="dto">بيانات الإسناد (معرّف السائق).</param>
    /// <response code="200">تم إسناد السائق بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب أو السائق غير موجود.</response>
    [HttpPut("{id:int}/assign-driver")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
    {
        var caller = User.GetCallerContext();

        await _orderService.AssignDriverAsync(id, dto, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.DriverAssignedSuccessfully));
    }

    // ============================================================
    // 11. START DELIVERY ✅ (تم إضافة Driver)
    // ============================================================
    /// <summary>بدء عملية التوصيل للطلب المسند للسائق.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم بدء التوصيل بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود أو غير مسند لهذا السائق.</response>
    [HttpPut("{id:int}/start-delivery")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]  // ✅ Driver مُضاف
    public async Task<IActionResult> StartDelivery(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.StartDeliveryAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.DeliveryStartedSuccessfully));
    }

    // ============================================================
    // 12. DELIVER ✅ (تم إضافة Driver)
    // ============================================================
    /// <summary>تسليم الطلب (تأكيد وصوله للعميل).</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم تسليم الطلب بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}/deliver")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]  // ✅ Driver مُضاف
    public async Task<IActionResult> Deliver(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.DeliverOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderDeliveredSuccessfully));
    }

    // ============================================================
    // 13. CLOSE
    // ============================================================
    /// <summary>إغلاق الطلب واستكماله (إنهاء دورة حياته).</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم إغلاق الطلب بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}/close")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Close(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.CloseOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderClosedSuccessfully));
    }

    // ============================================================
    // 14. REJECT
    // ============================================================
    /// <summary>رفض الطلب مع إمكانية إرفاق سبب الرفض.</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <param name="dto">سبب الرفض (اختياري).</param>
    /// <response code="200">تم رفض الطلب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderDto dto)
    {
        var caller = User.GetCallerContext();

        await _orderService.RejectOrderAsync(id, dto.Reason, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderRejectedSuccessfully));
    }

    // ============================================================
    // 15. CANCEL ✅ (تم إضافة Driver)
    // ============================================================
    /// <summary>إلغاء طلب (ممكن من كل الأطراف المعنية).</summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم إلغاء الطلب بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">الطلب غير موجود.</response>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClientOrDriver)]  // ✅ Driver مُضاف
    public async Task<IActionResult> Cancel(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.CancelOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderCancelledSuccessfully));
    }
}