using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ConcreteTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class ConcreteTypesController : BaseController
{
    private readonly IConcreteTypeApiService _concreteTypeService;
    private readonly IFactoryApiService _factoryService;
    private readonly IConcreteCatalogService _catalogService;
    private readonly ILogger<ConcreteTypesController> _logger;

    public ConcreteTypesController(
        IConcreteTypeApiService concreteTypeService,
        IFactoryApiService factoryService,
        IConcreteCatalogService catalogService,
        ILogger<ConcreteTypesController> logger)
    {
        _concreteTypeService = concreteTypeService;
        _factoryService = factoryService;
        _catalogService = catalogService;
        _logger = logger;
    }

    private bool IsFactoryInactive() =>
        RoleValue == UserRole.FactoryEmployee && FactoryIsActive == false;

    private void LoadConcreteCatalog(CreateConcreteTypeViewModel model)
    {
        model.ConcreteCatalog = _catalogService.GetAll();
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20, string? search = null)
    {
        var concreteTypes = await _concreteTypeService.GetAllAsync(pageNumber, pageSize, search);
        return View(concreteTypes);
    }

    [HttpGet]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Create()
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = AppMessages.Common.FactoryInactiveCannotAddConcreteTypes;
            return RedirectToAction(nameof(Index));
        }

        var model = new CreateConcreteTypeViewModel();
        LoadConcreteCatalog(model);

        if (RoleValue == UserRole.Admin)
        {
            var factoryResult = await _factoryService.GetAllAsync();
            model.Factories = (factoryResult.Data ?? [])
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem { Value = f.FactoryId.ToString(), Text = f.FactoryName })
                .ToList();
        }
        else if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
        {
            model.FactoryId = FactoryId.Value;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Create(CreateConcreteTypeViewModel model)
    {
        LoadConcreteCatalog(model);

        if (RoleValue == UserRole.Admin)
        {
            var factoryResult = await _factoryService.GetAllAsync();
            model.Factories = (factoryResult.Data ?? [])
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem { Value = f.FactoryId.ToString(), Text = f.FactoryName })
                .ToList();
        }
        else if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
        {
            model.FactoryId = FactoryId.Value;
        }

        if (!model.IsCustomType)
        {
            var standard = _catalogService.GetByCode(model.SelectedConcreteCode ?? string.Empty);
            if (standard == null)
                ModelState.AddModelError(nameof(model.SelectedConcreteCode), AppMessages.Validation.ConcreteTypeRequired);
            else
            {
                model.Name = standard.Code;
                model.Strength = standard.Strength;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(model.CustomName))
                ModelState.AddModelError(nameof(model.CustomName), AppMessages.Validation.CustomConcreteTypeNameRequired);

            if (!model.CustomStrength.HasValue)
                ModelState.AddModelError(nameof(model.CustomStrength), AppMessages.Validation.ConcreteStrengthRequired);

            if (ModelState.IsValid)
            {
                model.Name = model.CustomName!;
                model.Strength = model.CustomStrength ?? 0;
            }
        }

        if (!ModelState.IsValid)
        {
            // أعد تعبئة البيانات المطلوبة للـ View في حالة فشل التحقق
            LoadConcreteCatalog(model);
            if (RoleValue == UserRole.Admin)
            {
                var factoryResult = await _factoryService.GetAllAsync();
                model.Factories = (factoryResult.Data ?? [])
                    .Where(f => f.IsActive)
                    .Select(f => new SelectListItem { Value = f.FactoryId.ToString(), Text = f.FactoryName })
                    .ToList();
            }
            else if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
            {
                model.FactoryId = FactoryId.Value;
            }
            return View(model);
        }

        await _concreteTypeService.CreateAsync(model);

        TempData[TempDataSuccess] = AppMessages.Success.ConcreteTypeCreated;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Edit(int id)
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = AppMessages.Common.FactoryInactiveCannotEditConcreteTypes;
            return RedirectToAction(nameof(Index));
        }

        var concreteType = await _concreteTypeService.GetByIdAsync(id);

        var model = new UpdateConcreteTypeViewModel
        {
            ConcreteTypeId = concreteType.ConcreteTypeId,
            Name = concreteType.Name,
            Strength = concreteType.Strength,
            UnitPrice = concreteType.UnitPrice,
            ImageUrl = concreteType.ImageUrl,
            Description = concreteType.Description,
            IsActive = concreteType.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Edit(int id, UpdateConcreteTypeViewModel model)
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = AppMessages.Common.FactoryInactiveCannotEditConcreteTypes;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        await _concreteTypeService.UpdateAsync(id, model);

        TempData[TempDataSuccess] = AppMessages.Success.ConcreteTypeUpdated;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Delete(int id)
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = AppMessages.Common.FactoryInactiveCannotDeleteConcreteTypes;
            return RedirectToAction(nameof(Index));
        }

        await _concreteTypeService.DeleteAsync(id);

        TempData[TempDataSuccess] = AppMessages.Success.ConcreteTypeDeleted;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Archived()
    {
        var archivedTypes = await _concreteTypeService.GetArchivedAsync();
        return View(archivedTypes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Restore(int id)
    {
        await _concreteTypeService.RestoreAsync(id);
        TempData[TempDataSuccess] = AppMessages.Success.ConcreteTypeRestored;
        return RedirectToAction(nameof(Archived));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var concreteType = await _concreteTypeService.GetByIdAsync(id);
        return View(concreteType);
    }
}