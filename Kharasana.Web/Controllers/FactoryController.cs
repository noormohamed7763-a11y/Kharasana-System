using Kharasana.Web.Common;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.ViewModels.Factories;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class FactoriesController : BaseController
{
    private readonly IFactoryApiService _factoryService;
    private readonly IUserApiService _userService;
    private readonly ILogger<FactoriesController> _logger;

    public FactoriesController(
        IFactoryApiService factoryService,
        IUserApiService userService,
        ILogger<FactoriesController> logger)
    {
        _factoryService = factoryService;
        _userService = userService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation(">>> FactoriesController.Index START");
        var result = await _factoryService.GetAllAsync();
        _logger.LogInformation(">>> FactoriesController.Index AFTER API, Succeeded={Succeeded}", result.Succeeded);

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;

        _logger.LogInformation(">>> FactoriesController.Index BEFORE View");
        return View(result.Data ?? new List<FactoryListItemViewModel>());
    }

    [HttpGet]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> Archived()
    {
        var result = await _factoryService.GetArchivedAsync();

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;

        return View(result.Data ?? new List<FactoryListItemViewModel>());
    }

    [HttpGet]
    [SessionAuthorize("Admin")]
    public IActionResult Create()
    {
        return View(new CreateFactoryViewModel());
    }

    [HttpPost]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFactoryViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _factoryService.CreateAsync(model);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData[TempDataSuccess] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _factoryService.GetByIdAsync(id);

        if (!result.Succeeded)
        {
            TempData[TempDataError] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpGet]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _factoryService.GetByIdAsync(id);

        if (!result.Succeeded)
        {
            TempData[TempDataError] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var factory = result.Data!;

        var model = new UpdateFactoryViewModel
        {
            FactoryId = factory.FactoryId,
            FactoryName = factory.FactoryName,
            OwnerName = factory.OwnerName,
            Phone = factory.Phone,
            WhatsApp = factory.WhatsApp,
            Email = factory.Email,
            Area = factory.Area,
            Address = factory.Address,
            Latitude = factory.Latitude,
            Longitude = factory.Longitude,
            Logo = factory.Logo,
            IsActive = factory.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateFactoryViewModel model)
    {
        if (id != model.FactoryId)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _factoryService.UpdateAsync(id, model);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData[TempDataSuccess] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _factoryService.GetByIdAsync(id);

        if (!result.Succeeded)
        {
            TempData[TempDataError] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ActionName("Delete")]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _factoryService.DeleteAsync(id);

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;
        else
            TempData[TempDataSuccess] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    // ===========================
    // إدارة شعار المصنع (رفع وحذف)
    // ===========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile logoFile)
    {
        var result = await _factoryService.UploadLogoAsync(id, logoFile);

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;
        else
            TempData[TempDataSuccess] = result.Message;

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> DeleteLogo(int id)
    {
        var result = await _factoryService.DeleteLogoAsync(id);

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;
        else
            TempData[TempDataSuccess] = result.Message;

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ===========================
    // إنشاء حساب للمصنع
    // ===========================
    [HttpPost]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> CreateFactoryAccount([FromBody] CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = AppMessages.Common.InvalidData
            });
        }

        try
        {
            var response = await _userService.CreateAsync(model);

            if (response.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = response.Message ?? AppMessages.Success.AccountCreated
                });
            }

            return BadRequest(new
            {
                success = false,
                message = response.Message ?? AppMessages.Error.AccountCreate
            });
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في إنشاء حساب المصنع StatusCode={StatusCode}", (int)ex.StatusCode);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في إنشاء حساب المصنع");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = AppMessages.Common.OperationFailed,
                detail = ex.Message
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _factoryService.RestoreAsync(id);

        if (!result.Succeeded)
            TempData[TempDataError] = result.Message;
        else
            TempData[TempDataSuccess] = result.Message;

        return RedirectToAction(nameof(Archived));
    }
}