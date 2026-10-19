using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;

namespace Kharasana.Infrastructure.Repositories;

public class OrderPaymentRepository : GenericRepository<OrderPayment>, IOrderPaymentRepository
{
    public OrderPaymentRepository(KharasanaDbContext context) : base(context)
    {
    }
}
