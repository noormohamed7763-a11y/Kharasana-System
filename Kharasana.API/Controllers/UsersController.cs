using Asp.Versioning;
using Kharasana.Application.Common;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// إدارة مستخدمي النظام (عملاء، سائقون، موظفو مصانع) مع عزل بيانات المصانع
/// — موظف المصنع لا يرى ولا يعدّل إلا سائقي مصنعه.
/// </summary>
/// <remarks>
/// كل نقطة نهاية تستقبل جسماً هنا محميّة بـ <c>[ServiceFilter(typeof(ValidationFilter&lt;T&gt;))]</c>،
/// فيعمل <c>CreateUserDtoValidator</c> ورفاقه فعلياً قبل دخول الدالة.
/// <para>ملاحظة ترتيب مقصودة: الفلتر يعمل قبل جسم الدالة، فطلب دور <c>Admin</c> يُردّ
/// 400 من قاعدة <c>NotEqual(UserRole.Admin)</c> بدل 403 من الخدمة، بينما أي دور آخر
/// غير مسموح لموظف المصنع يبقى 403 من <c>Forbid()</c>. كلا الردّين رفض، والخدمة
/// تُعيد فحص الدور بنفسها (دفاع في العمق).</para>
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // حد أدنى: توكن صالح
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // ============================================================
    // 1. GET ALL - عرض المستخدمين مع عزل المصانع
    // ============================================================
    /// <summary>جلب قائمة المستخدمين مع فلاتر (الدور والمصنع وحالة السائق) وترقيم الصفحات.</summary>
    /// <remarks>موظف المصنع يرى سائقي مصنعه فقط (عزل إجباري).</remarks>
    /// <param name="role">الدور لتصفية القائمة.</param>
    /// <param name="factoryId">المصنع لتصفية القائمة (للمدير فقط).</param>
    /// <param name="driverStatus">حالة السائق لتصفية القائمة.</param>
    /// <param name="isActive">تصفية الحسابات النشطة/الموقوفة — تُصفّى على الخادم.</param>
    /// <param name="pagination">خيارات الترقيم.</param>
    /// <response code="200">تم جلب المستخدمين بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserRole? role,
        [FromQuery] int? factoryId,
        [FromQuery] DriverStatus? driverStatus,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationParams pagination)
    {
        var caller = User.GetCallerContext();

        // ✅ عزل إجباري: الموظف لا يرى إلا مصنعه
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            factoryId = caller.FactoryId;
        }

        var result = await _userService.GetPagedAsync(role, factoryId, driverStatus, pagination, isActive);

        return Ok(new ApiResponse<PagedResult<UserDto>>
        {
            Success = true,
            Message = Messages.UsersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 2. GET BY ID - عرض مستخدم مع عزل المصانع
    // ============================================================
    /// <summary>جلب تفاصيل مستخدم بمعرّفه.</summary>
    /// <remarks>المدير أي مستخدم؛ موظف المصنع سائقو مصنعه فقط.</remarks>
    /// <param name="id">معرّف المستخدم.</param>
    /// <response code="200">تم جلب المستخدم بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">المستخدم غير موجود أو لا ينتمي لمصنع الموظف.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetById(int id)
    {
        var caller = User.GetCallerContext();

        // ✅ للمدير: يمكنه رؤية أي مستخدم
        if (caller.Role == UserRole.Admin)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = Messages.UserRetrievedSuccessfully,
                Data = user
            });
        }

        // ✅ لموظف المصنع: يتحقق من أن المستخدم سائق ويتبع مصنعه
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        var driver = await GetDriverForCurrentFactoryAsync(id, caller.FactoryId.Value);
        if (driver == null)
        {
            return NotFound(ApiResponse.Fail(Messages.UserNotFound));
        }

        return Ok(new ApiResponse<UserDto>
        {
            Success = true,
            Message = Messages.UserRetrievedSuccessfully,
            Data = driver
        });
    }

    // ============================================================
    // 3. CREATE - إنشاء مستخدم جديد
    // ============================================================
    /// <summary>إنشاء مستخدم جديد بأي دور (للمدير) أو سائق فقط (لموظف المصنع).</summary>
    /// <remarks>موظف المصنع يُجبر FactoryId على مصنعه من التوكن.</remarks>
    /// <param name="dto">بيانات المستخدم الجديد.</param>
    /// <response code="201">تم إنشاء المستخدم بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">موظف المصنع يحاول إنشاء دور غير السائق.</response>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<CreateUserDto>))]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var caller = User.GetCallerContext();

        // ✅ للمدير: يمكنه إنشاء أي مستخدم
        if (caller.Role == UserRole.Admin)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetById),
                new { id = user.UserId },
                new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = Messages.CreatedSuccessfully,
                    Data = user
                });
        }

        // ✅ لموظف المصنع: يسمح فقط بإنشاء سائق
        if (dto.Role != UserRole.Driver)
        {
            return Forbid();
        }

        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        // ✅ فرض FactoryId من التوكن
        dto.FactoryId = caller.FactoryId.Value;

        var newUser = await _userService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = newUser.UserId },
            new ApiResponse<UserDto>
            {
                Success = true,
                Message = Messages.CreatedSuccessfully,
                Data = newUser
            });
    }

    // ============================================================
    // 4. UPDATE - تعديل مستخدم
    // ============================================================
    /// <summary>تعديل بيانات مستخدم موجود.</summary>
    /// <remarks>المدير أي مستخدم؛ موظف المصنع سائقو مصنعه فقط.</remarks>
    /// <param name="id">معرّف المستخدم.</param>
    /// <param name="dto">البيانات الجديدة للمستخدم.</param>
    /// <response code="200">تم التعديل بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">المستخدم غير موجود أو لا ينتمي لمصنع الموظف.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<UpdateUserDto>))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var caller = User.GetCallerContext();

        // ✅ للمدير: يمكنه تعديل أي مستخدم
        if (caller.Role == UserRole.Admin)
        {
            await _userService.UpdateAsync(id, dto);
            return Ok(ApiResponse.Ok(Messages.UpdatedSuccessfully));
        }

        // ✅ لموظف المصنع: يتحقق من أن المستخدم سائق ويتبع مصنعه
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        var driver = await GetDriverForCurrentFactoryAsync(id, caller.FactoryId.Value);
        if (driver == null)
        {
            return NotFound(ApiResponse.Fail(Messages.UserNotFound));
        }

        // ✅ موظف المصنع يسمح فقط بتعديل السائقين
        if (dto.Role != UserRole.Driver)
        {
            return Forbid();
        }

        dto.FactoryId = caller.FactoryId.Value;

        await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse.Ok(Messages.UpdatedSuccessfully));
    }

    // ============================================================
    // 5. UPDATE MY PROFILE - تعديل الملف الشخصي
    // ============================================================
    /// <summary>تعديل الملف الشخصي للمستخدم الحالي.</summary>
    /// <param name="dto">البيانات الجديدة للملف الشخصي.</param>
    /// <response code="200">تم التعديل بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpPut("me")]
    [Authorize]
    [ServiceFilter(typeof(ValidationFilter<UpdateMyProfileDto>))]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto dto)
    {
        var caller = User.GetCallerContext();

        await _userService.UpdateMyProfileAsync(caller.UserId, dto);

        return Ok(ApiResponse.Ok(Messages.UpdatedSuccessfully));
    }

    // ============================================================
    // 6. DELETE - حذف مستخدم
    // ============================================================
    /// <summary>حذف مستخدم.</summary>
    /// <remarks>المدير أي مستخدم؛ موظف المصنع سائقو مصنعه فقط.</remarks>
    /// <param name="id">معرّف المستخدم.</param>
    /// <response code="200">تم الحذف بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">المستخدم غير موجود أو لا ينتمي لمصنع الموظف.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();

        // ✅ للمدير: يمكنه حذف أي مستخدم
        if (caller.Role == UserRole.Admin)
        {
            await _userService.DeleteAsync(id);
            return Ok(ApiResponse.Ok(Messages.DeletedSuccessfully));
        }

        // ✅ لموظف المصنع: يتحقق من أن المستخدم سائق ويتبع مصنعه
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        var driver = await GetDriverForCurrentFactoryAsync(id, caller.FactoryId.Value);
        if (driver == null)
        {
            return NotFound(ApiResponse.Fail(Messages.UserNotFound));
        }

        await _userService.DeleteAsync(id);
        return Ok(ApiResponse.Ok(Messages.DeletedSuccessfully));
    }

    // ============================================================
    // 7. UPDATE DRIVER STATUS - تحديث حالة السائق
    // ============================================================
    /// <summary>تحديث حالة عمل السائق (متاح/مشغول/غير متاح...).</summary>
    /// <remarks>عند FactoryEmployee تتأكد الخدمة من انتماء السائق لمصنعه.</remarks>
    /// <param name="id">معرّف السائق.</param>
    /// <param name="dto">الحالة الجديدة.</param>
    /// <response code="200">تم تحديث الحالة بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">السائق غير موجود.</response>
    [HttpPut("{id:int}/driver-status")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<UpdateDriverStatusDto>))]
    public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] UpdateDriverStatusDto dto)
    {
        var caller = User.GetCallerContext();

        // ✅ الخدمة ستتحقق من الصلاحيات (عند FactoryEmployee تتحقق من FactoryId)
        await _userService.UpdateDriverStatusAsync(id, dto, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.DriverStatusUpdatedSuccessfully));
    }

    // ============================================================
    // 8. TOGGLE ACTIVE - تفعيل/تعطيل حساب السائق
    // ============================================================
    /// <summary>تفعيل أو تعطيل حساب سائق (تبديل الحالة).</summary>
    /// <param name="id">معرّف السائق.</param>
    /// <response code="200">تم تبديل الحالة بنجاح — يرجع الحالة الجديدة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">السائق غير موجود.</response>
    [HttpPut("{id:int}/toggle-active")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var caller = User.GetCallerContext();

        var isActive = await _userService.ToggleDriverActiveAsync(id, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(isActive, isActive ? Messages.DriverActivatedSuccessfully : Messages.DriverDeactivatedSuccessfully));
    }

    // ============================================================
    // 9. HELPER - التحقق من أن المستخدم سائق ويتبع مصنع معين
    // ============================================================
    private async Task<UserDto?> GetDriverForCurrentFactoryAsync(int userId, int factoryId)
    {
        var user = await _userService.GetByIdAsync(userId);

        // ✅ التحقق: المستخدم موجود، سائق، ويتبع المصنع المطلوب
        if (user == null || user.Role != UserRole.Driver.ToString() || user.FactoryId != factoryId)
        {
            return null;
        }

        return user;
    }
}
