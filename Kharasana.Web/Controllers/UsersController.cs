using System.Threading.Tasks;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.Controllers;

[SessionAuthorize]
public class UsersController : BaseController
{
    private readonly IUserApiService _userApiService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserApiService userApiService,
        ILogger<UsersController> logger)
    {
        _userApiService = userApiService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET: Users/Index
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null)
    {
        int? factoryId = null;

        if (Role == "FactoryEmployee")
        {
            factoryId = FactoryId;
        }

        var users = await _userApiService.GetUsersAsync(
            pageNumber,
            pageSize,
            search,
            role,
            factoryId);

        // أعداد الأدوار عبر كل الصفحات — فشلها لا ينبغي أن يُسقط الصفحة بعد أن حمّلنا القائمة
        var counts = (Admins: 0, FactoryEmployees: 0, Drivers: 0);
        if (users != null)
        {
            try
            {
                counts = await _userApiService.GetRoleCountsAsync(search, factoryId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "تعذر جلب إحصاءات أدوار المستخدمين — ستُعرض البطاقات بقيمة صفر.");
            }
        }

        var model = new UsersIndexViewModel
        {
            PagedUsers = users,
            Search = search,
            Role = role,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalUsers = users?.TotalCount ?? 0,
            AdminsCount = counts.Admins,
            FactoryEmployeesCount = counts.FactoryEmployees,
            DriversCount = counts.Drivers
        };

        return View(model);
    }

    // GET: Users/Create
    [HttpGet]
    [SessionAuthorize("Admin")]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    // POST: Users/Create
    [HttpPost]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        // تحقق إضافي: FactoryId مطلوب للسائق وموظف المصنع
        if ((model.Role == UserRole.Driver || model.Role == UserRole.FactoryEmployee)
            && !model.FactoryId.HasValue)
        {
            ModelState.AddModelError(nameof(model.FactoryId), "المصنع مطلوب للسائق وموظف المصنع.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _userApiService.CreateAsync(model);

            if (result.Success)
            {
                TempData[TempDataSuccess] = "✅ تم إنشاء المستخدم بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "حدث خطأ أثناء إنشاء المستخدم.");
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في إنشاء مستخدم جديد StatusCode={StatusCode}", (int)ex.StatusCode);
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في إنشاء مستخدم جديد");
            ModelState.AddModelError(string.Empty, AppMessages.Common.OperationFailed);
        }

        return View(model);
    }

    // GET: Users/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userApiService.GetUserByIdAsync(id);

        if (user == null)
        {
            TempData[TempDataError] = AppMessages.Common.NotFound;
            return RedirectToAction(nameof(Index));
        }

        var model = new UserDetailsViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            WhatsApp = user.WhatsApp,
            Role = user.Role,
            RoleName = user.Role switch
            {
                "Admin" => "مدير النظام",
                "FactoryEmployee" => "موظف مصنع",
                "Driver" => "سائق",
                "Client" => "عميل",
                _ => user.Role
            },
            FactoryId = user.FactoryId,
            FactoryName = user.FactoryName,
            IsActive = user.IsActive,
            LicenseNumber = user.LicenseNumber,
            DriverStatus = user.DriverStatus,
            DriverStatusName = user.DriverStatus switch
            {
                "Available" => "متاح",
                "Busy" => "مشغول",
                "Offline" => "غير متصل",
                _ => user.DriverStatus
            },
            CreatedAt = null,
            UpdatedAt = null
        };

        return View(model);
    }

    // GET: Users/Edit/{id}
    [HttpGet]
    [SessionAuthorize("Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userApiService.GetUserByIdAsync(id);

        if (user == null)
        {
            TempData[TempDataError] = AppMessages.Common.NotFound;
            return RedirectToAction(nameof(Index));
        }

        var role = Enum.TryParse<UserRole>(user.Role, out var parsedRole)
            ? parsedRole
            : UserRole.Client;

        DriverStatus? driverStatus = null;
        if (!string.IsNullOrWhiteSpace(user.DriverStatus)
            && Enum.TryParse<DriverStatus>(user.DriverStatus, out var parsedStatus))
        {
            driverStatus = parsedStatus;
        }

        var model = new UpdateUserViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            WhatsApp = user.WhatsApp,
            Role = role,
            LicenseNumber = user.LicenseNumber,
            DriverStatus = driverStatus,
            FactoryId = user.FactoryId,
            IsActive = user.IsActive
        };

        return View(model);
    }

    // POST: Users/Edit/{id}
    [HttpPost]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateUserViewModel model)
    {
        // تحقق إضافي: FactoryId مطلوب للسائق وموظف المصنع
        if ((model.Role == UserRole.Driver || model.Role == UserRole.FactoryEmployee)
            && !model.FactoryId.HasValue)
        {
            ModelState.AddModelError(nameof(model.FactoryId), "المصنع مطلوب للسائق وموظف المصنع.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _userApiService.UpdateAsync(id, model);

            if (result.Success)
            {
                TempData[TempDataSuccess] = "✅ تم تحديث المستخدم بنجاح.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "حدث خطأ أثناء تحديث المستخدم.");
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في تعديل مستخدم StatusCode={StatusCode}", (int)ex.StatusCode);
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في تعديل مستخدم");
            ModelState.AddModelError(string.Empty, AppMessages.Common.OperationFailed);
        }

        return View(model);
    }

    // POST: Users/Delete/{id}
    [HttpPost]
    [SessionAuthorize("Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _userApiService.DeleteAsync(id);

            if (result.Success)
            {
                TempData[TempDataSuccess] = "✅ تم حذف المستخدم بنجاح.";
            }
            else
            {
                TempData[TempDataError] = result.Message ?? "حدث خطأ أثناء حذف المستخدم.";
            }
        }
        catch (ApiServiceException ex)
        {
            _logger.LogError(ex, "خطأ في حذف مستخدم StatusCode={StatusCode}", (int)ex.StatusCode);
            TempData[TempDataError] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في حذف مستخدم");
            TempData[TempDataError] = AppMessages.Common.OperationFailed;
        }

        return RedirectToAction(nameof(Index));
    }
}