using Kharasana.Application.Common;
using Kharasana.Web.Helpers;
using Kharasana.Web.ViewModels.Dashboard;
using Kharasana.Web.ViewModels.Orders;
using Kharasana.Web.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    public class DashboardApiService : IDashboardApiService
    {
        private readonly ApiClient _apiClient;
        private readonly IOrdersApiService _ordersApiService;
        private readonly ILogger<DashboardApiService> _logger;

        public DashboardApiService(ApiClient apiClient, IOrdersApiService ordersApiService, ILogger<DashboardApiService> logger)
        {
            _apiClient = apiClient;
            _ordersApiService = ordersApiService;
            _logger = logger;
        }

        public async Task<DashboardViewModel?> GetAdminDashboardAsync()
        {
            try
            {
                _logger.LogDebug("طلب بيانات لوحة المدير من الـ API");
                var response = await _apiClient.GetAsync<ApiResponse<AdminDashboardDto>>("Dashboard/admin");

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("أعادت واجهة لوحة المدير ردًّا فارغًا. تُعاد قيمة افتراضية.");
                    return new DashboardViewModel();
                }

                var vm = new DashboardViewModel
                {
                    TotalFactories = response.Data.TotalFactories,
                    TotalClients = response.Data.TotalClients,
                    TotalEmployees = response.Data.TotalEmployees,
                    TotalDrivers = response.Data.TotalDrivers,
                    TotalOrders = response.Data.TotalOrders
                };

                // أحدث الطلبات: `_AdminDashboard` يعرضها ويتعامل مع فراغها، وكانت
                // تُترك فارغة هنا دائماً فيظهر «لا توجد طلبات مسجلة في النظام بعد»
                // ولو كان في النظام مئات الطلبات — معلومة خاطئة لا ناقصة.
                // بلا معرّف مصنع: الـ API لا يحصر الطلبات على الأدمن فيعيدها كلها،
                // وهو المطلوب لعرض «أحدث الطلبات» على مستوى النظام.
                vm.RecentOrders = await LoadRecentOrdersAsync(null);

                return vm;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل لوحة المدير");
                return new DashboardViewModel();
            }
        }

        public async Task<DashboardViewModel?> GetFactoryDashboardAsync(int factoryId)
        {
            try
            {
                _logger.LogDebug("طلب بيانات لوحة المصنع ({FactoryId}) من الـ API", factoryId);
                var response = await _apiClient.GetAsync<ApiResponse<FactoryDashboardDto>>($"Dashboard/factory?factoryId={factoryId}");

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("أعادت واجهة لوحة المصنع ردًّا فارغًا للمصنع {FactoryId}. تُعاد لوحة فارغة.", factoryId);

                    return new DashboardViewModel();
                }

                // تعيين البيانات القادمة من الـ API إلى الأسماء النظيفة الجديدة في الـ ViewModel
                var vm = new DashboardViewModel
                {
                    TodayOrdersCount = response.Data.TodayOrders,
                    InProgressOrdersCount = response.Data.PendingOrders,
                    ReadyOrdersCount = response.Data.ApprovedOrders,
                    OnTheWayOrders = response.Data.OnTheWayOrders,
                    DeliveredToday = response.Data.DeliveredToday,
                    AvailableDrivers = response.Data.AvailableDrivers
                };

                vm.RecentOrders = await LoadRecentOrdersAsync(factoryId);

                _logger.LogInformation("حُمّلت لوحة المصنع {FactoryId}: طلبات اليوم={TodayOrdersCount}", factoryId, vm.TodayOrdersCount);

                return vm;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل لوحة المصنع {FactoryId}", factoryId);
                return new DashboardViewModel();
            }
        }

        /// <summary>
        /// تحميل آخر الطلبات الحقيقية (الأحدث أولاً) لعرضها في اللوحة.
        /// يستخدم نفس Endpoint (Orders) وبالتالي يُطبَّق عليه عزل المصنع تلقائياً من الـ JWT.
        /// </summary>
        /// <param name="factoryId">
        /// معرّف المصنع (لوحة المصنع)، أو <c>null</c> (لوحة المدير) فيعيد الـ API
        /// طلبات النظام كله لأن المدير غير محصور بمصنع.
        /// </param>
        private async Task<List<RecentOrderDto>> LoadRecentOrdersAsync(int? factoryId)
        {
            const int pageSize = 6;

            var result = await _ordersApiService.GetOrdersAsync(pageNumber: 1, pageSize: pageSize, factoryId: factoryId);
            if (result == null)
            {
                _logger.LogWarning("تعذّر جلب أحدث الطلبات للوحة (المصنع {FactoryId}). تُعرض حالة فارغة.", factoryId);
                return new List<RecentOrderDto>();
            }

            return result.Items
                .Select(o => new RecentOrderDto
                {
                    Id = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    ClientName = o.ClientName,
                    DriverName = o.DriverName,
                    Status = o.Status,
                    StatusDisplay = o.StatusArabic,
                    // ✅ اللاحقة من طبقة العرض الوحيدة OrderStatusExtensions — كانت هنا خريطة
                    //    ثالثة كاملة للواحق الحالات. ملاحظة: RecentOrderDto.StatusCssClass يحمل
                    //    اللاحقة وحدها (بدون status-)، ويضيفها الـ View.
                    StatusCssClass = o.Status.GetCssSuffix(),
                    OrderDate = o.CreatedAt
                })
                .ToList();
        }
    }
}
