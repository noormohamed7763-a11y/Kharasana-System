using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات ConcreteTypeService — التركيز على:
/// • منع إضافة نوع خرسانة في مصنع مؤرشف
/// • منع إضافة/تعديل نوع خرسانة في مصنع موقوف
/// • المصنع الموقوف لا يسمح بالتعديل
/// • الحذف الناعم لنوع الخرسانة وسلامة الطلبات التاريخية
/// • استعادة النوع المحذوف وتعارض الاسم عند الاستعادة
/// • منع تكرار الاسم بين نوعين نشطين داخل المصنع (409)
/// • إعادة استخدام اسم نوع محذوف ناعمًا لإنشاء نوع نشط جديد (خيار B)
/// </summary>
public class ConcreteTypeServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly ConcreteTypeService _service;

    public ConcreteTypeServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _service = new ConcreteTypeService(new UnitOfWork(_context));
    }

    [Fact]
    public async Task Create_ArchivedFactory_ThrowsFactoryArchived()
    {
        // Arrange — مصنع محذوف
        var factory = TestDataSeeder.CreateFactory(100, "محذوفة", isActive: false, isDeleted: true);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 100,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryArchived);
    }

    [Fact]
    public async Task Create_InactiveFactory_ThrowsFactoryInactive()
    {
        // Arrange — مصنع موقوف (غير محذوف)
        var factory = TestDataSeeder.CreateFactory(101, "موقوفة", isActive: false);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 101,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryInactive);
    }

    [Fact]
    public async Task Update_InactiveFactory_ThrowsFactoryInactive()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(102, "موقوفة2", isActive: false);
        var ct = TestDataSeeder.CreateConcreteType(200, 102, "C25");
        _context.Factories.Add(factory);
        _context.ConcreteTypes.Add(ct);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.UpdateAsync(200, new UpdateConcreteTypeDto
        {
            Name = "C30",
            Strength = 30,
            UnitPrice = 180m,
            IsActive = true
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryInactive);
    }

    [Fact]
    public async Task GetAll_FiltersByFactory()
    {
        // Arrange — مصنعان، كلٌّ له نوعان
        var f1 = TestDataSeeder.CreateFactory(103, "مصنع_أ");
        var f2 = TestDataSeeder.CreateFactory(104, "مصنع_ب");
        _context.Factories.AddRange(f1, f2);
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(201, 103, "C25_أ"),
            TestDataSeeder.CreateConcreteType(202, 103, "C30_أ"),
            TestDataSeeder.CreateConcreteType(203, 104, "C25_ب"));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync(103)).ToList();

        // Assert — فقط أنواع المصنع 103
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.FactoryId == 103);
    }

    // ─────────────────────────────────────────────
    // ①′  قائمة المؤرشفة — GetArchived (الوجه الآخر للحذف الناعم)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetArchived_WithFactoryFilter_ReturnsOnlyDeletedTypesOfThatFactory()
    {
        // Arrange — مصنعان، في كلٍّ منهما نوع محذوف ونوع نشط
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(105, "مصنع_أرشيف_أ"),
            TestDataSeeder.CreateFactory(106, "مصنع_أرشيف_ب"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(210, 105, "محذوف_أ", isDeleted: true, isActive: false),
            TestDataSeeder.CreateConcreteType(211, 105, "نشط_أ"),
            TestDataSeeder.CreateConcreteType(212, 106, "محذوف_ب", isDeleted: true, isActive: false));
        await _context.SaveChangesAsync();

        // Act — موظف المصنع 105
        var result = (await _service.GetArchivedAsync(105)).ToList();

        // Assert — المحذوف من مصنعه وحده، والنشط مُستبعَد
        result.Should().HaveCount(1);
        result[0].ConcreteTypeId.Should().Be(210);
        result[0].FactoryId.Should().Be(105);
        result.Should().OnlyContain(x => x.FactoryId == 105);
    }

    [Fact]
    public async Task GetArchived_WithoutFactoryFilter_ReturnsDeletedTypesOfEveryFactory()
    {
        // Arrange — نفس الترتيب، لكن بلا عزل (المدير)
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(107, "مصنع_أرشيف_ج"),
            TestDataSeeder.CreateFactory(108, "مصنع_أرشيف_د"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(220, 107, "محذوف_ج", isDeleted: true, isActive: false),
            TestDataSeeder.CreateConcreteType(221, 108, "محذوف_د", isDeleted: true, isActive: false),
            TestDataSeeder.CreateConcreteType(222, 108, "نشط_د"));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetArchivedAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.ConcreteTypeId).Should().BeEquivalentTo(new[] { 220, 221 });
    }

    [Fact]
    public async Task GetArchived_AfterDelete_CarriesArchiveTimestamp()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(109, "مصنع_ختم"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(230, 109, "C25"));
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(230, currentFactoryId: 109);
        var archived = (await _service.GetArchivedAsync(109)).ToList();

        // Assert — تاريخ الأرشفة الذي تعرضه الصفحة يأتي من UpdatedAt التي تكتبها DeleteAsync
        archived.Should().HaveCount(1);
        archived[0].UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetArchived_AfterRestore_NoLongerListsTheType()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(111, "مصنع_استعادة"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(240, 111, "C30"));
        await _context.SaveChangesAsync();
        await _service.DeleteAsync(240, currentFactoryId: 111);

        // Act — استعادة ثم قراءة الأرشيف
        await _service.RestoreAsync(240, currentFactoryId: 111);
        var archived = (await _service.GetArchivedAsync(111)).ToList();

        // Assert — عاد إلى القائمة العادية وخرج من الأرشيف
        archived.Should().BeEmpty();
        var active = (await _service.GetAllAsync(111)).ToList();
        active.Should().ContainSingle(x => x.ConcreteTypeId == 240);
    }

    // ─────────────────────────────────────────────
    // ②  الحذف الناعم — Soft Delete
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_OwnFactoryType_SoftDeletesAndHidesFromNormalQueries()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(110, "مصنع_حذف");
        _context.Factories.Add(factory);
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(300, 110, "C25"));
        await _context.SaveChangesAsync();

        // Act — موظف المصنع يحذف نوعًا يتبع مصنعه
        await _service.DeleteAsync(300, currentFactoryId: 110);

        // Assert — الصف ما زال موجودًا فعليًا (حذف ناعم لا فيزيائي)
        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 300);

        stored.IsDeleted.Should().BeTrue();
        stored.IsActive.Should().BeFalse();

        // ويختفي من الاستعلامات العادية بفضل فلتر الاستعلام العام
        var all = (await _service.GetAllAsync(110)).ToList();
        all.Should().NotContain(x => x.ConcreteTypeId == 300);

        var act = () => _service.GetByIdAsync(300);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_TypeReferencedByExistingOrder_KeepsOrderValid()
    {
        // Arrange — طلب قائم يشير إلى نوع الخرسانة
        _context.Factories.Add(TestDataSeeder.CreateFactory(111, "مصنع_طلبات"));
        _context.Users.Add(TestDataSeeder.CreateUser(300, "عميل_اختبار", UserRole.Client, phone: "050000300"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(301, 111, "C30"));
        await _context.SaveChangesAsync();

        var order = TestDataSeeder.CreateOrder(0, clientId: 300, factoryId: 111, concreteTypeId: 301);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act — المدير يحذف النوع (لا يُمرَّر مصنع للمدير)
        await _service.DeleteAsync(301);

        // Assert — الطلب التاريخي سليم: لم يُحذف ولم يتغيّر مرجعه
        var storedOrder = await _context.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(o => o.OrderId == order.OrderId);

        storedOrder.ConcreteTypeId.Should().Be(301);
        storedOrder.IsDeleted.Should().BeFalse();

        // ونوع الخرسانة نفسه ما زال محفوظًا باسمه (مرجع العرض التاريخي لم ينكسر)
        var storedType = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 301);

        storedType.Name.Should().Be("C30");
        storedType.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_TypeOfAnotherFactory_ThrowsForbiddenAndLeavesItUntouched()
    {
        // Arrange — النوع يتبع المصنع 112 والمتصل موظف المصنع 113
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(112, "مصنع_أ_للحذف"),
            TestDataSeeder.CreateFactory(113, "مصنع_ب_للحذف"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(302, 112, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.DeleteAsync(302, currentFactoryId: 113);

        // Assert — العزل محفوظ ولم يتغيّر السجل
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.FactoryEmployeeFactoryMismatch);

        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 302);

        stored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFound()
    {
        var act = () => _service.DeleteAsync(9999, currentFactoryId: 110);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.ConcreteTypeNotFound);
    }

    // ─────────────────────────────────────────────
    // ③  الاستعادة — Restore
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Restore_OwnDeletedType_MakesItActiveAndVisibleAgain()
    {
        // Arrange — نوع محذوف ناعمًا في المصنع 114
        _context.Factories.Add(TestDataSeeder.CreateFactory(114, "مصنع_استعادة"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(310, 114, "C35", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        await _service.RestoreAsync(310, currentFactoryId: 114);

        // Assert
        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 310);

        stored.IsDeleted.Should().BeFalse();
        stored.IsActive.Should().BeTrue();

        var all = (await _service.GetAllAsync(114)).ToList();
        all.Should().Contain(x => x.ConcreteTypeId == 310);
    }

    [Fact]
    public async Task Restore_TypeOfAnotherFactory_ThrowsForbiddenAndLeavesItDeleted()
    {
        // Arrange — النوع يتبع المصنع 115 والمتصل موظف المصنع 116
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(115, "مصنع_ج_للاستعادة"),
            TestDataSeeder.CreateFactory(116, "مصنع_د_للاستعادة"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(311, 115, "C25", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.RestoreAsync(311, currentFactoryId: 116);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage(Messages.FactoryEmployeeFactoryMismatch);

        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 311);

        stored.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Restore_ActiveSameNameExists_ThrowsConflictAndModifiesNeitherRecord()
    {
        // Arrange — نوع محذوف ونوع غير محذوف بالاسم نفسه داخل المصنع 117
        _context.Factories.Add(TestDataSeeder.CreateFactory(117, "مصنع_تعارض"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(320, 117, "C25", isActive: false, isDeleted: true),
            TestDataSeeder.CreateConcreteType(321, 117, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.RestoreAsync(320, currentFactoryId: 117);

        // Assert — 409 برسالة تشرح أن الاسم مستخدم ويجب إعادة التسمية/الحذف أولاً
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(string.Format(Messages.ConcreteTypeRestoreNameConflict, "C25"));

        // لم يتغيّر أي من السجلين: لا استعادة ولا إعادة تسمية ولا حذف تلقائي للنوع النشط
        var storedDeleted = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 320);

        storedDeleted.IsDeleted.Should().BeTrue();
        storedDeleted.Name.Should().Be("C25");

        var storedActive = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 321);

        storedActive.IsDeleted.Should().BeFalse();
        storedActive.IsActive.Should().BeTrue();
        storedActive.Name.Should().Be("C25");
    }

    [Fact]
    public async Task Restore_NotDeletedType_ThrowsNotFound()
    {
        // Arrange — النوع موجود وغير محذوف
        _context.Factories.Add(TestDataSeeder.CreateFactory(118, "مصنع_غير_محذوف"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(322, 118, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.RestoreAsync(322, currentFactoryId: 118);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.ConcreteTypeNotFound);
    }

    [Fact]
    public async Task Restore_NonExistent_ThrowsNotFound()
    {
        var act = () => _service.RestoreAsync(9998, currentFactoryId: 118);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.ConcreteTypeNotFound);
    }

    // ─────────────────────────────────────────────
    // ④  منع التكرار — Duplicates
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DuplicateNameInSameFactory_ThrowsConflict()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(120, "مصنع_تكرار"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(330, 120, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 120,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.ConcreteTypeAlreadyExists);

        (await _context.ConcreteTypes.CountAsync(x => x.FactoryId == 120)).Should().Be(1);
    }

    [Fact]
    public async Task Create_DuplicateNameWithDifferentCase_ThrowsConflict()
    {
        // Arrange — ترتيب SQL Server الافتراضي غير حساس لحالة الأحرف، فالفحص المسبق يطابقه
        _context.Factories.Add(TestDataSeeder.CreateFactory(121, "مصنع_حالة_الأحرف"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(331, 121, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 121,
            Name = "c25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.ConcreteTypeAlreadyExists);
    }

    [Fact]
    public async Task Create_NameOfSoftDeletedType_Succeeds()
    {
        // Arrange — اسم محرَّر بحذف ناعم: الفهرس الفريد مُرشَّح بـ WHERE IsDeleted = 0
        // فيسمح بنوع جديد نشط بالاسم نفسه داخل المصنع نفسه (خيار B).
        _context.Factories.Add(TestDataSeeder.CreateFactory(122, "مصنع_اسم_محرَّر"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(332, 122, "C40", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var created = await _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 122,
            Name = "C40",
            Strength = 40,
            UnitPrice = 200m
        });

        // Assert — النوع الجديد أُنشئ، والنوع المحذوف بقي كما هو دون إعادة تسمية
        created.Name.Should().Be("C40");
        created.IsActive.Should().BeTrue();

        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.FactoryId == 122)
            .ToListAsync();

        stored.Should().HaveCount(2); // القديم المحذوف + الجديد النشط، بالاسم نفسه
        stored.Single(x => x.ConcreteTypeId == 332).IsDeleted.Should().BeTrue();
        stored.Single(x => x.ConcreteTypeId != 332).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NameOfSoftDeletedType_WithDifferentCase_Succeeds()
    {
        // Arrange — المقارنة غير الحساسة لحالة الأحرف تنطبق على المحذوف أيضًا،
        // فلا يعوق الاسم المحرَّر إن كُتب بحالة أحرف مختلفة.
        _context.Factories.Add(TestDataSeeder.CreateFactory(127, "مصنع_حالة_محرَّرة"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(343, 127, "C45", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var created = await _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 127,
            Name = "c45",
            Strength = 45,
            UnitPrice = 210m
        });

        // Assert
        created.Name.Should().Be("c45");
    }

    [Fact]
    public async Task Update_RenameToNameOfSoftDeletedType_Succeeds()
    {
        // Arrange — نوع نشط، وآخر محذوف باسم "C50" في المصنع نفسه
        _context.Factories.Add(TestDataSeeder.CreateFactory(128, "مصنع_تعديل_محرَّر"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(344, 128, "C25"),
            TestDataSeeder.CreateConcreteType(345, 128, "C50", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act — إعادة تسمية النوع النشط إلى اسم النوع المحذوف
        var result = await _service.UpdateAsync(344, new UpdateConcreteTypeDto
        {
            Name = "C50",
            Strength = 25,
            UnitPrice = 150m,
            IsActive = true
        }, currentFactoryId: 128);

        // Assert
        result.Should().BeTrue();

        var stored = await _context.ConcreteTypes
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 344);

        stored.Name.Should().Be("C50");
    }

    [Fact]
    public async Task Restore_ActiveSameNameWithDifferentCase_ThrowsConflict()
    {
        // Arrange — تعارض الاستعادة يُكتشف بغضّ النظر عن حالة الأحرف
        _context.Factories.Add(TestDataSeeder.CreateFactory(129, "مصنع_تعارض_حالة"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(346, 129, "C25", isActive: false, isDeleted: true),
            TestDataSeeder.CreateConcreteType(347, 129, "c25"));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.RestoreAsync(346, currentFactoryId: 129);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(string.Format(Messages.ConcreteTypeRestoreNameConflict, "C25"));

        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 346);

        stored.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Restore_AfterNameReusedByActiveType_ThrowsConflict()
    {
        // Arrange — السيناريو الكامل لخيار B: حذف ثم إعادة استخدام الاسم بنوع نشط جديد
        _context.Factories.Add(TestDataSeeder.CreateFactory(130, "مصنع_إعادة_استخدام"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(348, 130, "C60", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // إعادة استخدام الاسم بنوع نشط جديد — يجب أن ينجح
        var recreated = await _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 130,
            Name = "C60",
            Strength = 60,
            UnitPrice = 260m
        });

        // Act — استعادة النوع القديم بعد أن صار الاسم مستخدمًا
        var act = () => _service.RestoreAsync(348, currentFactoryId: 130);

        // Assert — 409 دون تعديل أي من السجلين
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(string.Format(Messages.ConcreteTypeRestoreNameConflict, "C60"));

        var stored = await _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.FactoryId == 130)
            .ToListAsync();

        stored.Single(x => x.ConcreteTypeId == 348).IsDeleted.Should().BeTrue();
        stored.Single(x => x.ConcreteTypeId == recreated.ConcreteTypeId).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Create_SameNameInAnotherFactory_Succeeds()
    {
        // Arrange — قيد التفرّد مركّب (FactoryId, Name) لا يمنع الاسم نفسه في مصنع آخر
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(123, "مصنع_هـ"),
            TestDataSeeder.CreateFactory(124, "مصنع_و"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(333, 123, "C25"));
        await _context.SaveChangesAsync();

        // Act
        var created = await _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 124,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        created.FactoryId.Should().Be(124);
        created.Name.Should().Be("C25");
    }

    [Fact]
    public async Task Create_NameOfSoftDeletedTypeInAnotherFactory_StillSucceeds()
    {
        // Arrange — نوع محذوف في مصنع آخر لا علاقة له بهذا المصنع أصلاً (عزل المصانع محفوظ)
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(131, "مصنع_ز_محذوف"),
            TestDataSeeder.CreateFactory(132, "مصنع_ح_نشط"));
        _context.ConcreteTypes.Add(
            TestDataSeeder.CreateConcreteType(349, 131, "C70", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var created = await _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 132,
            Name = "C70",
            Strength = 70,
            UnitPrice = 300m
        });

        // Assert
        created.FactoryId.Should().Be(132);
        created.Name.Should().Be("C70");
    }

    [Fact]
    public async Task Update_RenameToExistingName_ThrowsConflictAndKeepsOldName()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(125, "مصنع_تعديل"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(340, 125, "C25"),
            TestDataSeeder.CreateConcreteType(341, 125, "C30"));
        await _context.SaveChangesAsync();

        // Act — إعادة تسمية النوع 341 إلى اسم النوع 340
        var act = () => _service.UpdateAsync(341, new UpdateConcreteTypeDto
        {
            Name = "C25",
            Strength = 30,
            UnitPrice = 180m,
            IsActive = true
        }, currentFactoryId: 125);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.ConcreteTypeAlreadyExists);

        var stored = await _context.ConcreteTypes
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 341);

        stored.Name.Should().Be("C30");
    }

    [Fact]
    public async Task Update_KeepingSameName_Succeeds()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(126, "مصنع_نفس_الاسم"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(342, 126, "C25"));
        await _context.SaveChangesAsync();

        // Act — الإبقاء على الاسم نفسه ليس تكرارًا
        var result = await _service.UpdateAsync(342, new UpdateConcreteTypeDto
        {
            Name = "C25",
            Strength = 30,
            UnitPrice = 190m,
            IsActive = true
        }, currentFactoryId: 126);

        // Assert
        result.Should().BeTrue();

        var stored = await _context.ConcreteTypes
            .AsNoTracking()
            .FirstAsync(x => x.ConcreteTypeId == 342);

        stored.Strength.Should().Be(30);
    }

    /// <summary>
    /// حماية نموذج EF من إزالة فهرس التفرّد (FactoryId, Name) أو إسقاط ترشيحه —
    /// قاعدة البيانات هي المرجع النهائي ضد التكرار، والترشيح <c>WHERE IsDeleted = 0</c>
    /// هو ما يجعل الأسماء المحذوفة قابلة لإعادة الاستخدام (خيار B).
    /// ملاحظة: هذا يتحقق من تعريف النموذج فقط، وليس من إنفاذ SQL Server الفعلي.
    /// </summary>
    [Fact]
    public void Model_ConcreteTypeNameIndex_IsUniqueAndFilteredToNotDeleted()
    {
        var entityType = _context.Model.FindEntityType(typeof(ConcreteType));

        var index = entityType!.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { "FactoryId", "Name" }));

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("[IsDeleted] = 0");
    }

    /// <summary>
    /// الفهرس الفريد على أنواع الخرسانة ليس فهرسًا عامًا على IsDeleted — التأكد من أن
    /// ترشيح التفرّد لم يُعمَّم على فهارس أخرى بالخطأ.
    /// </summary>
    [Fact]
    public void Model_OnlyConcreteTypeNameIndex_IsFilteredToNotDeleted()
    {
        var filtered = _context.Model.GetEntityTypes()
            .SelectMany(e => e.GetIndexes())
            .Where(i => i.GetFilter() == "[IsDeleted] = 0")
            .Select(i => $"{i.DeclaringEntityType.ClrType.Name}.{string.Join("_", i.Properties.Select(p => p.Name))}")
            .ToList();

        filtered.Should().Equal("ConcreteType.FactoryId_Name");
    }

    public void Dispose() => _context.Dispose();
}