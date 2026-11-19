using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Interfaces.Services;

public interface IFinanceService
{
    Task<IEnumerable<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId, int pageNumber = 1, int pageSize = 10);
    Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes, string? idempotencyKey = null);
    Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason, string? idempotencyKey = null);
}
