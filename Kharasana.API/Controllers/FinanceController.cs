using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.AdminOrFactoryEmployee)]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    /// <summary>
    /// جلب الحوالات والدفعات بانتظار التحقق لمصنع محدد.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPayments(
        [FromQuery] int? factoryId, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        var caller = User.GetCallerContext();
        var targetFactoryId = caller.Role == UserRole.Admin
            ? (factoryId ?? 0)
            : (caller.FactoryId ?? 0);

        if (targetFactoryId == 0)
        {
            return BadRequest(new ApiResponse<IEnumerable<OrderPaymentDto>>
            {
                Success = false,
                Message = "يجب تحديد المصنع لجلب الدفعات المعلقة."
            });
        }

        var payments = await _financeService.GetPendingPaymentsAsync(targetFactoryId, pageNumber, pageSize);

        return Ok(new ApiResponse<IEnumerable<OrderPaymentDto>>
        {
            Success = true,
            Data = payments
        });
    }

    /// <summary>
    /// قبول الحوالة/الدفعة بعد التحقق.
    /// </summary>
    [HttpPost("{paymentId:int}/approve")]
    public async Task<IActionResult> ApprovePayment(
        int paymentId, 
        [FromBody] string? verificationNotes,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _financeService.ApprovePaymentAsync(paymentId, verificationNotes ?? "تم التحقق", idempotencyKey);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "تم قبول وتأكيد الدفعة بنجاح.",
            Data = result
        });
    }

    /// <summary>
    /// رفض الحوالة/الدفعة.
    /// </summary>
    [HttpPost("{paymentId:int}/reject")]
    public async Task<IActionResult> RejectPayment(
        int paymentId, 
        [FromBody] string? rejectionReason,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _financeService.RejectPaymentAsync(paymentId, rejectionReason ?? "تم رفض الحوالة لعدم المطابقة", idempotencyKey);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "تم رفض الدفعة.",
            Data = result
        });
    }
}
