# متطلبات اختبارات التكامل على SQL Server

> **الحالة الحالية: غير مُنفَّذة.** هذا المستند يوثّق الإعداد المطلوب إضافته لاحقًا،
> والسلوكيات التي **لا** يمكن إثباتها باختبارات InMemory الحالية.
> توجد بنية CI عامة في `.github/workflows/ci.yml` (build ثم `dotnet test Kharasana.slnx`)،
> لكنها **لا** تشغّل أي اختبار تكامل على SQL Server حقيقي: لا Docker، ولا Testcontainers،
> ولا LocalDB، ولا مشروع `Kharasana.IntegrationTests` — بقرار من مالك المشروع.

---

## 1. لماذا InMemory لا يكفي

مزوّد `Microsoft.EntityFrameworkCore.InMemory` **لا يُنفّذ** أي من:

- الفهارس الفريدة (Unique Indexes) وقيود `UNIQUE`.
- قيود المفاتيح الأجنبية (`FK`) و`ON DELETE RESTRICT`.
- سلوك `RowVersion` / `ConcurrencyToken` الحقيقي (التزامن المتفائل).
- الدلالات الحقيقية للمعاملات (Transactions) والتراجع عند الفشل.
- حساسية المقارنات لحالة الأحرف حسب ترتيب (Collation) الخادم.

لذلك أي اختبار InMemory يبدو أنه «يثبت» أحد هذه السلوكيات هو اختبار **مضلّل**،
ولا يجوز اعتباره دليلًا. اختبارات `Kharasana.Tests` الحالية تتحقق من **منطق الخدمة**
وتدفّق الاستثناءات فقط، وليس من إنفاذ قاعدة البيانات.

---

## 2. السلوكيات الواجب تغطيتها لاحقًا على SQL Server حقيقي

| # | السلوك | لماذا يحتاج SQL Server |
|---|--------|------------------------|
| 1 | إنفاذ الفهرس الفريد **المُرشَّح** `(FactoryId, Name) WHERE IsDeleted = 0` على `ConcreteTypes` و`FactoryName` على `Factories` | InMemory لا يفرض الفهارس ولا يحاكي ترشيحها |
| 2 | تحويل انتهاك قيد التفرّد إلى **409 Conflict** برسالة واضحة | يحتاج `SqlException` برقم خطأ 2601/2627 فعليًا |
| 3 | الحذف الناعم لنوع خرسانة له طلبات قائمة — سلامة مرجع `ConcreteTypeId` | يحتاج قيد `FK` + `ON DELETE RESTRICT` حقيقيًا |
| 4 | إعادة استخدام اسم نوع محذوف ناعمًا لإنشاء نوع نشط جديد في المصنع نفسه | يحتاج فهرسًا فريدًا مُرشَّحًا فعليًا: صفّان بالاسم نفسه أحدهما `IsDeleted = 1` يجب أن يُقبلا |
| 5 | تعارض الاستعادة عند وجود نوع نشط بالاسم نفسه → 409 دون تعديل أي سجل | يحتاج معاملة حقيقية + الفهرس الفريد المُرشَّح |
| 6 | سلوك `RowVersion` الحقيقي عند تعديل متزامن لنوع خرسانة | `[Timestamp]` لا يُحدَّث في InMemory |
| 7 | بقاء طلبات نوع خرسانة محذوف ناعمًا داخل تقرير التجميع (`GetCountByConcreteTypeAsync`) | يحتاج الانضمام الفعلي على `ConcreteTypes` مع تجاهل الفلتر: InMemory يحاكي السقوط جزئيًا فقط (يُسقط العنصر ويُبقي `TotalCount`)، بينما SQL Server يُسقط الصف من `COUNT` و`JOIN` معًا |

### حالات إضافية موصى بها

- سباق حقيقي: طلبان ينشئان النوع نفسه بالتوازي → أحدهما 201 والآخر **409** (وليس 500).
- سباق حقيقي: إنشاء نوع باسم نوع محذوف ناعمًا بالتوازي مع استعادته → يجب ألا ينجح الاثنان
  (الفهرس المُرشَّح يسمح بواحد فقط من الصفين غير المحذوفين).
- فشل مفتاح أجنبي حقيقي (حذف مصنع له أنواع) → يبقى **500/خطأ قاعدة بيانات**،
  ويجب **ألا** يُترجم إلى 409.
- بقاء القيمة الافتراضية لحساسية حالة الأحرف: إنشاء `c25` ثم `C25` في المصنع نفسه → 409.
- ترحيل `FilterConcreteTypeNameIndexOnNotDeleted` على قاعدة تحتوي بيانات قائمة:
  يُسقط الفهرس القديم ويُنشئ المُرشَّح بنفس الاسم، بلا فهرس مكرر ولا فشل
  "index already exists" (يُتحقق منه بـ `sys.indexes`).

