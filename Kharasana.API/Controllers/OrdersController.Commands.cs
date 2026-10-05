using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

// الجزء الخاص بالأوامر: إنشاء الطلب وتعديله وإنشاء الطلب الهاتفي وأرشفته.
// الحقول والبنّاء في OrdersController.cs.
public partial class OrdersController
{
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
    [ServiceFilter(typeof(ValidationFilter<CreateOrderDto>))]
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
    /// <remarks>
    /// موظف المصنع يُنشئ الطلب لمصنعه تلقائياً؛ المدير يحدّد المصنع في الحمولة.
    /// إن لم يكن رقم العميل مسجَّلاً يُنشأ له حساب، وتُعاد <c>newClientTemporaryPassword</c>
    /// في الاستجابة <b>مرة واحدة فقط</b> لتسليمها للعميل — لا تُخزَّن نصاً صريحاً ولا تُعاد لاحقاً.
    /// </remarks>
    /// <param name="dto">بيانات الطلب الهاتفي.</param>
    /// <response code="201">تم إنشاء الطلب بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpPost("phone-order")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<PhoneOrderDto>))]
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
            new { id = result.Order.OrderId },
            new ApiResponse<PhoneOrderResultDto>
            {
                Success = true,
                Message = Messages.OrderCreatedSuccess,
                Data = result
            });
    }

    // ============================================================
    // 16. DELETE (Soft Delete)
    // ============================================================
    /// <summary>
    /// حذف ناعم (أرشفة) للطلب.
    /// </summary>
    /// <param name="id">معرّف الطلب.</param>
    /// <response code="200">تم أرشفة الطلب بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير أو موظف المصنع.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();

        await _orderService.DeleteOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.OrderArchivedSuccessfully));
    }
}
