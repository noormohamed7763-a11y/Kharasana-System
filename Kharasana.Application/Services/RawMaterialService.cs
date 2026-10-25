using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Application.Interfaces;

namespace Kharasana.Application.Services;

public class RawMaterialService : IRawMaterialService
{
    private readonly IUnitOfWork _unitOfWork;

    public RawMaterialService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RawMaterial>> GetFactoryMaterialsAsync(int factoryId)
    {
        return await _unitOfWork.RawMaterials.GetByFactoryIdAsync(factoryId);
    }

    public async Task<RawMaterial> GetByIdAsync(int id, int factoryId)
    {
        var material = await _unitOfWork.RawMaterials.GetByIdAsync(id);
        if (material == null || material.FactoryId != factoryId)
            throw new Exception("المادة غير موجودة أو لا تتبع لهذا المصنع");
        return material;
    }

    public async Task<RawMaterial> CreateAsync(RawMaterial material, int factoryId)
    {
        material.FactoryId = factoryId;
        await _unitOfWork.RawMaterials.AddAsync(material);
        await _unitOfWork.SaveChangesAsync();
        return material;
    }

    public async Task UpdateStockAsync(int id, decimal quantityChange, int factoryId)
    {
        var material = await GetByIdAsync(id, factoryId);
        material.CurrentStock += quantityChange;
        _unitOfWork.RawMaterials.Update(material);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int factoryId)
    {
        var material = await GetByIdAsync(id, factoryId);
        _unitOfWork.RawMaterials.Delete(material);
        await _unitOfWork.SaveChangesAsync();
    }
}
