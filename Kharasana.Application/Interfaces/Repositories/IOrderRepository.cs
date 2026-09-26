using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Customer;
using Kharasana.Application.DTOs.Report;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    /// <summary>
    /// قراءة الطلب مع تفاصيله للعرض فقط (AsNoTracking).
    /// </summary>
    Task<Order?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// قراءة الطلب مع تفاصيله <b>مع تتبّع التغييرات</b> — لمسارات التعديل فقط.
    ///
    /// السبب: كيانات التفاصيل (العميل، المصنع، نوع الخرسانة، السائق) كلها تحمل
    /// <c>RowVersion</c>. قراءتها بلا تتبّع ثم تمريرها إلى <c>Update</c> تُرفق الرسم
    /// البياني كاملاً بحالة Modified، فيُرسل UPDATE إضافي محروس بـ RowVersion على
    /// كل كيان مرتبط مع كل تغيير حالة طلب — كتابات زائدة، وتضخّم RowVersion لكيانات
    /// لم تتغيّر فيفشل تعديل مشروع عليها بتعارض وهمي (409)، وعند إسناد سائق جديد
    /// لطلب له سائق سابق يتعارض المثيلان فيرمي EF استثناء تتبّع (500).
    /// </summary>
    Task<Order?> GetByIdWithDetailsForUpdateAsync(int id);

    Task<PagedResult<Order>> GetPagedAsync(
        int? factoryId, int? clientId, int? driverId, OrderStatus? status, string? search, int pageNumber, int pageSize);

    Task<PagedResult<CustomerSummaryDto>> GetFactoryCustomersAsync(
        int? factoryId, string? search, int pageNumber, int pageSize);

    /// <summary>Reports: order count grouped by status.</summary>
    Task<List<OrderStatusCountDto>> GetCountByStatusAsync(int? factoryId);

    /// <summary>Reports: order count grouped by concrete type.</summary>
    Task<List<ConcreteTypeCountDto>> GetCountByConcreteTypeAsync(int? factoryId);
}