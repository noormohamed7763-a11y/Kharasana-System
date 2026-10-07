using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.Extensions.Logging;
using System.Net;

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
                    "إرسال الطلب الهاتفي: العميل={ClientFullName}، المصنع={FactoryId}، المشروع={ProjectName}",
                    phoneOrderDto.ClientFullName,
                    phoneOrderDto.FactoryId,
                    phoneOrderDto.ProjectName);

                var response = await _apiClient.PostAsync<ApiResponse<PhoneOrderResultDto>>("Orders/phone-order", phoneOrderDto);

                if (response == null)
                {
                    _logger.LogWarning("CreatePhoneOrderAsync: الردّ فارغ");
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("فشل CreatePhoneOrderAsync. الرسالة: {Message}", response.Message);
                    throw new ApiServiceException(
                        HttpStatusCode.BadRequest,
                        new ApiError("ORDER_CREATE_FAILED", response.Message ?? "فشل إنشاء الطلب."),
                        new object[] { model.ClientPhone ?? "غير معروف" });
                }

                if (response.Data?.Order == null)
                {
                    _logger.LogWarning("CreatePhoneOrderAsync: بيانات الطلب في الردّ فارغة");
                    throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError);
                }

                // ✅ لا نسجّل كلمة المرور المؤقتة إطلاقاً — تُعرض في الواجهة مرة واحدة فقط
                _logger.LogInformation(
                    "CreatePhoneOrderAsync: أُنشئ الطلب الهاتفي. رقم الطلب: {OrderId}، حساب عميل جديد: {NewClientAccount}",
                    response.Data.Order.OrderId,
                    response.Data.NewClientTemporaryPassword != null);

                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء في CreatePhoneOrderAsync: {Message}", ex.Message);
                throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
            }
        }

        // ============================================================
        // UPDATE ORDER - تعديل طلب
        // ============================================================
        public async Task<OrderDto?> UpdateOrderAsync(int id, EditOrderViewModel model)
        {
            try
            {
                _logger.LogInformation("تعديل الطلب {Id}", id);

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
                    _logger.LogWarning("فشل UpdateOrderAsync للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    throw new ApiServiceException(
                        HttpStatusCode.BadRequest,
                        ApiErrorCatalog.OrderStatusConflict, // أو خطأ عام حسب الرد
                        new object[] { id, response?.Message ?? "حالة غير معروفة" });
                }

                _logger.LogInformation("UpdateOrderAsync: عُدّل الطلب {Id}", id);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء في UpdateOrderAsync للمعرّف {Id}: {Message}", id, ex.Message);
                throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
            }
        }

        // ============================================================
        // DELETE ORDER - حذف طلب
        // ============================================================
        public async Task<ServiceResult> DeleteOrderAsync(int id)
        {
            _logger.LogInformation("حذف الطلب {Id}", id);

            return await ExecuteOrderActionAsync(
                nameof(DeleteOrderAsync),
                $"حُذف الطلب {id}",
                () => _apiClient.DeleteAsync<ApiResponse<object>>($"Orders/{id}"),
                id);
        }
    }
}
