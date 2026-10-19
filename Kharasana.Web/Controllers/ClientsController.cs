using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Clients;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class ClientsController : BaseController
{
    private readonly IClientApiService _clientApiService;
    private readonly IOrdersApiService _ordersApiService;

    public ClientsController(
        IClientApiService clientApiService,
        IOrdersApiService ordersApiService)
    {
        _clientApiService = clientApiService;
        _ordersApiService = ordersApiService;
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
            RoleValue == UserRole.FactoryEmployee
                ? FactoryId
                : null;

        var pagedClientsTask =
            _clientApiService.GetClientsAsync(
                pageNumber,
                pageSize,
                search,
                factoryId);

        var statsTask =
            _clientApiService.GetClientStatsAsync(factoryId);

        await Task.WhenAll(pagedClientsTask, statsTask);

        var vm = new ClientsIndexViewModel
        {
            PagedClients = await pagedClientsTask,
            Stats = await statsTask,
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
        var model = await _clientApiService.GetDetailsAsync(id);
        return View(model);
    }

    // ============================================================
    // STATEMENT - كشف حساب ومطابقة صبات العميل
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Statement(int id)
    {
        var client = await _clientApiService.GetDetailsAsync(id);
        if (client == null)
        {
            TempData[TempDataError] = AppMessages.Common.NotFound;
            return RedirectToAction(nameof(Index));
        }

        int? factoryIdFilter = IsFactoryUser ? FactoryId : null;
        var pagedOrders = await _ordersApiService.GetOrdersAsync(1, 100, null, factoryIdFilter, null);
        var clientOrders = pagedOrders?.Items != null
            ? pagedOrders.Items
                .Where(o => o.ClientId == id || o.ClientPhone == client.Phone)
                .OrderByDescending(o => o.PouringDate ?? o.CreatedAt)
                .ToList()
            : new List<OrderDto>();

        var model = new ClientStatementViewModel
        {
            ClientId = client.UserId,
            ClientName = client.FullName,
            Phone = client.Phone,
            WhatsApp = client.WhatsApp,
            FactoryId = factoryIdFilter,
            FactoryName = HttpContext.Session.GetString("FactoryName") ?? "مصنع الخرسانة الجاهزة",
            StatementDate = DateTime.Now,
            TotalOrders = clientOrders.Count,
            TotalQuantity = clientOrders.Sum(o => o.Quantity),
            TotalAmount = clientOrders.Sum(o => o.TotalPrice ?? 0),
            Orders = clientOrders
        };

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

        await _clientApiService.CreateAsync(model);

        TempData[TempDataSuccess] =
            AppMessages.Success.ClientAccountCreated;

        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // EDIT - GET
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _clientApiService.GetForEditAsync(id);
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

        await _clientApiService.UpdateAsync(id, model);

        TempData[TempDataSuccess] =
            AppMessages.Success.ClientUpdated;

        return RedirectToAction(nameof(Index));
    }
}