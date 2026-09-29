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
                _logger.LogWarning("معرّف طلب غير صالح: {OrderId}", id);
                TempData[TempDataError] = AppMessages.Error.InvalidId;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation("نداء الـ API لجلب الطلب {OrderId}", id);

                var order = await _ordersApiService.GetOrderByIdAsync(id);

                if (order == null)
                {
                    _logger.LogWarning("الطلب {OrderId} غير موجود في الـ API", id);
                    TempData[TempDataError] = AppMessages.Common.NotFound;
                    return RedirectToAction(nameof(Index));
                }

                if (IsFactoryIsolated(order.FactoryId))
                {
                    _logger.LogWarning("عدم تطابق المصنع: مصنع الطلب={OrderFactoryId}، مصنع المستخدم={UserFactoryId}", order.FactoryId, FactoryId);
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

                    _logger.LogInformation("إجمالي السائقين: {TotalCount}، سائقو المصنع: {FactoryCount}", allDrivers.Count, factoryDrivers.Count);
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
                _logger.LogError(ex, "خطأ في تحميل الطلب {OrderId} الحالة={StatusCode}", id, (int)ex.StatusCode);
                TempData[TempDataError] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ غير متوقع في تحميل الطلب {OrderId}", id);
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

                var concreteTypes = await _lookupApiService.GetConcreteTypesAsync();

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
    }
}