---

## 3. الإعداد المطلوب إضافته (عند الحاجة)

### 3.1 مشروع اختبار منفصل

يُفضَّل مشروع مستقل `Kharasana.IntegrationTests` (لا يُدمج في تشغيل `dotnet test` اليومي)
حتى لا تفشل دورة التطوير المحلية عند غياب SQL Server.

### 3.2 مصدر الاتصال

عبر متغيّر بيئة، **ولا يُخزَّن أي سر في المستودع** (كما في `appsettings.Production*.json`
المستثنى في `.gitignore`):

```csharp
// KHARASANA_TEST_SQLSERVER مثال:
// Server=.;Database=Kharasana_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True
var connectionString = Environment.GetEnvironmentVariable("KHARASANA_TEST_SQLSERVER");
```

إن كان المتغيّر غير موجود → تُتخطّى اختبارات التكامل (`Skip`) بدل الفشل.

### 3.3 عزل قاعدة البيانات لكل تشغيل

قاعدة بيانات باسم فريد لكل تشغيل ثم حذفها في النهاية:

```csharp
var databaseName = $"Kharasana_IntegrationTests_{Guid.NewGuid():N}";
var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName };
// إنشاء المخطط: context.Database.Migrate() (نفس ترحيلات المشروع — لا EnsureCreated)
// التنظيف: context.Database.EnsureDeleted()
```

### 3.4 Fixture مشترك

`ICollectionFixture<KharasanaSqlServerFixture>` يرث `IAsyncLifetime`، ويُنشئ
`KharasanaDbContext` على SQL Server بعد `Database.Migrate()`، مزوّدًا بدالة مساعدة
لتفريغ الجداول بين الاختبارات (`DELETE` مرتّب حسب المفاتيح الأجنبية).

### 3.5 وسم اختبارات التكامل

```csharp
[Trait("Category", "Integration")]
```

حتى يمكن تشغيلها وحدها: `dotnet test --filter Category=Integration`.

---

## 4. ملاحظات مرتبطة بالتنفيذ الحالي

- فهرس `IX_ConcreteTypes_FactoryId_Name` أصبح **مُرشَّحًا** بـ `WHERE IsDeleted = 0`
  (ترحيل `FilterConcreteTypeNameIndexOnNotDeleted`)، فالتفرّد يُفرض على الأنواع
  غير المحذوفة فقط، وهذا **مقصود**: الاسم المحرَّر بحذف ناعم يجوز إعادة استخدامه
  لإنشاء نوع نشط جديد في المصنع نفسه.
- نتيجة لذلك، فحص التكرار على مستوى التطبيق (`FindActiveByNameInFactoryAsync`) يتجاهل
  الصفوف المحذوفة عمدًا، بينما الاستعادة تقرأ الصف المحذوف وتفشل بـ **409** إن وُجد
  نوع نشط بالاسم نفسه. هذا يعكس قرارًا سابقًا كان يحجز الاسم على النوع المحذوف.
- `UniqueConstraintDetector` يترجم رقمي الخطأ 2601 و2627 فقط؛ أي خطأ آخر
  (ومنها انتهاك المفتاح الأجنبي 547) يمرّ كما هو ولا يُعتبر تكرارًا.
- `Kharasana.Tests/Tests/UniqueConstraintDetectorTests.cs` يتحقق من ضيق هذا الكشف
  على مستوى أرقام الأخطاء فقط، لأن بناء `SqlException` حقيقي يحتاج خادمًا فعليًا.
- تقرير `GetCountByConcreteTypeAsync` يتجاوز فلتر الحذف على `ConcreteTypes` وحده
  (عبر `IgnoreQueryFilters` على مجموعة الانضمام) حتى لا تسقط طلبات نوع محذوف.
  فلتر الطلبات `!IsDeleted` باقٍ كما هو، وسائر التقارير والاستعلامات لم تُمسّ.
- **مشكلة قائمة لم تُصلَح في هذه الجولة:** `OrderRepository.OrdersWithDetails()`
  يستخدم `Include(o => o.ConcreteType)`، وEF يترجمه إلى
  `INNER JOIN (... WHERE [IsDeleted] = 0)`، أي أن الطلبات التابعة لنوع محذوف ناعمًا
  **تسقط من قائمة الطلبات وتفاصيل الطلب** (لا من التقرير فقط). الإصلاح يتطلب تعطيل
  الفلاتر لكل التنقلات (`IgnoreQueryFilters` على الجذر) وهو ما يغيّر سلوك العملاء
  والمصانع المحذوفين أيضًا، فتُرك خارج نطاق هذه الجولة عمدًا.
  يمكن التحقق من نصّ الاستعلام بـ `ToQueryString()` على مزوّد SQL Server دون خادم فعلي.
