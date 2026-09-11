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

    /// <summary>
    /// هل المستخدم موظف مصنعٍ موقوف (IsActive=false)؟
    /// موظفو المصنع الموقوف لا يمكنهم إضافة أو تعديل أنواع الخرسانة.
    /// </summary>
    private bool IsFactoryInactive() =>
        Role == "FactoryEmployee" && FactoryIsActive == false;

    private void LoadConcreteStandards(CreateConcreteTypeViewModel model)
    {
        var concreteTypes = _catalogService.GetAll();

        model.ConcreteStandards = concreteTypes
            .Select(x => new SelectListItem
            {
                Value = x.Code,
                Text = $"{x.Code} - {x.Usage}"
            })
            .ToList();

        ViewBag.ConcreteCatalog = concreteTypes;
    }

    // ===========================
    // عرض جميع أنواع الخرسانة
    // ===========================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var concreteTypes = await _concreteTypeService.GetAllAsync();
            return View(concreteTypes);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل قائمة أنواع الخرسانة StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(new List<ConcreteTypeListItemViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل قائمة أنواع الخرسانة");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(new List<ConcreteTypeListItemViewModel>());
        }
    }

    // ===========================
    // إنشاء نوع خرسانة (GET)
    // ===========================
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = "مصنعك غير نشط حالياً، لذا لا يمكنك إضافة أنواع خرسانة.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var model = new CreateConcreteTypeViewModel();

            LoadConcreteStandards(model);

            // ✅ التصحيح هنا: استخدام Role مباشرة بدون (string?)
            if (Role == "Admin")
            {
                var factoryResult = await _factoryService.GetAllAsync();

                model.Factories = (factoryResult.Data ?? [])
                    .Where(f => f.IsActive)
                    .Select(f => new SelectListItem
                    {
                        Value = f.FactoryId.ToString(),
                        Text = f.FactoryName
                    })
                    .ToList();
            }
            else if (Role == "FactoryEmployee" && FactoryId.HasValue)
            {
                model.FactoryId = FactoryId.Value;
            }

            return View(model);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل صفحة إنشاء نوع خرسانة StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل صفحة إنشاء نوع خرسانة");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ===========================
    // إنشاء نوع خرسانة (POST)
    // ===========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateConcreteTypeViewModel model)
    {
        try
        {
            // تحميل قائمة الأنواع القياسية دائماً
            LoadConcreteStandards(model);

            // تحميل المصانع
            if (Role == "Admin")
            {
                var factoryResult = await _factoryService.GetAllAsync();

                model.Factories = (factoryResult.Data ?? [])
                    .Where(f => f.IsActive)
                    .Select(f => new SelectListItem
                    {
                        Value = f.FactoryId.ToString(),
                        Text = f.FactoryName
                    })
                    .ToList();
            }
            else if (Role == "FactoryEmployee" && FactoryId.HasValue)
            {
                model.FactoryId = FactoryId.Value;
            }

            // تحديد اسم النوع والمقاومة
            if (!model.IsCustomType)
            {
                var standard = _catalogService.GetByCode(model.SelectedConcreteCode ?? string.Empty);

                if (standard == null)
                {
                    ModelState.AddModelError(nameof(model.SelectedConcreteCode),
                        "يرجى اختيار نوع الخرسانة.");
                }
                else
                {
                    model.Name = standard.Code;
                    model.Strength = standard.Strength;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.CustomName))
                {
                    ModelState.AddModelError(nameof(model.CustomName),
                        "اسم النوع المخصص مطلوب.");
                }

                if (!model.CustomStrength.HasValue)
                {
                    ModelState.AddModelError(nameof(model.CustomStrength),
                        "المقاومة مطلوبة.");
                }

                if (ModelState.IsValid)
                {
                    model.Name = model.CustomName!;
                    model.Strength = model.CustomStrength ?? 0;
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var success = await _concreteTypeService.CreateAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "تعذر إنشاء نوع الخرسانة.");
                return View(model);
            }

            TempData[TempDataSuccess] = "تم إنشاء نوع الخرسانة بنجاح.";

            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في إنشاء نوع خرسانة StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في إنشاء نوع خرسانة");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(model);
        }
    }

    // ===========================
    // تعديل نوع خرسانة (GET)
    // ===========================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = "مصنعك غير نشط حالياً، لذا لا يمكنك تعديل أنواع الخرسانة.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var concreteType = await _concreteTypeService.GetByIdAsync(id);

            if (concreteType == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

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
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل نوع الخرسانة للتعديل {ConcreteTypeId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل نوع الخرسانة للتعديل {ConcreteTypeId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ===========================
    // تعديل نوع خرسانة (POST)
    // ===========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateConcreteTypeViewModel model)
    {
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = "مصنعك غير نشط حالياً، لذا لا يمكنك تعديل أنواع الخرسانة.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var success = await _concreteTypeService.UpdateAsync(id, model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "تعذر تحديث نوع الخرسانة.");
                return View(model);
            }

            TempData[TempDataSuccess] = "تم تحديث نوع الخرسانة بنجاح.";

            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحديث نوع الخرسانة {ConcreteTypeId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحديث نوع الخرسانة {ConcreteTypeId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(model);
        }
    }

    // ===========================
    // حذف نوع خرسانة (POST)
    // ===========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _concreteTypeService.DeleteAsync(id);

            if (!success)
            {
                TempData[TempDataError] = "تعذر حذف نوع الخرسانة.";
                return RedirectToAction(nameof(Index));
            }

            TempData[TempDataSuccess] = "تم حذف نوع الخرسانة بنجاح.";

            return RedirectToAction(nameof(Index));
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في حذف نوع الخرسانة {ConcreteTypeId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في حذف نوع الخرسانة {ConcreteTypeId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }

    // ===========================
    // عرض تفاصيل نوع الخرسانة
    // ===========================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var concreteType = await _concreteTypeService.GetByIdAsync(id);

            if (concreteType == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

            return View(concreteType);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل تفاصيل نوع الخرسانة {ConcreteTypeId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل تفاصيل نوع الخرسانة {ConcreteTypeId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Index));
        }
    }
}