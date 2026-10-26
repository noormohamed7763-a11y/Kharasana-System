using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

public class FinanceController : BaseController
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var factoryId = FactoryId;
        if (!factoryId.HasValue)
            return Forbid();

        var payments = await _financeService.GetPendingPaymentsAsync(factoryId.Value);

        var pagedResult = new PagedResult<OrderPaymentDto>
        {
            Items = payments.Skip((page - 1) * 10).Take(10),
            PageNumber = page,
            PageSize = 10,
            TotalCount = payments.Count()
        };

        return View(pagedResult);
    }
}
