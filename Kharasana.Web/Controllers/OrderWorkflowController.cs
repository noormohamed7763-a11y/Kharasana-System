using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    /// <summary>
    /// عمليات AJAX لدورة حياة الطلب: تسعير، موافقة، رفض، إلغاء، توصيل، إغلاق، تعيين سائق، تغيير حالة.
    /// تُفصّل عن OrdersController (الذي يتعامل مع العرض CRUD) لتقليل حجم الكود وتبسيط الصيانة.
    /// </summary>
    [SessionAuthorize]
    public class OrderWorkflowController : BaseController
    {
        private readonly IOrdersApiService _ordersApiService;
        private readonly ILookupApiService _lookupApiService;
        private readonly ILogger<OrderWorkflowController> _logger;

        public OrderWorkflowController(
            IOrdersApiService ordersApiService,
            ILookupApiService lookupApiService,
            ILogger<OrderWorkflowController> logger)
        {
            _ordersApiService = ordersApiService ?? throw new ArgumentNullException(nameof(ordersApiService));
            _lookupApiService = lookupApiService ?? throw new ArgumentNullException(nameof(lookupApiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================================
        // SAVE PRICE - حفظ سعر المتر (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePrice(int id, [FromBody] SavePriceDto dto)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            if (dto == null)
                return AjaxFail(AppMessages.Validation.RequiredField);

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.SavePriceAsync(id, dto.UnitPrice);

                if (result)
                    return AjaxSuccess(AppMessages.Success.PriceSaved);

                return AjaxFail(AppMessages.Error.PriceSave);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في حفظ سعر الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في حفظ سعر الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // APPROVE - موافقة العميل (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.ApproveOrderAsync(id);

                if (result)
                    return AjaxSuccess(AppMessages.Success.Approved);

                return AjaxFail(AppMessages.Error.Approved);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في الموافقة على الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في الموافقة على الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // REJECT - رفض الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderDto dto)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            if (dto == null)
                return AjaxFail(AppMessages.Validation.RequiredField);

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.RejectOrderAsync(id, dto.Reason ?? string.Empty);

                if (result)
                    return AjaxSuccess(AppMessages.Success.Rejected);

                return AjaxFail(AppMessages.Error.Rejected);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في رفض الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في رفض الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // CANCEL - إلغاء الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.CancelOrderAsync(id);

                if (result)
                    return AjaxSuccess(AppMessages.Success.Cancelled);

                return AjaxFail(AppMessages.Error.Cancelled);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في إلغاء الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في إلغاء الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // START DELIVERY - بدء التوصيل (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDelivery(int id)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.StartDeliveryAsync(id);

                if (result)
                    return AjaxSuccess(AppMessages.Success.DeliveryStarted);

                return AjaxFail(AppMessages.Error.DeliveryStarted);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في بدء توصيل الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في بدء توصيل الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // DELIVER - تم التسليم (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deliver(int id)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.DeliverOrderAsync(id);

                if (result)
                    return AjaxSuccess(AppMessages.Success.Delivered);

                return AjaxFail(AppMessages.Error.Delivered);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تسليم الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تسليم الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // CLOSE - إغلاق الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var result = await _ordersApiService.CloseOrderAsync(id);

                if (result)
                    return AjaxSuccess(AppMessages.Success.Closed);

                return AjaxFail(AppMessages.Error.Closed);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في إغلاق الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في إغلاق الطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // ASSIGN DRIVER - تعيين سائق (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverViewModel model)
        {
            if (!EnsureValidId(id))
                return AjaxInvalidId();

            if (model == null)
                return AjaxFail(AppMessages.Common.InvalidData);

            if (!ModelState.IsValid)
                return AjaxFail(AppMessages.Common.InvalidData);

            try
            {
                var authResult = await EnsureCanModifyOrderAsync(id);
                if (authResult != null) return authResult;

                var allDrivers = await _lookupApiService.GetAvailableDriversAsync();

                // ✅ موظف المصنع يُقيَّد بسائقي مصنعه، والمدير (بلا مصنع) يرى القائمة كاملة
                //    مطابقةً لقائمة النافذة في Orders/Details؛ التحقق النهائي من تطابق
                //    مصنع السائق مع مصنع الطلب مسؤولية OrderService.AssignDriverAsync.
                var factoryDrivers = FactoryId.HasValue
                    ? allDrivers.Where(d => d.FactoryId == FactoryId.Value).ToList()
                    : allDrivers;

                var driverExists = factoryDrivers.Any(d => d.Id == model.DriverId);

                if (!driverExists)
                    return AjaxFail(AppMessages.Common.DriverNotInFactory);

                var ok = await _ordersApiService.AssignDriverAsync(id, model);

                if (!ok)
                    return AjaxFail(AppMessages.Error.DriverAssign);

                return AjaxSuccess(AppMessages.Success.DriverAssigned);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تعيين سائق للطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return AjaxFail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تعيين سائق للطلب {OrderId}", id);
                return AjaxFail(ex);
            }
        }

        // ============================================================
        // Helpers — تقليل التكرار عبر الأفعال
        // ============================================================

        /// <summary>
        /// تحقق من صحة المعرّف؛ يُعيد BadRequest بصيغة JSON إذا كان غير صالح.
        ///
        /// <para>⚠️ لا يكتب TempData: مستدعياته الثمانية كلها أفعال AJAX تُعيد
        /// <see cref="AjaxInvalidId"/>، والـ JS يقرأ الرسالة من الـ JSON. والكتابة في
        /// TempData هنا كانت تُبقي الرسالة حتى أول صفحة كاملة تالية فتظهر في غير
        /// موضعها عبر <c>_Alerts</c> (وهي تُقرأ وتُفرَّغ عند كل عرض كامل).</para>
        /// </summary>
        private bool EnsureValidId(int id) => id > 0;

        /// <summary>تحقق أن FactoryEmployee يملك صلاحية تعديل الطلب؛ يُعيد BadRequest إذا لا.</summary>
        private async Task<IActionResult?> EnsureCanModifyOrderAsync(int id)
        {
            if (RoleValue == UserRole.FactoryEmployee)
            {
                var order = await _ordersApiService.GetOrderByIdAsync(id);
                if (order == null || IsFactoryIsolated(order.FactoryId))
                    return BadRequest(new { success = false, message = AppMessages.Common.OrderCannotModify });
            }
            return null;
        }

        /// <summary>تحقق من معرّف الطلب في أفعال AJAX؛ يُعيد BadRequest إذا غير صالح.</summary>
        private IActionResult AjaxInvalidId() =>
            BadRequest(new { success = false, message = AppMessages.Error.InvalidId });

        private IActionResult AjaxFail(string message) =>
            BadRequest(new { success = false, message });

        /// <summary>
        /// فشل عام في فعل AJAX: الرسالة العامة عمدًا ولا يُمرَّر نصّ الاستثناء إلى العميل
        /// (التفاصيل تُسجَّل في الـ logger عند موضع النداء). <paramref name="ex"/> غير
        /// مستعمل عن قصد — لا تُحوِّل هذا إلى <c>ex.Message</c>.
        /// </summary>
        private IActionResult AjaxFail(Exception ex) =>
            BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });

        private IActionResult AjaxSuccess(string message) =>
            Ok(new { success = true, message });

        // ============================================================
        // DTOs
        // ============================================================
        public class SavePriceDto
        {
            public decimal UnitPrice { get; set; }
        }
    }
}
