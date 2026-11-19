using Kharasana.Application.DTOs.Trips;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Domain.Helpers;

namespace Kharasana.Application.Services;

public class TripService : ITripService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;

    public TripService(IUnitOfWork uow, ICurrentUserService currentUserService)
    {
        _uow = uow;
        _currentUserService = currentUserService;
    }

    // Helper للحصول على الطلب والتحقق من الصلاحيات
    private async Task<Order> GetOrderForFactoryOrThrowAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId)
                    ?? throw new NotFoundException($"لم يتم العثور على الطلب رقم {orderId}");

        if (_currentUserService.IsInRole("Admin")) return order;
        
        if (_currentUserService.FactoryId != order.FactoryId) 
            throw new ForbiddenException("ليس لديك صلاحية على هذا الطلب.");

        if (_currentUserService.IsInRole("Client") && order.ClientId.ToString() != _currentUserService.UserId) 
            throw new ForbiddenException("ليس لديك صلاحية على هذا الطلب.");

        return order;
    }

    // Helper للحصول على الرحلة والتحقق من الصلاحيات
    private async Task<Trip> GetTripOrThrowAsync(int tripId)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId)
                   ?? throw new NotFoundException($"لم يتم العثور على الرحلة رقم {tripId}");

        if (_currentUserService.IsInRole("Admin")) return trip;
        
        if (_currentUserService.FactoryId != trip.FactoryId) 
            throw new ForbiddenException("ليس لديك صلاحية على هذه الرحلة.");

        // Driver can only see/update their own trips
        if (_currentUserService.IsInRole("Driver") && trip.DriverId.ToString() != _currentUserService.UserId) 
           
        if (_currentUserService.IsInRole("Driver") && trip.DriverId.ToString() != _currentUserService.UserId) 
            throw new ForbiddenException("ليس لديك صلاحية على هذه الرحلة.");

        return trip;
    }

    // Helper لتأكيد أن السائق ينتمي لنفس المصنع
    private async Task EnsureDriverBelongsToFactoryAsync(int driverId, int factoryId)
    {
        var driver = await _uow.Users.GetByIdAsync(driverId)
                     ?? throw new NotFoundException("السائق غير موجود.");
        if (driver.FactoryId != factoryId || driver.Role != UserRole.Driver)
        {            throw new BusinessException("السائق لا ينتمي لهذا المصنع أو ليس سائقاً.");
        }
    }

    // Helper للتحقق من الكمية
    private void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new BusinessException("الكمية يجب أن تكون أكبر من صفر.");
    }

    // Mapper
    private TripDto MapToDto(Trip trip)
    {
        // TODO: Expand this to fetch DriverName and PumpDriverName from the User entity using Include or Projection
        return new TripDto
        {
            TripId = trip.TripId,
            OrderId = trip.OrderId,
            Sequence = trip.Sequence,
            Quantity = trip.Quantity,
            DriverId = trip.DriverId,
            TruckPlate = trip.TruckPlate,
            HasPump = trip.HasPump,
            Status = trip.Status,
            StatusNameAr = TripStatusHelper.GetArabicName(trip.Status),
            ScheduledAt = trip.ScheduledAt,
            CompletedAt = trip.CompletedAt
        };
    }

    // منطق تجميع حالة الطلب من الرحلات
    private async Task RecalculateOrderStatusAsync(Order order)
    {        var activeTrips = order.Trips.Where(t => !t.IsDeleted && t.Status != TripStatus.Cancelled).ToList();
        if (!activeTrips.Any())
        {            if (order.Status == OrderStatus.Scheduled)
            {
                order.Status = OrderStatus.Approved;
                order.UpdatedAt = DateTime.UtcNow;
            }
            return;
        }

        // كل الرحلات مكتملة → Delivered
        if (activeTrips.All(t => t.Status == TripStatus.Completed))
        {
            if (order.Status is not (OrderStatus.Delivered or OrderStatus.Closed))
            {
                order.Status = OrderStatus.Delivered;
                order.DeliveredAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
            }
            return;
        }

        // أي رحلة خرجت أو بعدها → OnTheWay
        if (activeTrips.Any(t => t.Status is TripStatus.Departed or TripStatus.AtSite or TripStatus.Unloaded))
        {
            if (order.Status is OrderStatus.Approved or OrderStatus.Scheduled)
            {
                order.Status = OrderStatus.OnTheWay;
                order.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public async Task<IReadOnlyList<TripDto>> GetByOrderAsync(int orderId)
    {
        var order = await GetOrderForFactoryOrThrowAsync(orderId);

        var trips = await _uow.Trips.GetByOrderIdAsync(orderId);
        return trips.Select(MapToDto).ToList();
    }

    public async Task<TripDto> CreateAsync(int orderId, CreateTripDto dto)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.Orders.GetByIdWithTripsAsync(orderId);
            if (order == null) throw new NotFoundException($"لم يتم العثور على الطلب رقم {orderId}");

            // Permissions for order-related actions
            if (_currentUserService.IsInRole("Client") || _currentUserService.IsInRole("Driver"))
                throw new ForbiddenException("ليس لديك صلاحية لإضافة رحلات لهذا الطلب.");

            if (!_currentUserService.IsInRole("Admin") && order.FactoryId != _currentUserService.FactoryId)
                 throw new ForbiddenException("ليس لديك صلاحية لإضافة رحلات لهذا الطلب.");

            if (order.Status is not (OrderStatus.Approved or OrderStatus.Scheduled or OrderStatus.OnTheWay))
                throw new BusinessException("لا يمكن إضافة رحلات لهذه الحالة");

            ValidateQuantity(dto.Quantity);

            var existingQty = order.Trips
                .Where(t => !t.IsDeleted && t.Status != TripStatus.Cancelled)
                .Sum(t => t.Quantity);

            if (existingQty + dto.Quantity > order.Quantity)
                throw new BusinessException($"الكمية المتبقية {order.Quantity - existingQty} م³ فقط");

            if (dto.DriverId.HasValue)
                await EnsureDriverBelongsToFactoryAsync(dto.DriverId.Value, order.FactoryId);

            var sequence = order.Trips.Count(t => !t.IsDeleted) + 1;

            var trip = new Trip
            {
                OrderId = order.OrderId,
                FactoryId = order.FactoryId,
                Sequence = sequence,
                Quantity = dto.Quantity,
                DriverId = dto.DriverId,
                TruckPlate = dto.TruckPlate,
                HasPump = dto.HasPump,
                PumpDriverId = dto.PumpDriverId,
                Status = TripStatus.Scheduled,
                ScheduledAt = dto.ScheduledAt,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Trips.AddAsync(trip);

            if (order.Status == OrderStatus.Approved)
            {
                order.Status = OrderStatus.Scheduled;
                order.UpdatedAt = DateTime.UtcNow;
            }

            // Temporary compatibility: fill DriverId/TruckPlate on Order from first trip if empty
            if (order.DriverId == null && dto.DriverId.HasValue)
                order.DriverId = dto.DriverId;
            if (string.IsNullOrEmpty(order.TruckPlate) && !string.IsNullOrEmpty(dto.TruckPlate))
                order.TruckPlate = dto.TruckPlate;

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            return MapToDto(trip);
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<TripDto>> CreateBatchAsync(int orderId, CreateTripsBatchDto dto)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.Orders.GetByIdWithTripsAsync(orderId);
            if (order == null) throw new NotFoundException($"لم يتم العثور على الطلب رقم {orderId}");

            // Permissions for order-related actions
            if (_currentUserService.IsInRole("Client") || _currentUserService.IsInRole("Driver"))
                throw new ForbiddenException("ليس لديك صلاحية لإضافة رحلات لهذا الطلب.");

            if (!_currentUserService.IsInRole("Admin") && (order.FactoryId != _currentUserService.FactoryId))
                 throw new ForbiddenException("ليس لديك صلاحية لإضافة رحلات لهذا الطلب.");

            if (order.Status is not (OrderStatus.Approved or OrderStatus.Scheduled or OrderStatus.OnTheWay))
                throw new BusinessException("لا يمكن إضافة رحلات لهذه الحالة");

            var newTrips = new List<Trip>();
            var currentSequence = order.Trips.Count(t => !t.IsDeleted) + 1;
            var existingQty = order.Trips.Where(t => !t.IsDeleted && t.Status != TripStatus.Cancelled).Sum(t => t.Quantity);
            decimal totalNewQuantity = 0;

            foreach (var tripDto in dto.Trips)
            {
                ValidateQuantity(tripDto.Quantity);
                totalNewQuantity += tripDto.Quantity;
                if (existingQty + totalNewQuantity > order.Quantity)
                    throw new BusinessException($"الكمية الإجمالية للرحلات تتجاوز الكمية المطلوبة: {order.Quantity} م³");

                if (tripDto.DriverId.HasValue)
                    await EnsureDriverBelongsToFactoryAsync(tripDto.DriverId.Value, order.FactoryId);

                newTrips.Add(new Trip
                {
                    OrderId = order.OrderId,
                    FactoryId = order.FactoryId,
                    Sequence = currentSequence++,
                    Quantity = tripDto.Quantity,
                    DriverId = tripDto.DriverId,
                    TruckPlate = tripDto.TruckPlate,
                    HasPump = tripDto.HasPump,
                    PumpDriverId = tripDto.PumpDriverId,
                    Status = TripStatus.Scheduled,
                    ScheduledAt = tripDto.ScheduledAt,
                    Notes = tripDto.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _uow.Trips.AddRangeAsync(newTrips);

            if (order.Status == OrderStatus.Approved)
            {
                order.Status = OrderStatus.Scheduled;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            return newTrips.Select(MapToDto).ToList();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<TripDto> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId);
        if (trip == null) throw new NotFoundException($"لم يتم العثور على الرحلة رقم {tripId}");

        // Admin can update any trip status
        // Factory Employee can update any trip status in their factory
        // Driver can only update status of their own trips
        if (_currentUserService.IsInRole("Driver") && trip.DriverId.ToString() != _currentUserService.UserId) 
            throw new ForbiddenException("ليس لديك صلاحية لتحديث حالة هذه الرحلة.");

        if (!_currentUserService.IsInRole("Admin") && (trip.FactoryId != _currentUserService.FactoryId))
            throw new ForbiddenException("ليس لديك صلاحية لتحديث حالة هذه الرحلة.");

        if (!TripStatusHelper.CanTransition(trip.Status, dto.Status))
            throw new BusinessException(
                $"لا يمكن الانتقال من {TripStatusHelper.GetArabicName(trip.Status)} إلى {TripStatusHelper.GetArabicName(dto.Status)}");

        var now = DateTime.UtcNow;
        trip.Status = dto.Status;
        trip.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            trip.Notes = dto.Notes;

        switch (dto.Status)
        {
            case TripStatus.Departed:
                trip.DepartedAt = now;
                break;
            case TripStatus.AtSite:
                trip.ArrivedAt = now;
                break;
            case TripStatus.Unloaded:
                trip.UnloadedAt = now;
                break;
            case TripStatus.Completed:
                trip.CompletedAt = now;
                break;
            case TripStatus.Cancelled:
                break;
        }

        var order = await _uow.Orders.GetByIdWithTripsAsync(trip.OrderId);
        if (order == null) throw new NotFoundException($"الطلب المرتبط بالرحلة رقم {trip.OrderId} غير موجود.");
        await RecalculateOrderStatusAsync(order);

        // TODO: RefreshDriverStatusAsync (ليس جزءاً من النطاق الحالي)

        await _uow.SaveChangesAsync();
        return MapToDto(trip);
    }

    public async Task SoftDeleteAsync(int tripId)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId);
        if (trip == null) throw new NotFoundException($"لم يتم العثور على الرحلة رقم {tripId}");

        if (_currentUserService.IsInRole("Driver") || _currentUserService.IsInRole("Client"))
            throw new ForbiddenException("ليس لديك صلاحية لحذف هذه الرحلة.");

        if (!_currentUserService.IsInRole("Admin") && (trip.FactoryId != _currentUserService.FactoryId))
            throw new ForbiddenException("ليس لديك صلاحية لحذف هذه الرحلة.");

        if (trip.Status is not (TripStatus.Scheduled or TripStatus.Cancelled))
            throw new BusinessException("لا يمكن حذف رحلة بدأت التنفيذ.");

        trip.IsDeleted = true;
        trip.UpdatedAt = DateTime.UtcNow;

        var order = await _uow.Orders.GetByIdWithTripsAsync(trip.OrderId);
        if (order == null) throw new NotFoundException($"الطلب المرتبط بالرحلة رقم {trip.OrderId} غير موجود.");
        await RecalculateOrderStatusAsync(order);

        await _uow.SaveChangesAsync();
    }
}
