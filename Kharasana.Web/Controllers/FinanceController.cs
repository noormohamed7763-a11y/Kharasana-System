using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize("Admin,FactoryEmployee")]
public class FinanceController : BaseController
{
    private readonly IFinanceApiService _financeApiService;

    public FinanceController(IFinanceApiService financeApiService)
    {
        _financeApiService = financeApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var factoryId = FactoryId;
        if (!factoryId.HasValue && RoleValue != UserRole.Admin)
        {
            TempData[TempDataError] = "يجب تحديد المصنع لعرض السجلات المالية.";
            return RedirectToAction("Index", "Dashboard");
        }

        var targetFactoryId = factoryId ?? 0;
        var pagedResult = await _financeApiService.GetPendingPaymentsAsync(targetFactoryId, page, 10);

        return View(pagedResult);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int paymentId, string? verificationNotes)
    {
        var success = await _financeApiService.ApprovePaymentAsync(paymentId, verificationNotes ?? "تم التدقيق والمطابقة");
        if (success)
        {
            TempData[TempDataSuccess] = "تم قبول واعتماد الدفعة المالية بنجاح.";
        }
        else
        {
            TempData[TempDataError] = "حدث خطأ أثناء اعتماد الدفعة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int paymentId, string? rejectionReason)
    {
        var success = await _financeApiService.RejectPaymentAsync(paymentId, rejectionReason ?? "تم الرفض لعدم مطابقة الحوالة");
        if (success)
        {
            TempData[TempDataSuccess] = "تم رفض الدفعة المالية.";
        }
        else
        {
            TempData[TempDataError] = "حدث خطأ أثناء رفض الدفعة.";
        }

        return RedirectToAction(nameof(Index));
    }
}
