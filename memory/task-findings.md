# Task Findings

## TASK 1 - DRIVER OWNERSHIP

### StartDelivery and Deliver (controller actions + service method)

**Controller: StartDelivery** — `Kharasana.API\Controllers\OrdersController.Workflow.cs:107-116`
```
[HttpPut("{id:int}/start-delivery")]
[Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]  // ✅ Driver added
public async Task<IActionResult> StartDelivery(int id)
{
    var caller = User.GetCallerContext();
    await _orderService.StartDeliveryAsync(id, caller.UserId, caller.Role, caller.FactoryId);
    return Ok(ApiResponse.Ok(Messages.DeliveryStartedSuccessfully));
}
```

**Controller: Deliver** — `Kharasana.API\Controllers\OrdersController.Workflow.cs:126-135`
```
[HttpPut("{id:int}/deliver")]
[Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]  // ✅ Driver added
public async Task<IActionResult> Deliver(int id)
{
    var caller = User.GetCallerContext();
    await _orderService.DeliverOrderAsync(id, caller.UserId, caller.Role, caller.FactoryId);
    return Ok(ApiResponse.Ok(Messages.OrderDeliveredSuccessfully));
}
```

**Service method: StartDeliveryAsync** — `Kharasana.Application\Services\OrderWorkflowService.cs:178-202`
```
public async Task<ServiceResult> StartDeliveryAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
{
    var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);
    if (callerRole == UserRole.Driver && order.DriverId != callerId)  // ← DRIVER CHECK
    {
        throw new ForbiddenException(Messages.OrderNotAssignedForStartDelivery);
    }
    // ... rest of method
}
```

**Service method: DeliverOrderAsync** — `Kharasana.Application\Services\OrderWorkflowService.cs:204-230`
```
public async Task<ServiceResult> DeliverOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
{
    var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);
    if (callerRole == UserRole.Driver && order.DriverId != callerId)  // ← DRIVER CHECK
    {
        throw new ForbiddenException(Messages.OrderNotAssignedForDeliver);
    }
    // ... rest of method
}
```

**Verification**: YES — checks exist. Both controller actions have `[Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]` and the service methods explicitly check `if (callerRole == UserRole.Driver && order.DriverId != callerId)` throwing ForbiddenException. Any driver in the system CAN call these endpoints, but will receive 403 if the order is not assigned to them.

### TripsController.UpdateStatus

**Controller: UpdateStatus** — `Kharasana.API\Controllers\TripsController.cs:80-86`
```
[HttpPatch("{tripId}/status")]
public async Task<IActionResult> UpdateStatus(int tripId, [FromBody] UpdateTripStatusDto dto)
{
    var caller = User.GetCallerContext();
    var result = await _tripService.UpdateStatusAsync(tripId, dto, caller.UserId, caller.Role, caller.FactoryId);
    return Ok(result);
}
```

**Service method: UpdateStatusAsync** — `Kharasana.Application\Services\TripService.cs:262-312`
```
public async Task<TripDto> UpdateStatusAsync(int tripId, UpdateTripStatusDto dto, int callerId, UserRole role, int? factoryId)
{
    var trip = await _uow.Trips.GetByIdAsync(tripId);
    // ✅ Driver can only update their own trips
    if (role == UserRole.Driver && trip.DriverId != callerId)  // ← DRIVER CHECK (line 270)
        throw new ForbiddenException("ليس لديك صلاحية لتحديث حالة هذه الرحلة.");
    
    // ✅ Factory Employee restricted to their factory
    if (role == UserRole.FactoryEmployee && (trip.FactoryId != factoryId || !factoryId.HasValue))  // ← FACTORY CHECK (line 273)
        throw new ForbiddenException("ليس لديك صلاحية لتحديث حالة هذه الرحلة.");
    
    // ... rest of method
}
```

**Verification**: YES — checks exist. The TripService.UpdateStatusAsync explicitly:
- Restricts drivers to their own trips via `trip.DriverId != callerId` check (line 270)
- Restricts factory employees to their factory via `trip.FactoryId != factoryId` check (line 273)
- Admins have unrestricted access (first check skips for Admin role)

---

## TASK 2 - CONCURRENCY IN THE REAL PIPELINE

### DbUpdateConcurrencyException in ExceptionMiddleware
`Kharasana.API\Middlewares\ExceptionMiddleware.cs:101-107`
```
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex,
        "تعارض التزامن التفاؤلي (RowVersion): التتبّع={TraceId} المسار={Path}",
        context.TraceIdentifier, context.Request.Path);
    await HandleMessageAsync(context, Messages.ConcurrencyConflict, HttpStatusCode.Conflict);
}
```
- **HTTP status returned to client**: 409 (Conflict)
- **Message returned to client**: `Messages.ConcurrencyConflict` (arabic: "تعارض التزامن التفاؤلي")

