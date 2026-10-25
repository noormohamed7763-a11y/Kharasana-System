using Kharasana.Application.Common;
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
        // For now, using Application DTO as it's already used in the view.
        // We might need a Web DTO mapping later.
        var payments = await _financeService.GetPendingPaymentsAsync(GetCurrentFactoryId());

        // Simulating pagination
        var pagedList = Kharasana.Application.Common.Models.PaginatedList<Kharasana.Application.DTOs.Order.OrderPaymentDto>.Create(payments.ToList(), page, 10);

        return View(pagedList);
    }
}
