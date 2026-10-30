using System.Collections.Generic;
using System.Linq;
using AppOrderDto = Kharasana.Application.DTOs.Order.OrderDto;
using WebOrderDto = Kharasana.Web.ViewModels.Orders.OrderDto;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class DispatchController : BaseController
{
    private readonly IOrdersApiService _ordersApiService;
    private readonly ITripApiService _tripApiService;
    private readonly IDriverApiService _driverApiService;
    private readonly ILogger<DispatchController> _logger;

    public DispatchController(
        IOrdersApiService ordersApiService,
        ITripApiService tripApiService,
        IDriverApiService driverApiService,
        ILogger<DispatchController> logger)
    {
        _ordersApiService = ordersApiService;
        _tripApiService = tripApiService;
        _driverApiService = driverApiService;
        _logger = logger;
    }

    // GET: Dispatch
    public async Task<IActionResult> Index(DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        var factoryId = RoleValue.HasValue ? (int?)RoleValue.Value : null;

        if (!factoryId.HasValue)
        {
            TempData[TempDataError] = "يجب تحديد المصنع للوصول إلى شاشة الجدولة.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var readyOrders = await _ordersApiService.GetReadyForDispatchAsync(factoryId.Value, targetDate);

            // جلب السائقين الحقيقيين للمصنع
            var driversResult = await _driverApiService.GetDriversAsync(factoryId: factoryId.Value, pageSize: 100);

            // تحويل DTO إلى ViewModel
            var ordersList = readyOrders?.Select(o => new WebOrderDto
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                ClientName = o.ClientName,
                Quantity = o.Quantity,
                PouringDate = o.PouringDate
            }).ToList() ?? new List<WebOrderDto>();

            ViewBag.Date = targetDate;
            ViewBag.FactoryId = factoryId.Value;
            ViewBag.ReadyOrders = ordersList;
            ViewBag.Drivers = driversResult?.Items ?? new List<Kharasana.Web.ViewModels.Drivers.DriverListItemViewModel>();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب طلبات الجدولة للتاريخ {Date}", targetDate);
            TempData[TempDataError] = "فشل في جلب طلبات الجدولة. يرجى المحاولة لاحقاً.";
            return RedirectToAction("Index", "Home");
        }
    }
}