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

    /// <summary>
    /// جلب بيانات المصنع المرتبط بموظف المصنع الحالي (للعرض فقط).
    /// </summary>
    /// <remarks>لا توجد هنا أي إمكانية للتعديل؛ تحرير بيانات المصنع من صلاحيات Admin عبر نقاط المصانع.</remarks>
    /// <response code="200">تم جلب بيانات المصنع بنجاح.</response>
    /// <response code="401">لا يوجد مصنع مرتبط بالحساب.</response>
    [HttpGet]
    public async Task<IActionResult> GetMySettings()
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

        var factory = await _factoryService.GetByIdAsync(factoryId.Value);

        return Ok(new ApiResponse<FactoryDto>
        {
            Success = true,
            Message = Messages.FactorySettingsRetrievedSuccessfully,
            Data = factory
        });
    }

    /// <summary>
    /// رفع أو استبدال شعار المصنع الخاص بالموظف الحالي فقط.
    /// </summary>
    /// <remarks>يُرسل الملف بصيغة form-data ضمن حقل اسمه <c>file</c>.</remarks>
    /// <param name="file">ملف صورة الشعار الجديد.</param>
    /// <response code="200">تم رفع الشعار بنجاح — يرجع مسار الشعار الجديد.</response>
    /// <response code="400">لم يتم إرسال ملف صورة صالح.</response>
    /// <response code="401">لا يوجد مصنع مرتبط بالحساب.</response>
    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
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

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.InvalidLogoFile
            });
        }

        await using var stream = file.OpenReadStream();
        var logoPath = await _factoryService.UploadLogoAsync(factoryId.Value, stream, file.FileName, file.Length);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.FactoryLogoUploadedSuccessfully,
            Data = new { logo = logoPath }
        });
    }

    /// <summary>
    /// حذف شعار المصنع الخاص بالموظف الحالي فقط.
    /// </summary>
    /// <response code="200">تم حذف الشعار بنجاح.</response>
    /// <response code="401">لا يوجد مصنع مرتبط بالحساب.</response>
    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo()
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

        await _factoryService.DeleteLogoAsync(factoryId.Value);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.FactoryLogoDeletedSuccessfully,
            Data = null
        });
    }
}