using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kharasana.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly KharasanaDbContext _context;

    public IFactoryRepository Factories { get; }

    public IUserRepository Users { get; }

    public IConcreteTypeRepository ConcreteTypes { get; }

    public IOrderRepository Orders { get; }
    public IFactoryRegistrationRequestRepository FactoryRegistrationRequests { get; }
    public IActivationTokenRepository ActivationTokens { get; }

    public UnitOfWork(KharasanaDbContext context)
    {
        _context = context;

        Factories = new FactoryRepository(context);
        Users = new UserRepository(context);
        ConcreteTypes = new ConcreteTypeRepository(context);
        Orders = new OrderRepository(context);
        FactoryRegistrationRequests = new FactoryRegistrationRequestRepository(context);
        ActivationTokens = new ActivationTokenRepository(context);
    }

    private IDbContextTransaction? _currentTransaction;

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }
        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction == null) throw new InvalidOperationException("لا توجد معاملة نشطة للالتزام بها.");

        await _currentTransaction.CommitAsync();
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
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
        catch (DbUpdateException ex) when (UniqueConstraintDetector.IsUniqueViolation(ex))
        {
            throw new ConflictException(Messages.DuplicateValueConflict);
        }
        catch (Exception)
        {
            throw;
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