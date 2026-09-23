using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

    public virtual async Task<int> SaveChangesAsync()
    {
        try
        {
            return await SaveChangesInternalAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }
    }

    protected virtual async Task<int> SaveChangesInternalAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        // تُدار دورة حياة KharasanaDbContext عبر حاوية الـ DI لتجنب ObjectDisposedException
    }
}