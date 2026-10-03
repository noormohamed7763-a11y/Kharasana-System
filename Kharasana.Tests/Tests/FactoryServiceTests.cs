using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات FactoryService — التركيز على:
/// • GetAll يُرجع المنشآت النشطة فقط
/// • GetArchived يُرجع المنشآت المحذوفة فقط
/// • Delete يضبط IsDeleted = true
/// • Restore يُلغي الحذف
/// • HasAccount يُحدّث بشكل صحيح عبر الاستعلام المجمّع
/// • منع تكرار اسم المصنع (409) — بما فيه الأسماء المؤرشفة
/// </summary>
public class FactoryServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly FakeImageStorage _imageStorage;
    private readonly FactoryService _service;

    public FactoryServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _imageStorage = new FakeImageStorage();
        _service = new FactoryService(new UnitOfWork(_context), _imageStorage);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeletedFactories()
    {
        // Arrange
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(1, "نشيطة"),
            TestDataSeeder.CreateFactory(2, "محذوفة", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].FactoryId.Should().Be(1);
    }

    [Fact]
    public async Task GetArchived_ReturnsOnlyDeletedFactories()
    {
        // Arrange
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(1, "نشيطة2"),
            TestDataSeeder.CreateFactory(2, "محذوفة2", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetArchivedAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].FactoryId.Should().Be(2);
    }

    [Fact]
    public async Task Delete_SetsIsDeletedAndClearsLogo()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(3, "لحذف");
        factory.Logo = "/images/factory-logo.png";
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var success = await _service.DeleteAsync(3);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Factories.FindAsync(3);
        updated!.IsDeleted.Should().BeTrue();
        updated.IsActive.Should().BeFalse();
        updated.Logo.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFound()
    {
        var act = () => _service.DeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.FactoryNotFound);
    }

    [Fact]
    public async Task Restore_UnsetsIsDeleted()
    {
        // Arrange — مصنع محذوف
        var factory = TestDataSeeder.CreateFactory(4, "لاستعادته", isActive: false, isDeleted: true);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var success = await _service.RestoreAsync(4);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Factories.FindAsync(4);
        updated!.IsDeleted.Should().BeFalse();
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_MapsHasAccountCorrectly()
    {
        // Arrange — مصنع بموظف + مصنع بدون موظف
        var f1 = TestDataSeeder.CreateFactory(5, "لديها_حساب");
        var f2 = TestDataSeeder.CreateFactory(6, "بدون_حساب");
        var employee = TestDataSeeder.CreateUser(50, "موظف", UserRole.FactoryEmployee, factoryId: 5);
        _context.Factories.AddRange(f1, f2);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).OrderBy(x => x.FactoryId).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First(x => x.FactoryId == 5).HasAccount.Should().BeTrue();
        result.First(x => x.FactoryId == 6).HasAccount.Should().BeFalse();
    }

    // ─────────────────────────────────────────────
    //  منع تكرار اسم المصنع — Duplicates
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DuplicateFactoryName_ThrowsConflict()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(60, "مصنع_اسم_مكرر"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateFactoryDto
        {
            FactoryName = "مصنع_اسم_مكرر",
            Area = "صنعاء",
            Address = "شارع الاختبار"
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.FactoryAlreadyExists);

        (await _context.Factories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_DuplicateFactoryNameWithDifferentCase_ThrowsConflict()
    {
        // Arrange — ترتيب SQL Server الافتراضي غير حساس لحالة الأحرف
        _context.Factories.Add(TestDataSeeder.CreateFactory(61, "AlNoor"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateFactoryDto
        {
            FactoryName = "alnoor",
            Area = "صنعاء",
            Address = "شارع الاختبار"
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.FactoryAlreadyExists);
    }

    [Fact]
    public async Task Create_ArchivedFactoryName_ThrowsConflict()
    {
        // Arrange — فهرس التفرّد على FactoryName يشمل المصانع المؤرشفة، فالاسم يبقى محجوزًا
        _context.Factories.Add(
            TestDataSeeder.CreateFactory(62, "مصنع_مؤرشف_الاسم", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateFactoryDto
        {
            FactoryName = "مصنع_مؤرشف_الاسم",
            Area = "صنعاء",
            Address = "شارع الاختبار"
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.FactoryAlreadyExists);
    }

    [Fact]
    public async Task Update_RenameToExistingFactoryName_ThrowsConflict()
    {
        // Arrange
        var first = TestDataSeeder.CreateFactory(63, "مصنع_الاول");
        var second = TestDataSeeder.CreateFactory(64, "مصنع_الثاني");
        _context.Factories.AddRange(first, second);
        await _context.SaveChangesAsync();

        // Act — إعادة تسمية المصنع 64 إلى اسم المصنع 63
        var act = () => _service.UpdateAsync(64, new UpdateFactoryDto
        {
            FactoryName = "مصنع_الاول",
            Area = "صنعاء",
            Address = "شارع الاختبار",
            IsActive = true
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.FactoryAlreadyExists);

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 64);
        stored.FactoryName.Should().Be("مصنع_الثاني");
    }

    [Fact]
    public async Task Update_KeepingSameFactoryName_Succeeds()
    {
        // Arrange — الإبقاء على الاسم نفسه ليس تكرارًا
        _context.Factories.Add(TestDataSeeder.CreateFactory(65, "مصنع_نفسه"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateAsync(65, new UpdateFactoryDto
        {
            FactoryName = "مصنع_نفسه",
            Area = "عدن",
            Address = "شارع جديد",
            IsActive = true
        });

        // Assert
        result.Should().BeTrue();

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 65);
        stored.Area.Should().Be("عدن");
    }

    /// <summary>
    /// تحديث بلا <c>IsActive</c> لا يوقف المصنع صامتًا.
    ///
    /// <para><b>العلّة التي يمنعها:</b> كان <c>UpdateFactoryDto.IsActive</c>
    /// <c>bool</c> غير قابل للقيم الفارغة، فيُسند <c>false</c> افتراضيًا —
    /// فجسم PUT لا يحوي <c>isActive</c> (أو يحمله <c>null</c> في JSON) كان
    /// يُوقف المصنع <b>دون قصد</b>. جعله <c>bool?</c> يجعل الغياب = إبقاء
    /// الحالة الحالية، ويُعدَّل الحقل فقط حين يُرسَل صراحةً.</para>
    /// </summary>
    [Fact]
    public async Task Update_WithoutIsActive_KeepsTheCurrentActivation()
    {
        // Arrange — مصنع نشط
        _context.Factories.Add(TestDataSeeder.CreateFactory(80, "مصنع_نشط"));
        await _context.SaveChangesAsync();

        // Act — جسم لا يحوي IsActive إطلاقًا
        var result = await _service.UpdateAsync(80, new UpdateFactoryDto
        {
            FactoryName = "مصنع_نشط",
            Area = "صنعاء",
            Address = "شارع جديد"
        });

        // Assert — نجح التعديل وبقي المصنع نشطًا
        result.Should().BeTrue();

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 80);
        stored.IsActive.Should().BeTrue("جسم بلا isActive لا يجوز أن يوقف المصنع صامتًا");
        stored.Area.Should().Be("صنعاء");
    }

    /// <summary>
    /// إرسال <c>IsActive = false</c> صراحةً يوقف المصنع فعلاً —
    /// الإصلاح لا يُفقد الوظيفة، بل يمنع الإيقاف <b>غير المقصود</b> فقط.
    /// </summary>
    [Fact]
    public async Task Update_WithIsActiveFalse_StopsTheFactory()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(81, "مصنع_سيتوقف"));
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateAsync(81, new UpdateFactoryDto
        {
            FactoryName = "مصنع_سيتوقف",
            Area = "عدن",
            Address = "شارع جديد",
            IsActive = false
        });

        // Assert
        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 81);
        stored.IsActive.Should().BeFalse("إرسال false صريح يجب أن يوقف المصنع");
    }

    /// <summary>
    /// رفع شعار مرفوض لا يجوز أن يمحو الشعار القائم.
    ///
    /// <para><b>العلّة التي يمنعها:</b> كان <c>UploadLogoAsync</c> يحذف ملف الشعار القديم
    /// <b>قبل</b> استدعاء <c>SaveImageAsync</c> — وهي التي تتحقق من الامتداد والحجم وبصمة
    /// المحتوى وترمي عند الرفض. فرفع ملف ‎.pdf‎ أو أكبر من 5 ميجابايت كان يمحو الشعار من
    /// القرص ويردّ 400، بينما <c>factory.Logo</c> ما زال في قاعدة البيانات يشير إلى الملف
    /// المحذوف — صورة مكسورة بلا رجعة.</para>
    /// </summary>
    [Fact]
    public async Task UploadLogo_WhenTheNewFileIsRejected_KeepsTheExistingLogoIntact()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(70, "بشعار"));
        await _context.SaveChangesAsync();

        var factory = await _context.Factories.FirstAsync(f => f.FactoryId == 70);
        factory.Logo = "/Images/Factories/old.png";
        await _context.SaveChangesAsync();

        _imageStorage.FailNextSave = true;

        var act = () => _service.UploadLogoAsync(70, new MemoryStream([1, 2, 3]), "bad.pdf", 3);

        await act.Should().ThrowAsync<BusinessException>();

        _imageStorage.Operations.Should().BeEmpty(
            "ملف مرفوض يعني ألّا يُمَس الشعار القديم إطلاقاً");

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 70);
        stored.Logo.Should().Be("/Images/Factories/old.png");
    }

    /// <summary>الحذف لا يسبق نجاح الحفظ — وإلا صار أي فشل لاحق مُدمّراً للشعار القائم.</summary>
    [Fact]
    public async Task UploadLogo_OnSuccess_StoresTheNewFileBeforeDeletingTheOldOne()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(71, "بشعار٢"));
        await _context.SaveChangesAsync();

        var factory = await _context.Factories.FirstAsync(f => f.FactoryId == 71);
        factory.Logo = "/Images/Factories/old.png";
        await _context.SaveChangesAsync();

        var returned = await _service.UploadLogoAsync(71, new MemoryStream([1, 2, 3]), "new.png", 3);

        _imageStorage.Operations.Should().Equal(
            "save:/Images/Factories/new.png",
            "delete:/Images/Factories/old.png");

        returned.Should().Be("/Images/Factories/new.png");

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 71);
        stored.Logo.Should().Be("/Images/Factories/new.png");
    }

    /// <summary>حذف الشعار يُفرّغ الحقل في قاعدة البيانات ثم يحذف الملف — لا العكس.</summary>
    [Fact]
    public async Task DeleteLogo_ClearsTheStoredPathAndThenRemovesTheFile()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(72, "بشعار٣"));
        await _context.SaveChangesAsync();

        var factory = await _context.Factories.FirstAsync(f => f.FactoryId == 72);
        factory.Logo = "/Images/Factories/old.png";
        await _context.SaveChangesAsync();

        await _service.DeleteLogoAsync(72);

        _imageStorage.Operations.Should().Equal("delete:/Images/Factories/old.png");

        var stored = await _context.Factories.AsNoTracking().FirstAsync(f => f.FactoryId == 72);
        stored.Logo.Should().BeNull();
    }

    /// <summary>
    /// أرشفة المصنع تُفرّغ الشعار وتؤرشف في قاعدة البيانات ثم تحذف الملف — لا العكس.
    /// نفس علّة ترتيب الحذف في <c>UploadLogoAsync</c>: الحذف قبل التثبيت يجعل أي فشل
    /// في <c>SaveChanges</c> يُتلف الملف بينما الصف لم يتغيّر بعد.
    /// </summary>
    [Fact]
    public async Task Delete_ClearsTheLogoAndArchivesBeforeRemovingTheFile()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(73, "بشعار٤"));
        await _context.SaveChangesAsync();

        var factory = await _context.Factories.FirstAsync(f => f.FactoryId == 73);
        factory.Logo = "/Images/Factories/old.png";
        await _context.SaveChangesAsync();

        await _service.DeleteAsync(73);

        _imageStorage.Operations.Should().Equal("delete:/Images/Factories/old.png");

        // ← المصنع صار مؤرشفاً، وفلتر الاستعلام العام يستبعده، فلا بدّ من تجاوزه للقراءة.
        var stored = await _context.Factories.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(f => f.FactoryId == 73);
        stored.Logo.Should().BeNull();
        stored.IsDeleted.Should().BeTrue();
        stored.IsActive.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}

