using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Application.Interfaces;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class ProductionBatchService : IProductionBatchService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionBatchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductionBatch>> GetFactoryProductionBatchesAsync(int factoryId)
    {
        return await _unitOfWork.ProductionBatches.GetByFactoryIdAsync(factoryId);
    }

    public async Task<IEnumerable<ProductionBatch>> GetByOrderIdAsync(int orderId)
    {
        return await _unitOfWork.ProductionBatches.GetByOrderIdAsync(orderId);
    }

    public async Task<ProductionBatch> CreateAsync(ProductionBatch batch, int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
            throw new Exception("الطلب غير موجود");

        batch.OrderId = orderId;
        batch.FactoryId = order.FactoryId;
        batch.ConcreteTypeId = order.ConcreteTypeId;
        batch.Status = ProductionStatus.Pending;

        await _unitOfWork.ProductionBatches.AddAsync(batch);
        await _unitOfWork.SaveChangesAsync();
        return batch;
    }

    public async Task UpdateStatusAsync(int id, ProductionStatus status, int factoryId)
    {
        var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(id);
        if (batch == null || batch.FactoryId != factoryId)
            throw new Exception("دفعة الإنتاج غير موجودة أو لا تتبع لهذا المصنع");

        batch.Status = status;
        _unitOfWork.ProductionBatches.Update(batch);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int factoryId)
    {
        var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(id);
        if (batch == null || batch.FactoryId != factoryId)
            throw new Exception("دفعة الإنتاج غير موجودة أو لا تتبع لهذا المصنع");

        _unitOfWork.ProductionBatches.Delete(batch);
        await _unitOfWork.SaveChangesAsync();
    }
}
