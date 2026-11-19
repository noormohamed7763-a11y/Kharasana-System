using Kharasana.Application.Common;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IOrderPaymentRepository : IGenericRepository<OrderPayment>
{
    /// <summary>
    /// جلب جميع الدفعات المعلقة للتحقق (Pending) لمصنع معين — للوحة "مكتب المراجعة المالية".
    /// مع دعم التصفح (Pagination).
    /// </summary>
    Task<IEnumerable<OrderPayment>> GetPendingVerificationsAsync(int factoryId, int pageNumber, int pageSize);

    /// <summary>
    /// جلب الدفعات المعلقة التي تجاوزت مدة الانتظار المسموح بها (متأخرة) — للتنبيهات الاستباقية.
    /// </summary>
    Task<IEnumerable<OrderPayment>> GetOverdueVerificationsAsync(DateTime cutoffDate);
}
