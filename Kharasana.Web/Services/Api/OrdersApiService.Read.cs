using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    // الجزء الخاص بالقراءة: جلب الطلبات وتفاصيلها وعدّ الحالات وطلبات السائق.
    // الحقول والبنية التحتية المشتركة في OrdersApiService.cs.
    public partial class OrdersApiService
    {
        // ============================================================
        // GET ORDERS - جلب قائمة الطلبات مع ترقيم الصفحات والتصفية
        // ============================================================
        public async Task<PagedResult<OrderDto>?> GetOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? search = null,
            int? factoryId = null,
            int? status = null)
        {
            try
            {
                var query = $"Orders?PageNumber={pageNumber}&PageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search))
                    query += $"&Search={Uri.EscapeDataString(search)}";
                if (factoryId.HasValue)
                    query += $"&factoryId={factoryId.Value}";
                if (status.HasValue)
                    query += $"&status={status.Value}";

                _logger.LogInformation("📋 Fetching orders with query: {Query}", query);

                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<OrderDto>>>(query);

                if (response == null)
                {
                    _logger.LogWarning("❌ GetOrdersAsync: API returned null for query {Query}", query);
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ GetOrdersAsync: API returned success=false. Message: {Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("✅ GetOrdersAsync: Retrieved {Count} orders", response.Data?.Items?.Count() ?? 0);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in GetOrdersAsync: {Message}", ex.Message);
                return null;
            }
        }

        // ============================================================
        // GET STATUS COUNTS - عدّ الطلبات حسب الحالة عبر كل الصفحات
        // ============================================================
        public async Task<(int Pending, int Closed, int Cancelled, int Rejected)> GetStatusCountsAsync(
            string? search = null,
            int? factoryId = null)
        {
            try
            {
                // PageSize=1 لجلب TotalCount فقط دون تحميل بيانات الصفحة
                string CountQuery(OrderStatus status) =>
                    $"Orders?PageNumber=1&PageSize=1&status={(int)status}"
                    + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
                    + (factoryId.HasValue ? $"&factoryId={factoryId.Value}" : "");

                _logger.LogInformation("📊 Fetching order status counts (Search={Search}, FactoryId={FactoryId})", search, factoryId);

                // ✅ تنفيذ متوازٍ — ApiClient يضيف رأس Authorization لكل طلب على حدة
                var pendingTask = _apiClient.GetPagedTotalAsync<OrderDto>(CountQuery(OrderStatus.Pending));
                var closedTask = _apiClient.GetPagedTotalAsync<OrderDto>(CountQuery(OrderStatus.Closed));
                var cancelledTask = _apiClient.GetPagedTotalAsync<OrderDto>(CountQuery(OrderStatus.Cancelled));
                var rejectedTask = _apiClient.GetPagedTotalAsync<OrderDto>(CountQuery(OrderStatus.Rejected));

                await Task.WhenAll(pendingTask, closedTask, cancelledTask, rejectedTask);

                return (pendingTask.Result, closedTask.Result, cancelledTask.Result, rejectedTask.Result);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in GetStatusCountsAsync: {Message}", ex.Message);
                return (0, 0, 0, 0);
            }
        }

        // ============================================================
        // GET ORDER BY ID - جلب تفاصيل طلب محدد
        // ============================================================
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Fetching order details for ID: {Id}", id);

                var response = await _apiClient.GetAsync<ApiResponse<OrderDto>>($"Orders/{id}");

                if (response == null)
                {
                    _logger.LogWarning("❌ GetOrderByIdAsync: API returned null for id={Id}", id);
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ GetOrderByIdAsync: API returned success=false for id={Id}. Message: {Message}", id, response.Message);
                    return null;
                }

                if (response.Data == null)
                {
                    _logger.LogWarning("❌ GetOrderByIdAsync: Response.Data is null for id={Id}", id);
                    return null;
                }

                _logger.LogInformation("✅ GetOrderByIdAsync: Retrieved order {Id} - {OrderNumber}", id, response.Data.OrderNumber);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in GetOrderByIdAsync for id={Id}: {Message}", id, ex.Message);
                return null;
            }
        }

        // ============================================================
        // GET ORDERS BY DRIVER ID - طلبات سائق لتقرير الطباعة
        // ============================================================
        public async Task<IEnumerable<OrderDto>?> GetOrdersByDriverIdAsync(int driverId)
        {
            try
            {
                _logger.LogInformation("📋 Fetching all orders for driver {DriverId} (print report)", driverId);

                var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<OrderDto>>>($"Orders/by-driver/{driverId}");

                if (response == null)
                {
                    _logger.LogWarning("❌ GetOrdersByDriverIdAsync: API returned null for driverId={DriverId}", driverId);
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ GetOrdersByDriverIdAsync: API returned success=false for driverId={DriverId}. Message: {Message}", driverId, response.Message);
                    return null;
                }

                _logger.LogInformation("✅ GetOrdersByDriverIdAsync: Retrieved {Count} orders for driver {DriverId}", response.Data?.Count() ?? 0, driverId);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in GetOrdersByDriverIdAsync for driverId={DriverId}: {Message}", driverId, ex.Message);
                return null;
            }
        }
    }
}
