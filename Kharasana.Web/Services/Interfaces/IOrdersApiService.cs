using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Orders;

namespace Kharasana.Web.Services.Interfaces
{
    /// <summary>
    /// واجهة خدمات الطلبات - مسؤولة عن التواصل مع API الطلبات
    /// </summary>
    public interface IOrdersApiService
    {
        // ============================================================
        // 1. READ - عمليات القراءة
        // ============================================================

        /// <summary>
        /// جلب قائمة الطلبات مع ترقيم الصفحات والبحث والتصفية حسب الحالة
        /// </summary>
        /// <param name="pageNumber">رقم الصفحة</param>
        /// <param name="pageSize">عدد العناصر في الصفحة</param>
        /// <param name="search">نص البحث</param>
        /// <param name="factoryId">معرف المصنع (للموظف)</param>
        /// <param name="status">تصفية حسب الحالة (اختياري)</param>
        /// <returns>قائمة الطلبات مع معلومات الترقيم</returns>
        Task<PagedResult<OrderDto>?> GetOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? search = null,
            int? factoryId = null,
            int? status = null); // ✅ أضف معامل status


        /// <summary>
        /// جلب تفاصيل طلب محدد
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>تفاصيل الطلب</returns>
        Task<OrderDto?> GetOrderByIdAsync(int id);


        // ============================================================
        // 2. CREATE - عمليات الإنشاء
        // ============================================================

        /// <summary>
        /// إنشاء طلب هاتفي (بواسطة موظف المصنع)
        /// </summary>
        /// <param name="model">بيانات الطلب الهاتفي</param>
        /// <returns>تفاصيل الطلب المنشأ</returns>
        Task<OrderDto?> CreatePhoneOrderAsync(CreatePhoneOrderViewModel model);


        // ============================================================
        // 3. UPDATE - عمليات التحديث
        // ============================================================

        /// <summary>
        /// تعديل طلب موجود
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <param name="model">بيانات الطلب المعدلة</param>
        /// <returns>تفاصيل الطلب المعدل</returns>
        Task<OrderDto?> UpdateOrderAsync(int id, EditOrderViewModel model);

        /// <summary>
        /// حذف طلب
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تم الحذف بنجاح</returns>
        Task<bool> DeleteOrderAsync(int id);


        // ============================================================
        // 4. PRICING - التسعير
        // ============================================================

        /// <summary>
        /// حفظ سعر المتر للطلب
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <param name="unitPrice">سعر المتر</param>
        /// <returns>true إذا تم الحفظ بنجاح</returns>
        Task<bool> SavePriceAsync(int id, decimal unitPrice);


        // ============================================================
        // 5. APPROVAL - الموافقة والرفض والإلغاء
        // ============================================================

        /// <summary>
        /// موافقة العميل على الطلب (Pending → Approved)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تمت الموافقة بنجاح</returns>
        Task<bool> ApproveOrderAsync(int id);

        /// <summary>
        /// رفض الطلب من قبل المصنع (New/Pending → Rejected)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <param name="reason">سبب الرفض (اختياري)</param>
        /// <returns>true إذا تم الرفض بنجاح</returns>
        Task<bool> RejectOrderAsync(int id, string? reason); // ✅ أضف ? لتكون اختيارية

        /// <summary>
        /// إلغاء الطلب من قبل العميل أو الموظف (قبل التسليم)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تم الإلغاء بنجاح</returns>
        Task<bool> CancelOrderAsync(int id);


        // ============================================================
        // 6. DELIVERY - التوصيل
        // ============================================================

        /// <summary>
        /// بدء التوصيل (Approved → OnTheWay)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تم بدء التوصيل بنجاح</returns>
        Task<bool> StartDeliveryAsync(int id);

        /// <summary>
        /// تأكيد تسليم الطلب (OnTheWay → Delivered)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تم تأكيد التسليم بنجاح</returns>
        Task<bool> DeliverOrderAsync(int id);

        /// <summary>
        /// إغلاق الطلب نهائياً (Delivered → Closed)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <returns>true إذا تم الإغلاق بنجاح</returns>
        Task<bool> CloseOrderAsync(int id);


        // ============================================================
        // 7. DRIVER - السائق
        // ============================================================

        /// <summary>
        /// تعيين سائق للطلب
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <param name="model">بيانات السائق ورقم الشاحنة</param>
        /// <returns>true إذا تم التعيين بنجاح</returns>
        Task<bool> AssignDriverAsync(int id, AssignDriverViewModel model);


        // ============================================================
        // 8. STATUS - الحالة (عام)
        // ============================================================

        /// <summary>
        /// تحديث حالة الطلب (استخدام عام)
        /// </summary>
        /// <param name="id">معرف الطلب</param>
        /// <param name="model">الحالة الجديدة</param>
        /// <returns>true إذا تم التحديث بنجاح</returns>
        Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusViewModel model);
    }
}