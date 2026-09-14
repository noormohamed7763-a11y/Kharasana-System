using System;
using System.Collections.Generic;
using System.Linq;
using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Dashboard;
using Kharasana.Web.ViewModels.Orders;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Domain.Enums;
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
                _logger.LogDebug("Requesting Admin dashboard data from API");
                var response = await _apiClient.GetAsync<ApiResponse<AdminDashboardDto>>("Dashboard/admin");

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("Admin dashboard API returned null or empty. Returning default VM.");
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

                return vm;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while loading admin dashboard");
                return new DashboardViewModel();
            }
        }

        public async Task<DashboardViewModel?> GetFactoryDashboardAsync(int factoryId)
        {
            try
            {
                _logger.LogDebug("Requesting Factory({FactoryId}) dashboard data from API", factoryId);
                var response = await _apiClient.GetAsync<ApiResponse<FactoryDashboardDto>>($"Dashboard/factory?factoryId={factoryId}");

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("Factory dashboard API returned null or empty for factoryId={FactoryId}. Returning empty factory dashboard.", factoryId);

                    return new DashboardViewModel();
                }

                // تعيين البيانات القادمة من الـ API إلى الأسماء النظيفة الجديدة في الـ ViewModel
                var vm = new DashboardViewModel
                {
                    TodayOrdersCount = response.Data.NewOrders,
                    InProgressOrdersCount = response.Data.PendingOrders,
                    ReadyOrdersCount = response.Data.ApprovedOrders,
                    OnTheWayOrders = response.Data.OnTheWayOrders,
                    DeliveredToday = response.Data.DeliveredToday,
                    AvailableDrivers = response.Data.AvailableDrivers
                };

                vm.RecentOrders = await LoadRecentOrdersAsync(factoryId);

                _logger.LogInformation("Factory dashboard loaded for factoryId={FactoryId}: TodayOrdersCount={TodayOrdersCount}", factoryId, vm.TodayOrdersCount);

                return vm;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while loading factory dashboard for factoryId={FactoryId}", factoryId);
                return new DashboardViewModel();
            }
        }

        /// <summary>
        /// تحميل آخر طلبات المصنع الحقيقية (الأحدث أولاً) لعرضها في لوحة المصنع.
        /// يستخدم نفس Endpoint (Orders) وبالتالي يُطبَّق عليه عزل المصنع تلقائياً من الـ JWT.
        /// </summary>
        private async Task<List<RecentOrderDto>> LoadRecentOrdersAsync(int factoryId)
        {
            const int pageSize = 6;

            var result = await _ordersApiService.GetOrdersAsync(pageNumber: 1, pageSize: pageSize, factoryId: factoryId);
            if (result == null)
            {
                _logger.LogWarning("Failed to load recent orders for dashboard (factoryId={FactoryId}). Showing empty state.", factoryId);
                return new List<RecentOrderDto>();
            }

            return result.Items
                .Select(o => new RecentOrderDto
                {
                    Id = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    ClientName = o.ClientName,
                    DriverName = o.DriverName,
                    StatusDisplay = o.StatusArabic,
                    StatusCssClass = GetStatusCssSuffix(o.Status),
                    OrderDate = o.CreatedAt
                })
                .ToList();
        }

        /// <summary>
        /// لاحقة كلاس الحالة (بدون بادئة status-) بحيث يكوّن الـ View `status-new`... إلخ
        /// بما يطابق الأنماط المعرّفة في components.css (أحرف صغيرة).
        /// </summary>
        private static string GetStatusCssSuffix(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "new",
                OrderStatus.Pending => "pending",
                OrderStatus.Approved => "approved",
                OrderStatus.Rejected => "rejected",
                OrderStatus.Cancelled => "cancelled",
                OrderStatus.OnTheWay => "ontheway",
                OrderStatus.Delivered => "delivered",
                OrderStatus.Closed => "closed",
                _ => "new"
            };
        }
    }
}
