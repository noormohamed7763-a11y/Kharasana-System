using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConcreteTypesController : ControllerBase
{
    private readonly IConcreteTypeService _concreteTypeService;

    public ConcreteTypesController(IConcreteTypeService concreteTypeService)
    {
        _concreteTypeService = concreteTypeService;
    }

    // ============================================================
    // ✅ GET ALL - تم إضافة Client للصلاحيات
    // ============================================================
    [HttpGet]
    [Authorize(Roles = "Admin,FactoryEmployee,Client")]  // ✅ إضافة Client
    public async Task<IActionResult> GetAll()
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

        // ============================================================
        // ✅ Client - جميع أنواع الخرسانة النشطة
        // ============================================================
        if (currentRole == UserRole.Client)
        {
            var allTypes = await _concreteTypeService.GetAllAsync(null);
            var activeTypes = allTypes.Where(t => t.IsActive).ToList();

            return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
            {
                Success = true,
                Message = "تم جلب أنواع الخرسانة بنجاح.",
                Data = activeTypes
            });
        }

        // ============================================================
        // ✅ FactoryEmployee - أنواع الخرسانة لمصنعه فقط
        // ============================================================
        if (currentRole == UserRole.FactoryEmployee)
        {
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

            var factoryTypes = await _concreteTypeService.GetAllAsync(factoryId);
            return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
            {
                Success = true,
                Message = "تم جلب أنواع الخرسانة بنجاح.",
                Data = factoryTypes
            });
        }

        // ============================================================
        // ✅ Admin - جميع أنواع الخرسانة (بما في ذلك غير النشطة)
        // ============================================================
        var concreteTypes = await _concreteTypeService.GetAllAsync(null);
        return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
        {
            Success = true,
            Message = Messages.ConcreteTypesRetrievedSuccessfully,
            Data = concreteTypes
        });
    }

    // ============================================================
    // ✅ GET BY ID - تم إضافة Client للصلاحيات
    // ============================================================
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,FactoryEmployee,Client")]  // ✅ إضافة Client
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

        var concreteType = await _concreteTypeService.GetByIdAsync(id);
        if (concreteType == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "نوع الخرسانة غير موجود.",
                Data = null
            });
        }

        // ============================================================
        // ✅ Client - يمكنه مشاهدة أي نوع خرسانة نشط
        // ============================================================
        if (currentRole == UserRole.Client)
        {
            if (!concreteType.IsActive)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "نوع الخرسانة غير نشط.",
                    Data = null
                });
            }
            return Ok(new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.ConcreteTypeRetrievedSuccessfully,
                Data = concreteType
            });
        }

        // ============================================================
        // ✅ FactoryEmployee - يمكنه مشاهدة أنواع خرسانة مصنعه فقط
        // ============================================================
        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null || concreteType.FactoryId != factoryId)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "نوع الخرسانة غير موجود أو لا ينتمي لمصنعك.",
                    Data = null
                });
            }
            return Ok(new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.ConcreteTypeRetrievedSuccessfully,
                Data = concreteType
            });
        }

        // ============================================================
        // ✅ Admin - يمكنه مشاهدة أي نوع خرسانة
        // ============================================================
        return Ok(new ApiResponse<ConcreteTypeDto>
        {
            Success = true,
            Message = Messages.ConcreteTypeRetrievedSuccessfully,
            Data = concreteType
        });
    }

    // ============================================================
    // ✅ CREATE - للمدير وموظف المصنع
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Create([FromBody] CreateConcreteTypeDto dto)
    {
        int? currentFactoryId = null;

        if (User.TryGetRole(out var currentRole) && currentRole == UserRole.FactoryEmployee)
        {
            currentFactoryId = User.GetFactoryId();
            if (currentFactoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = Messages.FactoryNotFoundForUser,
                    Data = null
                });
            }
        }

        var concreteType = await _concreteTypeService.CreateAsync(dto, currentFactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = concreteType.ConcreteTypeId },
            new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.CreatedSuccessfully,
                Data = concreteType
            });
    }

    // ============================================================
    // ✅ UPDATE - للمدير وموظف المصنع
    // ============================================================
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConcreteTypeDto dto)
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

        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            var concreteType = await _concreteTypeService.GetByIdAsync(id);

            if (concreteType == null || concreteType.FactoryId != factoryId)
            {
                throw new Application.Common.Exceptions.BusinessException(
                    "لا يمكنك تعديل نوع خرسانة لا ينتمي لمصنعك.");
            }
        }

        await _concreteTypeService.UpdateAsync(id, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.UpdatedSuccessfully,
            Data = null
        });
    }

    // ============================================================
    // ✅ DELETE - للمدير فقط
    // ============================================================
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _concreteTypeService.DeleteAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.DeletedSuccessfully,
            Data = null
        });
    }
}