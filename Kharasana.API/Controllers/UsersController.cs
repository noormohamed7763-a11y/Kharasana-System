using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

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
                    Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
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
                Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
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
                Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
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
                Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
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
                Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
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
            Message = isActive ? "تم تفعيل حساب السائق بنجاح." : "تم إيقاف حساب السائق بنجاح.",
            Data = isActive
        });
    }

    // ============================================================
    // 9. HELPER - التحقق من أن المستخدم سائق ويتبع مصنع معين
    // ============================================================
    private async Task<UserDto?> GetDriverForCurrentFactoryAsync(int userId, int factoryId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);

            // ✅ التحقق: المستخدم موجود، سائق، ويتبع المصنع المطلوب
            if (user == null || user.Role != UserRole.Driver.ToString() || user.FactoryId != factoryId)
            {
                return null;
            }

            return user;
        }
        catch
        {
            return null;
        }
    }
}