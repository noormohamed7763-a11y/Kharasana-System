using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class ProductionBatchService : IProductionBatchService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionBatchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductionBatchDto>> GetFactoryProductionBatchesAsync(int factoryId)
    {
        var batches = await _unitOfWork.ProductionBatches.GetByFactoryIdAsync(factoryId);
        return batches.Select(b => new ProductionBatchDto
        {
            ProductionBatchId = b.ProductionBatchId,
            OrderId = b.OrderId,
            ConcreteTypeId = b.ConcreteTypeId,
            Quantity = b.Quantity,
            Status = b.Status,
            CreatedAt = b.CreatedAt
        });
    }

    public async Task<IEnumerable<ProductionBatchDto>> GetByOrderIdAsync(int orderId)
    {
        var batches = await _unitOfWork.ProductionBatches.GetByOrderIdAsync(orderId);
        return batches.Select(b => new ProductionBatchDto
        {
            ProductionBatchId = b.ProductionBatchId,
            OrderId = b.OrderId,
            ConcreteTypeId = b.ConcreteTypeId,
            Quantity = b.Quantity,
            Status = b.Status,
            CreatedAt = b.CreatedAt
        });
    }

    public async Task<ProductionBatchDto> CreateAsync(CreateProductionBatchDto dto, int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
            throw new Exception("الطلب غير موجود");

        var batch = new Kharasana.Domain.Entities.ProductionBatch
        {
            OrderId = orderId,
            ConcreteTypeId = dto.ConcreteTypeId,
            Quantity = dto.Quantity,
            FactoryId = order.FactoryId,
            Status = Kharasana.Domain.Enums.ProductionStatus.Pending
        };

        await _unitOfWork.ProductionBatches.AddAsync(batch);
        await _unitOfWork.SaveChangesAsync();

        return new ProductionBatchDto
        {
            ProductionBatchId = batch.ProductionBatchId,
            OrderId = batch.OrderId,
            ConcreteTypeId = batch.ConcreteTypeId,
            Quantity = batch.Quantity,
            Status = batch.Status,
            CreatedAt = batch.CreatedAt
        };
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
