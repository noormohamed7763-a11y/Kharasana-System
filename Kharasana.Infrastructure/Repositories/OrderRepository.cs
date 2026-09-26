using Kharasana.Application.Common;
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

    /// <summary>
    /// استعلام الطلب مع تفاصيله.
    /// <paramref name="trackChanges"/> = false للقراءة فقط (AsNoTracking)،
    /// و true لمسارات التعديل حيث يعتمد الحفظ على كشف EF للتغييرات.
    /// </summary>
    private IQueryable<Order> OrdersWithDetails(bool trackChanges)
    {
        var query = _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Factory)
            .Include(o => o.ConcreteType)
            .Include(o => o.Driver);

        return trackChanges ? query : query.AsNoTracking();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int id)
    {
        return await OrdersWithDetails(trackChanges: false)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<Order?> GetByIdWithDetailsForUpdateAsync(int id)
    {
        return await OrdersWithDetails(trackChanges: true)
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    public async Task<PagedResult<Order>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, OrderStatus? status, string? search, int pageNumber, int pageSize)
    {
        var query = OrdersWithDetails(trackChanges: false);

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
            var normalizedNameTerm = ArabicTextNormalization.Normalize(term);
            var phoneDigits = YemeniPhoneHelper.NormalizeForSearch(term);
            var hasPhoneDigits = !string.IsNullOrEmpty(phoneDigits);

            // تطبيع الاسم يُبنى تعبيرياً بدل تكرار سلسلة Replace يدوياً (انظر ArabicTextNormalization)
            var clientNameMatch = ArabicTextNormalization.Contains<Order>(o => o.Client.FullName, normalizedNameTerm);
            var numberOrNameMatch = ArabicTextNormalization.Or(clientNameMatch, o => o.OrderNumber.Contains(term));

            query = query.Where(
                ArabicTextNormalization.Or(numberOrNameMatch, o => hasPhoneDigits && o.Client.Phone != null && o.Client.Phone.Contains(phoneDigits)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<CustomerSummaryDto>> GetFactoryCustomersAsync(
        int? factoryId, string? search, int pageNumber, int pageSize)
    {
        var query = OrdersWithDetails(trackChanges: false);

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
            var normalizedNameTerm = ArabicTextNormalization.Normalize(term);
            var phoneDigits = YemeniPhoneHelper.NormalizeForSearch(term);
            var hasPhoneDigits = !string.IsNullOrEmpty(phoneDigits);

            var customerNameMatch = ArabicTextNormalization.Contains<CustomerSummaryDto>(
                c => c.FullName, normalizedNameTerm);

            grouped = grouped.Where(
                ArabicTextNormalization.Or(customerNameMatch, c => hasPhoneDigits && c.Phone != null && c.Phone.Contains(phoneDigits)));
        }

        var totalCount = await grouped.CountAsync();

        var items = await grouped
            .OrderByDescending(c => c.LastOrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<CustomerSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
                Status = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Status)
            .ToListAsync();

        // Set Arabic names (EF Core can't translate static methods)
        foreach (var item in grouped)
            item.StatusName = OrderStatusHelper.GetArabicName(item.Status);

        return grouped;
    }

    public async Task<List<ConcreteTypeCountDto>> GetCountByConcreteTypeAsync(int? factoryId)
    {
        // ⚠️ IgnoreQueryFilters هنا يلغي فلتر الحذف الناعم لكل الكيانات في الاستعلام —
        //    لا لمجموعة أنواع الخرسانة وحدها — لذا يُعاد تطبيق فلتر الطلبات (!IsDeleted)
        //    يدويًا في السطر التالي حتى لا تُحتسب الطلبات المحذوفة في التقرير.
        var orders = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted)
            .AsNoTracking();

        if (factoryId.HasValue)
            orders = orders.Where(o => o.FactoryId == factoryId.Value);

        // ✅ الانضمام إلى أنواع الخرسانة لا يحمل فلتر الحذف الناعم عمدًا:
        //    لو طُبِّق لصار INNER JOIN يُسقط طلبات نوع خرسانة محذوف فتختفي طلبات تاريخية.
        //    الصف المحذوف ناعمًا ما زال موجودًا فعليًا باسمه، فيبقى العدّ صحيحًا
        //    ويظل النوع مُعرَّفًا بمعرّفه واسمه.
        var grouped = await (
                from o in orders
                join c in _context.ConcreteTypes.IgnoreQueryFilters().AsNoTracking()
                    on o.ConcreteTypeId equals c.ConcreteTypeId
                group o by new { o.ConcreteTypeId, c.Name }
                into g
                select new ConcreteTypeCountDto
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