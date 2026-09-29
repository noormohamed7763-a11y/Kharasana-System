using Kharasana.Application.Common;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Drivers;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class DriversController : BaseController
{
    private readonly IDriverApiService _driverApiService;
    private readonly ILogger<DriversController> _logger;

    public DriversController(IDriverApiService driverApiService, ILogger<DriversController> logger)
    {
        _driverApiService = driverApiService;
        _logger = logger;
    }

    // ============================================================
    // INDEX
    // ============================================================
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20, string? search = null)
    {
        try
        {
            int? factoryId = RoleValue == UserRole.FactoryEmployee ? FactoryId : null;

            var pagedDrivers = await _driverApiService.GetDriversAsync(pageNumber, pageSize, search, factoryId);

            // أعداد الحالة عبر كل الصفحات — فشلها لا ينبغي أن يُسقط الصفحة بعد أن حمّلنا القائمة
            var counts = (Available: 0, Busy: 0, Offline: 0);
            if (pagedDrivers != null)
            {
                try
                {
                    counts = await _driverApiService.GetStatusCountsAsync(search, factoryId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "تعذر جلب إحصاءات حالة السائقين — ستُعرض البطاقات بقيمة صفر.");
                }
            }

            var vm = new DriversIndexViewModel
            {
                PagedDrivers = pagedDrivers,
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalDrivers = pagedDrivers?.TotalCount ?? 0,
                AvailableDrivers = counts.Available,
                BusyDrivers = counts.Busy,
                OfflineDrivers = counts.Offline
            };

            return View(vm);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل قائمة السائقين StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(new DriversIndexViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل قائمة السائقين");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(new DriversIndexViewModel());
        }
    }

    // ============================================================
    // DETAILS
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriversViewForbidden;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var driver = await _driverApiService.GetByIdAsync(id);

            if (driver == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

            if (IsFactoryIsolated(driver.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }

            return View(driver);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل بيانات السائق {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل بيانات السائق {DriverId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // CREATE (GET)
    // ============================================================
    [HttpGet]
    public IActionResult Create()
    {
        if (RoleValue != UserRole.FactoryEmployee || !FactoryId.HasValue)
        {
            TempData[TempDataError] = AppMessages.Common.DriverCreateFactoryEmployeeOnly;
            return RedirectToAction(nameof(Index));
        }

        var model = new CreateDriverViewModel
        {
            FactoryId = FactoryId.Value
        };

        return View(model);
    }

    // ============================================================
    // CREATE (POST)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDriverViewModel model)
    {
        if (RoleValue != UserRole.FactoryEmployee || !FactoryId.HasValue)
        {
            TempData[TempDataError] = AppMessages.Common.DriverCreateFactoryEmployeeOnly;
            return RedirectToAction(nameof(Index));
        }

        model.FactoryId = FactoryId.Value;

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var (success, message) = await _driverApiService.CreateAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message ?? AppMessages.Error.DriverCreate);
                return View(model);
            }

            TempData[TempDataSuccess] = AppMessages.Success.DriverAccountCreated;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في إنشاء سائق جديد StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في إنشاء سائق جديد");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(model);
        }
    }

    // ============================================================
    // EDIT (GET)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverEditForbidden;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var model = await _driverApiService.GetForEditAsync(id);

            if (model == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if (IsFactoryIsolated(model.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل بيانات السائق للتعديل {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل بيانات السائق للتعديل {DriverId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // EDIT (POST)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditDriverViewModel model)
    {
        if (id != model.UserId)
            return BadRequest();

        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverEditForbidden;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if (RoleValue == UserRole.FactoryEmployee)
            {
                var existingDriver = await _driverApiService.GetByIdAsync(id);
                if (existingDriver == null || IsFactoryIsolated(existingDriver.FactoryId))
                {
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            var (success, message) = await _driverApiService.UpdateAsync(id, model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message ?? AppMessages.Error.DriverUpdate);
                return View(model);
            }

            TempData[TempDataSuccess] = AppMessages.Success.DriverUpdated;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تعديل بيانات السائق {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تعديل بيانات السائق {DriverId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(model);
        }
    }

    // ============================================================
    // DELETE
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverDeleteForbidden;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if (RoleValue == UserRole.FactoryEmployee)
            {
                var driver = await _driverApiService.GetByIdAsync(id);
                if (driver == null || IsFactoryIsolated(driver.FactoryId))
                {
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            var success = await _driverApiService.DeleteAsync(id);

            if (!success)
            {
                TempData[TempDataError] = AppMessages.Error.DriverDelete;
            }
            else
            {
                TempData[TempDataSuccess] = AppMessages.Success.DriverDeleted;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في حذف السائق {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في حذف السائق {DriverId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // UPDATE STATUS
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, DriverStatus driverStatus)
    {
        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = AppMessages.Common.PermissionDeniedShort });
            }
            TempData[TempDataError] = AppMessages.Common.DriverStatusForbidden;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var model = new UpdateDriverStatusViewModel { DriverStatus = driverStatus };
            var success = await _driverApiService.UpdateStatusAsync(id, model);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return success
                    ? Ok(new { success = true, message = AppMessages.Success.DriverStatusUpdatedShort })
                    : BadRequest(new { success = false, message = AppMessages.Error.DriverStatusUpdateShort });
            }

            TempData[success ? TempDataSuccess : TempDataError] = success
                ? AppMessages.Success.DriverStatusUpdated
                : AppMessages.Error.DriverStatusUpdate;

            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحديث حالة السائق {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحديث حالة السائق {DriverId}", id);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // TOGGLE ACTIVE - تفعيل/إيقاف حساب السائق
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        // ✅ التحقق من الصلاحية
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = AppMessages.Common.PermissionDeniedShort });
            }
            TempData[TempDataError] = AppMessages.Common.DriverStatusForbidden;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if (RoleValue == UserRole.FactoryEmployee)
            {
                var driver = await _driverApiService.GetByIdAsync(id);
                if (driver == null || IsFactoryIsolated(driver.FactoryId))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return BadRequest(new { success = false, message = AppMessages.Common.Forbidden });
                    }
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            var isActive = await _driverApiService.ToggleActiveAsync(id);

            var message = isActive
                ? AppMessages.Success.DriverActivatedShort
                : AppMessages.Success.DriverDeactivatedShort;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true, message });
            }

            TempData[TempDataSuccess] = message;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تفعيل/إيقاف السائق {DriverId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تفعيل/إيقاف السائق {DriverId}", id);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }
}