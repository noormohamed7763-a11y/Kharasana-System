using Kharasana.Application.Common;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IOrderPaymentRepository : IGenericRepository<OrderPayment>
{
    /// <summary>
    /// جلب جميع الدفعات المعلقة للت验证 (Pending) لمصنع معين — للوحة "مكتب المراجعة المالية".
    /// </summary>
    Task<IEnumerable<OrderPayment>> GetPendingVerificationsAsync(int factoryId);
}
