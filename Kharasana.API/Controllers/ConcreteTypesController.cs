using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// أنواع الخرسانة لكل مصنع: قائمة وتفاصيل وإنشاء وتعديل وحذف.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ConcreteTypesController : ControllerBase
{
    private readonly IConcreteTypeService _concreteTypeService;

    public ConcreteTypesController(IConcreteTypeService concreteTypeService)
    {
        _concreteTypeService = concreteTypeService;
    }

    // ============================================================
    // ✅ GET ALL - تم إضافة Client للصلاحيات
    // ============================================================
    /// <summary>جلب قائمة أنواع الخرسانة بحسب صلاحية المتصل.</summary>
    /// <remarks>
    /// - <b>Admin:</b> كل الأنواع (بما فيها غير النشطة).
    /// - <b>FactoryEmployee:</b> أنواع مصنعه فقط.
    /// - <b>Client:</b> الأنواع النشطة فقط.
    /// </remarks>
    /// <response code="200">تم جلب الأنواع بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClient)]  // ✅ إضافة Client
    public async Task<IActionResult> GetAll()
    {
        var caller = User.GetCallerContext();

        // ============================================================
        // ✅ Client - جميع أنواع الخرسانة النشطة
        // ============================================================
        if (caller.Role == UserRole.Client)
        {
            var allTypes = await _concreteTypeService.GetAllAsync(null);
            var activeTypes = allTypes.Where(t => t.IsActive).ToList();

            return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
            {
                Success = true,
                Message = Messages.ConcreteTypesRetrievedSuccessfully,
                Data = activeTypes
            });
        }

        // ============================================================
        // ✅ FactoryEmployee - أنواع الخرسانة لمصنعه فقط
        // ============================================================
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);

            var factoryTypes = await _concreteTypeService.GetAllAsync(caller.FactoryId);
            return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
            {
                Success = true,
                Message = Messages.ConcreteTypesRetrievedSuccessfully,
                Data = factoryTypes
            });
        }

        // ============================================================
        // ✅ Admin - جميع أنواع الخرسانة (بما في ذلك غير النشطة)
        // ============================================================
        var concreteTypes = await _concreteTypeService.GetAllAsync(null);
        return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
        {
            Success = true,
            Message = Messages.ConcreteTypesRetrievedSuccessfully,
            Data = concreteTypes
        });
    }

    // ============================================================
    // ✅ GET ARCHIVED - المؤرشفة (المحذوفة حذفًا ناعمًا)
    // ============================================================
    /// <summary>جلب أنواع الخرسانة المؤرشفة بحسب صلاحية المتصل.</summary>
    /// <remarks>
    /// - <b>Admin:</b> المؤرشفة في كل المصانع.
    /// - <b>FactoryEmployee:</b> المؤرشفة في مصنعه فقط.
    /// مسار منفصل عن <c>GET /api/ConcreteTypes</c> لا وسيط عليه، لأن فلتر الحذف العام
    /// يستبعد المؤرشفة فلا يمكن التعبير عن هذا الطلب بالاستعلام العادي.
    /// </remarks>
    /// <response code="200">تم جلب الأنواع المؤرشفة بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    [HttpGet("archived")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetArchived()
    {
        var caller = User.GetCallerContext();

        int? currentFactoryId = null;
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            currentFactoryId = caller.FactoryId;
        }

        var archivedTypes = await _concreteTypeService.GetArchivedAsync(currentFactoryId);

        return Ok(new ApiResponse<IEnumerable<ConcreteTypeDto>>
        {
            Success = true,
            Message = Messages.ArchivedConcreteTypesRetrievedSuccessfully,
            Data = archivedTypes
        });
    }

    // ============================================================
    // ✅ GET BY ID - تم إضافة Client للصلاحيات
    // ============================================================
    /// <summary>جلب نوع خرسانة واحد بمعرّفه.</summary>
    /// <remarks>
    /// - <b>Admin:</b> أي نوع.
    /// - <b>FactoryEmployee:</b> أنواع مصنعه فقط.
    /// - <b>Client:</b> الأنواع النشطة فقط.
    /// </remarks>
    /// <param name="id">معرّف نوع الخرسانة.</param>
    /// <response code="200">تم جلب النوع بنجاح.</response>
    /// <response code="404">النوع غير موجود، أو غير نشط، أو لا ينتمي لمصنع الموظف.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrClient)]  // ✅ إضافة Client
    public async Task<IActionResult> GetById(int id)
    {
        var caller = User.GetCallerContext();

        var concreteType = await _concreteTypeService.GetByIdAsync(id);
        if (concreteType == null)
        {
            return NotFound(ApiResponse.Fail(Messages.ConcreteTypeNotFoundShort));
        }

        // ============================================================
        // ✅ Client - يمكنه مشاهدة أي نوع خرسانة نشط
        // ============================================================
        if (caller.Role == UserRole.Client)
        {
            if (!concreteType.IsActive)
            {
                return NotFound(ApiResponse.Fail(Messages.ConcreteTypeNotActiveForClient));
            }
            return Ok(new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.ConcreteTypeRetrievedSuccessfully,
                Data = concreteType
            });
        }

        // ============================================================
        // ✅ FactoryEmployee - يمكنه مشاهدة أنواع خرسانة مصنعه فقط
        // ============================================================
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null || concreteType.FactoryId != caller.FactoryId)
            {
                return NotFound(ApiResponse.Fail(Messages.ConcreteTypeNotFoundOrNotForFactory));
            }
            return Ok(new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.ConcreteTypeRetrievedSuccessfully,
                Data = concreteType
            });
        }

        // ============================================================
        // ✅ Admin - يمكنه مشاهدة أي نوع خرسانة
        // ============================================================
        return Ok(new ApiResponse<ConcreteTypeDto>
        {
            Success = true,
            Message = Messages.ConcreteTypeRetrievedSuccessfully,
            Data = concreteType
        });
    }

    // ============================================================
    // ✅ CREATE - للمدير وموظف المصنع
    // ============================================================
    /// <summary>إنشاء نوع خرسانة جديد.</summary>
    /// <remarks>موظف المصنع يُنشئ النوع لمصنعه تلقائياً؛ المدير يحدّد المصنع في الحمولة.</remarks>
    /// <param name="dto">بيانات النوع الجديد.</param>
    /// <response code="201">تم إنشاء النوع بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية الإنشاء.</response>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<CreateConcreteTypeDto>))]
    public async Task<IActionResult> Create([FromBody] CreateConcreteTypeDto dto)
    {
        var caller = User.GetCallerContext();

        int? currentFactoryId = null;
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            currentFactoryId = caller.FactoryId;
        }

        var concreteType = await _concreteTypeService.CreateAsync(dto, currentFactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = concreteType.ConcreteTypeId },
            new ApiResponse<ConcreteTypeDto>
            {
                Success = true,
                Message = Messages.CreatedSuccessfully,
                Data = concreteType
            });
    }

    // ============================================================
    // ✅ UPDATE - للمدير وموظف المصنع
    // ============================================================
    /// <summary>تعديل نوع خرسانة موجود.</summary>
    /// <remarks>موظف المصنع لا يعدّل إلا نوعاً يتبع مصنعه.</remarks>
    /// <param name="id">معرّف النوع.</param>
    /// <param name="dto">البيانات الجديدة للنوع.</param>
    /// <response code="200">تم التعديل بنجاح.</response>
    /// <response code="400">بيانات غير صالحة.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">النوع لا ينتمي لمصنع الموظف.</response>
    /// <response code="404">النوع غير موجود.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<UpdateConcreteTypeDto>))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConcreteTypeDto dto)
    {
        var caller = User.GetCallerContext();

        int? currentFactoryId = null;
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            currentFactoryId = caller.FactoryId;
        }

        await _concreteTypeService.UpdateAsync(id, dto, currentFactoryId);

        return Ok(ApiResponse.Ok(Messages.UpdatedSuccessfully));
    }

    // ============================================================
    // ✅ DELETE - للمدير وموظف المصنع (حذف ناعم)
    // ============================================================
    /// <summary>حذف (أرشفة) نوع خرسانة — حذف ناعم يبقى قابلاً للاستعادة.</summary>
    /// <remarks>
    /// - <b>Admin:</b> أي نوع.
    /// - <b>FactoryEmployee:</b> أنواع مصنعه فقط.
    /// لا يُحذف الصف فعليًا، فتبقى الطلبات التاريخية التي تشير إليه سليمة.
    /// </remarks>
    /// <param name="id">معرّف النوع.</param>
    /// <response code="200">تم الحذف بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">النوع لا ينتمي لمصنع الموظف.</response>
    /// <response code="404">النوع غير موجود.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();

        int? currentFactoryId = null;
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            currentFactoryId = caller.FactoryId;
        }

        await _concreteTypeService.DeleteAsync(id, currentFactoryId);

        return Ok(ApiResponse.Ok(Messages.DeletedSuccessfully));
    }

    // ============================================================
    // ✅ RESTORE - لموظف المصنع صاحب النوع
    // ============================================================
    /// <summary>استعادة نوع خرسانة محذوف (مؤرشف).</summary>
    /// <remarks>
    /// للمدير (أي مصنع) ولموظف المصنع (أنواع مصنعه فقط) — مطابقةً لباقي عمليات الكتابة
    /// على هذا المورد (Create/Update/Delete).
    /// تفشل بـ 409 إن كان الاسم نفسه مستخدمًا بنوع غير محذوف في المصنع نفسه؛
    /// يجب إعادة تسمية النوع الحالي أو حذفه قبل الاستعادة (لا إعادة تسمية تلقائية).
    /// </remarks>
    /// <param name="id">معرّف النوع المحذوف.</param>
    /// <response code="200">تمت الاستعادة بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">النوع لا ينتمي لمصنع الموظف.</response>
    /// <response code="404">النوع غير موجود أو غير محذوف.</response>
    /// <response code="409">الاسم مستخدم بالفعل بنوع غير محذوف في المصنع.</response>
    [HttpPost("restore/{id:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> Restore(int id)
    {
        var caller = User.GetCallerContext();

        // ✅ المدير غير مرتبط بمصنع: يمرّر null فتتخطى الخدمة فحص العزل (defense-in-depth)،
        //    بينما موظف المصنع بلا مصنع مُسنَد يبقى مرفوضًا — نفس نمط Create.
        int? currentFactoryId = null;
        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            currentFactoryId = caller.FactoryId;
        }

        await _concreteTypeService.RestoreAsync(id, currentFactoryId);

        return Ok(ApiResponse.Ok(Messages.ConcreteTypeRestoredSuccessfully));
    }
}