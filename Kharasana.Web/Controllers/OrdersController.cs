using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Kharasana.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers
{
    /// <summary>
    /// عرض وإدارة الطلبات: قائمة، تفاصيل، إنشاء، تعديل، حذف.
    /// العمليات المنبثقة (تسعير، موافقة، رفض، إلغاء، توصيل، إغلاق، تعيين سائق، تغيير حالة)
    /// نُقلت إلى <see cref="OrderWorkflowController"/> بينما تبقى هنا العمليات CRUD الأساسية.
    /// </summary>
    [SessionAuthorize]
    public class OrdersController : BaseController
    {
        private readonly IOrdersApiService _ordersApiService;
        private readonly ILookupApiService _lookupApiService;
        private readonly IFactoryApiService _factoryService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrdersApiService ordersApiService,
            ILookupApiService lookupApiService,
            IFactoryApiService factoryService,
            ILogger<OrdersController> logger)
        {
            _ordersApiService = ordersApiService ?? throw new ArgumentNullException(nameof(ordersApiService));
            _lookupApiService = lookupApiService ?? throw new ArgumentNullException(nameof(lookupApiService));
            _factoryService = factoryService ?? throw new ArgumentNullException(nameof(factoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
            try
            {
                int? factoryIdFilter = factoryId;
                if (RoleValue == UserRole.FactoryEmployee)
                {
                    factoryIdFilter = FactoryId;
                }

                var paged = await _ordersApiService.GetOrdersAsync(
                    pageNumber, pageSize, search, factoryIdFilter, status);

                // ✅ عدّادات حقيقية عبر كل الصفحات (PageSize=1 → TotalCount) بدلًا من
                //    عدّ عناصر الصفحة الحالية الذي كان يُظهر أرقامًا ناقصة عند وجود أكثر من صفحة.
                //    تُصفّى بنفس فلاتر القائمة (البحث + المصنع) دون فلتر الحالة — البطاقات للتنقّل.
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
                    // فشل العدّادات لا يُفشل الصفحة — تُعرض أصفار
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
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة الطلبات StatusCode={StatusCode}", (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                return View(new OrdersIndexViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تحميل صفحة الطلبات");
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
                return View(new OrdersIndexViewModel());
            }
        }

        // ============================================================
        // DETAILS - عرض تفاصيل الطلب
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Invalid order ID: {OrderId}", id);
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation("Calling API to get order {OrderId}", id);

                var order = await _ordersApiService.GetOrderByIdAsync(id);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found in API", id);
                    TempData[TempDataError] = AppMessages.Common.NotFound;
                    return RedirectToAction(nameof(Index));
                }

                if (IsFactoryIsolated(order.FactoryId))
                {
                    _logger.LogWarning("Factory mismatch: order.FactoryId={OrderFactoryId}, user.FactoryId={UserFactoryId}", order.FactoryId, FactoryId);
                    TempData[TempDataError] = AppMessages.Common.Forbidden;
                    return RedirectToAction(nameof(Index));
                }

                // ✅ جلب جميع السائقين المتاحين ثم تصفيتهم حسب المصنع
                var allDrivers = await _lookupApiService.GetAvailableDriversAsync();

                // ✅ تصفية السائقين حسب المصنع الحالي
                var factoryDrivers = new List<LookupDto>();

                if (FactoryId.HasValue)
                {
                    factoryDrivers = allDrivers
                        .Where(d => d.FactoryId == FactoryId.Value)
                        .ToList();

                    _logger.LogInformation("Total drivers: {TotalCount}, Factory drivers: {FactoryCount}", allDrivers.Count, factoryDrivers.Count);
                }
                else
                {
                    factoryDrivers = allDrivers;
                }

                ViewBag.AvailableDrivers = factoryDrivers;
                ViewBag.FactoryId = FactoryId ?? 0;

                return View(order);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "Error loading order {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading order {OrderId}", id);
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // CREATE - عرض نموذج إنشاء طلب (GET)
        // ============================================================
        public async Task<IActionResult> Create()
        {
            try
            {
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                ViewBag.ConcreteTypes = concreteTypes;

                var vm = new CreatePhoneOrderViewModel();

                if (RoleValue == UserRole.FactoryEmployee && FactoryId.HasValue)
                {
                    vm.FactoryId = FactoryId.Value;
                }

                return View(vm);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة إنشاء طلب StatusCode={StatusCode}", (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تحميل صفحة إنشاء طلب");
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
                return RedirectToAction(nameof(Index));
            }
        }

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

                if (created == null)
                {
                    TempData[TempDataError] = AppMessages.Common.OperationFailed;
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.ConcreteTypes = concreteTypes;
                    return View(model);
                }

                TempData[TempDataSuccess] = AppMessages.Success.Created;
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
        // EDIT - عرض نموذج تعديل الطلب (GET)
        // ============================================================
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            try
            {
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

                ViewBag.OrderNumber = order.OrderNumber;
                ViewBag.ClientName = order.ClientName;
                ViewBag.Status = order.Status;
                ViewBag.UnitPrice = order.UnitPrice ?? 0;
                ViewBag.OrderId = order.OrderId;
                ViewBag.ShowDeleteButton = true;

                var clients = await _lookupApiService.GetClientsAsync();
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();

                ViewBag.Clients = clients;
                ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", order.ConcreteTypeId);

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
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الطلب للتعديل {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تحميل الطلب للتعديل {OrderId}", id);
                TempData[TempDataError] = AppMessages.Common.OperationFailed;
                return RedirectToAction(nameof(Index));
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
                _logger.LogWarning("OrderId mismatch: model.OrderId={ModelOrderId}, route id={RouteId}", model.OrderId, id);
                model.OrderId = id;
            }

            if (!ModelState.IsValid)
            {
                try
                {
                    var clients = await _lookupApiService.GetClientsAsync();
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.Clients = clients;
                    ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", model.ConcreteTypeId);

                    ViewBag.OrderNumber = model.OrderId.ToString();
                    ViewBag.ClientName = "العميل";
                    ViewBag.OrderId = model.OrderId;
                    ViewBag.ShowDeleteButton = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحميل البيانات للتعديل");
                }
                return View(model);
            }

            try
            {
                var existingOrder = await _ordersApiService.GetOrderByIdAsync(id);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for editing", id);
                    TempData[TempDataError] = AppMessages.Common.NotFound;
                    return RedirectToAction(nameof(Index));
                }

                var updated = await _ordersApiService.UpdateOrderAsync(id, model);

                if (updated == null)
                {
                    TempData[TempDataError] = AppMessages.Common.OperationFailed;
                    var clients = await _lookupApiService.GetClientsAsync();
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.Clients = clients;
                    ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", model.ConcreteTypeId);
                    ViewBag.OrderNumber = existingOrder.OrderNumber;
                    ViewBag.ClientName = existingOrder.ClientName;
                    ViewBag.OrderId = model.OrderId;
                    ViewBag.ShowDeleteButton = true;
                    return View(model);
                }

                _logger.LogInformation("Order {OrderId} updated successfully", id);
                TempData[TempDataSuccess] = AppMessages.Success.Updated;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "Error editing order {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;

                await ReloadEditDropdownsAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing order {OrderId}", id);
                TempData[TempDataError] = AppMessages.Common.OperationFailed;

                await ReloadEditDropdownsAsync(model);
                return View(model);
            }
        }

        /// <summary>إعادة تحميل القوائم المنسدلة بعد فشل التعديل لعرض النموذج كاملاً.</summary>
        private async Task ReloadEditDropdownsAsync(EditOrderViewModel model)
        {
            try
            {
                var clients = await _lookupApiService.GetClientsAsync();
                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                ViewBag.Clients = clients;
                ViewBag.ConcreteTypes = new SelectList(concreteTypes, "Id", "Name", model.ConcreteTypeId);
                ViewBag.OrderNumber = model.OrderId.ToString();
                ViewBag.ClientName = "العميل";
                ViewBag.OrderId = model.OrderId;
                ViewBag.ShowDeleteButton = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل البيانات بعد فشل التعديل");
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
