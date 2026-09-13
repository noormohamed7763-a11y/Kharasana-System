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
            int? factoryId = (string?)Role == "FactoryEmployee" ? FactoryId : null;

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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            TempData[TempDataError] = "ليس لديك صلاحية لعرض بيانات السائقين.";
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

            if ((string?)Role == "FactoryEmployee" && driver.FactoryId != FactoryId)
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
        if ((string?)Role != "FactoryEmployee" || !FactoryId.HasValue)
        {
            TempData[TempDataError] = "إضافة السائقين متاحة حاليًا لموظف المصنع فقط.";
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
        if ((string?)Role != "FactoryEmployee" || !FactoryId.HasValue)
        {
            TempData[TempDataError] = "إضافة السائقين متاحة حاليًا لموظف المصنع فقط.";
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
                ModelState.AddModelError(string.Empty, message ?? "تعذر إنشاء حساب السائق لسبب غير معروف.");
                return View(model);
            }

            TempData[TempDataSuccess] = "تم إنشاء حساب السائق بنجاح.";
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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            TempData[TempDataError] = "ليس لديك صلاحية لتعديل السائقين.";
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
            if ((string?)Role == "FactoryEmployee" && model.FactoryId != FactoryId)
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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            TempData[TempDataError] = "ليس لديك صلاحية لتعديل السائقين.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if ((string?)Role == "FactoryEmployee")
            {
                var existingDriver = await _driverApiService.GetByIdAsync(id);
                if (existingDriver == null || existingDriver.FactoryId != FactoryId)
                {
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            var (success, message) = await _driverApiService.UpdateAsync(id, model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message ?? "تعذر تعديل بيانات السائق لسبب غير معروف.");
                return View(model);
            }

            TempData[TempDataSuccess] = "تم تعديل بيانات السائق بنجاح.";
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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            TempData[TempDataError] = "ليس لديك صلاحية لحذف السائقين.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if ((string?)Role == "FactoryEmployee")
            {
                var driver = await _driverApiService.GetByIdAsync(id);
                if (driver == null || driver.FactoryId != FactoryId)
                {
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            var success = await _driverApiService.DeleteAsync(id);

            if (!success)
            {
                TempData[TempDataError] = "تعذر حذف/تعطيل السائق. تأكد أنه غير مرتبط برحلات نشطة.";
            }
            else
            {
                TempData[TempDataSuccess] = "تم حذف السائق بنجاح.";
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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = "ليس لديك صلاحية." });
            }
            TempData[TempDataError] = "ليس لديك صلاحية لتحديث حالة السائقين.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var model = new UpdateDriverStatusViewModel { DriverStatus = driverStatus };
            var success = await _driverApiService.UpdateStatusAsync(id, model);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return success
                    ? Ok(new { success = true, message = "تم تحديث حالة السائق." })
                    : BadRequest(new { success = false, message = "تعذر تحديث الحالة." });
            }

            TempData[success ? TempDataSuccess : TempDataError] = success
                ? "تم تحديث حالة السائق بنجاح."
                : "تعذر تحديث حالة السائق.";

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
        if ((string?)Role != "Admin" && (string?)Role != "FactoryEmployee")
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = "ليس لديك صلاحية." });
            }
            TempData[TempDataError] = "ليس لديك صلاحية لتحديث حالة السائقين.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // ✅ التحقق من أن السائق يتبع نفس المصنع (للموظف فقط)
            if ((string?)Role == "FactoryEmployee")
            {
                var driver = await _driverApiService.GetByIdAsync(id);
                if (driver == null || driver.FactoryId != FactoryId)
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

            var message = isActive ? "تم تفعيل حساب السائق." : "تم إيقاف حساب السائق.";

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