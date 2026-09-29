using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers
{
    // الأجزاء الأخرى من هذا المتحكّم:
    //   OrdersController.Read.cs     — Index / Details / Create (GET) / Edit (GET).
    //   OrdersController.Commands.cs — Create (POST) / Edit (POST) / Delete (POST).
    /// <summary>
    /// عرض وإدارة الطلبات: قائمة، تفاصيل، إنشاء، تعديل، حذف.
    /// العمليات المنبثقة (تسعير، موافقة، رفض، إلغاء، توصيل، إغلاق، تعيين سائق، تغيير حالة)
    /// نُقلت إلى <see cref="OrderWorkflowController"/> بينما تبقى هنا العمليات CRUD الأساسية.
    /// </summary>
    [SessionAuthorize]
    public partial class OrdersController : BaseController
    {
        private readonly IOrdersApiService _ordersApiService;
        private readonly ILookupApiService _lookupApiService;
        private readonly IFactoryApiService _factoryService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrdersApiService ordersApiService,
            ILookupApiService lookupApiService,
            IFactoryApiService factoryService,
            ILogger<OrdersController> logger)
        {
            _ordersApiService = ordersApiService ?? throw new ArgumentNullException(nameof(ordersApiService));
            _lookupApiService = lookupApiService ?? throw new ArgumentNullException(nameof(lookupApiService));
            _factoryService = factoryService ?? throw new ArgumentNullException(nameof(factoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// إعادة تحميل بيانات الطلب والقوائم المنسدلة بعد فشل التعديل، لعرض النموذج كاملاً.
        /// </summary>
        /// <remarks>
        /// القيم المشتقّة من النموذج تُضبط قبل نداء الـ API، فيبقى النموذج قابلاً للإرسال
        /// حتى لو فشل النداء — وهو المرجَّح في مسارات الالتقاط التي تستدعي هذه الدالة.
        /// وبيانات الطلب للقراءة فقط (رقم الطلب، العميل، الحالة، سعر المتر) تُقرأ من الطلب
        /// نفسه: وضع قيمة حرفية مكانها كان يُظهر «العميل» اسمًا للعميل، و«غير معروف» حالةً،
        /// و«-» سعرًا — معلومة خاطئة لا ناقصة.
        /// </remarks>
        private async Task ReloadEditViewDataAsync(EditOrderViewModel model)
        {
            ViewBag.OrderId = model.OrderId;
            ViewBag.OrderNumber = model.OrderId.ToString();
            ViewBag.ShowDeleteButton = true;

            try
            {
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", model.ConcreteTypeId);

                var order = await _ordersApiService.GetOrderByIdAsync(model.OrderId);

                // نفس حصر مسار GET: بيانات طلب مصنع آخر لا تُعرض لموظّف مصنع. لازمة هنا
                // لأن فرع «النموذج غير صالح» يقع قبل أي نداء API، فلا يمرّ بالحصر هناك.
                if (order == null || IsFactoryIsolated(order.FactoryId))
                    return;

                ViewBag.OrderNumber = order.OrderNumber;
                ViewBag.ClientName = order.ClientName;
                ViewBag.Status = order.Status;
                ViewBag.UnitPrice = order.UnitPrice ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الطلب بعد فشل التعديل");
            }
        }
    }
}
