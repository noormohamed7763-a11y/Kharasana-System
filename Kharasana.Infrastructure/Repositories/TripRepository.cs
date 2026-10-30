using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class TripRepository : GenericRepository<Trip>, ITripRepository
{
    public TripRepository(KharasanaDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Trip>> GetDispatchAsync(int factoryId, DateTime from, DateTime to, int? driverId = null)
    {
        var query = _context.Trips
            .Where(t => t.FactoryId == factoryId && !t.IsDeleted)
            .Where(t => (t.ScheduledAt >= from && t.ScheduledAt < to) || (t.CreatedAt >= from && t.CreatedAt < to));

        if (driverId.HasValue)
            query = query.Where(t => t.DriverId == driverId.Value);

        return await query
            .Include(t => t.Driver)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Trip>> GetByDriverAsync(int driverId, DateTime from, DateTime to)
    {
        return await _context.Trips
            .Where(t => t.DriverId == driverId && !t.IsDeleted)
            .Where(t => t.ScheduledAt >= from && t.ScheduledAt < to)
            .Include(t => t.Order)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Trip>> GetByOrderIdAsync(int orderId)
    {
        return await _context.Trips
            .Where(t => t.OrderId == orderId && !t.IsDeleted)
            .Include(t => t.Driver)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Trip> trips)
    {
        await _context.Trips.AddRangeAsync(trips);
    }
}
