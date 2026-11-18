using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.Interfaces.Services;

public interface IFinanceService
{
    Task<IEnumerable<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId);
    Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes, int approverId);
    Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason, int approverId);
}
