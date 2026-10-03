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
        // ✅ منع تكرار اسم المصنع — فحص مسبق برسالة واضحة، وفهرس التفرّد هو الحماية النهائية
        await EnsureNameIsFreeAsync(dto.FactoryName);

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

        // ✅ منع تكرار اسم المصنع (باستثناء المصنع نفسه)
        await EnsureNameIsFreeAsync(dto.FactoryName, factory.FactoryId);

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
        // IsActive قابل للقيم الفارغة: يُعدَّل فقط حين يُرسَل صراحةً، وإلا تبقى
        // الحالة الحالية — جسمًا بلا isActive كان يُوقف المصنع صامتًا (فخ عقد الـAPI).
        if (dto.IsActive.HasValue)
            factory.IsActive = dto.IsActive.Value;
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

        // ✅ الترتيب مقصود: نُفرغ حقل الشعار ونؤرشف ونثبّت، ثم نحذف الملف آخر خطوة.
        //    حذف الملف قبل التثبيت كان يعني أن أي فشل في SaveChanges يُتلف الشعار على
        //    القرص بينما قاعدة البيانات لم تُؤرشف المصنع بعد ولا تزال تشير إلى الملف.
        var logoPath = factory.Logo;

        factory.Logo = null;
        factory.IsDeleted = true;
        factory.IsActive = false;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            _imageStorageService.DeleteImage(logoPath);
        }

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

    public async Task<string> UploadLogoAsync(int id, Stream fileStream, string originalFileName, long fileLength)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        // ✅ الترتيب مقصود: نحفظ الجديد أولاً لأن SaveImageAsync هي التي تتحقق من الامتداد
        //    والحجم وبصمة المحتوى، وترمي BusinessException عند الرفض. حذف القديم قبلها كان
        //    يعني أن رفع ملف مرفوض (‎.pdf‎ أو أكبر من 5 ميجابايت) يمحو الشعار القائم من
        //    القرص بينما قاعدة البيانات ما زالت تشير إليه — صورة مكسورة بلا رجعة.
        var oldLogoPath = factory.Logo;

        var newLogoPath = await _imageStorageService.SaveImageAsync(fileStream, originalFileName, fileLength, "Factories");

        factory.Logo = newLogoPath;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // فشل التثبيت: لا نُبقي على القرص ملفاً لا تشير إليه قاعدة البيانات.
            _imageStorageService.DeleteImage(newLogoPath);
            throw;
        }

        // ✅ الحذف آخر خطوة: لا يُحذف ملف إلا بعد نجاح التثبيت، فلو فشل حفظ الجديد بقي
        //    القديم سليماً وما زالت قاعدة البيانات تشير إليه.
        if (!string.IsNullOrWhiteSpace(oldLogoPath)
            && !string.Equals(oldLogoPath, newLogoPath, StringComparison.OrdinalIgnoreCase))
        {
            _imageStorageService.DeleteImage(oldLogoPath);
        }

        return newLogoPath;
    }

    public async Task<bool> DeleteLogoAsync(int id)
    {
        var factory = await _unitOfWork.Factories.GetByIdAsync(id);
        if (factory == null || factory.IsDeleted)
            throw new NotFoundException(Messages.FactoryNotFound);

        if (string.IsNullOrWhiteSpace(factory.Logo))
            throw new BusinessException(Messages.FactoryHasNoLogoToDelete);

        // ✅ الترتيب مقصود: نُفرغ الحقل ونثبّت أولاً ثم نحذف الملف. لو حُذف الملف قبل
        //    التثبيت وفشل التثبيت، لبقيت قاعدة البيانات تشير إلى ملف غير موجود.
        var logoPath = factory.Logo;

        factory.Logo = null;
        factory.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Factories.Update(factory);
        await _unitOfWork.SaveChangesAsync();

        _imageStorageService.DeleteImage(logoPath);

        return true;
    }

    /// <summary>
    /// فحص مسبق لتفرّد اسم المصنع — يرمي <c>ConflictException</c> (409) برسالة واضحة.
    /// فهرس التفرّد على <c>FactoryName</c> يبقى الحماية النهائية ضد حالات السباق.
    /// </summary>
    private async Task EnsureNameIsFreeAsync(string factoryName, int? excludeFactoryId = null)
    {
        var exists = await _unitOfWork.Factories.FactoryNameExistsAsync(factoryName, excludeFactoryId);
        if (exists)
            throw new ConflictException(Messages.FactoryAlreadyExists);
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