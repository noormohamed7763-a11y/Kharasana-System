using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IEnumerable<OrderPayment>> GetOverdueVerificationsAsync(DateTime cutoffDate)
    {
        return await _context.OrderPayments
            .Include(p => p.Order)
            .Where(p => p.VerificationStatus == PaymentVerificationStatus.PendingVerification
                        && p.CreatedAt < cutoffDate)
            .ToListAsync();
    }
}
