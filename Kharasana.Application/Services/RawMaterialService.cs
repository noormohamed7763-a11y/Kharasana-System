using Kharasana.Application.DTOs.RawMaterial;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Services;

public class RawMaterialService : IRawMaterialService
{
    private readonly IUnitOfWork _unitOfWork;

    public RawMaterialService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RawMaterialDto>> GetFactoryMaterialsAsync(int factoryId)
    {
        var materials = await _unitOfWork.RawMaterials.GetByFactoryIdAsync(factoryId);
        return materials.Select(m => new RawMaterialDto
        {
            RawMaterialId = m.RawMaterialId,
            Name = m.Name,
            CurrentStock = m.CurrentStock,
            Unit = m.Unit,
            FactoryId = m.FactoryId
        });
    }

    public async Task<RawMaterialDto> GetByIdAsync(int id, int factoryId)
    {
        var material = await _unitOfWork.RawMaterials.GetByIdAsync(id);
        if (material == null || material.FactoryId != factoryId)
            throw new Exception("المادة غير موجودة أو لا تتبع لهذا المصنع");

        return new RawMaterialDto
        {
            RawMaterialId = material.RawMaterialId,
            Name = material.Name,
            CurrentStock = material.CurrentStock,
            Unit = material.Unit,
            FactoryId = material.FactoryId
        };
    }

    public async Task<RawMaterialDto> CreateAsync(CreateRawMaterialDto dto, int factoryId)
    {
        var material = new RawMaterial
        {
            Name = dto.Name,
            CurrentStock = dto.CurrentStock,
            Unit = dto.Unit,
            FactoryId = factoryId
        };

        await _unitOfWork.RawMaterials.AddAsync(material);
        await _unitOfWork.SaveChangesAsync();

        return new RawMaterialDto
        {
            RawMaterialId = material.RawMaterialId,
            Name = material.Name,
            CurrentStock = material.CurrentStock,
            Unit = material.Unit,
            FactoryId = material.FactoryId
        };
    }

    public async Task UpdateStockAsync(int id, decimal quantityChange, int factoryId)
    {
        var material = await GetByIdAsync(id, factoryId);
        // We need to fetch the actual entity again since GetByIdAsync returns DTO now
        var entity = await _unitOfWork.RawMaterials.GetByIdAsync(id);
        if (entity == null || entity.FactoryId != factoryId)
            throw new Exception("المادة غير موجودة أو لا تتبع لهذا المصنع");

        entity.CurrentStock += quantityChange;
        _unitOfWork.RawMaterials.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int factoryId)
    {
        var material = await _unitOfWork.RawMaterials.GetByIdAsync(id);
        if (material == null || material.FactoryId != factoryId)
            throw new Exception("المادة غير موجودة أو لا تتبع لهذا المصنع");

        _unitOfWork.RawMaterials.Delete(material);
        await _unitOfWork.SaveChangesAsync();
    }
}