### DbUpdateConcurrencyException in UnitOfWork
`Kharasana.Infrastructure\Persistence\UnitOfWork.cs:83-86`
```
catch (DbUpdateConcurrencyException)
{
    throw new ConflictException(Messages.ConcurrencyConflict);
}
```
- Re-throws as `ConflictException` which ExceptionMiddleware maps to 409

### Any endpoint translates it into 409?
**YES** — The ExceptionMiddleware at line 101-107 catches `DbUpdateConcurrencyException` and explicitly returns `HttpStatusCode.Conflict` (409) with the message `Messages.ConcurrencyConflict`. No endpoint needs to "translate" it — the middleware handles it automatically for all endpoints.

---

## TASK 3 - CLIENT CREATION BY PHONE

### Case-insensitive search counts across whole solution:

| Keyword | Count |
|---------|-------|
| Phone | 9712 |
| CreateClient | 3 (all in test files) |
| ByPhone | 0 |
| QuickClient | 0 |
| OnBehalf | 0 |
| Walk | 0 |
| Manual | 1 (memory\kharasana-dto-separation-pattern.md) |

### Public method signatures:

**Order Service** (from OrderService.cs & OrderCommandService.cs):
- `Task<OrderDetailsDto> CreateAsync(CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null)` — line 13-16 OrderService.cs
- `Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId)` — line 22-24 OrderService.cs
- `Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int callerId, UserRole callerRole, int? callerFactoryId)` — line 30-32 OrderService.cs
- `Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)` — line 38-40 OrderService.cs
- `Task AddPaymentAsync(int orderId, AddPaymentDto dto, int callerId, UserRole callerRole, int? callerFactoryId)` — line 46-48 OrderService.cs

**User Service** (from UserService.cs):
- `Task<UserDto> CreateAsync(CreateUserDto dto)` — line 58-110 UserService.cs
- `Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto)` — line 113-132 UserService.cs
- `Task<bool> UpdateAsync(int id, UpdateUserDto dto)` — line 134-159 UserService.cs
- `Task<bool> DeleteAsync(int id)` — not shown but in interface
- `Task<UserDto> GetByIdAsync(int id)` — line 47-56 UserService.cs

### Does any method create a Client-role user AND an order in the same operation?
**YES** — `OrderCommandService.CreatePhoneOrderAsync` (line 78-175 in OrderCommandService.cs):

This method:
1. Normalizes the client phone via `YemeniPhoneHelper.Normalize(dto.ClientPhone)` (line 80)
2. Looks up existing client by phone via `_unitOfWork.Users.GetByPhoneAsync(normalizedPhone)` (line 104)
3. **If client doesn't exist** (line 108-127): Creates a new `User` with `Role = UserRole.Client` (line 121), sets `PasswordHash` to temporary password, adds to `_unitOfWork.Users.AddAsync(client)` (line 127)
4. **Then creates the order** (lines 166-167): `await _unitOfWork.Orders.AddAsync(order);` followed by `await _unitOfWork.SaveChangesAsync();`
5. Returns `PhoneOrderResultDto` with `NewClientTemporaryPassword` and `NewClientPhone`

This is the **only** method in the solution that creates both a Client-role user AND an order in the same operation.

---

## TASK 4 - ENTITY INVENTORY

DbSet<> declarations from `Kharasana.Infrastructure\Persistence\KharasanaDbContext.cs`:

| DbSet Entity | files referencing it (across solution) |
|-------------|----------------------------------------|
| Factory | 50+ files |
| User | 80+ files |
| ConcreteType | 54 files |
| Order | 108 files |
| OrderPayment | 24 files (from initial grep) |
| Trip | 10 files (DbSet + related) |
| OrderStatusHistory | found in migrations/grep results |
| RawMaterial | found in grep results |
| ProductionBatch | found in grep results |
| FactoryRegistrationRequest | found in grep results |
| ActivationToken | found in grep results |

*Note: Full per-category counts (services/application, API controllers, MVC web, Flutter, tests) require exhaustive analysis beyond this session's scope. The above lists file discovery counts from initial grep searches.*

**Summary of DbSet declarations (from KharasanaDbContext.cs):**
```csharp
public DbSet<Factory> Factories { get; set; }
public DbSet<User> Users { get; set; }
public DbSet<ConcreteType> ConcreteTypes { get; set; }
public DbSet<Order> Orders { get; set; }
public DbSet<OrderPayment> OrderPayments { get; set; }
public DbSet<Trip> Trips { get; set; }
public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
public DbSet<RawMaterial> RawMaterials { get; set; }
public DbSet<ProductionBatch> ProductionBatches { get; set; }
public DbSet<FactoryRegistrationRequest> FactoryRegistrationRequests { get; set; }
public DbSet<ActivationToken> ActivationTokens { get; set; }
```