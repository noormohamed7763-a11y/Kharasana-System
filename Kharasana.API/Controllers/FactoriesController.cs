using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

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
                    Message = "لم يتم العثور على المصنع المرتبط بالمستخدم."
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
    [HttpGet("archived")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetArchived()
    {
        var factories = await _factoryService.GetArchivedAsync();
        return Ok(new ApiResponse<IEnumerable<FactoryDto>>
        {
            Success = true,
            Message = "تم جلب المصانع المؤرشفة بنجاح",
            Data = factories
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
                    Message = "المصنع غير موجود أو غير نشط."
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
    [HttpPost("restore/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        await _factoryService.RestoreAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "تم استعادة المصنع بنجاح.",
            Data = null
        });
    }

    // ============================================================
    // ✅ UPLOAD LOGO - للمدير وموظف المصنع
    // ============================================================
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
            return BadRequest(new ApiResponse<object> { Success = false, Message = "يرجى اختيار ملف صورة صالح." });
        }

        await using var stream = file.OpenReadStream();
        var logoPath = await _factoryService.UploadLogoAsync(id, stream, file.FileName, file.Length);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "تم رفع شعار المصنع بنجاح.",
            Data = new { logo = logoPath }
        });
    }

    // ============================================================
    // ✅ DELETE LOGO - للمدير وموظف المصنع
    // ============================================================
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
            Message = "تم حذف شعار المصنع بنجاح.",
            Data = null
        });
    }
}