using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Services;

public class FactoryService : IFactoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService; // ✅ جديد

    public FactoryService(IUnitOfWork unitOfWork, IImageStorageService imageStorageService)
    {
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
    }

    public async Task<IEnumerable<FactoryDto>> GetAllAsync()
    {
        var factories = await _unitOfWork.Factories.GetAllAsync();
        var activeFactories = factories.Where(f => !f.IsDeleted).ToList();

        // ✅ تجميع "للمصنع حساب موظف؟" في استعلام واحد بدلاً من استعلام لكل مصنع (N+1)
        var factoryIdsWithEmployee = await _unitOfWork.Users.GetFactoryIdsWithEmployeeAsync();

        var result = new List<FactoryDto>(activeFactories.Count);
        foreach (var factory in activeFactories)
        {
            result.Add(await MapToDtoAsync(factory, factoryIdsWithEmployee));
        }
        return result;
    }

    public async Task<IEnumerable<FactoryDto>> GetArchivedAsync()
    {
        // GetArchivedAsync في المستودع يتجاوز الفلتر العام (!IsDeleted) لاسترجاع المؤرشفة فقط
        var archivedFactories = (await _unitOfWork.Factories.GetArchivedAsync()).ToList();

        var factoryIdsWithEmployee = await _unitOfWork.Users.GetFactoryIdsWithEmployeeAsync();

        var result = new List<FactoryDto>(archivedFactories.Count);
        foreach (var factory in archivedFactories)
        {
            result.Add(await MapToDtoAsync(factory, factoryIdsWithEmployee));
        }
        return result;
    }

    public async Task<FactoryDto?> GetByIdAsync(int id)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        return await MapToDtoAsync(factory);
    }

    public async Task<FactoryDto> CreateAsync(CreateFactoryDto dto)
    {
        var factory = new Factory
        {
            FactoryName = dto.FactoryName,
            OwnerName = dto.OwnerName,
            Phone = dto.Phone,
            WhatsApp = dto.WhatsApp,
            Email = dto.Email,
            Area = dto.Area,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Logo = dto.Logo, // ملاحظة: يبقى قابلاً للاستخدام يدوياً إن رغبت، لكن الرفع الفعلي سيتم عبر UploadLogoAsync
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Factories.AddAsync(factory);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(factory);
    }

    public async Task<bool> UpdateAsync(int id, UpdateFactoryDto dto)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        factory.FactoryName = dto.FactoryName;
        factory.OwnerName = dto.OwnerName;
        factory.Phone = dto.Phone;
        factory.WhatsApp = dto.WhatsApp;
        factory.Email = dto.Email;
        factory.Area = dto.Area;
        factory.Address = dto.Address;
        factory.Latitude = dto.Latitude;
        factory.Longitude = dto.Longitude;
        // ⚠️ لا نعدل factory.Logo هنا إطلاقاً — إدارة الشعار أصبحت مسؤولية UploadLogoAsync/DeleteLogoAsync فقط
        factory.IsActive = dto.IsActive;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        // ✅ حذف ملف الشعار (إن وجد) قبل أرشفة المصنع، لتجنب بقاء ملفات يتيمة داخل wwwroot
        if (!string.IsNullOrWhiteSpace(factory.Logo))
        {
            _imageStorageService.DeleteImage(factory.Logo);
            factory.Logo = null;
        }

        factory.IsDeleted = true;
        factory.IsActive = false;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        // ✅ يجب أن تجد المصنع المؤرشف — القراءة العادية تستبعد المحذوف عبر فلتر الاستعلام العام
        var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(id);
        if (factory == null || !factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        factory.IsDeleted = false;
        factory.IsActive = true;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ✅ جديد: رفع/استبدال شعار المصنع
    public async Task<string> UploadLogoAsync(int id, Stream fileStream, string originalFileName, long fileLength)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        // إن وجدت صورة قديمة، احذفها أولاً حتى لا تتراكم ملفات يتيمة
        if (!string.IsNullOrWhiteSpace(factory.Logo))
        {
            _imageStorageService.DeleteImage(factory.Logo);
        }

        var newLogoPath = await _imageStorageService.SaveImageAsync(fileStream, originalFileName, fileLength, "Factories");

        factory.Logo = newLogoPath;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        return newLogoPath;
    }

    // ✅ جديد: حذف شعار المصنع
    public async Task<bool> DeleteLogoAsync(int id)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (string.IsNullOrWhiteSpace(factory.Logo))
            throw new BusinessException("لا يوجد شعار لهذا المصنع لحذفه.");

        _imageStorageService.DeleteImage(factory.Logo);

        factory.Logo = null;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<FactoryDto> MapToDtoAsync(Factory factory, HashSet<int>? factoryIdsWithEmployee = null)
    {
        var hasAccount = factoryIdsWithEmployee != null
            ? factoryIdsWithEmployee.Contains(factory.FactoryId)
            : await _unitOfWork.Users.FactoryHasAccountAsync(factory.FactoryId);

        return new FactoryDto
        {
            FactoryId = factory.FactoryId,
            FactoryName = factory.FactoryName,
            OwnerName = factory.OwnerName,
            Phone = factory.Phone,
            WhatsApp = factory.WhatsApp,
            Email = factory.Email,
            Area = factory.Area,
            Address = factory.Address,
            Latitude = factory.Latitude,
            Longitude = factory.Longitude,
            Logo = factory.Logo,
            IsActive = factory.IsActive,
            HasAccount = hasAccount
        };
    }
}