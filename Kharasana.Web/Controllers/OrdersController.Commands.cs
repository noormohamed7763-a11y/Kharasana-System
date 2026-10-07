using Kharasana.Domain.Enums;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;

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
                await ReloadConcreteTypesAsync();
                return View(model);
            }

            if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
            {
                model.FactoryId = FactoryId.Value;
            }

            // الخدمة ترمي استثناءً إذا فشلت، والفلتر العالمي سيعالجه
            var created = await _ordersApiService.CreatePhoneOrderAsync(model);

            if (!string.IsNullOrEmpty(created?.NewClientTemporaryPassword))
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
                await ReloadEditViewDataAsync(model);
                return View(model);
            }

            // الخدمة ترمي استثناءً إذا فشلت، والفلتر العالمي سيعالجه
            await _ordersApiService.UpdateOrderAsync(id, model);

            _logger.LogInformation("عُدّل الطلب {OrderId}", id);
            TempData[TempDataSuccess] = AppMessages.Success.Updated;
            return RedirectToAction(nameof(Details), new { id });
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

            if (RoleValue == UserRole.FactoryEmployee)
            {
                var order = await _ordersApiService.GetOrderByIdAsync(id);
                if (order == null || IsFactoryIsolated(order.FactoryId))
                {
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }
            }

            // الخدمة ترمي استثناءً إذا فشلت، والفلتر العالمي سيعالجه
            await _ordersApiService.DeleteOrderAsync(id);

            TempData[TempDataSuccess] = AppMessages.Success.Deleted;
            return RedirectToAction(nameof(Index));
        }
    }
}
