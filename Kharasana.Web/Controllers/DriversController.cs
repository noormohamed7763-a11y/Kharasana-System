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
        int? factoryId = RoleValue == UserRole.FactoryEmployee ? FactoryId : null;

        var pagedDrivers = await _driverApiService.GetDriversAsync(pageNumber, pageSize, search, factoryId);

        var counts = (Available: 0, Busy: 0, Offline: 0);
        try
        {
            counts = await _driverApiService.GetStatusCountsAsync(search, factoryId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "تعذر جلب إحصاءات حالة السائقين — ستُعرض البطاقات بقيمة صفر.");
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

    // ============================================================
    // DETAILS
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriversViewForbidden;
            return RedirectToAction(nameof(Index));
        }

        var driver = await _driverApiService.GetByIdAsync(id);

        if (IsFactoryIsolated(driver.FactoryId))
        {
            TempData[TempDataError] = AppMessages.Common.Forbidden;
            return RedirectToAction(nameof(Index));
        }

        return View(driver);
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

        return View(new CreateDriverViewModel { FactoryId = FactoryId.Value });
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

        await _driverApiService.CreateAsync(model);

        TempData[TempDataSuccess] = AppMessages.Success.DriverAccountCreated;
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // EDIT (GET)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverEditForbidden;
            return RedirectToAction(nameof(Index));
        }

        var model = await _driverApiService.GetForEditAsync(id);

        if (IsFactoryIsolated(model.FactoryId))
        {
            TempData[TempDataError] = AppMessages.Common.Forbidden;
            return RedirectToAction(nameof(Index));
        }

        return View(model);
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

        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverEditForbidden;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        if (RoleValue == UserRole.FactoryEmployee)
        {
            var existingDriver = await _driverApiService.GetByIdAsync(id);
            if (IsFactoryIsolated(existingDriver.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }
        }

        await _driverApiService.UpdateAsync(id, model);

        TempData[TempDataSuccess] = AppMessages.Success.DriverUpdated;
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // DELETE
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            TempData[TempDataError] = AppMessages.Common.DriverDeleteForbidden;
            return RedirectToAction(nameof(Index));
        }

        if (RoleValue == UserRole.FactoryEmployee)
        {
            var driver = await _driverApiService.GetByIdAsync(id);
            if (IsFactoryIsolated(driver.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }
        }

        await _driverApiService.DeleteAsync(id);

        TempData[TempDataSuccess] = AppMessages.Success.DriverDeleted;
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // UPDATE STATUS
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, DriverStatus driverStatus)
    {
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { success = false, message = AppMessages.Common.PermissionDeniedShort });

            TempData[TempDataError] = AppMessages.Common.DriverStatusForbidden;
            return RedirectToAction(nameof(Index));
        }

        await _driverApiService.UpdateStatusAsync(id, new UpdateDriverStatusViewModel { DriverStatus = driverStatus });

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { success = true, message = AppMessages.Success.DriverStatusUpdatedShort });

        TempData[TempDataSuccess] = AppMessages.Success.DriverStatusUpdated;
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // TOGGLE ACTIVE - تفعيل/إيقاف حساب السائق
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (RoleValue != UserRole.Admin && RoleValue != UserRole.FactoryEmployee)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { success = false, message = AppMessages.Common.PermissionDeniedShort });

            TempData[TempDataError] = AppMessages.Common.DriverStatusForbidden;
            return RedirectToAction(nameof(Index));
        }

        if (RoleValue == UserRole.FactoryEmployee)
        {
            var driver = await _driverApiService.GetByIdAsync(id);
            if (IsFactoryIsolated(driver.FactoryId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = AppMessages.Common.Forbidden });

                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }
        }

        var isActive = await _driverApiService.ToggleActiveAsync(id);

        var message = isActive
            ? AppMessages.Success.DriverActivatedShort
            : AppMessages.Success.DriverDeactivatedShort;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Ok(new { success = true, message });

        TempData[TempDataSuccess] = message;
        return RedirectToAction(nameof(Index));
    }
}