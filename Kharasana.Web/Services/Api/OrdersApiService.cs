using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace Kharasana.Web.Services.Api
{
    public class OrdersApiService : IOrdersApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<OrdersApiService> _logger;

        public OrdersApiService(ApiClient apiClient, ILogger<OrdersApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

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

        // ============================================================
        // CREATE PHONE ORDER - إنشاء طلب هاتفي
        // ============================================================
        public async Task<OrderDto?> CreatePhoneOrderAsync(CreatePhoneOrderViewModel model)
        {
            try
            {
                // ✅ تحويل البيانات إلى PhoneOrderDto
                var phoneOrderDto = new
                {
                    clientPhone = model.ClientPhone,
                    clientFullName = model.ClientFullName,
                    factoryId = model.FactoryId,
                    concreteTypeId = model.ConcreteTypeId,
                    projectName = model.ProjectName,
                    projectOwnerName = model.ProjectOwnerName,
                    siteArea = model.SiteArea,
                    siteDescription = model.SiteDescription,
                    slabType = (int)model.SlabType,
                    quantity = model.Quantity,
                    needPump = model.NeedPump,
                    floorNumber = model.FloorNumber,
                    pouringDate = model.PouringDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    transportMethod = (int)model.TransportMethod,
                    notes = model.Notes
                };

                // ✅ سجل معلومات مختصرة فقط
                _logger.LogInformation(
                    "📤 Sending PhoneOrder: Client={ClientFullName}, Factory={FactoryId}, Project={ProjectName}",
                    phoneOrderDto.clientFullName,
                    phoneOrderDto.factoryId,
                    phoneOrderDto.projectName);

                var response = await _apiClient.PostAsync<ApiResponse<OrderDto>>("Orders/phone-order", phoneOrderDto);

                if (response == null)
                {
                    _logger.LogWarning("❌ CreatePhoneOrderAsync: Response is null");
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ CreatePhoneOrderAsync failed. Message: {Message}", response.Message);
                    return null;
                }

                if (response.Data == null)
                {
                    _logger.LogWarning("❌ CreatePhoneOrderAsync: Response.Data is null");
                    return null;
                }

                _logger.LogInformation("✅ CreatePhoneOrderAsync: Phone order created successfully. Order ID: {OrderId}", response.Data.OrderId);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in CreatePhoneOrderAsync: {Message}", ex.Message);
                return null;
            }
        }

        // ============================================================
        // UPDATE ORDER - تعديل طلب
        // ============================================================
        public async Task<OrderDto?> UpdateOrderAsync(int id, EditOrderViewModel model)
        {
            try
            {
                _logger.LogInformation("📋 Updating order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<OrderDto>>($"Orders/{id}", model);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("❌ UpdateOrderAsync failed for id={Id}. Message: {Message}", id, response?.Message);
                    return null;
                }

                _logger.LogInformation("✅ UpdateOrderAsync: Order {Id} updated successfully", id);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in UpdateOrderAsync for id={Id}: {Message}", id, ex.Message);
                return null;
            }
        }

        // ============================================================
        // DELETE ORDER - حذف طلب
        // ============================================================
        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Deleting order {Id}", id);

                var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Orders/{id}");

                if (response == null)
                {
                    _logger.LogWarning("❌ DeleteOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ DeleteOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ DeleteOrderAsync: Order {Id} deleted successfully", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in DeleteOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // SAVE PRICE - حفظ سعر المتر
        // ============================================================
        public async Task<bool> SavePriceAsync(int id, decimal unitPrice)
        {
            try
            {
                _logger.LogInformation("📋 Saving price for order {Id}: {UnitPrice}", id, unitPrice);

                var payload = new { unitPrice = unitPrice };
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/price", payload);

                if (response == null)
                {
                    _logger.LogWarning("❌ SavePriceAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ SavePriceAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ SavePriceAsync: Price saved for order {Id}", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in SavePriceAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // APPROVE ORDER - موافقة العميل على الطلب
        // ============================================================
        public async Task<bool> ApproveOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Approving order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/approve", new { });

                if (response == null)
                {
                    _logger.LogWarning("❌ ApproveOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ ApproveOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ ApproveOrderAsync: Order {Id} approved successfully", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in ApproveOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // REJECT ORDER - رفض الطلب مع سبب (اختياري)
        // ============================================================
        public async Task<bool> RejectOrderAsync(int id, string? reason)
        {
            try
            {
                _logger.LogInformation("📋 Rejecting order {Id}. Reason: {Reason}", id, reason ?? "No reason provided");

                var payload = new { reason = reason ?? string.Empty };
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/reject", payload);

                if (response == null)
                {
                    _logger.LogWarning("❌ RejectOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ RejectOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ RejectOrderAsync: Order {Id} rejected successfully", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in RejectOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // CANCEL ORDER - إلغاء الطلب
        // ============================================================
        public async Task<bool> CancelOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Cancelling order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/cancel", new { });

                if (response == null)
                {
                    _logger.LogWarning("❌ CancelOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ CancelOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ CancelOrderAsync: Order {Id} cancelled successfully", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in CancelOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // START DELIVERY - بدء التوصيل
        // ============================================================
        public async Task<bool> StartDeliveryAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Starting delivery for order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/start-delivery", new { });

                if (response == null)
                {
                    _logger.LogWarning("❌ StartDeliveryAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ StartDeliveryAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ StartDeliveryAsync: Delivery started for order {Id}", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in StartDeliveryAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // DELIVER ORDER - تم تسليم الطلب
        // ============================================================
        public async Task<bool> DeliverOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Marking order {Id} as delivered", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/deliver", new { });

                if (response == null)
                {
                    _logger.LogWarning("❌ DeliverOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ DeliverOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ DeliverOrderAsync: Order {Id} marked as delivered", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in DeliverOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // CLOSE ORDER - إغلاق الطلب
        // ============================================================
        public async Task<bool> CloseOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("📋 Closing order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/close", new { });

                if (response == null)
                {
                    _logger.LogWarning("❌ CloseOrderAsync: API returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ CloseOrderAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ CloseOrderAsync: Order {Id} closed successfully", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in CloseOrderAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // ASSIGN DRIVER - تعيين سائق
        // ============================================================
        public async Task<bool> AssignDriverAsync(int id, AssignDriverViewModel model)
        {
            try
            {
                _logger.LogInformation("📋 Assigning driver to order {Id}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/assign-driver", model);

                if (response == null)
                {
                    _logger.LogWarning("❌ AssignDriverAsync returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ AssignDriverAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ AssignDriverAsync: Driver assigned to order {Id}", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in AssignDriverAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }

        // ============================================================
        // UPDATE ORDER STATUS - تحديث حالة الطلب
        // ============================================================
        public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusViewModel model)
        {
            try
            {
                _logger.LogInformation("📋 Updating status for order {Id} to {Status}", id, model.Status);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/status", model);

                if (response == null)
                {
                    _logger.LogWarning("❌ UpdateOrderStatusAsync returned null for id={Id}", id);
                    return false;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("❌ UpdateOrderStatusAsync failed for id={Id}. Message: {Message}", id, response.Message);
                    return false;
                }

                _logger.LogInformation("✅ UpdateOrderStatusAsync: Status updated for order {Id}", id);
                return true;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in UpdateOrderStatusAsync for id={Id}: {Message}", id, ex.Message);
                return false;
            }
        }
    }
}
