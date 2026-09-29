using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    // الجزء الخاص بدورة حياة الطلب: التسعير والموافقة والرفض والإلغاء والتوصيل والإغلاق وإسناد السائق.
    // الحقول والبنية التحتية المشتركة في OrdersApiService.cs، والمساعد المشترك ExecuteOrderActionAsync فيه أيضاً.
    public partial class OrdersApiService
    {
        // ============================================================
        // SAVE PRICE - حفظ سعر المتر
        // ============================================================
        public async Task<bool> SavePriceAsync(int id, decimal unitPrice)
        {
            _logger.LogInformation("📋 Saving price for order {Id}: {UnitPrice}", id, unitPrice);

            return await ExecuteOrderActionAsync(
                nameof(SavePriceAsync),
                $"Price saved for order {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/price", new { unitPrice }),
                id);
        }

        // ============================================================
        // APPROVE ORDER - موافقة العميل على الطلب
        // ============================================================
        public async Task<bool> ApproveOrderAsync(int id)
        {
            _logger.LogInformation("📋 Approving order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(ApproveOrderAsync),
                $"Order {id} approved successfully",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/approve", new { }),
                id);
        }

        // ============================================================
        // REJECT ORDER - رفض الطلب مع سبب (اختياري)
        // ============================================================
        public async Task<bool> RejectOrderAsync(int id, string? reason)
        {
            _logger.LogInformation("📋 Rejecting order {Id}. Reason: {Reason}", id, reason ?? "No reason provided");

            return await ExecuteOrderActionAsync(
                nameof(RejectOrderAsync),
                $"Order {id} rejected successfully",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/reject", new { reason = reason ?? string.Empty }),
                id);
        }

        // ============================================================
        // CANCEL ORDER - إلغاء الطلب
        // ============================================================
        public async Task<bool> CancelOrderAsync(int id)
        {
            _logger.LogInformation("📋 Cancelling order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(CancelOrderAsync),
                $"Order {id} cancelled successfully",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/cancel", new { }),
                id);
        }

        // ============================================================
        // START DELIVERY - بدء التوصيل
        // ============================================================
        public async Task<bool> StartDeliveryAsync(int id)
        {
            _logger.LogInformation("📋 Starting delivery for order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(StartDeliveryAsync),
                $"Delivery started for order {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/start-delivery", new { }),
                id);
        }

        // ============================================================
        // DELIVER ORDER - تم تسليم الطلب
        // ============================================================
        public async Task<bool> DeliverOrderAsync(int id)
        {
            _logger.LogInformation("📋 Marking order {Id} as delivered", id);

            return await ExecuteOrderActionAsync(
                nameof(DeliverOrderAsync),
                $"Order {id} marked as delivered",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/deliver", new { }),
                id);
        }

        // ============================================================
        // CLOSE ORDER - إغلاق الطلب
        // ============================================================
        public async Task<bool> CloseOrderAsync(int id)
        {
            _logger.LogInformation("📋 Closing order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(CloseOrderAsync),
                $"Order {id} closed successfully",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/close", new { }),
                id);
        }

        // ============================================================
        // ASSIGN DRIVER - تعيين سائق
        // ============================================================
        public async Task<bool> AssignDriverAsync(int id, AssignDriverViewModel model)
        {
            _logger.LogInformation("📋 Assigning driver to order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(AssignDriverAsync),
                $"Driver assigned to order {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/assign-driver", model),
                id);
        }
    }
}
