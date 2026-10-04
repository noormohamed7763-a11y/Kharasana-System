using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
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
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.FactoryEmployee)]
public class SettingsController : ControllerBase
{
    private readonly IFactoryService _factoryService;
    private readonly IImageCleanupService _imageCleanupService;

    public SettingsController(
        IFactoryService factoryService,
        IImageCleanupService imageCleanupService)
    {
        _factoryService = factoryService;
        _imageCleanupService = imageCleanupService;
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
        var caller = User.GetCallerContext();
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        var factory = await _factoryService.GetByIdAsync(caller.FactoryId.Value);

        return Ok(new ApiResponse<FactoryDto>
        {
            Success = true,
            Message = Messages.FactorySettingsRetrievedSuccessfully,
            Data = factory is null ? null : this.ToAbsoluteLogo(factory)
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
        var caller = User.GetCallerContext();
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail(Messages.InvalidLogoFile));
        }

        await using var stream = file.OpenReadStream();
        var logoPath = await _factoryService.UploadLogoAsync(caller.FactoryId.Value, stream, file.FileName, file.Length);

        return Ok(ApiResponse.Ok(new { logo = this.ToAbsoluteLogoUrl(logoPath) }, Messages.FactoryLogoUploadedSuccessfully));
    }

    /// <summary>
    /// حذف شعار المصنع الخاص بالموظف الحالي فقط.
    /// </summary>
    /// <response code="200">تم حذف الشعار بنجاح.</response>
    /// <response code="401">لا يوجد مصنع مرتبط بالحساب.</response>
    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo()
    {
        var caller = User.GetCallerContext();
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        await _factoryService.DeleteLogoAsync(caller.FactoryId.Value);

        return Ok(ApiResponse.Ok(Messages.FactoryLogoDeletedSuccessfully));
    }

    /// <summary>
    /// فحص مجلد الصور وحذف الملفات اليتيمة (غير المرتبطة بأي مصنع أو مستخدم).
    /// </summary>
    /// <remarks>
    /// تشغيل يدوي من صفحة الإعدادات. تُجرى العملية في الخادم المضيف للـ API
    /// حيث يقع مجلد wwwroot/Images فعليًا.
    /// </remarks>
    /// <response code="200">تم الفحص والحذف — يرجع عدد الملفات المحذوفة.</response>
    /// <response code="401">لا يوجد مصنع مرتبط بالحساب.</response>
    [HttpPost("cleanup-images")]
    public async Task<IActionResult> CleanupImages()
    {
        var caller = User.GetCallerContext();
        if (caller.FactoryId is null)
            throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

        var deletedCount = await _imageCleanupService.CleanupOrphanedImagesAsync();

        return Ok(ApiResponse.Ok(
            new { deletedCount },
            deletedCount == 0
                ? "لم يُعثر على ملفات صور يتيمة."
                : $"تم حذف {deletedCount} ملف صورة يتيم."));
    }
}