using System.Collections.Generic;
using System.Linq;
using WebOrderDto = Kharasana.Web.ViewModels.Orders.OrderDto;
using Kharasana.Application.DTOs.Trips;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize("Admin,FactoryEmployee")]
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
        var factoryId = FactoryId;

        if (!IsFactoryUser || !factoryId.HasValue)
        {
            TempData[TempDataError] = "يجب تحديد المصنع للوصول إلى شاشة الجدولة.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var readyOrders = new List<WebOrderDto>();
            var pageNumber = 1;
            var pageSize = 100;
            Kharasana.Application.Common.PagedResult<WebOrderDto>? page;

            do
            {
                page = await _ordersApiService.GetOrdersAsync(
                    pageNumber,
                    pageSize,
                    factoryId: factoryId.Value,
                    status: (int)OrderStatus.Approved);

                if (page == null)
                    throw new InvalidOperationException("تعذّر تحميل الطلبات الجاهزة للجدولة.");

                readyOrders.AddRange(page.Items.Where(order => order.PouringDate?.Date == targetDate.Date));
                pageNumber++;
            }
            while (pageNumber <= page.TotalPages);

            // جلب السائقين الحقيقيين للمصنع
            var driversResult = await _driverApiService.GetDriversAsync(factoryId: factoryId.Value, pageSize: 100);

            // تحويل DTO إلى ViewModel
            var ordersList = readyOrders;

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

    [HttpGet]
    public async Task<IActionResult> GetTrips(int orderId)
    {
        var trips = await _tripApiService.GetByOrderAsync(orderId);
        return Ok(trips);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrips(int orderId, [FromBody] CreateTripsBatchDto dto)
    {
        var trips = await _tripApiService.CreateBatchAsync(orderId, dto);
        return Ok(trips);
    }
}