/// <summary>
/// تنفيذ وهمي لـ IImageStorageService للاختبارات (بدون ملفات فعلية).
///
/// <para>يسجّل عملياته بترتيبها في <see cref="Operations"/> كي يمكن إثبات أن الحذف لا
/// يسبق نجاح الحفظ، ويمكن جعله يرفض الحفظ عبر <see cref="FailNextSave"/> لمحاكاة ملف
/// مرفوض (امتداد أو حجم أو بصمة محتوى) كما تفعل الخدمة الحقيقية.</para>
/// </summary>
internal class FakeImageStorage : IImageStorageService
{
    /// <summary>سجل مرتّب: <c>save:المسار</c> أو <c>delete:المسار</c>.</summary>
    public List<string> Operations { get; } = [];

    /// <summary>عند ضبطه يرمي الحفظ استثناءً بدل أن ينجح.</summary>
    public bool FailNextSave { get; set; }

    public Task<string> SaveImageAsync(System.IO.Stream stream, string fileName, long fileSize, string subFolder)
    {
        if (FailNextSave)
            return Task.FromException<string>(new BusinessException("الملف المرفوع غير صالح."));

        var path = $"/Images/{subFolder}/{fileName}";
        Operations.Add($"save:{path}");
        return Task.FromResult(path);
    }

    public void DeleteImage(string? imagePath)
    {
        if (!string.IsNullOrWhiteSpace(imagePath))
            Operations.Add($"delete:{imagePath}");
    }
}