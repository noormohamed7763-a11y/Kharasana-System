using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kharasana.Application.DTOs.Customer;

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

    // ------------------------------------------------------------
    // Helper مشترك: استخراج هوية المستدعي
    // ------------------------------------------------------------
    private bool TryGetCallerContext(out int callerId, out UserRole callerRole, out int? callerFactoryId, out IActionResult? error)
    {
        callerId = 0;
        callerRole = default;
        callerFactoryId = null;
        error = null;

        if (!User.TryGetRole(out callerRole))
        {
            error = Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
            return false;
        }

        var uid = User.GetUserId();
        if (uid == null)
        {
            error = Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserIdNotFound, Data = null });
            return false;
        }

        callerId = uid.Value;
        callerFactoryId = User.GetFactoryId();
        return true;
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
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        int? factoryId = null, clientId = null, driverId = null;

        switch (callerRole)
        {
            case UserRole.FactoryEmployee:
                if (callerFactoryId == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
                }
                factoryId = callerFactoryId;
                break;

            case UserRole.Client:
                clientId = callerId;
                break;

            case UserRole.Driver:
                driverId = callerId;
                break;

            case UserRole.Admin:
                // المدير يرى كل الطلبات، ويمكنه تضييق النطاق عبر فلتر المصنع في القائمة
                factoryId = pagination.FactoryId;
                break;
        }

        var result = await _orderService.GetPagedAsync(factoryId, clientId, driverId, callerRole, pagination);

        return Ok(new ApiResponse<PagedResult<OrderDto>>
        {
            Success = true,
            Message = Messages.OrdersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 2. GET CUSTOMERS
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> GetCustomers([FromQuery] int? factoryId, [FromQuery] PaginationParams pagination)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
        }

        if (currentRole == UserRole.FactoryEmployee)
        {
            var callerFactoryId = User.GetFactoryId();
            if (callerFactoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
            }
            factoryId = callerFactoryId;
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
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var order = await _orderService.GetByIdAsync(id, callerId, callerRole, callerFactoryId);

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
    [Authorize(Roles = "Client,Admin,FactoryEmployee")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var result = await _orderService.CreateAsync(dto, callerId, callerRole, callerFactoryId);

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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var result = await _orderService.UpdateOrderAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<OrderDto>
        {
            Success = true,
            Message = "تم تحديث الطلب بنجاح.",
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> CreatePhoneOrder([FromBody] PhoneOrderDto dto)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
        }

        int employeeFactoryId;

        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
            }
            employeeFactoryId = factoryId.Value;
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> SetPrice(int id, [FromBody] SetPriceDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.SetPriceAsync(id, dto.UnitPrice, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم حفظ السعر بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Approve(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.ApproveOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تمت موافقة العميل بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.UpdateStatusAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = Messages.OrderStatusUpdated, Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.AssignDriverAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = Messages.DriverAssignedSuccessfully, Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> StartDelivery(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.StartDeliveryAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم بدء التوصيل بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> Deliver(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.DeliverOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم تسليم الطلب بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Close(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.CloseOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم إغلاق الطلب بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.RejectOrderAsync(id, dto.Reason, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم رفض الطلب بنجاح.", Data = null });
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
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.CancelOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم إلغاء الطلب بنجاح.", Data = null });
    }
}

public class RejectOrderDto
{
    public string? Reason { get; set; }
}