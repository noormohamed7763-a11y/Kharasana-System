using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(KharasanaDbContext context)
        : base(context)
    {
    }

    private IQueryable<Order> OrdersWithDetails()
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.Client)
            .Include(o => o.Factory)
            .Include(o => o.ConcreteType)
            .Include(o => o.Driver);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int id)
    {
        return await OrdersWithDetails()
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, OrderStatus? status, string? search, int pageNumber, int pageSize)
    {
        var query = OrdersWithDetails();

        if (factoryId.HasValue)
            query = query.Where(o => o.FactoryId == factoryId.Value);

        if (clientId.HasValue)
            query = query.Where(o => o.ClientId == clientId.Value);

        if (driverId.HasValue)
            query = query.Where(o => o.DriverId == driverId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedNameTerm = ArabicTextNormalizer.Normalize(term);
            var phoneDigits = YemeniPhoneHelper.NormalizeForSearch(term);
            var hasPhoneDigits = !string.IsNullOrEmpty(phoneDigits);

            query = query.Where(o =>
                o.OrderNumber.Contains(term)
                || o.Client.FullName
                    .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                    .Replace("ى", "ي").Replace("ة", "ه")
                    .Contains(normalizedNameTerm)
                || (hasPhoneDigits && o.Client.Phone != null && o.Client.Phone.Contains(phoneDigits)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<CustomerSummaryDto> Items, int TotalCount)> GetFactoryCustomersAsync(
        int? factoryId, string? search, int pageNumber, int pageSize)
    {
        var query = OrdersWithDetails();

        if (factoryId.HasValue)
            query = query.Where(o => o.FactoryId == factoryId.Value);

        var grouped = query
            .GroupBy(o => new
            {
                o.ClientId,
                o.Client.FullName,
                o.Client.Phone
            })
            .Select(g => new CustomerSummaryDto
            {
                UserId = g.Key.ClientId,
                FullName = g.Key.FullName,
                Phone = g.Key.Phone,
                OrdersCount = g.Count(),
                TotalQuantity = g.Sum(o => o.Quantity),
                LastOrderDate = g.Max(o => (DateTime?)o.CreatedAt)
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedNameTerm = ArabicTextNormalizer.Normalize(term);
            var phoneDigits = YemeniPhoneHelper.NormalizeForSearch(term);
            var hasPhoneDigits = !string.IsNullOrEmpty(phoneDigits);

            grouped = grouped.Where(c =>
                c.FullName
                    .Replace("أ", "ا")
                    .Replace("إ", "ا")
                    .Replace("آ", "ا")
                    .Replace("ى", "ي")
                    .Replace("ة", "ه")
                    .Contains(normalizedNameTerm)
                || (hasPhoneDigits &&
                    c.Phone != null &&
                    c.Phone.Contains(phoneDigits)));
        }

        var totalCount = await grouped.CountAsync();

        var items = await grouped
            .OrderByDescending(c => c.LastOrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // ============================================================
    // REPORTS — queries executed server-side (no in-memory materialization)
    // ============================================================

    public async Task<List<OrderStatusCountDto>> GetCountByStatusAsync(int? factoryId)
    {
        var query = _context.Orders.AsNoTracking();

        if (factoryId.HasValue)
            query = query.Where(o => o.FactoryId == factoryId.Value);

        var grouped = await query
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCountDto
            {
                Status = (int)g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Status)
            .ToListAsync();

        // Set Arabic names (EF Core can't translate static methods)
        foreach (var item in grouped)
            item.StatusName = OrderStatusHelper.GetArabicName((OrderStatus)item.Status);

        return grouped;
    }

    public async Task<List<ConcreteTypeCountDto>> GetCountByConcreteTypeAsync(int? factoryId)
    {
        IQueryable<Order> query = _context.Orders.AsNoTracking();

        if (factoryId.HasValue)
            query = query.Where(o => o.FactoryId == factoryId.Value);

        var grouped = await query
            .GroupBy(o => new { o.ConcreteTypeId, Name = o.ConcreteType.Name })
            .Select(g => new ConcreteTypeCountDto
            {
                ConcreteTypeId = g.Key.ConcreteTypeId,
                ConcreteTypeName = g.Key.Name,
                Count = g.Count(),
                TotalQuantity = g.Sum(o => o.Quantity)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return grouped;
    }
}