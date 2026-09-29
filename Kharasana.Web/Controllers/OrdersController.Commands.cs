using Kharasana.Domain.Enums;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers
{
    // الجزء الخاص بالأوامر: إنشاء الطلب وتعديله وحذفه (POST).
    // الحقول والبنّاء والمساعد المشترك في OrdersController.cs.
    public partial class OrdersController
    {
        // ============================================================
        // CREATE - إنشاء طلب (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePhoneOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.ConcreteTypes = concreteTypes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحميل أنواع الخرسانة");
                }
                return View(model);
            }

            try
            {
                if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
                {
                    model.FactoryId = FactoryId.Value;
                }

                var created = await _ordersApiService.CreatePhoneOrderAsync(model);

                if (created?.Order == null)
                {
                    TempData[TempDataError] = AppMessages.Common.OperationFailed;
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.ConcreteTypes = concreteTypes;
                    return View(model);
                }

                // ✅ إن أُنشئ حساب عميل جديد تُعرض كلمة المرور المؤقتة مرة واحدة فقط هنا.
                //    TempData يعيش لدورة إعادة توجيه واحدة ثم يُقرأ ويُحذف في _Alerts،
                //    فلا تظهر في تحديث الصفحة ولا في أي طلب لاحق.
                if (!string.IsNullOrEmpty(created.NewClientTemporaryPassword))
                {
                    TempData[TempDataSuccess] = string.Format(
                        AppMessages.Success.NewClientAccountCreated,
                        created.NewClientPhone ?? model.ClientPhone,
                        created.NewClientTemporaryPassword);
                }
                else
                {
                    TempData[TempDataSuccess] = AppMessages.Success.Created;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء طلب جديد StatusCode={StatusCode}", (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                ViewBag.ConcreteTypes = concreteTypes;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في إنشاء طلب جديد");
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                ViewBag.ConcreteTypes = concreteTypes;
                return View(model);
            }
        }

        // ============================================================
        // EDIT - تعديل الطلب (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditOrderViewModel model)
        {
            if (id <= 0)
            {
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            if (model.OrderId != id)
            {
                _logger.LogWarning("عدم تطابق معرّف الطلب: النموذج={ModelOrderId}، المسار={RouteId}", model.OrderId, id);
                model.OrderId = id;
            }

            if (!ModelState.IsValid)
            {
                // مصدر واحد لبيانات النموذج: نفس مسار إعادة التحميل الذي تستعمله مسارات
                // الفشل الأخرى، فلا تتباعد نسختان من العرض مرة أخرى.
                await ReloadEditViewDataAsync(model);
                return View(model);
            }

            try
            {
                var existingOrder = await _ordersApiService.GetOrderByIdAsync(id);
                if (existingOrder == null)
                {
                    _logger.LogWarning("الطلب {OrderId} غير موجود للتعديل", id);
                    TempData[TempDataError] = AppMessages.Common.NotFound;
                    return RedirectToAction(nameof(Index));
                }

                var updated = await _ordersApiService.UpdateOrderAsync(id, model);

                if (updated == null)
                {
                    TempData[TempDataError] = AppMessages.Common.OperationFailed;
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", model.ConcreteTypeId);
                    ViewBag.OrderNumber = existingOrder.OrderNumber;
                    ViewBag.ClientName = existingOrder.ClientName;
                    ViewBag.OrderId = model.OrderId;
                    ViewBag.ShowDeleteButton = true;
                    return View(model);
                }

                _logger.LogInformation("عُدّل الطلب {OrderId}", id);
                TempData[TempDataSuccess] = AppMessages.Success.Updated;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تعديل الطلب {OrderId} الحالة={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;

                await ReloadEditViewDataAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تعديل الطلب {OrderId}", id);
                TempData[TempDataError] = AppMessages.Common.OperationFailed;

                await ReloadEditViewDataAsync(model);
                return View(model);
            }
        }

        // ============================================================
        // DELETE - حذف الطلب
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (RoleValue == UserRole.FactoryEmployee)
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || IsFactoryIsolated(order.FactoryId))
                    {
                        TempData[TempDataError] = AppMessages.Common.Forbidden;
                        return RedirectToAction(nameof(Index));
                    }
                }

                var ok = await _ordersApiService.DeleteOrderAsync(id);
                if (!ok)
                {
                    TempData[TempDataError] = AppMessages.Error.Deleted;
                }
                else
                {
                    TempData[TempDataSuccess] = AppMessages.Success.Deleted;
                }
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في حذف الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في حذف الطلب {OrderId}", id);
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
