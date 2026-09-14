using Kharasana.Application.DTOs.Order;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Orders;
using Kharasana.Web.ViewModels.Shared;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kharasana.Web.Controllers
{
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
                if ((string?)Role == "FactoryEmployee")
                {
                    factoryIdFilter = FactoryId;
                }

                var paged = await _ordersApiService.GetOrdersAsync(
                    pageNumber, pageSize, search, factoryIdFilter, status);

                var vm = new OrdersIndexViewModel
                {
                    PagedOrders = paged,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Search = search,
                    StatusFilter = status,
                    FactoryIdFilter = factoryIdFilter,

                    NewCount = paged?.Items?.Count(o => o.Status == OrderStatus.New) ?? 0,
                    PendingCount = paged?.Items?.Count(o => o.Status == OrderStatus.Pending) ?? 0,
                    ApprovedCount = paged?.Items?.Count(o => o.Status == OrderStatus.Approved) ?? 0,
                    RejectedCount = paged?.Items?.Count(o => o.Status == OrderStatus.Rejected) ?? 0,
                    CancelledCount = paged?.Items?.Count(o => o.Status == OrderStatus.Cancelled) ?? 0,
                    OnTheWayCount = paged?.Items?.Count(o => o.Status == OrderStatus.OnTheWay) ?? 0,
                    DeliveredCount = paged?.Items?.Count(o => o.Status == OrderStatus.Delivered) ?? 0,
                    ClosedCount = paged?.Items?.Count(o => o.Status == OrderStatus.Closed) ?? 0
                };

                if ((string?)Role == "Admin")
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

                if ((string?)Role == "FactoryEmployee")
                {
                    if (order.FactoryId != FactoryId)
                    {
                        _logger.LogWarning("Factory mismatch: order.FactoryId={OrderFactoryId}, user.FactoryId={UserFactoryId}", order.FactoryId, FactoryId);
                        TempData[TempDataError] = AppMessages.Common.Forbidden;
                        return RedirectToAction(nameof(Index));
                    }
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

                ViewBag.Statuses = Enum.GetValues<OrderStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s switch
                        {
                            OrderStatus.New => "جديد",
                            OrderStatus.Pending => "⏳ قيد الانتظار",
                            OrderStatus.Approved => "✅ معتمد",
                            OrderStatus.Rejected => "❌ مرفوض",
                            OrderStatus.Cancelled => "🚫 ملغي",
                            OrderStatus.OnTheWay => "🚚 في الطريق",
                            OrderStatus.Delivered => "📦 تم التسليم",
                            OrderStatus.Closed => "🔒 مغلق",
                            _ => s.ToString()
                        }
                    })
                    .ToList();

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

                if ((string?)Role == "FactoryEmployee" && FactoryId.HasValue)
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
                var created = await _ordersApiService.CreatePhoneOrderAsync(model);

                if (created == null)
                {
                    TempData[TempDataError] = "فشل إنشاء الطلب. يرجى المحاولة مرة أخرى.";
                    var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();
                    ViewBag.ConcreteTypes = concreteTypes;
                    return View(model);
                }

                TempData[TempDataSuccess] = "تم إنشاء الطلب بنجاح.";
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

                if ((string?)Role == "FactoryEmployee")
                {
                    if (order.FactoryId != FactoryId)
                    {
                        TempData[TempDataError] = AppMessages.Common.Forbidden;
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending)
                {
                    TempData[TempDataError] = "لا يمكن تعديل الطلب في حالته الحالية.";
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
                    Quantity = (double)order.Quantity,
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
                    TempData[TempDataError] = "فشل تعديل الطلب. يرجى المحاولة مرة أخرى.";
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
                TempData[TempDataSuccess] = "تم تعديل الطلب بنجاح.";
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
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        TempData[TempDataError] = AppMessages.Common.Forbidden;
                        return RedirectToAction(nameof(Index));
                    }
                }

                var ok = await _ordersApiService.DeleteOrderAsync(id);
                if (!ok)
                {
                    TempData[TempDataError] = "فشل حذف الطلب.";
                }
                else
                {
                    TempData[TempDataSuccess] = "تم حذف الطلب بنجاح.";
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

        // ============================================================
        // SAVE PRICE - حفظ سعر المتر (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePrice(int id, [FromBody] SavePriceDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            if (dto == null)
            {
                return BadRequest(new { success = false, message = "بيانات السعر مطلوبة." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.SavePriceAsync(id, dto.UnitPrice);

                if (result)
                {
                    return Ok(new { success = true, message = "تم حفظ السعر بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل حفظ السعر" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في حفظ سعر الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في حفظ سعر الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // APPROVE - موافقة العميل (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.ApproveOrderAsync(id);

                if (result)
                {
                    return Ok(new { success = true, message = "تمت موافقة العميل بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشلت الموافقة" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في الموافقة على الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في الموافقة على الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // REJECT - رفض الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            if (dto == null)
            {
                return BadRequest(new { success = false, message = "بيانات الرفض مطلوبة." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.RejectOrderAsync(id, dto.Reason ?? string.Empty);

                if (result)
                {
                    return Ok(new { success = true, message = "تم رفض الطلب بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل رفض الطلب" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في رفض الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في رفض الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // CANCEL - إلغاء الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.CancelOrderAsync(id);

                if (result)
                {
                    return Ok(new { success = true, message = "تم إلغاء الطلب بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل إلغاء الطلب" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في إلغاء الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في إلغاء الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // START DELIVERY - بدء التوصيل (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDelivery(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.StartDeliveryAsync(id);

                if (result)
                {
                    return Ok(new { success = true, message = "تم بدء التوصيل بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل بدء التوصيل" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في بدء توصيل الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في بدء توصيل الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // DELIVER - تم التسليم (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deliver(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.DeliverOrderAsync(id);

                if (result)
                {
                    return Ok(new { success = true, message = "تم تسليم الطلب بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل تسليم الطلب" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تسليم الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تسليم الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // CLOSE - إغلاق الطلب (AJAX)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعديل هذا الطلب." });
                    }
                }

                var result = await _ordersApiService.CloseOrderAsync(id);

                if (result)
                {
                    return Ok(new { success = true, message = "تم إغلاق الطلب بنجاح" });
                }

                return BadRequest(new { success = false, message = "فشل إغلاق الطلب" });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في إغلاق الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في إغلاق الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // ASSIGN DRIVER - تعيين سائق (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverViewModel model)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تعيين سائق لهذا الطلب." });
                    }

                    // ✅ التحقق من أن السائق في نفس المصنع
                    var allDrivers = await _lookupApiService.GetAvailableDriversAsync();
                    var factoryDrivers = allDrivers
                        .Where(d => d.FactoryId == FactoryId)
                        .ToList();

                    var driverExists = factoryDrivers.Any(d => d.Id == model.DriverId);

                    if (!driverExists)
                    {
                        return BadRequest(new { success = false, message = "السائق المحدد لا يعمل في مصنعك أو غير متاح." });
                    }
                }

                var ok = await _ordersApiService.AssignDriverAsync(id, model);

                if (!ok)
                {
                    return BadRequest(new { success = false, message = "فشل تعيين السائق." });
                }

                return Ok(new { success = true, message = "تم تعيين السائق بنجاح." });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تعيين سائق للطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تعيين سائق للطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }

        // ============================================================
        // CHANGE STATUS - تغيير حالة الطلب (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateOrderStatusViewModel model)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "رقم الطلب غير صحيح." });
            }

            if (model == null)
            {
                return BadRequest(new { success = false, message = "بيانات تغيير الحالة مطلوبة." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "البيانات غير صحيحة" });
            }

            try
            {
                if ((string?)Role == "FactoryEmployee")
                {
                    var order = await _ordersApiService.GetOrderByIdAsync(id);
                    if (order == null || order.FactoryId != FactoryId)
                    {
                        return BadRequest(new { success = false, message = "لا تملك صلاحية تغيير حالة هذا الطلب." });
                    }
                }

                var ok = await _ordersApiService.UpdateOrderStatusAsync(id, model);

                if (!ok)
                {
                    return BadRequest(new { success = false, message = "فشل تحديث حالة الطلب." });
                }

                return Ok(new { success = true, message = "تم تحديث حالة الطلب بنجاح." });
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "خطأ في تغيير حالة الطلب {OrderId} StatusCode={StatusCode}", id, (int)ex.StatusCode);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تغيير حالة الطلب {OrderId}", id);
                return BadRequest(new { success = false, message = AppMessages.Common.OperationFailed });
            }
        }
    }

    // ============================================================
    // DTOs
    // ============================================================
    public class SavePriceDto
    {
        public decimal UnitPrice { get; set; }
    }

    }