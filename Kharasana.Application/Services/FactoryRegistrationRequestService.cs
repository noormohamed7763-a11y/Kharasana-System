using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class FactoryRegistrationRequestService : IFactoryRegistrationRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public FactoryRegistrationRequestService(IUnitOfWork unitOfWork, IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public async Task<ServiceResult> CreateRequestAsync(RegisterFactoryDto dto)
    {
        var request = new FactoryRegistrationRequest
        {
            FactoryName = dto.FactoryName,
            Area = dto.Area,
            Address = dto.Address,
            CommercialId = dto.BusinessRegistrationNumber,
            ContactName = dto.OwnerName,
            ContactEmail = dto.Email,
            ContactPhone = dto.Phone,
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.FactoryRegistrationRequests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<IEnumerable<FactoryRegistrationRequest>> GetAllRequestsAsync()
    {
        return await _unitOfWork.FactoryRegistrationRequests.GetAllAsync();
    }

    public async Task<IEnumerable<FactoryRegistrationRequest>> GetPendingRequestsAsync()
    {
        var all = await _unitOfWork.FactoryRegistrationRequests.GetAllAsync();
        return all.Where(r => r.Status == RegistrationStatus.Pending);
    }

    public async Task<ServiceResult> RejectRequestAsync(int requestId, string reason)
    {
        var request = await _unitOfWork.FactoryRegistrationRequests.GetByIdAsync(requestId);
        if (request == null || request.Status != RegistrationStatus.Pending)
            return ServiceResult.Fail("الطلب غير موجود أو تمت معالجته مسبقاً.");

        request.Status = RegistrationStatus.Rejected;
        request.ProcessedAt = DateTime.UtcNow;
        // هنا يمكن إضافة حقل للسبب إذا تم إضافته لاحقاً في الـ Entity

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<FactoryRegistrationRequest?> GetRequestDetailsAsync(int requestId)
    {
        return await _unitOfWork.FactoryRegistrationRequests.GetByIdAsync(requestId);
    }

    public async Task<ServiceResult> ApproveRequestAsync(int requestId)
    {
        var request = await _unitOfWork.FactoryRegistrationRequests.GetByIdAsync(requestId);
        if (request == null || request.Status != RegistrationStatus.Pending)
            return ServiceResult.Fail("الطلب غير موجود أو تمت معالجته مسبقاً.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            request.Status = RegistrationStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;

            var factory = new Factory
            {
                FactoryName = request.FactoryName,
                OwnerName = request.ContactName,
                Phone = request.ContactPhone,
                Email = request.ContactEmail,
                Area = request.Area,
                Address = request.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Factories.AddAsync(factory);
            await _unitOfWork.SaveChangesAsync();

            var createUserDto = new CreateUserDto
            {
                FullName = request.ContactName,
                Email = request.ContactEmail,
                Phone = request.ContactPhone,
                Password = string.Empty,
                Role = UserRole.FactoryAdmin,
                FactoryId = factory.FactoryId
            };

            var userDto = await _userService.CreateAsync(createUserDto);

            var token = new ActivationToken
            {
                UserId = userDto.UserId,
                TokenHash = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ActivationTokens.AddAsync(token);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult.Fail($"فشلت عملية القبول: {ex.Message}");
        }
    }
}