using Kharasana.Application.Common;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;

namespace Kharasana.Infrastructure.Repositories;

public class OrderPaymentRepository : GenericRepository<OrderPayment>, IOrderPaymentRepository
{
    public OrderPaymentRepository(KharasanaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OrderPayment>> GetPendingVerificationsAsync(int factoryId)
    {
        return await _context.OrderPayments
            .Include(p => p.Order)
            .Where(p => p.VerificationStatus == PaymentVerificationStatus.PendingVerification
                        && p.Order.FactoryId == factoryId)
            .ToListAsync();
    }
}
