using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    // الجزء الخاص بالأوامر: إنشاء الطلب الهاتفي وتعديله وحذفه.
    // الحقول والبنية التحتية المشتركة في OrdersApiService.cs.
    public partial class OrdersApiService
    {
        // ============================================================
        // CREATE PHONE ORDER - إنشاء طلب هاتفي
        // ============================================================
        public async Task<PhoneOrderResultDto?> CreatePhoneOrderAsync(CreatePhoneOrderViewModel model)
        {
            try
            {
                // ✅ تحويل البيانات إلى PhoneOrderDto (strongly-typed بدلاً من anonymous object)
                var phoneOrderDto = new Kharasana.Application.DTOs.Order.PhoneOrderDto
                {
                    ClientPhone = model.ClientPhone,
                    ClientFullName = model.ClientFullName,
                    FactoryId = model.FactoryId,
                    ConcreteTypeId = model.ConcreteTypeId,
                    ProjectName = model.ProjectName,
                    ProjectOwnerName = model.ProjectOwnerName,
                    SiteArea = model.SiteArea,
                    SiteDescription = model.SiteDescription,
                    SlabType = model.SlabType,
                    Quantity = model.Quantity,
                    NeedPump = model.NeedPump,
                    FloorNumber = model.FloorNumber,
                    PouringDate = model.PouringDate,
                    TransportMethod = model.TransportMethod,
                    Notes = model.Notes
                };

                // ✅ سجل معلومات مختصرة فقط
                _logger.LogInformation(
                    "📤 Sending PhoneOrder: Client={ClientFullName}, Factory={FactoryId}, Project={ProjectName}",
                    phoneOrderDto.ClientFullName,
                    phoneOrderDto.FactoryId,
                    phoneOrderDto.ProjectName);

                var response = await _apiClient.PostAsync<ApiResponse<PhoneOrderResultDto>>("Orders/phone-order", phoneOrderDto);

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

                if (response.Data?.Order == null)
                {
                    _logger.LogWarning("❌ CreatePhoneOrderAsync: Response.Data.Order is null");
                    return null;
                }

                // ✅ لا نسجّل كلمة المرور المؤقتة إطلاقاً — تُعرض في الواجهة مرة واحدة فقط
                _logger.LogInformation(
                    "✅ CreatePhoneOrderAsync: Phone order created successfully. Order ID: {OrderId}, NewClientAccount: {NewClientAccount}",
                    response.Data.Order.OrderId,
                    response.Data.NewClientTemporaryPassword != null);

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

                // ✅ تحويل صريح ViewModel → DTO
                var dto = new Kharasana.Application.DTOs.Order.UpdateOrderDto
                {
                    ConcreteTypeId = model.ConcreteTypeId,
                    ProjectName = model.ProjectName,
                    ProjectOwnerName = model.ProjectOwnerName,
                    SiteArea = model.SiteArea,
                    SiteDescription = model.SiteDescription,
                    SlabType = model.SlabType,
                    Quantity = model.Quantity,
                    NeedPump = model.NeedPump,
                    FloorNumber = model.FloorNumber,
                    PouringDate = model.PouringDate,
                    TransportMethod = model.TransportMethod,
                    Notes = model.Notes
                };

                var response = await _apiClient.PutAsync<ApiResponse<OrderDto>>($"Orders/{id}", dto);

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
            _logger.LogInformation("📋 Deleting order {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(DeleteOrderAsync),
                $"Order {id} deleted successfully",
                () => _apiClient.DeleteAsync<ApiResponse<object>>($"Orders/{id}"),
                id);
        }
    }
}
