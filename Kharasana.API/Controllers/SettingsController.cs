using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// إعدادات خاصة بموظف المصنع: عرض بيانات مصنعه (للقراءة فقط) وإدارة شعار المصنع.
/// لا يوجد أي إمكانية لتعديل بيانات المصنع الأساسية هنا؛ هذا من صلاحيات Admin فقط عبر FactoriesController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "FactoryEmployee")]
public class SettingsController : ControllerBase
{
    private readonly IFactoryService _factoryService;

    public SettingsController(IFactoryService factoryService)
    {
        _factoryService = factoryService;
    }

    // GET /api/Settings
    // يرجع بيانات المصنع المرتبط بموظف المصنع الحالي (للعرض فقط).
    [HttpGet]
    public async Task<IActionResult> GetMySettings()
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

        var factory = await _factoryService.GetByIdAsync(factoryId.Value);

        return Ok(new ApiResponse<FactoryDto>
        {
            Success = true,
            Message = "تم جلب بيانات المصنع بنجاح.",
            Data = factory
        });
    }

    // POST /api/Settings/logo
    // رفع أو استبدال شعار المصنع الخاص بالموظف الحالي فقط.
    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
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

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "يرجى اختيار ملف صورة صالح."
            });
        }

        await using var stream = file.OpenReadStream();
        var logoPath = await _factoryService.UploadLogoAsync(factoryId.Value, stream, file.FileName, file.Length);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "تم رفع شعار المصنع بنجاح.",
            Data = new { logo = logoPath }
        });
    }

    // DELETE /api/Settings/logo
    // حذف شعار المصنع الخاص بالموظف الحالي فقط.
    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo()
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

        await _factoryService.DeleteLogoAsync(factoryId.Value);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "تم حذف شعار المصنع بنجاح.",
            Data = null
        });
    }
}