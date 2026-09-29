using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    // الأجزاء الأخرى من هذه الخدمة:
    //   OrdersApiService.Read.cs      — جلب الطلبات وتفاصيلها وعدّ الحالات وطلبات السائق.
    //   OrdersApiService.Commands.cs  — إنشاء الطلب الهاتفي وتعديله وحذفه.
    //   OrdersApiService.Workflow.cs  — التسعير ودورة حياة الطلب وإسناد السائق.
    public partial class OrdersApiService : IOrdersApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<OrdersApiService> _logger;

        public OrdersApiService(ApiClient apiClient, ILogger<OrdersApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ============================================================
        // ACTION HELPER - توحيد معالجة إجراءات الطلب
        // ============================================================
        /// <summary>
        /// ينفّذ إجراءً على طلب (تسعير، موافقة، رفض، إلغاء، توصيل، إغلاق، إسناد سائق، حذف)
        /// ويوحّد فحص الاستجابة والتسجيل ومعالجة الأخطاء في مكان واحد.
        /// <see cref="ApiServiceException"/> تُعاد رميًا كما هي ليتعامل معها المستدعي،
        /// وأي استثناء آخر يُسجَّل وتُعاد <c>false</c>.
        /// </summary>
        /// <param name="operation">اسم العملية — يُستخدم في رسائل السجل (مطابق لاسم الدالة المستدعية).</param>
        /// <param name="successDetail">تفاصيل رسالة النجاح، بلا بادئة النجاح.</param>
        /// <param name="call">نداء الـ API الفعلي (يُنفَّذ داخل try ليشمل الشبكة والتسلسل).</param>
        /// <param name="id">معرّف الطلب — للتسجيل فقط.</param>
        private async Task<bool> ExecuteOrderActionAsync(
            string operation,
            string successDetail,
            Func<Task<ApiResponse<object>?>> call,
            int id)
        {
            try
            {
                var response = await call();

                if (response == null)
                {
                    _logger.LogWarning("{Operation}: ردّ الـ API فارغ للمعرّف {Id}", operation, id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("فشل {Operation} للمعرّف {Id}. الرسالة: {Message}", operation, id, response.Message);
                    return false;
                }

                _logger.LogInformation("{Operation}: {Detail}", operation, successDetail);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء في {Operation} للمعرّف {Id}: {Message}", operation, id, ex.Message);
                return false;
            }
        }
    }
}
