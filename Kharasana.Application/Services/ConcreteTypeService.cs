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

    public async Task<bool> DeleteAsync(int id)
    {
        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(id);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        _unitOfWork.ConcreteTypes.Delete(concreteType);
        await _unitOfWork.SaveChangesAsync();

        return true;
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
            IsActive = concreteType.IsActive
        };
    }
}