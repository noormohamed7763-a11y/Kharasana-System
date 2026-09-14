using Kharasana.API.Extensions;
using Kharasana.Application.Common;
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
[ApiController]
[Route("api/[controller]")]
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
    /// <param name="pagination">خيارات الترقيم.</param>
    /// <response code="200">تم جلب المستخدمين بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserRole? role,
        [FromQuery] int? factoryId,
        [FromQuery] DriverStatus? driverStatus,
        [FromQuery] PaginationParams pagination)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        // ✅ عزل إجباري: الموظف لا يرى إلا مصنعه
        if (currentRole == UserRole.FactoryEmployee)
        {
            var callerFactoryId = User.GetFactoryId();
            if (callerFactoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = Messages.FactoryNotFoundForUser,
                    Data = null
                });
            }
            factoryId = callerFactoryId;
        }

        var result = await _userService.GetPagedAsync(role, factoryId, driverStatus, pagination);

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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        // ✅ للمدير: يمكنه رؤية أي مستخدم
        if (currentRole == UserRole.Admin)
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
        var factoryId = User.GetFactoryId();
        if (factoryId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.FactoryNotFoundForUser,
                Data = null
            });
        }

        var driver = await GetDriverForCurrentFactoryAsync(id, factoryId.Value);
        if (driver == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserNotFound,
                Data = null
            });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        // ✅ للمدير: يمكنه إنشاء أي مستخدم
        if (currentRole == UserRole.Admin)
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

        var factoryId = User.GetFactoryId();
        if (factoryId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.FactoryNotFoundForUser,
                Data = null
            });
        }

        // ✅ فرض FactoryId من التوكن
        dto.FactoryId = factoryId.Value;

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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        // ✅ للمدير: يمكنه تعديل أي مستخدم
        if (currentRole == UserRole.Admin)
        {
            await _userService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = Messages.UpdatedSuccessfully,
                Data = null
            });
        }

        // ✅ لموظف المصنع: يتحقق من أن المستخدم سائق ويتبع مصنعه
        var factoryId = User.GetFactoryId();
        if (factoryId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.FactoryNotFoundForUser,
                Data = null
            });
        }

        var driver = await GetDriverForCurrentFactoryAsync(id, factoryId.Value);
        if (driver == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserNotFound,
                Data = null
            });
        }

        // ✅ موظف المصنع يسمح فقط بتعديل السائقين
        if (dto.Role != UserRole.Driver)
        {
            return Forbid();
        }

        dto.FactoryId = factoryId.Value;

        await _userService.UpdateAsync(id, dto);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.UpdatedSuccessfully,
            Data = null
        });
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
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserIdNotFound,
                Data = null
            });
        }

        await _userService.UpdateMyProfileAsync(userId.Value, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.UpdatedSuccessfully,
            Data = null
        });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        // ✅ للمدير: يمكنه حذف أي مستخدم
        if (currentRole == UserRole.Admin)
        {
            await _userService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = Messages.DeletedSuccessfully,
                Data = null
            });
        }

        // ✅ لموظف المصنع: يتحقق من أن المستخدم سائق ويتبع مصنعه
        var factoryId = User.GetFactoryId();
        if (factoryId == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.FactoryNotFoundForUser,
                Data = null
            });
        }

        var driver = await GetDriverForCurrentFactoryAsync(id, factoryId.Value);
        if (driver == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserNotFound,
                Data = null
            });
        }

        await _userService.DeleteAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.DeletedSuccessfully,
            Data = null
        });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] UpdateDriverStatusDto dto)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        var callerFactoryId = User.GetFactoryId();

        // ✅ الخدمة ستتحقق من الصلاحيات (عند FactoryEmployee تتحقق من FactoryId)
        await _userService.UpdateDriverStatusAsync(id, dto, currentRole, callerFactoryId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.DriverStatusUpdatedSuccessfully,
            Data = null
        });
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
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        var callerFactoryId = User.GetFactoryId();

        var isActive = await _userService.ToggleDriverActiveAsync(id, currentRole, callerFactoryId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = isActive ? Messages.DriverActivatedSuccessfully : Messages.DriverDeactivatedSuccessfully,
            Data = isActive
        });
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