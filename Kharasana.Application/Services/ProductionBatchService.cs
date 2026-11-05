using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class ProductionBatchService : IProductionBatchService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionBatchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductionBatchDto>> GetFactoryProductionBatchesAsync(UserRole callerRole, int? factoryId)
    {
        IEnumerable<ProductionBatch> batches;
        if (callerRole == UserRole.Admin)
        {
            batches = await _unitOfWork.ProductionBatches.GetAllWithDetailsAsync();
        }
        else if (callerRole == UserRole.FactoryEmployee && factoryId.HasValue)
        {
            batches = await _unitOfWork.ProductionBatches.GetByFactoryIdAsync(factoryId.Value);
        }
        else
        {
            throw new ForbiddenException("لا يمكن تحديد نطاق المصنع لعرض دفعات الإنتاج.");
        }

        return batches.Select(MapToDto);
    }

    public async Task<ProductionBatchDto> GetByIdAsync(int id, UserRole callerRole, int? callerFactoryId)
    {
        var batch = await _unitOfWork.ProductionBatches.GetByIdWithDetailsAsync(id)
            ?? throw new NotFoundException("دفعة الإنتاج غير موجودة.");

        EnsureFactoryAccess(batch.FactoryId, callerRole, callerFactoryId);
        return MapToDto(batch);
    }

    public async Task<IEnumerable<ProductionBatchDto>> GetByOrderIdAsync(
        int orderId,
        UserRole callerRole,
        int? callerFactoryId)
    {
        var order = await GetOrderForCallerAsync(orderId, callerRole, callerFactoryId);
        var batches = await _unitOfWork.ProductionBatches.GetByOrderIdAsync(orderId);
        return batches.Where(batch => batch.FactoryId == order.FactoryId).Select(MapToDto);
    }

    public async Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchDto dto,
        int orderId,
        UserRole callerRole,
        int? callerFactoryId)
    {
        var order = await GetOrderForCallerAsync(orderId, callerRole, callerFactoryId);
        if (order.ConcreteTypeId != dto.ConcreteTypeId)
            throw new BusinessException("نوع الخرسانة لا يطابق نوع الطلب.");

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

        return MapToDto(batch);
    }

    public async Task UpdateStatusAsync(int id, ProductionStatus status, UserRole callerRole, int? callerFactoryId)
    {
        var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(id);
        if (batch == null)
            throw new NotFoundException("دفعة الإنتاج غير موجودة.");
        EnsureFactoryAccess(batch.FactoryId, callerRole, callerFactoryId);

        batch.Status = status;
        _unitOfWork.ProductionBatches.Update(batch);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, UserRole callerRole, int? callerFactoryId)
    {
        var batch = await _unitOfWork.ProductionBatches.GetByIdAsync(id);
        if (batch == null)
            throw new NotFoundException("دفعة الإنتاج غير موجودة.");
        EnsureFactoryAccess(batch.FactoryId, callerRole, callerFactoryId);

        _unitOfWork.ProductionBatches.Delete(batch);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Order> GetOrderForCallerAsync(int orderId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId)
            ?? throw new NotFoundException("الطلب غير موجود.");

        EnsureFactoryAccess(order.FactoryId, callerRole, callerFactoryId);
        return order;
    }

    private static void EnsureFactoryAccess(int entityFactoryId, UserRole callerRole, int? callerFactoryId)
    {
        if (callerRole == UserRole.Admin)
            return;

        if (callerRole != UserRole.FactoryEmployee || callerFactoryId != entityFactoryId)
            throw new ForbiddenException("ليس لديك صلاحية على بيانات هذا المصنع.");
    }

    private static ProductionBatchDto MapToDto(ProductionBatch batch) => new()
    {
        ProductionBatchId = batch.ProductionBatchId,
        OrderId = batch.OrderId,
        ConcreteTypeId = batch.ConcreteTypeId,
        ConcreteTypeName = batch.ConcreteType?.Name ?? string.Empty,
        Quantity = batch.Quantity,
        Status = batch.Status,
        CreatedAt = batch.CreatedAt
    };
}
