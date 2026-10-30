using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface ITripRepository : IGenericRepository<Trip>
{
    Task<IReadOnlyList<Trip>> GetDispatchAsync(int factoryId, DateTime from, DateTime to, int? driverId = null);
    Task<IReadOnlyList<Trip>> GetByDriverAsync(int driverId, DateTime from, DateTime to);
    Task<IReadOnlyList<Trip>> GetByOrderIdAsync(int orderId);
    Task AddRangeAsync(IEnumerable<Trip> trips);
}
