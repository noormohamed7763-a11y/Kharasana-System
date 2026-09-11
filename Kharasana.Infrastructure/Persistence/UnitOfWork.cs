using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Infrastructure.Repositories;

namespace Kharasana.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly KharasanaDbContext _context;

    public IFactoryRepository Factories { get; }

    public IUserRepository Users { get; }

    public IConcreteTypeRepository ConcreteTypes { get; }

    public IOrderRepository Orders { get; }

    public UnitOfWork(KharasanaDbContext context)
    {
        _context = context;

        Factories = new FactoryRepository(context);
        Users = new UserRepository(context);
        ConcreteTypes = new ConcreteTypeRepository(context);
        Orders = new OrderRepository(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}