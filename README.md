# نظام خراسانة لإدارة مصانع الخرسانة الجاهزة

<p dir="rtl">
نظام ERP متكامل لإدارة مصانع الخرسانة الجاهزة ومتابعة الطلبات، المركبات، والعملاء. النظام مبني على مبادئ
<strong>Clean Architecture</strong> باستخدام إطار عمل <code>.NET 10</code>، ويأتي بواجهة ويب عربية <strong>RTL</strong> بالكامل
مصممة خصيصاً لتلبي احتياجات السوق المحلي (اليمن).
</p>

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10-512BD4?logo=entityfusion&logoColor=white)
![Architecture](https://img.shields.io/badge/Clean%20Architecture-Service%20Pattern-blue)
![JWT](https://img.shields.io/badge/Auth-JWT-orange)
![Tests](https://img.shields.io/badge/Tests-xUnit%20%2B%20FluentAssertions-blue)
![License](https://img.shields.io/badge/License-MIT-green)

</div>

---

## 📋 نظرة عامة

نظام **خراسانة** يوفر بيئة عمل موحدة ومتكاملة لإدارة مصانع الخرسانة الجاهزة، تغطي دورة العمل الكاملة:

- **إدارة المصانع:** التحكم في بيانات المصانع، إعدادات الشعار، والموظفين التابعين لكل مصنع.
- **إدارة الطلبات:** دورة حياة متكاملة لطلبات الخرسانة (الكمية، نوع الخرسانة، نوع السقف، الحاجة لمضخة، موقع الصب، وطريقة النقل).
- **إدارة الكيانات الأساسية:** الأنواع الخرسانية، العملاء، السائقين، والمركبات (سيارات النقل).
- **لوحات المعلومات (Dashboards):** إحصاءات وتقارير لحظية مخصصة للمشرف العام ولموظفي كل مصنع.

النظام مقسم إلى **واجهة API** (RESTful) مستقلة تماماً، تستخدمها **واجهة ويب (ASP.NET Core MVC)** كعميل (Client) عبر خدمة `ApiClient` المخصصة، مما يضمن فصلاً تاماً بين الطبقات وتسهيل دمج تطبيقات الهواتف المحمولة (مثل Flutter) مستقبلاً.

---

## 🎭 الأدوار والصلاحيات

يعتمد النظام على 4 أدوار أساسية (محددة في `UserRole`) لضمان عزل البيانات وحماية العمليات:

| الدور | الصلاحيات ووصف العمل |
|--------|-----------|
| **Admin (أدمن النظام)** | إدارة كاملة ومطلقة للنظام: المصانع، المستخدمين، جميع أنواع الخرسانة، العملاء، السائقين، الإحصاءات العامة، والتقارير. |
| **FactoryEmployee (موظف المصنع)** | إدارة خاصة بمصنعه فقط (عزل تام للبيانات بفضل فلاتر الـ API). يدير طلبات الخرسانة، أنواع الخرسانة الخاصة بمصنعه، وبيانات/شعار المصنع. |
| **Client (العميل)** | استعراض المصانع، تقديم طلبات الخرسانة، ومتابعة حالة الطلبات خطوة بخطوة. |
| **Driver (السائق)** | عرض الطلبات المسندة إليه، تحديث حالة التوصيل، وتأكيد التسليم للعميل. |

> **ملاحظة أمنية:** يتم فرض **عزل المصنع (Tenant Isolation)** بشكل صارم في طبقة الـ API والـ Services عبر `FactoryId`. لا يمكن لموظف مصنع الوصول أو التعديل على بيانات مصنع آخر إطلاقاً.

---

## ✨ المميزات التقنية والنظامية

- **التزامن وحل النزاعات (Concurrency Control):** تطبيق آلية `RowVersion` للكيانات لمنع تضارب التعديلات المتزامنة.
- **الحذف الناعم (Soft Delete):** أرشفة الكيانات (طلبات، مستخدمين، مصانع، أنواع خرسانة) عبر حقل `IsDeleted` بدون فقدان فعلي للبيانات من قاعدة البيانات، مع دعم استرجاعها.
- **نظام حماية متقدم:**
  - مصادقة **JWT Bearer** للـ API، ومصادقة **Session-based** مؤمنة لواجهة الويب.
  - تحديد معدل الطلبات (**Rate Limiting**) لمنع هجمات Brute-force (خاصة على شاشات تسجيل الدخول).
  - حماية ضد الاختراق عبر **SecurityHeadersMiddleware**.
  - نظام القفل التلقائي للحسابات (`LockoutEnd`) بعد تجاوز عدد معين من المحاولات الفاشلة (`FailedLoginAttempts`).
- **دعم مخصص للسوق المحلي:** واجهة عربية RTL بالكامل، مع فئات وأدوات التحقق من أرقام الهواتف اليمنية (مثل `+967...`).
- **سجلات تتبع شاملة (Logging):** تسجيل مفصل للأخطاء والطلبات يومياً في مجلد `Logs/` مع رقم تتبع فريد (`TraceId`) يربط بين خطأ واجهة الويب والخطأ الفعلي في الـ API.

---

## 🏗️ العمارة وهيكل الطبقات (Clean Architecture & Structure)

النظام مبني بدقة على مبادئ **Clean Architecture** مع الالتزام الصارم بـ **Dependency Inversion Principle**، حيث تتجه التبعيات دائماً للداخل نحو النواة (`Domain`):

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                        واجهات العرض والاستهلاك                          │
│   ┌───────────────────────────────┐   ┌─────────────────────────────┐   │
│   │   Kharasana.Web (MVC / UI)    │   │   Kharasana.API (RESTful)   │   │
│   └───────────────┬───────────────┘   └──────────────┬──────────────┘   │
└───────────────────┼──────────────────────────────────┼──────────────────┘
                    │                                  │
                    │  HTTP / JSON وقت التشغيل          │
                    └─────────────────────────────────▶│
                                                       │
                                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│             Kharasana.Infrastructure (البنية التحتية)                   │
│   EF Core DbContext, Repositories, JWT Token, PasswordHasher, Storage    │
└──────────────────────────────────────┬──────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│             Kharasana.Application (طبقة الأعمال والتطبيق)               │
│   Services, DTOs, FluentValidators, Interfaces (Repositories/Services)  │
└──────────────────────────────────────┬──────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                 Kharasana.Domain (نواة النظام والكيانات)                │
│   Entities, Value Objects, Enums, Domain Rules & Phone Normalization    │
└─────────────────────────────────────────────────────────────────────────┘
```

مراجع المشاريع الفعلية كما هي معرّفة في ملفات `.csproj`:

| المشروع | يشير إلى (`ProjectReference`) |
|---------|-------------------------------|
| `Kharasana.Web` | `Kharasana.Application`، `Kharasana.Domain` |
| `Kharasana.API` | `Kharasana.Application`، `Kharasana.Infrastructure` |
| `Kharasana.Infrastructure` | `Kharasana.Application`، `Kharasana.Domain` |
| `Kharasana.Application` | `Kharasana.Domain` |
| `Kharasana.Domain` | — (لا يشير إلى أي مشروع آخر) |
| `Kharasana.Tests` | `Kharasana.Application`، `Kharasana.Infrastructure`، `Kharasana.Domain` |

> **ملاحظة:** لا يشير `Kharasana.Web` إلى `Kharasana.Infrastructure` كمرجع مشروع إطلاقاً؛ فعلاقته بالـ API هي علاقة **وقت تشغيل** فقط (HTTP/JSON).

---

### 📂 تفصيل هيكل الطبقات ومسؤولياتها

#### 1. طبقة النواة: `Kharasana.Domain` (Core Domain)
الطبقة المركزية الأكثر نقاءً؛ لا تعتمد على أي مكتبات خارجية أو أي مشروع آخر داخل الـ Solution.
```text
Kharasana.Domain/
├── Common/                      # الفئات الأساسية المشتركة ومعالجة النصوص
│   ├── ArabicTextNormalization.cs # معالجة وتوحيد النصوص والهمزات العربية وتعبيرات البحث (LINQ)
│   └── YemeniPhoneHelper.cs     # توحيد ومعايرة أرقام الهواتف اليمنية بصيغة قياسية (+967)
├── Entities/                    # الكيانات الأساسية للنظام (Domain Models)
│   ├── Factory.cs               # كيان المصنع وبياناته وإعداداته
│   ├── User.cs                  # كيان المستخدمين بمختلف الأدوار
│   ├── Order.cs                 # كيان طلبات الخرسانة بكامل تفاصيلها
│   └── ConcreteType.cs          # كيان أنواع الخرسانة ومواصفاتها
├── Enums/                       # التعدادات الموحدة في النظام
│   ├── UserRole.cs              # أدوار المستخدمين (Admin, FactoryEmployee, Driver, Client)
│   ├── OrderStatus.cs           # دورة حياة وحالات الطلب
│   ├── OrderStatusHelper.cs     # الأسماء العربية للحالات وجدول الانتقالات المسموحة
│   ├── SlabType.cs              # أنواع الأسقف والعناصر الإنشائية
│   ├── TransportMethod.cs       # طرق النقل (سيارات مصنع / نقل ذاتي)
│   └── DriverStatus.cs          # حالات السائقين (Unset, Available, Busy, Offline)
└── Validation/                  # سمات التحقق الخاصة بالنطاق (Domain Validation Attributes)
    ├── YemeniEmailAttribute.cs  # سمة التحقق من صيغة البريد الإلكتروني الاختياري
    └── YemeniPhoneAttribute.cs  # سمة التحقق من صحة رقم الهاتف اليمني عبر YemeniPhoneHelper
```

---

#### 2. طبقة الأعمال: `Kharasana.Application` (Business Logic & Use Cases)
تحتوي على منطق العمل وحالات الاستخدام، وتعتمد حصراً على طبقة `Domain`.
```text
Kharasana.Application/
├── Common/                      # الأدوات المشتركة ونتائج العمليات
│   ├── ApiResponse.cs           # غلاف الاستجابة الموحد لجميع نقاط الـ API
│   ├── Messages.cs              # السجل المركزي لكافة نصوص ورسائل النظام العربية
│   ├── Roles.cs                 # أسماء الأدوار وتركيباتها كمصدر وحيد بدل تكرار السلاسل
│   ├── CustomClaimTypes.cs      # أنواع الـ Claims المخصصة (FactoryId)
│   ├── PhoneValidationHelper.cs # تطبيع أرقام الهواتف مع فحص التفرّد
│   ├── PagedResult.cs           # نموذج نتائج الصفحات والترقيم (Pagination Result)
│   ├── PaginationParams.cs      # معايير طلب الصفحات (PageNumber, PageSize)
│   ├── Exceptions/              # استثناءات الأعمال (BusinessException, NotFound, Conflict, ...)
│   └── Logging/                 # أدوات التسجيل والربط المشترك للسجلات
├── DTOs/                        # كائنات نقل البيانات مصنفة حسب المجال الوظيفي
│   ├── Auth/                    # نماذج تسجيل الدخول وإنشاء الحسابات
│   ├── Factory/                 # نماذج إنشاء وتعديل واستعراض المصانع
│   ├── Order/                   # نماذج إنشاء الطلبات وتحديث حالاتها وتفاصيلها
│   ├── ConcreteType/            # نماذج إدارة أصناف الخرسانة
│   ├── User/                    # نماذج إدارة وتعديل حسابات المستخدمين
│   ├── Dashboard/               # نماذج إحصائيات لوحات التحكم (Admin & Factory)
│   ├── Report/                  # نماذج التقارير المالية والإنتاجية وأداء السائقين
│   └── Customer/                # نماذج ملخصات العملاء (عدد الطلبات وإجمالي الكميات)
├── Interfaces/                  # العقود والواجهات البرمجية المنفصلة
│   ├── IUnitOfWork.cs           # واجهة وحدة العمل لضمان سلامة المعاملات
│   ├── Repositories/            # واجهات مستودعات البيانات (Generic + Specific)
│   │   ├── IGenericRepository.cs
│   │   ├── IFactoryRepository.cs
│   │   ├── IOrderRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── IConcreteTypeRepository.cs
│   └── Services/                # واجهات خدمات الأعمال والتطبيق
│       ├── IAuthService.cs
│       ├── IOrderService.cs
│       ├── IFactoryService.cs
│       ├── IConcreteTypeService.cs
│       ├── IUserService.cs
│       ├── IDashboardService.cs
│       ├── IReportService.cs
│       ├── ITokenService.cs
│       ├── IPasswordHasher.cs
│       └── IImageStorageService.cs
├── Services/                    # تنفيذ منطق الأعمال لخدمات التطبيق
│   ├── AuthService.cs           # مصادقة وإدارة الجلسات
│   ├── OrderService.cs          # دورة حياة الطلبات وعزل المصانع
│   ├── FactoryService.cs        # إدارة المصانع وتراخيصها
│   ├── ConcreteTypeService.cs   # معالجة أنواع الخرسانة
│   ├── UserService.cs           # إدارة المستخدمين والصلاحيات
│   ├── DashboardService.cs      # تجميع الإحصائيات والمؤشرات الحية
│   └── ReportService.cs         # استخراج التقارير التحليلية
├── Validators/                  # قواعد التحقق من المدخلات (FluentValidation)
│   ├── Auth/                    # التحقق من بيانات الدخول والتسجيل
│   ├── Order/                   # التحقق من دقة بيانات الطلب والكميات والمواقع
│   ├── Factory/                 # التحقق من بيانات المصنع
│   ├── ConcreteType/            # التحقق من معايير صنف الخرسانة
│   ├── User/                    # التحقق من كلمات المرور والملفات الشخصية
│   └── Common/                  # قواعد التحقق المشتركة (الهواتف، الترقيم)
└── DependencyInjection.cs       # تسجيل خدمات ومحققات التطبيق في حاوية الـ DI
```

---

#### 3. طبقة البنية التحتية: `Kharasana.Infrastructure` (Data & External Services)
مسؤولة عن الوصول لقاعدة البيانات والتعامل مع المكتبات الخارجية وتنفيذ الواجهات المعرفة في `Application`.
```text
Kharasana.Infrastructure/
├── Persistence/                 # سياق قاعدة البيانات وتكوين الاتصال
│   ├── KharasanaDbContext.cs    # سياق Entity Framework Core مع تكوين الفلاتر العامة
│   └── UnitOfWork.cs            # تنفيذ وحدة العمل وإدارة المعاملات (Transactions)
├── Configurations/              # تكوين خرائط الجداول والحقول والعلاقات (Fluent API)
│   ├── FactoryConfiguration.cs
│   ├── UserConfiguration.cs
│   ├── OrderConfiguration.cs
│   └── ConcreteTypeConfiguration.cs
├── Repositories/                # تنفيذ مستودعات البيانات والاستعلامات المخصصة
│   ├── GenericRepository.cs     # المستودع العام للعمليات الأساسية (CRUD)
│   ├── FactoryRepository.cs     # استعلامات المصانع والأرشفة
│   ├── OrderRepository.cs       # استعلامات الطلبات المعقدة والفلترة متعددة الأبعاد
│   ├── UserRepository.cs        # استعلامات المستخدمين والأدوار
│   └── ConcreteTypeRepository.cs # استعلامات أنواع الخرسانة
├── Authentication/              # آليات الأمان وتوليد المفاتيح
│   ├── TokenService.cs          # توليد والتحقق من JWT Bearer Tokens
│   ├── PasswordHasher.cs        # تشفير والتحقق من كلمات المرور عبر BCrypt/PBKDF2
│   └── JwtSettings.cs           # خيارات وإعدادات الـ JWT
├── Services/                    # تنفيذ الخدمات المادية
│   └── ImageStorageService.cs   # معالجة وتخزين صور وشعارات المصانع محلياً
├── Migrations/                  # ملفات ترحيل قاعدة البيانات التلقائية (EF Migrations)
└── DependencyInjection.cs       # تسجيل DbContext والمستودعات والخدمات في DI
```

---

#### 4. طبقة واجهة برمجة التطبيقات: `Kharasana.API` (RESTful Web API)
البوابة المركزية لمعالجة الطلبات القادمة من الويب أو تطبيقات الموبايل.
```text
Kharasana.API/
├── Controllers/                 # نقاط النهاية (Endpoints) المحمية بالـ JWT
│   ├── AuthController.cs        # تسجيل الدخول وإنشاء الحسابات (نقاط عامة بلا توكن)
│   ├── FactoriesController.cs   # إدارة المصانع وتحديث بياناتها وشعاراتها
│   ├── OrdersController.cs      # إدارة مسار الطلبات وإنشائها واعتمادها
│   ├── ConcreteTypesController.cs # إدارة وتصنيف أنواع الخرسانة
│   ├── UsersController.cs       # إدارة حسابات المستخدمين والموظفين والسائقين والعملاء
│   ├── DashboardController.cs   # إحصائيات لوحات التحكم الإدارية
│   ├── ReportsController.cs     # التقارير الإحصائية والمالية
│   └── SettingsController.cs    # إعدادات المصنع الخاصة بموظف المصنع
├── Middlewares/                 # البرمجيات الوسيطة الخاصة بالـ API
│   ├── ExceptionMiddleware.cs   # التقاط الاستثناءات وإعادتها بصيغة JSON موحدة
│   └── SecurityHeadersMiddleware.cs # حقن ترويسات الأمان (CSP, XSS, HSTS)
├── Common/                      # أدوات وفلاتر الـ API المساعدة
│   ├── CallerContext.cs         # سياق وهوية المستخدم المستخرجة من الـ JWT
│   └── ValidationFilter.cs      # فلتر عام للتحقق التلقائي من الـ Models عبر FluentValidation
├── Extensions/                  # ملحقات استخراج الهوية وتوليد الروابط المطلقة
│   ├── ClaimsPrincipalExtensions.cs # استخراج بيانات المستخدم (Tenant & Role) من الـ JWT
│   ├── CallerContextExtensions.cs   # تحويل ClaimsPrincipal إلى CallerContext
│   └── LogoUrlExtensions.cs     # تحويل مسار الشعار النسبي إلى رابط مطلق
└── Program.cs                   # نقطة تشغيل التطبيق، تكوين CORS، Swagger، Rate Limiting، والـ JWT
```

---

#### 5. طبقة واجهة المستخدم: `Kharasana.Web` (ASP.NET Core MVC Client)
واجهة المستخدم الإدارية المبنية بنمط MVC المتكامل مع الـ API عبر عميل HTTP مستقل تماماً.
```text
Kharasana.Web/
├── Controllers/                 # متحكمات الصفحات وتوجيه المستخدمين
│   ├── BaseController.cs        # المتحكم الأساسي المشترك (إدارة التنبيهات والـ Claims)
│   ├── AccountController.cs     # تسجيل الدخول والخروج وإدارة الجلسات
│   ├── DashboardController.cs   # لوحات التحكم التفاعلية (أدمن ومصانع)
│   ├── OrdersController.cs      # إدارة وجداول وعرض تفاصيل الطلبات
│   ├── OrderWorkflowController.cs # معالجة مسار اعتماد وتسليم الطلبات
│   ├── FactoryController.cs     # شاشات إدارة ومتابعة المصانع
│   ├── ConcreteTypesController.cs # شاشات إدارة أنواع الخرسانة
│   ├── UsersController.cs       # شاشات المستخدمين والموظفين
│   ├── ClientsController.cs     # شاشات إدارة العملاء
│   ├── DriversController.cs     # شاشات السائقين والتقارير الميدانية
│   ├── ReportsController.cs     # شاشات الرسوم البيانية والتقارير
│   ├── SettingsController.cs    # إعدادات الحساب والمصنع
│   ├── FilesController.cs       # خادم وسيط لعرض وتنزيل الصور والملفات
│   └── HomeController.cs        # الصفحة الترحيبية وتوجيه الجلسات
├── Services/                    # خدمات الاتصال بالـ API واسترجاع البيانات
│   ├── Api/                     # تنفيذ استدعاءات الـ REST API عبر HttpClient
│   │   ├── ApiClient.cs         # العميل العام مع معالجة الأخطاء وتمرير التوكن
│   │   ├── ApiServiceException.cs # استثناءات استدعاءات الـ API
│   │   ├── AuthApiService.cs
│   │   ├── OrdersApiService.cs
│   │   ├── FactoryApiService.cs
│   │   ├── DashboardApiService.cs
│   │   ├── ConcreteTypeApiService.cs
│   │   ├── UserApiService.cs
│   │   ├── ClientApiService.cs
│   │   ├── DriverApiService.cs
│   │   ├── ReportsApiService.cs
│   │   ├── SettingsApiService.cs
│   │   └── LookupApiService.cs
│   ├── ConcreteCatalogService.cs # كتالوج أنواع الخرسانة القياسية (قائمة ثابتة محلية)
│   └── Interfaces/              # عقود خدمات الويب (I*ApiService, IConcreteCatalogService)
├── ViewModels/                  # نماذج البيانات المهيأة للعرض في الشاشات
│   ├── Dashboard/               # نماذج اللوحات والـ KPIs
│   ├── Orders/                  # نماذج شاشات الطلبات
│   ├── Factories/               # نماذج شاشات المصانع
│   ├── ConcreteTypes/           # نماذج أنواع الخرسانة
│   ├── Users/                   # نماذج المستخدمين
│   ├── Drivers/                 # نماذج السائقين
│   ├── Reports/                 # نماذج التقارير والرسوم البيانية
│   └── Auth/                    # نماذج شاشات الدخول
├── Models/                      # نماذج المكونات المشتركة
│   ├── Components/              # نماذج المكونات الجزئية (SummaryCard, StatusBadge, ...)
│   │   └── Search/              # نماذج مكوّن البحث (SearchBox)
│   └── ConcreteCatalog/         # نماذج الكتالوج القياسي (ConcreteStandard)
├── Views/                       # شاشات Razor Views المنظمة حسب المتحكم
│   ├── Shared/                  # القالب الأساسي (_Layout, _Navbar, _Sidebar) والمكونات المشتركة
│   ├── Dashboard/               # شاشات لوحات المعلومات (_AdminDashboard, _FactoryDashboard)
│   ├── Orders/                  # شاشات إنشاء وتفاصيل وقوائم الطلبات
│   ├── Factories/               # شاشات إدارة وتفاصيل وأرشيف المصانع
│   ├── ConcreteTypes/           # شاشات إدارة أنواع الخرسانة
│   ├── Users/                   # شاشات إدارة وتعديل المستخدمين
│   ├── Drivers/                 # شاشات السائقين
│   ├── Reports/                 # شاشات التقارير العامة
│   └── Account/                 # شاشات تسجيل الدخول
├── Filters/                     # فلاتر الأمان ومعالجة أخطاء الواجهة
│   ├── SessionAuthorizeAttribute.cs # التحقق من جلسة المستخدم وصلاحياته
│   └── UnhandledExceptionFilter.cs # التعامل مع الاستثناءات وتوجيه لصفحات الأخطاء
├── Middlewares/                 # البرمجيات الوسيطة لواجهة الويب
│   ├── RequestLoggingMiddleware.cs # تسجيل زمن تنفيذ وحالة الطلبات مع الـ TraceId
│   └── SecurityHeadersMiddleware.cs # ترويسات حماية متصفح العميل
├── Helpers/                     # أدوات مساعدة للعرض وتوليد الوسوم (TagHelpers & Formatters)
└── wwwroot/                     # الملفات الثابتة (CSS مخصص RTL، مكتبات JS، الخطوط والصور)
```

---

#### 6. طبقة الاختبارات: `Kharasana.Tests` (Automated Unit Tests)
تضمن سلامة منطق الأعمال وقواعد النطاق وعزل التعديلات.
```text
Kharasana.Tests/
├── GlobalUsings.cs              # الاستيرادات العامة لمشروع الاختبارات
├── TestData/                    # بذور البيانات التجريبية والمولّدات
│   └── TestDataSeeder.cs        # بيانات اختبارية متكاملة لجميع السيناريوهات
└── Tests/                       # فئات الاختبارات المنطقية (xUnit & FluentAssertions)
    ├── AuthServiceTests.cs      # اختبارات المصادقة وتوليد التوكن
    ├── OrderServiceTests.cs     # اختبارات قواعد الطلبات وصلاحيات المصانع
    ├── FactoryServiceTests.cs   # اختبارات إدارة المصانع والأرشفة
    ├── ConcreteTypeServiceTests.cs # اختبارات أنواع الخرسانة
    ├── UserServiceTests.cs      # اختبارات إدارة المستخدمين وتغيير كلمات المرور
    ├── DashboardServiceTests.cs # اختبارات حساب مؤشرات الأداء والـ KPIs
    ├── ReportServiceTests.cs    # اختبارات دقة التقارير المالية والإنتاجية
    ├── PaginationParamsTests.cs # اختبارات حدود الترقيم وحجم الصفحات
    └── UniqueConstraintDetectorTests.cs # اختبارات كشف انتهاك قيود التفرّد (أرقام أخطاء SQL Server)
```

---

## 🔄 دورة حياة الطلب والتفاعل بين الطبقات (Request Flow)

يعتمد النظام على فصل كامل بين واجهة العرض (`Web`) والواجهة البرمجية (`API`). لفهم كيفية تفاعل الطبقات معاً، إليك مسار رحلة استعلام نموذجي (مثال: طلب موظف مصنع لعرض قائمة الطلبات):

1. **العميل (Client Browser):**
   - يطلب المستخدم صفحة عرض الطلبات من متصفحه.
   - يتلقى `OrdersController` في مشروع `Kharasana.Web` الطلب (يتحقق من جلسته عبر `[SessionAuthorize]`).

2. **خدمات الويب (Web Services & ApiClient):**
   - يقوم المتحكم بمناداة خدمة `IOrdersApiService`.
   - يقوم الـ `ApiClient` بتضمين الـ `JWT Token` الخاص بالمستخدم، ويرسل طلب `HTTP GET` إلى `Kharasana.API`.

3. **واجهة برمجة التطبيقات (Kharasana.API):**
   - يستقبل `OrdersController` الطلب، ويتحقق من الـ Token والصلاحيات (`[Authorize]`).
   - يقوم باستخراج هوية المستخدم وصلاحياته (`User.GetCallerContext()`).
   - يمرر الطلب إلى طبقة الأعمال عبر واجهة `IOrderService`.

4. **طبقة الأعمال (Kharasana.Application):**
   - تستقبل الخدمة الطلب، وتقوم بتطبيق قواعد العمل (Business Logic)، مثلاً: التأكد من أن المصنع نشط.
   - تنادي مستودع البيانات عبر `IUnitOfWork` (مثال: `_unitOfWork.Orders.GetPagedAsync`).

5. **البنية التحتية (Kharasana.Infrastructure):**
   - يقوم `OrderRepository` باستخدام `KharasanaDbContext` (Entity Framework Core).
   - يتم تحويل أمر LINQ إلى استعلام `SQL` ويتم تنفيذه على SQL Server، وتعود البيانات ككيانات (`Entities`).

6. **العودة وتنسيق البيانات:**
   - تقوم الخدمة (Application) بتحويل الكيانات إلى كائنات نقل بيانات (`DTOs`) لضمان عدم تسريب بنية قاعدة البيانات.
   - يقوم الـ API بتغليف الـ DTO بداخل كائن `ApiResponse` موحد ويعيده كـ `JSON`.
   - يقوم الـ `ApiClient` (في الويب) بفك تشفير الـ JSON وتمريره للـ View ليتم رسم الصفحة للمستخدم النهائي بأمان!

---

## 🗃️ الكيانات الأساسية والتعدادات

### الكيانات (Entities)
- `Factory`: بيانات المصانع وشعاراتها.
- `User`: بيانات المستخدمين (تشمل الأدمن، الموظفين، السائقين، والعملاء).
- `Order`: طلبات الخرسانة بتفاصيلها الكاملة.
- `ConcreteType`: أنواع الخرسانة المتاحة لكل مصنع.

### التعدادات (Enums) المهمة
- `OrderStatus`: (جديد، قيد الانتظار، معتمد، مرفوض، ملغي، في الطريق، تم التسليم، مغلق).
- `TransportMethod`: (نقل عبر سيارات المصنع، نقل ذاتي للعميل).
- `SlabType`: (أساسات، أعمدة، كمرات، سقف، أخرى).
- `UserRole`: (None, Admin, FactoryEmployee, Driver, Client).

---

## 🚀 البدء السريع (Quick Start)

### المتطلبات الأساسية
- حزمة تطوير [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- خادم قواعد بيانات **SQL Server** (محلي أو عبر LocalDB).
- بيئة تطوير متكاملة (Visual Studio 2022، JetBrains Rider، أو VS Code).

> ملف الحل هو `Kharasana.slnx` (لا يوجد ملف `.sln` في المستودع). لبناء كل المشاريع من جذر المستودع: `dotnet build Kharasana.slnx`.

### 1) إعداد وتشغيل واجهة الـ API
افتح موجه الأوامر (Terminal) ونفذ التالي:
```bash
cd Kharasana.API

# إعداد سلسلة الاتصال بقاعدة البيانات محلياً
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Kharasana;Trusted_Connection=True;TrustServerCertificate=True"

# إعداد المفتاح السري للـ JWT (يجب أن يكون 32 حرفاً على الأقل)
dotnet user-secrets set "Jwt:Key" "Your_Super_Secret_Key_For_JWT_Validation_123!"

dotnet run
```
تعمل الخدمة على `http://localhost:5000` (ومتوفرة واجهة Swagger التفاعلية في بيئة التطوير)، مع نقطة فحص الصحة على `/health`.

### 2) إعداد قاعدة البيانات (Migrations)
> **مهم:** يجب إتمام خطوة `user-secrets` أعلاه **قبل** تنفيذ أي أمر `dotnet ef`، لأن أدوات EF تستدعي `Program.cs` — الذي يرفض التشغيل إذا كان `Jwt:Key` مفقوداً أو أقصر من 32 حرفاً.

يجب تطبيق عمليات الترحيل لتحديث قاعدة البيانات وإنشاء الجداول المطلوبة (بما في ذلك حقول الـ `RowVersion` والحذف الناعم):
```bash
cd Kharasana.Infrastructure
dotnet ef database update --startup-project ../Kharasana.API
```

### 3) إعداد وتشغيل واجهة الويب (MVC)
في نافذة طرفية أخرى:
```bash
cd Kharasana.Web
dotnet run
```
ستعمل الواجهة على `http://localhost:5283` افتراضياً، وتتواصل مع الـ API من خلال العنوان المعرف في `ApiSettings:BaseUrl` داخل ملف `appsettings.json`.

---

## ⚙️ الإعدادات (Configuration)

يتم إدارة الإعدادات عبر عدة مستويات لضمان الأمان والمرونة:

| الملف / الآلية | الوظيفة |
|----------------|------------|
| `appsettings.json` | الإعدادات العامة (عناوين الـ API، سياسات الـ CORS، إعدادات JWT غير الحساسة). |
| `user-secrets` | مخصصة لبيئة **التطوير فقط**: تستخدم لحفظ سلسلة الاتصال (Connection String) ومفتاح JWT السري. لا يتم رفع هذا الملف إطلاقاً للـ Git. |
| `appsettings.Production.json` | إعدادات بيئة الإنتاج والنشر الفعلي (Production). |

---

## 🧪 الاختبارات (Testing)

يحتوي المشروع على بيئة اختبارات شاملة تغطي طبقات التطبيق:
```bash
dotnet test Kharasana.slnx
```
- يعتمد المشروع على **xUnit** و **FluentAssertions**.
- حالياً يوجد **128 اختباراً ناجحاً** تغطي كافة العمليات المنطقية الأساسية (التحقق من صحة الطلبات، قفل الحسابات، آليات الحذف الناعم والتحديثات).
- هذه اختبارات **InMemory** لمنطق الخدمات فقط، ولا تُثبت سلوك SQL Server الفعلي (الفهارس الفريدة المُرشَّحة، المفاتيح الأجنبية، `RowVersion`) — انظر `SQL_SERVER_INTEGRATION_TESTING.md`.
- يوجد تكامل مستمر (CI) في `.github/workflows/ci.yml` يعمل على `master`: يستعيد ويبني ثم ينفذ `dotnet test Kharasana.slnx` في وضع Release. الـ CI يشغّل نفس اختبارات InMemory هذه، ولا يشغّل أي اختبار تكامل على SQL Server حقيقي.

---

## 🔌 واجهة الـ API (API Endpoints)

تم توثيق نقاط النهاية باللغة العربية عبر **Swagger** (متاح في بيئة التطوير فقط). أهم النطاقات المتاحة:

- **`Auth`**: تسجيل حساب جديد (دور Client) وتسجيل الدخول وإصدار توكن JWT.
- **`Factories`**: إدارة المصانع وتفعيلها/إيقافها وتعديل الشعار.
- **`Orders`**: إنشاء طلبات الخرسانة، ومسار العمل (Workflow) الخاص بها.
- **`ConcreteTypes`**: إدارة الأصناف للخرسانة الجاهزة.
- **`Users`**: إدارة شاملة للحسابات — بما فيها العملاء والسائقون، إذ لا توجد متحكمات منفصلة باسم `Clients` أو `Drivers` في الـ API.
- **`Dashboard / Reports`**: نقاط مخصصة لسحب الإحصائيات وعرض التقارير اللحظية.
- **`Settings`**: إعدادات المصنع الخاصة بموظف المصنع.

*(جميع النقاط محمية بنظام JWT وتتطلب توكن يحمل الصلاحيات المناسبة، باستثناء نقطتَي التسجيل وتسجيل الدخول في `Auth` فهما عامّتان. تسجيل الخروج إجراء خاص بجلسة واجهة الويب وليس نقطة نهاية في الـ API).*

---

## 📦 النشر

- ملف نشر جاهز للاستضافة عبر **SiteAsp / runasp.net**:
  `Kharasana.API/Properties/PublishProfiles/site89235-WebDeploy.pubxml`
- التكوين الحساس (سلسلة الاتصال + مفتاح JWT) يُحقن وقت النشر الفعلي لتأمين الحسابات.

---

## 🗂️ السجلات ومعالجة الأخطاء (Logging & Error Handling)

- **رسائل موحدة:** يوجد نظام مركزي لرسائل النظام في `Kharasana.Application.Common.Messages.cs` يحتوي على أكثر من 120 رسالة (نجاح، فشل، أخطاء تحقق) لضمان توحيد النصوص عبر النظام.
- **سجل الأخطاء اليومي:** يتم حفظ الأخطاء تلقائياً في مجلد `Logs/log-yyyy-MM-dd.log`.
- **معالجة الاستثناءات:**
  - في الـ API: يقوم الـ `ExceptionMiddleware` بالتقاط الأخطاء وتحويلها لرموز حالة HTTP مناسبة (مثل 400 للأخطاء التجارية، 404، 500 وغيرها).
  - في الويب: يقوم `UnhandledExceptionFilter` و `RequestLoggingMiddleware` بالتعامل مع الأخطاء ورسم صفحات الـ Error بشكل أنيق للمستخدم النهائي مع إمكانية عرض الـ `TraceId` لتسهيل الصيانة.

---

<div dir="rtl" align="center">

**تم التطوير بواسطة [Mohamed Alnoor](https://github.com/noormohamed7763-a11y)** — مشروع نظام إدارة مصانع الخرسانة الجاهزة

</div>