using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Mapping;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class OrderCommandService : IOrderCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdempotencyService _idempotencyService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OrderCommandService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IIdempotencyService idempotencyService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _idempotencyService = idempotencyService;
    }

    public async Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, string? idempotencyKey = null, int? currentUserFactoryId = null)
    {
        // ✅ Idempotency Check - إذا تم توفير مفتاح، نتحقق منه أولاً
        string? requestHash = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            requestHash = ComputeRequestHash(dto, currentUserId, "CreateOrder");

            var (isDuplicate, existingResult, statusCode) = await _idempotencyService
                .CheckAndInitializeAsync<OrderDetailsDto>(idempotencyKey, currentUserId, "CreateOrder", requestHash);

            if (isDuplicate && existingResult != null)
            {
                // عملية مكررة - إرجاع النتيجة المخزنة
                return existingResult;
            }
        }

        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        if (concreteType.FactoryId != dto.FactoryId)
            throw new BusinessException(Messages.FactoryMismatch);

        if (!concreteType.IsActive)
            throw new BusinessException(Messages.ConcreteTypeInactive);

        int clientId;

        if (currentRole is UserRole.Admin or UserRole.FactoryEmployee)
        {
            if (currentRole == UserRole.FactoryEmployee && (!currentUserFactoryId.HasValue || dto.FactoryId != currentUserFactoryId.Value))
                throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

            clientId = await ResolveClientIdForStaffOrderAsync(dto.ClientId);
        }
        else if (currentRole == UserRole.Client)
        {
            var client = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            if (client == null)
                throw new NotFoundException(Messages.UserNotFound);

            if (!client.IsActive)
                throw new BusinessException(Messages.UserInactive);

            clientId = currentUserId;
        }
        else
        {
            throw new ForbiddenException(Messages.OrderCreationNotAllowed);
        }

        var order = BuildOrder(dto, clientId, concreteType);

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var result = OrderMapper.MapToDetailsDto(order, hidePricing: false);

        // ✅ تسجيل نجاح العملية في Idempotency
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && requestHash != null)
        {
            await _idempotencyService.CompleteAsync(idempotencyKey, currentUserId, "CreateOrder", result, 201);
        }

        return result;
    }

    /// <summary>
    /// يحسب تجزئة (Hash) للطلب لتحديد ما إذا كانت البيانات متطابقة عند إعادة الإرسال.
    /// </summary>
    private static string ComputeRequestHash<T>(T request, int userId, string operationType)
    {
        using var sha256 = SHA256.Create();
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var combined = $"{userId}:{operationType}:{json}";
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes);
    }

    public async Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId, string? idempotencyKey = null, int currentUserId = 0)
    {
        // ✅ Idempotency Check
        string? requestHash = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            requestHash = ComputeRequestHash(dto, currentUserId, "CreatePhoneOrder");

            var (isDuplicate, existingResult, statusCode) = await _idempotencyService
                .CheckAndInitializeAsync<PhoneOrderResultDto>(idempotencyKey, currentUserId, "CreatePhoneOrder", requestHash);

            if (isDuplicate && existingResult != null)
            {
                return existingResult;
            }
        }

        var normalizedPhone = YemeniPhoneHelper.Normalize(dto.ClientPhone);
        if (normalizedPhone == null)
            throw new BusinessException(Messages.InvalidYemeniPhone);

        if (dto.FactoryId != employeeFactoryId)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(dto.FactoryId);
        if (factory == null)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (factory.IsDeleted)
            throw new BusinessException(Messages.FactoryArchived);

        var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
        if (concreteType == null)
            throw new NotFoundException(Messages.ConcreteTypeNotFound);

        if (concreteType.FactoryId != dto.FactoryId)
            throw new BusinessException(Messages.FactoryMismatch);

        if (!concreteType.IsActive)
            throw new BusinessException(Messages.ConcreteTypeInactive);

        var client = await _unitOfWork.Users.GetByPhoneAsync(normalizedPhone);

        string? temporaryPassword = null;

        if (client == null)
        {
            if (string.IsNullOrWhiteSpace(dto.ClientFullName))
                throw new BusinessException(Messages.ClientFullNameRequiredForNewClient);

            temporaryPassword = GenerateTemporaryPassword();

            client = new User
            {
                FullName = dto.ClientFullName,
                Phone = normalizedPhone,
                Email = null,
                PasswordHash = _passwordHasher.Hash(temporaryPassword),
                Role = UserRole.Client,
                FactoryId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(client);
        }
        else if (client.Role == UserRole.Client)
        {
            if (!client.IsActive)
                throw new BusinessException(Messages.UserInactive);
        }
        else
        {
            throw new BusinessException(Messages.PhoneLinkedToNonClientAccount);
        }

        var createDto = new CreateOrderDto
        {
            ClientId = client.UserId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            Landmarks = dto.Landmarks,
            SupervisorName = dto.SupervisorName,
            SupervisorPhone = dto.SupervisorPhone,
            CementType = dto.CementType,
            AggregateSize = dto.AggregateSize,
            Admixture = dto.Admixture,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Notes = dto.Notes
        };

        var order = BuildOrder(createDto, client.UserId, concreteType);

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var result = new PhoneOrderResultDto
        {
            Order = OrderMapper.MapToDetailsDto(order, hidePricing: false),
            NewClientTemporaryPassword = temporaryPassword,
            NewClientPhone = temporaryPassword == null ? null : normalizedPhone
        };

        // ✅ تسجيل نجاح العملية في Idempotency
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && requestHash != null)
        {
            await _idempotencyService.CompleteAsync(idempotencyKey, currentUserId, "CreatePhoneOrder", result, 201);
        }

        return result;
    }

    public async Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (order.Status != OrderStatus.Pending)
        {
            var statusName = OrderStatusHelper.GetArabicName(order.Status);
            throw new BusinessException(string.Format(Messages.OrderCannotBeUpdatedInStatus, statusName));
        }

        if (order.ConcreteTypeId != dto.ConcreteTypeId)
        {
            var concreteType = await _unitOfWork.ConcreteTypes.GetByIdAsync(dto.ConcreteTypeId);
            if (concreteType == null)
                throw new BusinessException(string.Format(Messages.ConcreteTypeNotFoundById, dto.ConcreteTypeId));

            if (concreteType.FactoryId != order.FactoryId)
                throw new BusinessException(Messages.ConcreteTypeNotBelongsToFactory);

            if (!concreteType.IsActive)
                throw new BusinessException(string.Format(Messages.ConcreteTypeInactiveForOrder, concreteType.Name));

            order.ConcreteTypeId = dto.ConcreteTypeId;
            order.ConcreteType = concreteType;
            order.ConcreteTypeNameSnapshot = concreteType.Name;
            order.ConcreteTypeStrengthSnapshot = concreteType.Strength;

            // تحديث السعر فقط إذا لم يكن السعر محدداً يدوياً
            if (!order.IsManuallyPriced)
            {
                order.UnitPrice = concreteType.UnitPrice;
            }
        }

        order.ProjectName = dto.ProjectName;
        order.ProjectOwnerName = dto.ProjectOwnerName;
        order.SiteArea = dto.SiteArea;
        order.SiteDescription = dto.SiteDescription;
        order.SlabType = dto.SlabType;
        order.Quantity = dto.Quantity;
        order.NeedPump = dto.NeedPump;
        order.FloorNumber = dto.FloorNumber;
        order.PouringDate = dto.PouringDate;
        order.TransportMethod = dto.TransportMethod;
        order.Notes = dto.Notes;
        order.UpdatedAt = DateTime.UtcNow;

        order.TotalPrice = order.Quantity * order.UnitPrice;
        order.RecalculateNetAmount();

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return OrderMapper.MapToOrderDto(order, false);
    }

    public async Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (callerRole != UserRole.Admin && callerRole != UserRole.FactoryEmployee)
            throw new ForbiddenException(Messages.Unauthorized);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Closed)
            throw new BusinessException(Messages.CannotCancelDeliveredOrClosedOrder);

        ReleaseDriver(order);

        order.IsDeleted = true;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task AddPaymentAsync(int orderId, AddPaymentDto dto, int userId, UserRole role, string? idempotencyKey = null, int? factoryId = null)
    {
        // ✅ تسجيل الدفعة حركة مالية: مخصّصة للمدير ولموظف المصنع فقط.
        //    <c>GetOrderOrThrowAsync</c> وحده لا يكفي: يبقي للعميل والسائق حقّ
        //    الوصول إلى طلبهما (شرط لازم لا كافٍ)، فبلا هذا الحارس كان بإمكان
        //    العميل تسجيل دفعة على طلبه — وهو يمسّ دفتر حسابات المصنع.
        if (role is not (UserRole.Admin or UserRole.FactoryEmployee))
            throw new ForbiddenException(Messages.NotAuthorizedToRecordPayment);

        // ✅ Idempotency Check — يمنع تكرار تسجيل نفس الدفعة عند إعادة الإرسال
        //    (مثال: انقطع الاتصال بعد نجاح التسجيل، فيعيده Flutter بالمفتاح نفسه).
        string? requestHash = null;
        bool isNewOperation = false;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            requestHash = ComputeRequestHash(new { orderId, dto }, userId, "AddPayment");

            var (isDuplicate, _, _) = await _idempotencyService
                .CheckAndInitializeAsync<bool>(idempotencyKey, userId, "AddPayment", requestHash);

            if (isDuplicate)
            {
                // ✅ دفعة سابقة مكتملة بنفس المفتاح — لا ننشئ جديدة، النتيجة محفوظة
                return;
            }

            isNewOperation = true;
        }

        var order = await GetOrderOrThrowAsync(orderId, userId, role, factoryId);

        if (dto.Amount <= 0)
            throw new BusinessException(Messages.PaymentAmountMustBePositive);

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected)
            throw new BusinessException(Messages.CannotRecordPaymentForCancelledOrder);

        // ✅ حساب المتبقي: استثنِ الدفعات المرفوضة (Rejected) والممحولة (Soft Deleted).
        //    المدفوعات التي تحسب: Pending, Verified, وNotApplicable (الكاش).
        var alreadyPaid = order.Payments
            .Where(p => p.VerificationStatus != PaymentVerificationStatus.Rejected)
            .Sum(p => p.Amount);

        var netAmount = order.NetAmount;
        var remaining = netAmount - alreadyPaid;

        if (dto.Amount > remaining)
            throw new BusinessException(Messages.PaymentExceedsRemainingAmount);

        var payment = new OrderPayment
        {
            OrderId = orderId,
            Amount = dto.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNo,
            Notes = dto.Notes,
            VerificationStatus = PaymentVerificationStatus.PendingVerification,
            VerificationNotes = null,
            VerifiedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.OrderPayments.AddAsync(payment);

        // ✅ مسّ تحديث الطلب لتفعيل حارس RowVersion: تسجيل دفعة يغيّر رصيد الطلب
        //    منطقياً، فتغييره هنا يجعل SaveChanges يُرسل UPDATE على Orders محروساً
        //    بـ RowVersion. لو سجّل طلبان دفعتين في آنٍ واحد انطلاقاً من رصيد واحد
        //    (سباق التحقق من المتبقي)، يفسح الثاني بـ DbUpdateConcurrencyException
        //    يلتقطه الـ Middleware ويرد 409 بدل تجاوز المتبقي بصمت.
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // ✅ تسجيل نجاح العملية في Idempotency
        if (isNewOperation && !string.IsNullOrWhiteSpace(idempotencyKey) && requestHash != null)
        {
            await _idempotencyService.CompleteAsync(idempotencyKey, userId, "AddPayment", true, 200);
        }
    }

    /// <summary>
    /// يتولى المحاسب فحص الحوالة ويرسل حالة التحقق (مقروء/مرفوض).
    /// </summary>
    public async Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes, int approverId)
    {
        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        // لا يمكن التحقق من دفعات الكاش المباشرة (NotApplicable)
        if (payment.VerificationStatus == PaymentVerificationStatus.NotApplicable)
            throw new BusinessException(Messages.PaymentCannotBeApprovedCash);

        // ✅ تأشير الصلاحيات: مدير يستطيع أي دفعة، وموظف المصنع somente إذا belonged لمصنعه
        var order = await _unitOfWork.Orders.GetByIdWithDetailsForUpdateAsync(payment.OrderId);
        if (order == null)
            throw new NotFoundException(Messages.OrderNotFound);

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected)
            throw new BusinessException(Messages.CannotRecordPaymentForCancelledOrder);

        // ✅ التحقق من belonging للمصنع: Admin يتجاوز، FactoryEmployee ينتظر مصنعه
        //    التحقق يتم في طبقة controller بفحص Role + FactoryId, وهنا نكتفي بالتأكد من
        //    أن الطلب غير ملغى/مرفوض ولا يحوي حالة incompatible.

        payment.VerificationStatus = PaymentVerificationStatus.Verified;
        payment.VerificationNotes = verificationNotes;
        payment.VerifiedAt = DateTime.UtcNow;
        payment.VerifiedByUserId = approverId;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// رفض الحوالة وإدخال سبب الرفض من قبل المحاسب.
    /// </summary>
    public async Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason, int approverId)
    {
        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        payment.VerificationStatus = PaymentVerificationStatus.Rejected;
        payment.VerificationNotes = rejectionReason;
        payment.VerifiedAt = DateTime.UtcNow;
        payment.VerifiedByUserId = approverId;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<Order> GetOrderOrThrowAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await _unitOfWork.Orders.GetByIdWithDetailsForUpdateAsync(id);
        if (order == null)
            throw new NotFoundException(Messages.OrderNotFound);

        switch (callerRole)
        {
            case UserRole.Admin:
                break;

            case UserRole.FactoryEmployee:
                if (!callerFactoryId.HasValue || order.FactoryId != callerFactoryId.Value)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            case UserRole.Client:
                if (order.ClientId != callerId)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            case UserRole.Driver:
                if (order.DriverId != callerId)
                    throw new NotFoundException(Messages.OrderNotFound);
                break;

            default:
                throw new NotFoundException(Messages.OrderNotFound);
        }

        return order;
    }

    private void ReleaseDriver(Order order)
    {
        if (!order.DriverId.HasValue)
            return;

        var driver = order.Driver;
        if (driver != null && driver.Role == UserRole.Driver)
        {
            driver.DriverStatus = DriverStatus.Available;
            driver.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(driver);
        }
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const int length = 10;

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

        return new string(chars);
    }

    private async Task<int> ResolveClientIdForStaffOrderAsync(int clientId)
    {
        if (clientId <= 0)
            throw new BusinessException(Messages.ClientRequiredForAdminOrder);

        var client = await _unitOfWork.Users.GetByIdAsync(clientId);
        if (client == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (client.Role != UserRole.Client)
            throw new BusinessException(Messages.InvalidClient);

        if (!client.IsActive)
            throw new BusinessException(Messages.UserInactive);

        return clientId;
    }

    private static Order BuildOrder(CreateOrderDto dto, int clientId, ConcreteType concreteType)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var totalPrice = dto.Quantity * concreteType.UnitPrice;

        var order = new Order
        {
            OrderNumber = orderNumber,
            ClientId = clientId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            ConcreteTypeNameSnapshot = concreteType.Name,
            ConcreteTypeStrengthSnapshot = concreteType.Strength,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            Landmarks = dto.Landmarks,
            SupervisorName = dto.SupervisorName,
            SupervisorPhone = dto.SupervisorPhone,
            CementType = dto.CementType,
            AggregateSize = dto.AggregateSize,
            Admixture = dto.Admixture,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            UnitPrice = concreteType.UnitPrice,
            IsManuallyPriced = false,
            TotalPrice = totalPrice,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Status = OrderStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // ✅ صافي المبلغ = الإجمالي − الخصم (الخصم صفر عند الإنشاء).
        //    بدونها يبقى NetAmount صفراً فيبقى RemainingAmount سالباً كاذباً.
        order.RecalculateNetAmount();

        return order;
    }
}
