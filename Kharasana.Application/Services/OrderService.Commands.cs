using System.Security.Cryptography;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

// إنشاء الطلبات وتعديلها وحذفها: طلب عميل، طلب هاتفي (مع إنشاء حساب عميل عند الحاجة)،
// تعديل طلب قائم، وحذف ناعم.
public partial class OrderService
{
    // ============================================================
    // CREATE ORDER
    // ============================================================
    public async Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto dto, int currentUserId, UserRole currentRole, int? currentUserFactoryId = null)
    {
        // ✅ تشمل المؤرشف: لا تُنشأ طلبات على مصنع مؤرشف — خطأ واضح بدل "غير موجود"
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

        if (currentRole == UserRole.Admin)
        {
            clientId = await ResolveClientIdForStaffOrderAsync(dto.ClientId);
        }
        else if (currentRole == UserRole.FactoryEmployee)
        {
            if (!currentUserFactoryId.HasValue || dto.FactoryId != currentUserFactoryId.Value)
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

        return MapToDetailsDto(order, hidePricing: false);
    }

    // ============================================================
    // CREATE PHONE ORDER
    // ============================================================
    public async Task<PhoneOrderResultDto> CreatePhoneOrderAsync(PhoneOrderDto dto, int employeeFactoryId)
    {
        var normalizedPhone = YemeniPhoneHelper.Normalize(dto.ClientPhone);
        if (normalizedPhone == null)
            throw new BusinessException(Messages.InvalidYemeniPhone);

        if (dto.FactoryId != employeeFactoryId)
            throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);

        // ✅ تشمل المؤرشف: لا تُعدَّل طلبات تابعة لمصنع مؤرشف
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

        // تُملأ فقط عند إنشاء حساب جديد، وتُعاد للموظف مرة واحدة في نتيجة العملية.
        string? temporaryPassword = null;

        if (client == null)
        {
            if (string.IsNullOrWhiteSpace(dto.ClientFullName))
                throw new BusinessException(Messages.ClientFullNameRequiredForNewClient);

            // ✅ كلمة مرور مؤقتة مقروءة تُسلَّم للعميل شفهياً.
            //    قبل ذلك كانت تُولَّد كـ Guid عشوائي لا يمكن لأحد كتابته، فيُنشأ الحساب
            //    ولا يستطيع العميل تسجيل الدخول أبداً ولا يوجد مسار لاستعادة كلمة المرور.
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
            await _unitOfWork.SaveChangesAsync();
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

        return new PhoneOrderResultDto
        {
            Order = MapToDetailsDto(order, hidePricing: false),
            NewClientTemporaryPassword = temporaryPassword,
            NewClientPhone = temporaryPassword == null ? null : normalizedPhone
        };
    }

    // ============================================================
    // UPDATE ORDER
    // ============================================================
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

            // ✅ السعر مخزَّن كلقطة (snapshot) من سعر النوع وقت الإنشاء، ويجوز تجاوزه يدويًا عبر
            //    SetPriceAsync. نُحدّث اللقطة فقط إن كانت لا تزال تساوي سعر النوع السابق (أي لم
            //    تُتجاوَز يدويًا) حتى لا نُسقط سعرًا متفقًا عليه مع العميل عند تغيير النوع.
            var previousUnitPrice = order.ConcreteType?.UnitPrice;
            var wasManuallyPriced = previousUnitPrice.HasValue && order.UnitPrice != previousUnitPrice.Value;

            order.ConcreteTypeId = dto.ConcreteTypeId;

            // ✅ إسناد الـ navigation صراحةً: MapToOrderDto/MapToDetailsDto يقرآن Name وStrength
            //    من order.ConcreteType، فلا نتركهما رهينة توقيت إصلاح EF للـ navigation —
            //    بدونهما كانت الاستجابة تُعيد اسم وقوة النوع القديم.
            order.ConcreteType = concreteType;

            // ✅ ولقطتا النوع معه: تغيير النوع يغيّر الاسم والمقاومة المعروضين،
            //    فتُحدَّث اللقطتان كي يعبّر الطلب عن النوع الجديد فعلًا.
            order.ConcreteTypeNameSnapshot = concreteType.Name;
            order.ConcreteTypeStrengthSnapshot = concreteType.Strength;

            if (!wasManuallyPriced)
                order.UnitPrice = concreteType.UnitPrice;
        }

        // التحقق من الحقول (الكمية، التاريخ، المضخة والطابق) أصبح مسؤولية
        // FluentValidation عبر ValidationFilter — مصدر التحقق الوحيد قبل الوصول للخدمة.

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

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToOrderDto(order, false);
    }

    // ============================================================
    // DELETE - حذف ناعم (Soft Delete) للطلب
    // ============================================================
    public async Task<bool> DeleteOrderAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId)
    {
        var order = await GetOrderOrThrowAsync(id, callerId, callerRole, callerFactoryId);

        if (callerRole != UserRole.Admin && callerRole != UserRole.FactoryEmployee)
            throw new ForbiddenException(Messages.Unauthorized);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Closed)
            throw new BusinessException(Messages.CannotCancelDeliveredOrClosedOrder);

        // ✅ تحرير السائق قبل الحذف الناعم: الطلب المحذوف يختفي من كل الاستعلامات،
        //    فلو بقي السائق على DriverStatus.Busy لما ظهر في قائمة المتاحين ولا يمكن
        //    تحريره لاحقًا عبر أي مسار — تسريب دائم لمورد السائق.
        ReleaseDriverAsync(order);

        order.IsDeleted = true;
        order.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    /// <summary>
    /// كلمة مرور مؤقتة مقروءة (10 محارف) من أبجدية بلا محارف متشابهة
    /// (0/O و1/l/I) — تُقرأ وتُكتب يدويًا بلا لبس عند تسليمها هاتفيًا.
    /// تُولَّد بمولّد أرقام عشوائية تشفيري (<see cref="RandomNumberGenerator"/>) لا بـ Random.
    /// </summary>
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

    /// <summary>
    /// يبني الطلب مع لقطتَي نوع الخرسانة (الاسم والمقاومة).
    /// يُمرَّر النوع نفسه لا سعره وحده، كي تُلتقط اللقطتان من المصدر ذاته لحظة الإنشاء.
    /// </summary>
    private static Order BuildOrder(CreateOrderDto dto, int clientId, ConcreteType concreteType)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var totalPrice = dto.Quantity * concreteType.UnitPrice;

        return new Order
        {
            OrderNumber = orderNumber,
            ClientId = clientId,
            FactoryId = dto.FactoryId,
            ConcreteTypeId = dto.ConcreteTypeId,
            // ✅ ما دام النوع أمامنا الآن، نسجّل اسمه ومقاومته مع الطلب
            ConcreteTypeNameSnapshot = concreteType.Name,
            ConcreteTypeStrengthSnapshot = concreteType.Strength,
            ProjectName = dto.ProjectName,
            ProjectOwnerName = dto.ProjectOwnerName,
            SiteArea = dto.SiteArea,
            SiteDescription = dto.SiteDescription,
            SlabType = dto.SlabType,
            Quantity = dto.Quantity,
            NeedPump = dto.NeedPump,
            FloorNumber = dto.FloorNumber,
            UnitPrice = concreteType.UnitPrice,
            TotalPrice = totalPrice,
            PouringDate = dto.PouringDate,
            TransportMethod = dto.TransportMethod,
            Status = OrderStatus.Pending,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
