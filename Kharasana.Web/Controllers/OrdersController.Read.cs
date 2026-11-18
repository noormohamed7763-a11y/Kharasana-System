using Kharasana.Domain.Enums;
using Kharasana.Web.Localization;
using Kharasana.Web.Models;
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
                vm.ShowFactoryFilter = true;
                var factoryResult = await _factoryService.GetAllAsync();
                vm.Factories = (factoryResult.Data ?? []).Select(f => new SelectListItem
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

            try
            {
                var allDrivers = await _lookupApiService.GetAvailableDriversAsync();
                ViewBag.AvailableDrivers = FactoryId.HasValue
                    ? allDrivers.Where(d => d.FactoryId == FactoryId.Value).ToList()
                    : allDrivers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل السائقين المتاحين للطلب {OrderId}", id);
                ViewBag.AvailableDrivers = new List<SelectListItem>(); // قائمة فارغة لتجنب الانهيار
                TempData[TempDataError] = AppMessages.Error.FailedToLoadDrivers;
            }
            ViewBag.FactoryId = FactoryId ?? 0;

            return View(order);
        }

        // ============================================================
        // CREATE - عرض نموذج إنشاء طلب (GET)
        // ============================================================
        public async Task<IActionResult> Create(int? fromOrderId = null)
        {
            await ReloadConcreteTypesAsync();

            var vm = new CreatePhoneOrderViewModel();
            if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
            {
                vm.FactoryId = FactoryId.Value;
            }

            // استيراد بيانات صبة سابقة لنفس المشروع (تكرار صبة الموقع بنقرة واحدة)
            if (fromOrderId.HasValue && fromOrderId.Value > 0)
            {
                try
                {
                    var previousOrder = await _ordersApiService.GetOrderByIdAsync(fromOrderId.Value);
                    if (previousOrder != null && !IsFactoryIsolated(previousOrder.FactoryId))
                    {
                        vm.ClientPhone = previousOrder.ClientPhone ?? string.Empty;
                        vm.ClientFullName = previousOrder.ClientName;
                        vm.ProjectName = previousOrder.ProjectName;
                        vm.ProjectOwnerName = previousOrder.ProjectOwnerName;
                        vm.SiteArea = previousOrder.SiteArea;
                        vm.SiteDescription = previousOrder.SiteDescription;
                        vm.Landmarks = previousOrder.Landmarks;
                        vm.SupervisorName = previousOrder.SupervisorName;
                        vm.SupervisorPhone = previousOrder.SupervisorPhone;
                        vm.ConcreteTypeId = previousOrder.ConcreteTypeId;
                        vm.SlabType = (SlabType)previousOrder.SlabType;
                        vm.CementType = previousOrder.CementType;
                        vm.AggregateSize = previousOrder.AggregateSize;
                        vm.Admixture = previousOrder.Admixture;
                        vm.NeedPump = previousOrder.NeedPump;
                        vm.FloorNumber = previousOrder.FloorNumber.HasValue ? previousOrder.FloorNumber.Value + 1 : null;
                        vm.TransportMethod = previousOrder.TransportMethod;
                        vm.PouringDate = DateTime.Today;

                        ViewBag.ClonedFromOrderNumber = previousOrder.OrderNumber;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "فشل استيراد بيانات الطلب السابق {OrderId}", fromOrderId.Value);
                }
            }

            // إذا كان المستخدم عميلاً مسجلاً ولديه طلب قيد المراجعة، نعرض له التنبيه الذكي
            if (RoleValue == UserRole.Client && CurrentUserId.HasValue)
            {
                try
                {
                    var clientOrders = await _ordersApiService.GetOrdersAsync(1, 5, null, null, (int)OrderStatus.Pending);
                    var activePending = clientOrders?.Items?.FirstOrDefault(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.New);
                    if (activePending != null)
                    {
                        vm.HasPendingOrder = true;
                        vm.PendingOrderId = activePending.OrderId;
                        vm.PendingOrderNumber = activePending.OrderNumber;
                        vm.PendingProjectName = activePending.ProjectName;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "تعذر جلب الطلبات المعلقة السابقة للعميل");
                }
            }

            return View(vm);
        }

        // ============================================================
        // LOOKUP CLIENT BY PHONE (AJAX) - استعلام ذكي فوري برقم الهاتف
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> LookupClientByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length < 6)
            {
                return Json(new { found = false });
            }

            phone = phone.Trim();
            try
            {
                var clientsResult = await _clientApiService.GetClientsAsync(1, 5, search: phone, factoryId: FactoryId);
                var client = clientsResult?.Items?.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.Phone) &&
                    (c.Phone.Contains(phone) || phone.Contains(c.Phone)));

                // البحث عن آخر صبة أو طلب للعميل لنفس المصنع
                var ordersResult = await _ordersApiService.GetOrdersAsync(1, 1, search: phone, factoryId: FactoryId);
                var lastOrder = ordersResult?.Items?.FirstOrDefault();

                if (client != null || lastOrder != null)
                {
                    return Json(new
                    {
                        found = true,
                        clientId = client?.UserId,
                        clientName = client?.FullName ?? lastOrder?.ClientName,
                        clientPhone = client?.Phone ?? lastOrder?.ClientPhone,
                        ordersCount = client?.OrdersCount ?? (lastOrder != null ? 1 : 0),
                        lastOrder = lastOrder == null ? null : new
                        {
                            orderId = lastOrder.OrderId,
                            orderNumber = lastOrder.OrderNumber,
                            projectName = lastOrder.ProjectName,
                            projectOwnerName = lastOrder.ProjectOwnerName,
                            siteArea = lastOrder.SiteArea,
                            siteDescription = lastOrder.SiteDescription,
                            landmarks = lastOrder.Landmarks,
                            supervisorName = lastOrder.SupervisorName,
                            supervisorPhone = lastOrder.SupervisorPhone,
                            concreteTypeId = lastOrder.ConcreteTypeId,
                            concreteTypeName = lastOrder.ConcreteTypeName,
                            slabType = (int)lastOrder.SlabType,
                            cementType = (int)lastOrder.CementType,
                            aggregateSize = (int)lastOrder.AggregateSize,
                            admixture = (int)lastOrder.Admixture,
                            needPump = lastOrder.NeedPump,
                            floorNumber = lastOrder.FloorNumber,
                            unitPrice = lastOrder.UnitPrice
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "فشل الاستعلام الفوري عن العميل برقم الهاتف {Phone}", phone);
            }

            return Json(new { found = false });
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

            await ReloadEditViewDataAsync(vm);

            return View(vm);
        }
    }
}
