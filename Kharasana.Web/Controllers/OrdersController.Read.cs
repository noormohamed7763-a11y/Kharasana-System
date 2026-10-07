using Kharasana.Domain.Enums;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Kharasana.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers
{
    // الجزء الخاص بالقراءة والعرض: القائمة والتفاصيل ونماذج الإنشاء/التعديل (GET).
    // الحقول والبنّاء والمساعد المشترك في OrdersController.cs.
    public partial class OrdersController
    {
        // ============================================================
        // INDEX - عرض قائمة الطلبات
        // ============================================================
        public async Task<IActionResult> Index(
            int pageNumber = 1,
            int pageSize = 20,
            string? search = null,
            int? status = null,
            int? factoryId = null)
        {
            int? factoryIdFilter = IsFactoryUser ? FactoryId : factoryId;

            var paged = await _ordersApiService.GetOrdersAsync(
                pageNumber, pageSize, search, factoryIdFilter, status);

            int pendingCount = 0, rejectedCount = 0, cancelledCount = 0, closedCount = 0;
            try
            {
                var counts = await _ordersApiService.GetStatusCountsAsync(search, factoryIdFilter);
                pendingCount = counts.Pending;
                rejectedCount = counts.Rejected;
                cancelledCount = counts.Cancelled;
                closedCount = counts.Closed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "تعذر جلب عدّادات حالات الطلبات");
            }

            var vm = new OrdersIndexViewModel
            {
                PagedOrders = paged,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                StatusFilter = status,
                FactoryIdFilter = factoryIdFilter,
                PendingCount = pendingCount,
                RejectedCount = rejectedCount,
                CancelledCount = cancelledCount,
                ClosedCount = closedCount
            };

            if (RoleValue == UserRole.Admin)
            {
                ViewBag.ShowFactoryFilter = true;
                var factoryResult = await _factoryService.GetAllAsync();
                ViewBag.Factories = (factoryResult.Data ?? []).Select(f => new SelectListItem
                {
                    Value = f.FactoryId.ToString(),
                    Text = f.FactoryName
                });
            }

            return View(vm);
        }

        // ============================================================
        // DETAILS - عرض تفاصيل الطلب
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("معرّف طلب غير صالح: {OrderId}", id);
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            var order = await _ordersApiService.GetOrderByIdAsync(id);

            if (order == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

            if (IsFactoryIsolated(order.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }

            var allDrivers = await _lookupApiService.GetAvailableDriversAsync();
            ViewBag.AvailableDrivers = FactoryId.HasValue
                ? allDrivers.Where(d => d.FactoryId == FactoryId.Value).ToList()
                : allDrivers;
            ViewBag.FactoryId = FactoryId ?? 0;

            return View(order);
        }

        // ============================================================
        // CREATE - عرض نموذج إنشاء طلب (GET)
        // ============================================================
        public async Task<IActionResult> Create()
        {
            await ReloadConcreteTypesAsync();

            var vm = new CreatePhoneOrderViewModel();
            if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
            {
                vm.FactoryId = FactoryId.Value;
            }

            return View(vm);
        }

        // ============================================================
        // EDIT - عرض نموذج تعديل الطلب (GET)
        // ============================================================
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            var order = await _ordersApiService.GetOrderByIdAsync(id);

            if (order == null)
            {
                TempData[TempDataError] = AppMessages.Common.NotFound;
                return RedirectToAction(nameof(Index));
            }

            if (IsFactoryIsolated(order.FactoryId))
            {
                TempData[TempDataError] = AppMessages.Common.Forbidden;
                return RedirectToAction(nameof(Index));
            }

            if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending)
            {
                TempData[TempDataError] = AppMessages.Common.OrderCannotUpdateInStatus;
                return RedirectToAction(nameof(Details), new { id });
            }

            await ReloadEditViewDataAsync(new EditOrderViewModel { OrderId = order.OrderId });

            var vm = new EditOrderViewModel
            {
                OrderId = order.OrderId,
                ConcreteTypeId = order.ConcreteTypeId,
                Quantity = order.Quantity,
                PouringDate = order.PouringDate,
                SlabType = (SlabType)order.SlabType,
                TransportMethod = order.TransportMethod,
                NeedPump = order.NeedPump,
                FloorNumber = order.FloorNumber,
                ProjectName = order.ProjectName,
                ProjectOwnerName = order.ProjectOwnerName,
                SiteArea = order.SiteArea,
                SiteDescription = order.SiteDescription,
                Notes = order.Notes
            };

            return View(vm);
        }
    }
}
