using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// إدارة المصانع: قائمة وتفاصيل، إنشاء وتعديل، أرشفة واستعادة، وإدارة شعار كل مصنع.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FactoriesController : ControllerBase
{
    private readonly IFactoryService _factoryService;

    public FactoriesController(IFactoryService factoryService)
    {
        _factoryService = factoryService;
    }

    // ============================================================
    // ✅ GET ALL - تم إضافة Client للصلاحيات
    // ============================================================
    /// <summary>جلب قائمة المصانع بحسب صلاحية المتصل.</summary>
    /// <remarks>
    /// - <b>Admin:</b> جميع المصانع.
    /// - <b>FactoryEmployee:</b> مصنعه فقط.
    /// - <b>Client:</b> المصانع النشطة فقط.
    /// </remarks>
    /// <response code="200">تم جلب المصانع بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet]
    [Authorize(Roles = "Admin,FactoryEmployee,Client")]  // ✅ إضافة Client
    public async Task<IActionResult> GetAll()
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound });
        }

        // ============================================================
        // ✅ إذا كان المستخدم Client، أرجع جميع المصانع النشطة
        // ============================================================
        if (currentRole == UserRole.Client)
        {
            var allFactories = await _factoryService.GetAllAsync();
            var activeFactories = allFactories.Where(f => f.IsActive).ToList();

            return Ok(new ApiResponse<IEnumerable<FactoryDto>>
            {
                Success = true,
                Message = Messages.FactoriesRetrievedSuccessfully,
                Data = activeFactories
            });
        }

        // ============================================================
        // ✅ إذا كان FactoryEmployee، أرجع مصنعه فقط
        // ============================================================
        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = Messages.FactoryNotFoundForUser
                });
            }

            var ownFactory = await _factoryService.GetByIdAsync(factoryId.Value);
            return Ok(new ApiResponse<IEnumerable<FactoryDto>>
            {
                Success = true,
                Message = Messages.FactoriesRetrievedSuccessfully,
                Data = ownFactory == null ? Enumerable.Empty<FactoryDto>() : new[] { ownFactory }
            });
        }

        // ============================================================
        // ✅ Admin - جميع المصانع
        // ============================================================
        var factories = await _factoryService.GetAllAsync();
        return Ok(new ApiResponse<IEnumerable<FactoryDto>>
        {
            Success = true,
            Message = Messages.FactoriesRetrievedSuccessfully,
            Data = factories
        });
    }

    // ============================================================
    // ✅ GET ARCHIVED - للمدير فقط
    // ============================================================
    /// <summary>جلب قائمة المصانع المؤرشفة (المحذوفة).</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <response code="200">تم جلب المصانع المؤرشفة بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    [HttpGet("archived")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetArchived()
    {
        var factories = await _factoryService.GetArchivedAsync();
        return Ok(new ApiResponse<IEnumerable<FactoryDto>>
        {
            Success = true,
            Message = Messages.ArchivedFactoriesRetrievedSuccessfully,
            Data = factories
        });
    }

    // ============================================================
    // ✅ GET BY ID - تم إضافة Client للصلاحيات
    // ============================================================
    /// <summary>جلب تفاصيل مصنع واحد بمعرّفه.</summary>
    /// <remarks>
    /// - <b>Admin:</b> أي مصنع.
    /// - <b>FactoryEmployee:</b> مصنعه فقط، وإلا خطأ عمل.
    /// - <b>Client:</b> المصانع النشطة فقط.
    /// </remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <response code="200">تم جلب المصنع بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="404">المصنع غير موجود أو غير نشط (لعميل).</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,FactoryEmployee,Client")]  // ✅ إضافة Client
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound });
        }

        // ============================================================
        // ✅ Client يمكنه مشاهدة أي مصنع نشط
        // ============================================================
        if (currentRole == UserRole.Client)
        {
            var factory = await _factoryService.GetByIdAsync(id);
            if (factory == null || !factory.IsActive)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = Messages.FactoryNotFoundOrInactive
                });
            }
            return Ok(new ApiResponse<FactoryDto>
            {
                Success = true,
                Message = Messages.FactoryRetrievedSuccessfully,
                Data = factory
            });
        }

        // ============================================================
        // ✅ FactoryEmployee يمكنه مشاهدة مصنعه فقط
        // ============================================================
        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null || factoryId.Value != id)
                throw new Application.Common.Exceptions.BusinessException(Messages.FactoryEmployeeFactoryMismatch);
        }

        var factoryResult = await _factoryService.GetByIdAsync(id);
        return Ok(new ApiResponse<FactoryDto>
        {
            Success = true,
            Message = Messages.FactoryRetrievedSuccessfully,
            Data = factoryResult
        });
    }

    // ============================================================
    // ✅ CREATE - للمدير فقط
    // ============================================================
    /// <summary>إنشاء مصنع جديد.</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <param name="dto">بيانات المصنع الجديد.</param>
    /// <response code="201">تم إنشاء المصنع بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateFactoryDto dto)
    {
        var factory = await _factoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = factory.FactoryId }, new ApiResponse<FactoryDto>
        {
            Success = true,
            Message = Messages.CreatedSuccessfully,
            Data = factory
        });
    }

    // ============================================================
    // ✅ UPDATE - للمدير فقط
    // ============================================================
    /// <summary>تعديل بيانات مصنع موجود.</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <param name="dto">البيانات الجديدة للمصنع.</param>
    /// <response code="200">تم التعديل بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    /// <response code="404">المصنع غير موجود.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFactoryDto dto)
    {
        await _factoryService.UpdateAsync(id, dto);
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
    /// <summary>حذف (أرشفة) مصنع — يبقى قابلاً للاستعادة.</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <response code="200">تم الحذف بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _factoryService.DeleteAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.DeletedSuccessfully,
            Data = null
        });
    }

    // ============================================================
    // ✅ RESTORE - للمدير فقط
    // ============================================================
    /// <summary>استعادة مصنع مؤرشف (محذوف).</summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <response code="200">تمت الاستعادة بنجاح.</response>
    /// <response code="404">المصنع غير موجود.</response>
    [HttpPost("restore/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        await _factoryService.RestoreAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.FactoryRestoredSuccessfully,
            Data = null
        });
    }

    // ============================================================
    // ✅ UPLOAD LOGO - للمدير وموظف المصنع
    // ============================================================
    /// <summary>رفع أو استبدال شعار مصنع معيّن.</summary>
    /// <remarks>موظف المصنع لا يرفع إلا لمصنعه؛ المدير لأي مصنع. يُرسل الملف بصيغة form-data ضمن حقل <c>file</c>.</remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <param name="file">ملف صورة الشعار.</param>
    /// <response code="200">تم رفع الشعار بنجاح — يرجع المسار الجديد.</response>
    /// <response code="400">لم يتم إرسال ملف صالح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">غير مسموح لموظف المصنع برفع شعار مصنع آخر.</response>
    [HttpPost("{id:int}/logo")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound });
        }

        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null || factoryId.Value != id)
                throw new Application.Common.Exceptions.BusinessException(Messages.FactoryEmployeeFactoryMismatch);
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = Messages.InvalidLogoFile });
        }

        await using var stream = file.OpenReadStream();
        var logoPath = await _factoryService.UploadLogoAsync(id, stream, file.FileName, file.Length);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.FactoryLogoUploadedSuccessfully,
            Data = new { logo = logoPath }
        });
    }

    // ============================================================
    // ✅ DELETE LOGO - للمدير وموظف المصنع
    // ============================================================
    /// <summary>حذف شعار مصنع معيّن.</summary>
    /// <remarks>موظف المصنع لا يحذف إلا شعار مصنعه؛ المدير لأي مصنع.</remarks>
    /// <param name="id">معرّف المصنع.</param>
    /// <response code="200">تم حذف الشعار بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">غير مسموح لموظف المصنع بحذف شعار مصنع آخر.</response>
    [HttpDelete("{id:int}/logo")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> DeleteLogo(int id)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound });
        }

        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null || factoryId.Value != id)
                throw new Application.Common.Exceptions.BusinessException(Messages.FactoryEmployeeFactoryMismatch);
        }

        await _factoryService.DeleteLogoAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.FactoryLogoDeletedSuccessfully,
            Data = null
        });
    }
}