using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Services;

public class ConcreteTypeService : IConcreteTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public ConcreteTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConcreteTypeDto>> GetAllAsync(int? factoryId = null)
    {
        IEnumerable<ConcreteType> concreteTypes;

        if (factoryId.HasValue)
        {
            concreteTypes = await _unitOfWork.ConcreteTypes
                .GetByFactoryWithFactoryAsync(factoryId.Value);
        }
        else
        {
            concreteTypes = await _unitOfWork.ConcreteTypes
                .GetAllWithFactoryAsync();
        }

        return concreteTypes.Select(MapToDto);
    }

    public async Task<IEnumerable<ConcreteTypeDto>> GetArchivedAsync(int? factoryId = null)
    {
        // المستودع يتجاوز الفلتر العام (!IsDeleted) ليعيد المحذوف حذفًا ناعمًا وحده.
        var archivedTypes = await _unitOfWork.ConcreteTypes
            .GetArchivedWithFactoryAsync(factoryId);

        return archivedTypes.Select(MapToDto);
    }

    public async Task<ConcreteTypeDto> GetByIdAsync(int id, int? currentFactoryId = null)
    {
        var concreteType = await _unitOfWork.ConcreteTypes
            .GetByIdWithFactoryAsync(id);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        // ✅ defense-in-depth: التحقق من صلاحية المصنع
        if (currentFactoryId.HasValue && concreteType.FactoryId != currentFactoryId.Value)
            throw new ForbiddenException(Messages.CannotCreateConcreteTypeForOtherFactory);

        return MapToDto(concreteType);
    }

    public async Task<ConcreteTypeDto> CreateAsync(CreateConcreteTypeDto dto, int? currentFactoryId = null)
    {
        // ✅ التحقق من صلاحية FactoryEmployee
        if (currentFactoryId.HasValue && dto.FactoryId != currentFactoryId.Value)
        {
            throw new ForbiddenException(Messages.CannotCreateConcreteTypeForOtherFactory);
        }

        // التحقق من وجود المصنع وحالته — بما فيها حالة الأرشفة يستلزم قراءة الكيان المؤرشف
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        // ✅ المصنع موقوف — لا يُسمح بإضافة أنواع خرسانة جديدة
        if (!factory.IsActive)
            throw new BusinessException(Messages.FactoryInactive);

        // ✅ منع تكرار الاسم داخل المصنع — فحص مسبق برسالة واضحة،
        //    والفهرس الفريد المُرشَّح (FactoryId, Name) WHERE IsDeleted = 0 هو الحماية النهائية ضد السباق.
        //    الأسماء المحرَّرة بحذف ناعم لا تُحتسب تعارضًا (خيار B).
        await EnsureNameIsFreeAsync(dto.FactoryId, dto.Name);

        var concreteType = new ConcreteType
        {
            FactoryId = dto.FactoryId,
            Name = dto.Name,
            Strength = dto.Strength,
            UnitPrice = dto.UnitPrice,
            ImageUrl = dto.ImageUrl,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ConcreteTypes.AddAsync(concreteType);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(concreteType);
    }

    public async Task<bool> UpdateAsync(int id, UpdateConcreteTypeDto dto, int? currentFactoryId = null)
    {
        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(id);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        // ✅ defense-in-depth: التحقق من صلاحية المصنع
        if (currentFactoryId.HasValue && concreteType.FactoryId != currentFactoryId.Value)
            throw new ForbiddenException(Messages.CannotCreateConcreteTypeForOtherFactory);

        // ✅ المصنع موقوف أو مؤرشف — لا يُسمح بتعديل أنواع الخرسانة أيضاً
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(concreteType.FactoryId);
        if (factory != null && factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        if (factory is { IsActive: false })
            throw new BusinessException(Messages.FactoryInactive);

        // ✅ منع تكرار الاسم داخل المصنع (باستثناء النوع نفسه)
        await EnsureNameIsFreeAsync(concreteType.FactoryId, dto.Name, concreteType.ConcreteTypeId);

        concreteType.Name = dto.Name;
        concreteType.Strength = dto.Strength;
        concreteType.UnitPrice = dto.UnitPrice;
        concreteType.ImageUrl = dto.ImageUrl;
        concreteType.Description = dto.Description;
        concreteType.IsActive = dto.IsActive;
        concreteType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ConcreteTypes.Update(concreteType);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int? currentFactoryId = null)
    {
        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(id);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        // ✅ defense-in-depth: التحقق من صلاحية المصنع
        if (currentFactoryId.HasValue && concreteType.FactoryId != currentFactoryId.Value)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        // ✅ حذف ناعم: لا يُحذف الصف فعليًا حتى تبقى الطلبات التاريخية التي تشير إليه سليمة،
        //    ويختفي من الاستعلامات العادية عبر فلتر الاستعلام العام (!IsDeleted)
        concreteType.IsDeleted = true;
        concreteType.IsActive = false;
        concreteType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ConcreteTypes.Update(concreteType);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(int id, int? currentFactoryId = null)
    {
        // ✅ يجب أن نجد النوع المحذوف — القراءة العادية تستبعد المحذوف عبر فلتر الاستعلام العام
        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdIncludingDeletedAsync(id);
        if (concreteType == null || !concreteType.IsDeleted)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        // ✅ defense-in-depth: التحقق من صلاحية المصنع
        if (currentFactoryId.HasValue && concreteType.FactoryId != currentFactoryId.Value)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        // ✅ لا استعادة إن كان الاسم نفسه مستخدمًا بنوع غير محذوف في المصنع نفسه —
        //    لا نعيد التسمية ولا نحذف النوع النشط تلقائيًا.
        //    قاعدة البيانات تبقى المرجع النهائي: لو سُجّل نوع بنفس الاسم بعد هذا الفحص
        //    يرفضه الفهرس الفريد المُرشَّح ويتحول إلى 409 دون أي تعديل جزئي.
        var nameConflict = await _unitOfWork.ConcreteTypes.FindActiveByNameInFactoryAsync(
            concreteType.FactoryId, concreteType.Name, concreteType.ConcreteTypeId);
        if (nameConflict != null)
            throw new ConflictException(
                string.Format(Messages.ConcreteTypeRestoreNameConflict, concreteType.Name));

        concreteType.IsDeleted = false;
        concreteType.IsActive = true;
        concreteType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ConcreteTypes.Update(concreteType);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// فحص مسبق لتفرّد الاسم داخل المصنع — يرمي <c>ConflictException</c> (409) برسالة واضحة.
    /// الأسماء المحرَّرة بحذف ناعم <b>لا</b> تُحتسب تعارضًا: يجوز إنشاء نوع جديد بالاسم نفسه
    /// بعد حذف النوع القديم (خيار B)، ويبقى الفهرس الفريد المُرشَّح هو الحماية النهائية ضد السباق.
    /// </summary>
    private async Task EnsureNameIsFreeAsync(int factoryId, string name, int? excludeConcreteTypeId = null)
    {
        var conflict = await _unitOfWork.ConcreteTypes.FindActiveByNameInFactoryAsync(
            factoryId, name, excludeConcreteTypeId);

        if (conflict != null)
            throw new ConflictException(Messages.ConcreteTypeAlreadyExists);
    }

    private static ConcreteTypeDto MapToDto(ConcreteType concreteType)
    {
        return new ConcreteTypeDto
        {
            ConcreteTypeId = concreteType.ConcreteTypeId,
            FactoryId = concreteType.FactoryId,
            FactoryName = concreteType.Factory?.FactoryName ?? string.Empty,
            Name = concreteType.Name,
            Strength = concreteType.Strength,
            UnitPrice = concreteType.UnitPrice,
            ImageUrl = concreteType.ImageUrl,
            Description = concreteType.Description,
            IsActive = concreteType.IsActive,
            UpdatedAt = concreteType.UpdatedAt
        };
    }
}