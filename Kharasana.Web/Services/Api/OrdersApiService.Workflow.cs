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
            _logger.LogInformation("حفظ سعر الطلب {Id}: {UnitPrice}", id, unitPrice);

            return await ExecuteOrderActionAsync(
                nameof(SavePriceAsync),
                $"حُفظ سعر الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/price", new { unitPrice }),
                id);
        }

        // ============================================================
        // APPROVE ORDER - موافقة العميل على الطلب
        // ============================================================
        public async Task<bool> ApproveOrderAsync(int id)
        {
            _logger.LogInformation("اعتماد الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(ApproveOrderAsync),
                $"اعتُمد الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/approve", new { }),
                id);
        }

        // ============================================================
        // REJECT ORDER - رفض الطلب مع سبب (اختياري)
        // ============================================================
        public async Task<bool> RejectOrderAsync(int id, string? reason)
        {
            _logger.LogInformation("رفض الطلب {Id}. السبب: {Reason}", id, reason ?? "بلا سبب");

            return await ExecuteOrderActionAsync(
                nameof(RejectOrderAsync),
                $"رُفض الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/reject", new { reason = reason ?? string.Empty }),
                id);
        }

        // ============================================================
        // CANCEL ORDER - إلغاء الطلب
        // ============================================================
        public async Task<bool> CancelOrderAsync(int id)
        {
            _logger.LogInformation("إلغاء الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(CancelOrderAsync),
                $"أُلغي الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/cancel", new { }),
                id);
        }

        // ============================================================
        // START DELIVERY - بدء التوصيل
        // ============================================================
        public async Task<bool> StartDeliveryAsync(int id)
        {
            _logger.LogInformation("بدء توصيل الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(StartDeliveryAsync),
                $"بدأ توصيل الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/start-delivery", new { }),
                id);
        }

        // ============================================================
        // DELIVER ORDER - تم تسليم الطلب
        // ============================================================
        public async Task<bool> DeliverOrderAsync(int id)
        {
            _logger.LogInformation("تسجيل تسليم الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(DeliverOrderAsync),
                $"سُلّم الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/deliver", new { }),
                id);
        }

        // ============================================================
        // CLOSE ORDER - إغلاق الطلب
        // ============================================================
        public async Task<bool> CloseOrderAsync(int id)
        {
            _logger.LogInformation("إغلاق الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(CloseOrderAsync),
                $"أُغلق الطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/close", new { }),
                id);
        }

        // ============================================================
        // ASSIGN DRIVER - تعيين سائق
        // ============================================================
        public async Task<bool> AssignDriverAsync(int id, AssignDriverViewModel model)
        {
            _logger.LogInformation("إسناد سائق للطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(AssignDriverAsync),
                $"أُسند سائق للطلب {id}",
                () => _apiClient.PutAsync<ApiResponse<object>>($"Orders/{id}/assign-driver", model),
                id);
        }
    }
}
