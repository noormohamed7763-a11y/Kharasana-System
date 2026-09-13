using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// أنواع الخرسانة لكل مصنع: قائمة وتفاصيل وإنشاء وتعديل وحذف.
/// </summary>
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
    /// <summary>جلب قائمة أنواع الخرسانة بحسب صلاحية المتصل.</summary>
    /// <remarks>
    /// - <b>Admin:</b> كل الأنواع (بما فيها غير النشطة).
    /// - <b>FactoryEmployee:</b> أنواع مصنعه فقط.
    /// - <b>Client:</b> الأنواع النشطة فقط.
    /// </remarks>
    /// <response code="200">تم جلب الأنواع بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
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
    /// <summary>جلب نوع خرسانة واحد بمعرّفه.</summary>
    /// <remarks>
    /// - <b>Admin:</b> أي نوع.
    /// - <b>FactoryEmployee:</b> أنواع مصنعه فقط.
    /// - <b>Client:</b> الأنواع النشطة فقط.
    /// </remarks>
    /// <param name="id">معرّف نوع الخرسانة.</param>
    /// <response code="200">تم جلب النوع بنجاح.</response>
    /// <response code="404">النوع غير موجود، أو غير نشط، أو لا ينتمي لمصنع الموظف.</response>
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
    /// <summary>إنشاء نوع خرسانة جديد.</summary>
    /// <remarks>موظف المصنع يُنشئ النوع لمصنعه تلقائياً؛ المدير يحدّد المصنع في الحمولة.</remarks>
    /// <param name="dto">بيانات النوع الجديد.</param>
    /// <response code="201">تم إنشاء النوع بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية الإنشاء.</response>
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
    /// <summary>تعديل نوع خرسانة موجود.</summary>
    /// <remarks>موظف المصنع لا يعدّل إلا نوعاً يتبع مصنعه.</remarks>
    /// <param name="id">معرّف النوع.</param>
    /// <param name="dto">البيانات الجديدة للنوع.</param>
    /// <response code="200">تم التعديل بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">النوع لا ينتمي لمصنع الموظف.</response>
    /// <response code="404">النوع غير موجود.</response>
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
    /// <summary>حذف نوع خرسانة.</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <param name="id">معرّف النوع.</param>
    /// <response code="200">تم الحذف بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
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