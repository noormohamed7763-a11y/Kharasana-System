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

    /// <summary>
    /// هل المستخدم موظف مصنعٍ موقوف (IsActive=false)؟
    /// موظفو المصنع الموقوف لا يمكنهم إضافة أو تعديل أنواع الخرسانة.
    /// </summary>
    private bool IsFactoryInactive() =>
        RoleValue == UserRole.FactoryEmployee && FactoryIsActive == false;

    private void LoadConcreteCatalog(CreateConcreteTypeViewModel model)
    {
        model.ConcreteCatalog = _catalogService.GetAll();
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
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
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

            LoadConcreteCatalog(model);

            // ✅ التصحيح هنا: استخدام Role مباشرة بدون (string?)
            if (RoleValue == UserRole.Admin)
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
            else if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
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
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Create(CreateConcreteTypeViewModel model)
    {
        try
        {
            // تحميل قائمة الأنواع القياسية دائماً
            LoadConcreteCatalog(model);

            // تحميل المصانع
            if (RoleValue == UserRole.Admin)
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
            else if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
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
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
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
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
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
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Delete(int id)
    {
        // نفس بوابة Create/Edit: الحالة في الجلسة تُقرأ عند الدخول فقط، فإن أُوقف المصنع
        // بعد تسجيل الدخول بقيت قديمة هنا — والحارس النهائي في ConcreteTypeService.DeleteAsync.
        if (IsFactoryInactive())
        {
            TempData[TempDataWarning] = "مصنعك غير نشط حالياً، لذا لا يمكنك حذف أنواع الخرسانة.";
            return RedirectToAction(nameof(Index));
        }

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
    // عرض أنواع الخرسانة المؤرشفة (المحذوفة حذفًا ناعمًا)
    // ===========================
    [HttpGet]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Archived()
    {
        try
        {
            // العزل على المصنع يفرضه الـ API من التوكن، فلا نمرّر مصنعًا من هنا
            // (لو مُرِّر لتمكّن موظفٌ من قراءة أرشيف مصنع آخر بتغيير الوسيط).
            var archivedTypes = await _concreteTypeService.GetArchivedAsync();
            return View(archivedTypes);
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تحميل أنواع الخرسانة المؤرشفة StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return View(new List<ConcreteTypeListItemViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تحميل أنواع الخرسانة المؤرشفة");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return View(new List<ConcreteTypeListItemViewModel>());
        }
    }

    // ===========================
    // استعادة نوع خرسانة مؤرشف (POST)
    // ===========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            var success = await _concreteTypeService.RestoreAsync(id);

            if (!success)
                TempData[TempDataError] = "تعذر استعادة نوع الخرسانة.";
            else
                TempData[TempDataSuccess] = "تم استعادة نوع الخرسانة بنجاح.";

            // العودة إلى الأرشيف لا إلى القائمة: غالبًا تُستعاد عدة أنواع متتالية.
            return RedirectToAction(nameof(Archived));
        }
        catch (ApiServiceException ex)
        {
            // تعارض الاسم (409) يصل هنا برسالته العربية من الـ API، فيُعرض كما هو.
            _logger.LogError(ex, "خطأ في استعادة نوع الخرسانة {ConcreteTypeId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
            return RedirectToAction(nameof(Archived));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في استعادة نوع الخرسانة {ConcreteTypeId}", id);
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
            return RedirectToAction(nameof(Archived));
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