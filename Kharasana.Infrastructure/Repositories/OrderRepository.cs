using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Order;
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
    /// <remarks>
    /// ✅ <c>IgnoreQueryFilters</c> هنا مقصود، مع إعادة تطبيق فلتر الطلبات يدويًا بعده.
    ///
    /// السبب: <c>Client</c> و<c>Factory</c> و<c>ConcreteType</c> علاقات <b>إلزامية</b>
    /// (مفاتيحها غير قابلة للعدم)، فيُترجم <c>Include</c> عليها إلى INNER JOIN. وفلتر
    /// الحذف الناعم الخاص بالكيان المرجعي كان يُطبَّق على هذا الانضمام، فيُسقط
    /// <b>صف الطلب نفسه</b> لا صف الكيان المرجعي وحده — أي أن أرشفة نوع خرسانة واحد
    /// كانت تُمحي طلباته التاريخية من القوائم والتفاصيل وملخصات العملاء بلا أي خطأ ظاهر.
    ///
    /// و<c>Driver</c> علاقة اختيارية (LEFT JOIN) فلا تُسقط الصف، لكن الفلتر كان يُفرغ
    /// مرجعها: طلب مُسلَّم لسائق مؤرشف كان يُعرض بلا اسم سائق ولا هاتف ولا رقم شاحنة،
    /// فيضيع أثر التسليم. وتجاوز الفلتر يعيده.
    ///
    /// وتجاوز الفلتر آمن لأن الصف المحذوف ناعمًا ما زال موجودًا فعليًا في الجدول،
    /// فيبقى الكيان المرجعي مُعرَّفًا بمعرّفه واسمه كما كان وقت الطلب — وهو نفس المبدأ
    /// المُطبَّق في <see cref="GetCountByConcreteTypeAsync"/>.
    ///
    /// ⚠️ ومقابل ذلك يُعاد فرض فلتر الطلبات صراحةً (<c>!o.IsDeleted</c>) لأن
    /// <c>IgnoreQueryFilters</c> يلغي فلاتر <b>كل</b> الكيانات في الاستعلام لا مجموعة
    /// واحدة؛ فبدونه تصير كل مسارات القراءة والتعديل هذه بابًا خلفيًا للطلبات المحذوفة.
    /// الحارس: OrderRepositoryQueryFilterTests.
    /// </remarks>
    private IQueryable<Order> OrdersWithDetails(bool trackChanges)
    {
        var query = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted)
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

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        CallerContext caller, int? factoryId, int? clientId, int? driverId, OrderStatus? status, string? search, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted);

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
            .Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                ClientName = o.Client != null ? o.Client.FullName : string.Format(Messages.ClientFallback, o.ClientId),
                ClientPhone = o.Client != null ? o.Client.Phone : null,
                DriverId = o.DriverId,
                DriverName = o.Driver != null ? o.Driver.FullName : string.Empty,
                FactoryName = o.Factory != null ? o.Factory.FactoryName : string.Format(Messages.FactoryFallback, o.FactoryId),
                ConcreteTypeName = o.ConcreteTypeNameSnapshot ?? (o.ConcreteType != null ? o.ConcreteType.Name : string.Format(Messages.ConcreteTypeFallback, o.ConcreteTypeId)),
                Quantity = o.Quantity,
                TotalPrice = (caller.Role == Domain.Enums.UserRole.Driver) ? null : o.TotalPrice,
                TransportMethod = o.TransportMethod,
                Status = o.Status,
                PouringDate = o.PouringDate,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// استعلام «ملخص العميل» — تعريف واحد يخدم قائمة العملاء وقراءة عميل واحد،
    /// فلا يفترق العدّادان بين الشاشتين.
    /// </summary>
    private IQueryable<CustomerSummaryDto> CustomerSummaryQuery(int? factoryId)
    {
        var query = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted);

        if (factoryId.HasValue)
            query = query.Where(o => o.FactoryId == factoryId.Value);

        return query
            .GroupBy(o => new
            {
                o.ClientId,
                o.Client.FullName,
                o.Client.Phone,
                o.Client.Email,
                o.Client.WhatsApp,
                o.Client.IsActive
            })
            .Select(g => new CustomerSummaryDto
            {
                UserId = g.Key.ClientId,
                FullName = g.Key.FullName,
                Phone = g.Key.Phone,
                Email = g.Key.Email,
                WhatsApp = g.Key.WhatsApp,
                IsActive = g.Key.IsActive,
                OrdersCount = g.Count(),
                TotalQuantity = g.Sum(o => o.Quantity),
                LastOrderDate = g.Max(o => (DateTime?)o.CreatedAt)
            });
    }

    public async Task<CustomerSummaryDto?> GetFactoryCustomerAsync(int customerId, int? factoryId)
    {
        return await CustomerSummaryQuery(factoryId)
            .FirstOrDefaultAsync(c => c.UserId == customerId);
    }

    public async Task<PagedResult<CustomerSummaryDto>> GetFactoryCustomersAsync(
        int? factoryId, string? search, int pageNumber, int pageSize)
    {
        var grouped = CustomerSummaryQuery(factoryId);

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