using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;

namespace Kharasana.Web.Services.Interfaces;

public interface IFinanceApiService
{
    Task<PagedResult<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId, int pageNumber = 1, int pageSize = 10);
    Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes);
    Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason);
}
