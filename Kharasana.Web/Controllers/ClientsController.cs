using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Clients;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class ClientsController : BaseController
{
    private readonly IClientApiService _clientApiService;

    public ClientsController(IClientApiService clientApiService)
    {
        _clientApiService = clientApiService;
    }

    // ============================================================
    // INDEX - عرض العملاء
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null)
    {
        int? factoryId =
            Role == "FactoryEmployee"
                ? FactoryId
                : null;

        var pagedClients =
            await _clientApiService.GetClientsAsync(
                pageNumber,
                pageSize,
                search,
                factoryId);

        var vm = new ClientsIndexViewModel
        {
            PagedClients = pagedClients,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return View(vm);
    }

    // ============================================================
    // DETAILS - تفاصيل العميل
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model =
            await _clientApiService.GetDetailsAsync(id);

        if (model == null)
        {
            TempData[TempDataError] = AppMessages.Common.NotFound;
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // ============================================================
    // CREATE - GET
    // ============================================================
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateClientViewModel());
    }

    // ============================================================
    // CREATE - POST
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateClientViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success =
            await _clientApiService.CreateAsync(model);

        if (!success)
        {
            ModelState.AddModelError(
                string.Empty,
                "تعذر إنشاء حساب العميل. تأكد من عدم تكرار البريد أو الهاتف.");

            return View(model);
        }

        TempData[TempDataSuccess] =
            "تم إنشاء حساب العميل بنجاح.";

        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // EDIT - GET
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model =
            await _clientApiService.GetForEditAsync(id);

        if (model == null)
        {
            TempData[TempDataError] =
                AppMessages.Common.NotFound;

            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // ============================================================
    // EDIT - POST
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditClientViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success =
            await _clientApiService.UpdateAsync(id, model);

        if (!success)
        {
            ModelState.AddModelError(
                string.Empty,
                "تعذر تعديل بيانات العميل.");

            return View(model);
        }

        TempData[TempDataSuccess] =
            "تم تعديل بيانات العميل بنجاح.";

        return RedirectToAction(nameof(Index));
    }
}