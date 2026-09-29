using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

// الجزء الخاص بدورة حياة الطلب: التسعير والموافقة وتغيير الحالة والإسناد
// وبدء التوصيل والتسليم والإغلاق والرفض والإلغاء.
// الحقول والبنّاء في OrdersController.cs.
public partial class OrdersController
{
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
    [ServiceFilter(typeof(ValidationFilter<SetPriceDto>))]
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
    [ServiceFilter(typeof(ValidationFilter<UpdateOrderStatusDto>))]
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
    [ServiceFilter(typeof(ValidationFilter<AssignDriverDto>))]
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
    [ServiceFilter(typeof(ValidationFilter<RejectOrderDto>))]
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
