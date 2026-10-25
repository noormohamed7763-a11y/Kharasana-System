using Kharasana.Application.Interfaces.Repositories;

namespace Kharasana.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IFactoryRepository Factories { get; }
    IUserRepository Users { get; }
    IConcreteTypeRepository ConcreteTypes { get; }
    IOrderRepository Orders { get; }
    IFactoryRegistrationRequestRepository FactoryRegistrationRequests { get; }
    IActivationTokenRepository ActivationTokens { get; }
    IOrderPaymentRepository OrderPayments { get; }
    IRawMaterialRepository RawMaterials { get; }
    IProductionBatchRepository ProductionBatches { get; }

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<int> SaveChangesAsync();
}