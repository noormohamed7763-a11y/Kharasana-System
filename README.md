# نظام خراسانة لإدارة مصانع الخرسانة الجاهزة

نظام ERP متكامل لإدارة مصانع الخرسانة الجاهزة، مصمم لتغطية دورة العمل الكاملة من إدارة الطلبات، المخزون، الإنتاج، الموردين، العملاء، المستخدمين، والتقارير. يشمل المشروع بنية Clean Architecture مع واجهة ويب MVC + واجهة API RESTful، ويعمل على .NET 10 مع SQL Server.

<p dir="rtl">
  <strong>نظام خراسانة</strong> هو حل متكامل لإدارة مصانع الخرسانة الجاهزة ومتابعة الطلبات، المركبات، والعملاء، مع دعم أدوار المستخدمين، العزل حسب المصنع (Tenant Isolation)، وأدوات أمان متقدمة مثل JWT، Rate Limiting، Security Headers، ومؤشرات الأداء.
</p>

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10-512BD4?logo=entityframework&logoColor=white)
![Architecture](https://img.shields.io/badge/Clean%20Architecture-Service%20Pattern-blue)
![JWT](https://img.shields.io/badge/Auth-JWT-orange)
![Tests](https://img.shields.io/badge/Tests-xUnit%20%2B%20FluentAssertions%20%2B%20Moq%20%2B%20SQLServer-blue)
![License](https://img.shields.io/badge/License-MIT-green)

</div>

---

## 📋 نظرة عامة

نظام **خراسانة** يوفر بيئة عمل موحدة ومتكاملة لإدارة مصانع الخرسانة الجاهزة، تغطي دورة العمل الكاملة:

- **إدارة المصانع:** التحكم في بيانات المصانع، إعدادات الشعار، والموظفين التابعين لكل مصنع.
- **إدارة الطلبات:** دورة حياة متكاملة لطلبات الخرسانة (الكمية، نوع الخرسانة، نوع السقف، الحاجة لمضخة، موقع الصب، وطريقة النقل).
- **إدارة المخزون:** تتبع المواد الخام والمخزون الحالي لكل مصنع.
- **إدارة الإنتاج:** متابعة دفعات الإنتاج المرتبطة بالطلبات والحالة التشغيلية.
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
  - تحديد معدل الطلبات (**Rate Limiting**) لمنع هجمات Brute-force.
  - حماية ضد الاختراق عبر **SecurityHeadersMiddleware**.
  - نظام القفل التلقائي للحسابات بعد تجاوز عدد معين من المحاولات الفاشلة.
- **دعم مخصص للسوق المحلي:** واجهة عربية RTL بالكامل، مع فئات وأدوات التحقق من أرقام الهواتف اليمنية.
- **سجلات تتبع شاملة (Logging):** تسجيل مفصل للأخطاء والطلبات يومياً مع رقم تتبع فريد (`TraceId`) للربط بين الواجهات.

---

## 💎 Engineering Excellence (لماذا هذا النظام فريد؟)

نظام **خراسانة** ليس مجرد تطبيق إدارة، بل هو حل هندسي متكامل تم تصميمه لضمان الأداء العالي، الأمان الصارم، وقابلية التوسع:

*   **⚡ أداء عالي (N+1 Optimization):** بفضل استخدام **Projection (Select)** في جلب البيانات، يتفادى النظام مشكلة N+1 الشهيرة، مما يضمن سرعة استجابة فائقة عند التعامل مع سجلات الطلبات الضخمة.
*   **🛡️ عزل بيانات صارم (True Tenant Isolation):** يتم فرض عزل البيانات بين المصانع على مستوى قاعدة البيانات باستخدام **Global Query Filters**، مما يضمن حماية تامة لخصوصية بيانات كل مصنع.
*   **🔍 بحث عربي ذكي (Semantic Arabic Search):** يتضمن النظام محرك بحث مخصص يتعامل مع تعقيدات اللغة العربية (مثل تطبيع الألف والتاء المربوطة) مباشرة في استعلامات SQL، مما يضمن نتائج دقيقة وسريعة.
*   **🏗️ صمود هيكلي (Error Resilience):** بفضل هيكلية معالجة أخطاء مركزية (Global Exception Handling)، يوفر النظام استجابات مهنية وموحدة للمستخدم النهائي دون تعريض تفاصيل النظام الداخلية أو التسبب في انهيارات (500 Errors).
*   **⚙️ استقرار الإنتاج (DevOps Ready):** تصميم النظام يأخذ بعين الاعتبار بيئات التشغيل الحقيقية، بما في ذلك إمكانية إضافة فهارس (Indexes) يدوية دون أن يتم حذفها بواسطة الـ Migrations، مما يجعله صديقاً لفِرق العمليات.
*   **🚀 سرعة التنفيذ (Zero-Runtime Overhead Mapping):** بفضل استخدام مكتبة `Mapperly` الحديثة، يتم تنفيذ جميع عمليات تحويل البيانات (Mapping) في مرحلة البناء، مما يوفر أداءً استثنائياً مقارنة بالمكتبات التقليدية.
*   **✅ جودة بيانات مضمونة (Fluent Validation):** فصل كامل ومنظم لمنطق التحقق عن منطق الأعمال باستخدام `FluentValidation` لضمان أن كل مدخلات النظام صحيحة وموثوقة قبل معالجتها.
*   **🧪 استراتيجية اختبارات احترافية (Hardened Testing):** يعتمد النظام على بيئة اختبار تكامل (Integration Testing) متكاملة، تستخدم قاعدة بيانات حقيقية (SQL Server) مع آليات عزل ديناميكية لضمان موثوقية النظام واستقراره عند كل تحديث.

---

النظام مبني بدقة على مبادئ **Clean Architecture** مع الالتزام الصارم بـ **Dependency Inversion Principle**.

```text
Kharasana/
├── Kharasana.API/           # واجهة API RESTful
├── Kharasana.Web/           # واجهة الويب (MVC Client)
├── Kharasana.Application/   # طبقة الأعمال (Use Cases & DTOs)
├── Kharasana.Infrastructure/# تنفيذ البنية التحتية (EF Core, SQL Server)
├── Kharasana.Domain/        # نواة النظام (Entities, Common)
├── Kharasana.Tests/         # اختبارات الوحدة (Unit Tests)
├── Kharasana.IntegrationTests/ # اختبارات التكامل (SQL Server)
└── Kharasana.ContractTests/ # اختبارات العقود (API Contracts)
```

## 🧪 الاختبارات والجودة

يعتمد المشروع على استراتيجية اختبار متكاملة لضمان استقرار النظام:
- **اختبارات الوحدة (Unit Tests):** توجد في `Kharasana.Tests` (تستخدم xUnit, FluentAssertions, Moq).
- **اختبارات التكامل (Integration Tests):** توجد في `Kharasana.IntegrationTests` (تستخدم SQL Server حقيقي). لضبط بيئة الاختبارات، يرجى مراجعة [SQL_SERVER_INTEGRATION_TESTING.md](SQL_SERVER_INTEGRATION_TESTING.md).
- **اختبارات العقود (Contract Tests):** توجد في `Kharasana.ContractTests` لضمان صحة العقود بين الطبقات.

---

## 🚀 البدء السريع (Quick Start)

### المتطلبات الأساسية
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server
- Visual Studio 2022 / Rider / VS Code

> ملف الحل هو `Kharasana.slnx`. لبناء المشروع: `dotnet build Kharasana.slnx`.

### 1) إعداد وتشغيل واجهة الـ API
```bash
cd Kharasana.API
# إعداد سلسلة الاتصال والمفتاح السري (يجب أن يكون 32 حرفاً على الأقل)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "Jwt:Key" "Your_Super_Secret_Key_For_JWT_Validation_123!"
dotnet run
```

### 2) إعداد قاعدة البيانات
```bash
cd Kharasana.Infrastructure
dotnet ef database update --startup-project ../Kharasana.API
```

### 3) تشغيل واجهة الويب (MVC)
```bash
cd Kharasana.Web
dotnet run
```

---

## ⚙️ متغيرات البيئة (Environment Variables)

يجب ضبط المتغيرات التالية في بيئة التطوير عبر `User Secrets`، وفي الإنتاج عبر متغيرات بيئة النظام (Environment Variables) أو مخزن الأسرار (Azure Key Vault, AWS Secrets Manager, إلخ).

| المتغير | الوصف | مطلوب | مثال / تنسيق |
|----------|---------|:-----:|---------------|
| `ConnectionStrings__DefaultConnection` | سلسلة اتصال SQL Server | ✅ نعم | `Server=.;Database=KharasanaDb;Trusted_Connection=True;TrustServerCertificate=True` |
| `Jwt__Key` | مفتاح توقيع JWT (32 حرفاً كحد أدنى) | ✅ نعم | `Your_Super_Secret_Key_For_JWT_Validation_123!` |
| `Jwt__Issuer` | مُصدر التوكن | ✅ نعم | `Kharasana.API` |
| `Jwt__Audience` | جمهور التوكن | ✅ نعم | `Kharasana.Web` |
| `Cors__Origins` | النطاقات المسموحة (مفصولة بفاصلة) | ✅ نعم (إنتاج) | `https://app.kharasana.com,https://admin.kharasana.com` |
| `ApiSettings__BaseUrl` | رابط الـ API الأساسي للـ Web Client | ✅ نعم (إنتاج) | `https://api.kharasana.com` |
| `AdminSeed__Email` | بريد المدير الأولي (عند الإقلاع الأول) | ⚠️ اختياري | `admin@kharasana.local` |
| `AdminSeed__Password` | كلمة مرور المدير الأولي | ⚠️ اختياري | `Admin@12345` |

> ⚠️ **ملاحظة:** في بيئة التطوير، يتم قراءة `Cors__Origins` من `appsettings.Development.json` افتراضياً (يسمح بكل المصادر). في الإنتاج، **يجب** تعيينه صريحاً وإلا سيرفض التطبيق الإقلاع (Fail-fast).

---

## 🔗 نقاط نهاية API الرئيسية (API Endpoints)

الـ API مُرقم (Versioned) عبر المسار: `/api/v{version:apiVersion}/[controller]`.
للاستكشاف التفاعلي الكامل: **[Swagger UI](http://localhost:5000/swagger)** (في التطوير).

### المصادقة (`AuthController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `POST` | `/api/v1/Auth/login` | تسجيل الدخول، يعيد JWT + Refresh Token | عامة |
| `POST` | `/api/v1/Auth/refresh` | تجديد Access Token عبر Refresh Token | عامة |
| `POST` | `/api/v1/Auth/revoke` | إلغاء Refresh Token | مصادق |

### الطلبات (`OrdersController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `GET` | `/api/v1/Orders` | قائمة الطلبات (مرقمة، مرشحة) | Admin, FactoryEmployee, Client, Driver |
| `GET` | `/api/v1/Orders/{id}` | تفاصيل طلب واحد | أدوار ذات علاقة |
| `POST` | `/api/v1/Orders` | إنشاء طلب جديد | Client, FactoryEmployee |
| `PUT` | `/api/v1/Orders/{id}` | تعديل طلب | FactoryEmployee, Admin |
| `POST` | `/api/v1/Orders/{id}/assign-driver` | إسناد سائق | FactoryEmployee, Admin |
| `POST` | `/api/v1/Orders/{id}/start-delivery` | بدء التوصيل | Driver |
| `POST` | `/api/v1/Orders/{id}/confirm-delivery` | تأكيد التسليم | Driver |
| `POST` | `/api/v1/Orders/{id}/cancel` | إلغاء طلب | FactoryEmployee, Admin, Client, Driver |

### المصانع (`FactoriesController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `GET` | `/api/v1/Factories` | قائمة المصانع | Admin |
| `GET` | `/api/v1/Factories/{id}` | تفاصيل مصنع | Admin, FactoryEmployee (نفسه) |
| `POST` | `/api/v1/Factories` | إنشاء مصنع | Admin |
| `PUT` | `/api/v1/Factories/{id}` | تعديل مصنع | Admin, FactoryEmployee (نفسه) |

### المخزون (`RawMaterialsController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `GET` | `/api/v1/RawMaterials` | مواد خام المصنع الحالي | FactoryEmployee, Admin |
| `POST` | `/api/v1/RawMaterials` | إضافة مادة خام | FactoryEmployee, Admin |
| `PUT` | `/api/v1/RawMaterials/{id}/stock` | تحديث الرصيد (±) | FactoryEmployee, Admin |
| `DELETE` | `/api/v1/RawMaterials/{id}` | أرشفة مادة | FactoryEmployee, Admin |

### الإنتاج (`ProductionBatchesController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `GET` | `/api/v1/ProductionBatches` | دفعات إنتاج المصنع الحالي | FactoryEmployee, Admin |
| `GET` | `/api/v1/ProductionBatches/order/{orderId}` | دفعات لطلب محدد | FactoryEmployee, Admin |
| `POST` | `/api/v1/ProductionBatches/order/{orderId}` | بدء دفعة إنتاج | FactoryEmployee, Admin |
| `PUT` | `/api/v1/ProductionBatches/{id}/status` | تحديث حالة الدفعة | FactoryEmployee, Admin |

### المستخدمون (`UsersController`)
| الطريقة | المسار | الوصف | الصلاحيات |
|--------|--------|---------|-----------|
| `GET` | `/api/v1/Users` | قائمة المستخدمين (مرقمة) | Admin |
| `POST` | `/api/v1/Users` | إنشاء مستخدم (موظف/سائق) | Admin, FactoryEmployee |
| `PUT` | `/api/v1/Users/{id}` | تعديل مستخدم | Admin, FactoryEmployee |
| `POST` | `/api/v1/Users/{id}/toggle-active` | تفعيل/إيقاف مستخدم | Admin |

---

## 🗺️ خارطة الطريق والحالة الحالية (Roadmap & Status)

هذا المشروع في مرحلة **التطوير النشط والمستقر**. الجدول أدناه يوضح ما تم إنجازه وما هو مخطط له.

| الوحدة / الميزة | الحالة | ملاحظات |
|----------------|--------|---------|
| **البنية الأساسية (Core Architecture)** | ✅ **مكتمل** | Clean Architecture, DI, EF Core, Mapperly, FluentValidation |
| **المصادقة والصلاحيات (Auth & RBAC)** | ✅ **مكتمل** | JWT, Refresh Tokens, Roles, Tenant Isolation, Lockout |
| **إدارة الطلبات (Orders)** | ✅ **مكتمل** | دورة حياة كاملة، تسعير، إسناد سائق، توصيل، دفعات |
| **إدارة المخزون (Inventory)** | ✅ **مكتمل (CRUD)** | مواد خام، رصيد، عزل مصنع. *الخصم التلقائي قيد التخطيط* |
| **إدارة الإنتاج (Production)** | ✅ **مكتمل (CRUD)** | دفعات إنتاج، حالات، ربط بالطلبات. *وصفة الخلط (BOM) قيد التخطيط* |
| **السجل المالي والمدفوعات (Ledger & Payments)** | ✅ **مكتمل** | كشف حساب عميل، تسجيل دفعات، رصيد متبقي • **مميزات جديدة:** إدارةحوالات (Hawala Ledger) — نظام متكامل لقبول ورفض الحوالات (Hawala) من قبل المحاسب مع حالة التحقق (Pending, Verified, Rejected) وتدوين ملاحظات لكل دفعة، متصل بالكامل بلوحة "مكتب المراجعة المالية" في الويب. |
| **التقارير ولوحات التحكم (Dashboards)** | ✅ **مكتمل** | تقارير حالة، كميات، أنواع خرسانة، عملاء |
| **إدارة تسجيل المصانع (Factory Registration)** | ✅ **مكتمل** | طلب تسجيل، موافقة أدمن، تفعيل بتعيين كلمة مرور |
| **الإشعارات (WhatsApp)** | ✅ **مكتمل** | خدمة إشعارات موافقة تسجيل المصنع |
| **اختبارات التكامل (SQL Server Integration Tests)** | 🟡 **مجزأة** | البنية جاهزة، بعض الاختبارات `Skipped` بحاجة لتكوين SQL Server محلي |
| **Docker & CI/CD** | 🔴 **مخطط** | Dockerfile, docker-compose.yml, GitHub Actions Pipeline |
| **سجلات التدقيق (Audit Logs)** | 🔴 **مخطط** | تتبع من فعل ماذا ومتى على الكيانات الحساسة |
| **وصفة الخلط (Bill of Materials)** | 🔴 **مخطط** | تعريف مكونات كل نوع خرسانة للخصم التلقائي من المخزون |
| **تطبيق Flutter (Mobile App)** | 🟡 **قيد التطوير** | مستودع منفصل: [kharasana_app](https://github.com/noormohamed7763-a11y/kharasana_app) |

> **سياسة الإصدار:** نتبع **Semantic Versioning**. الإصدار الحالي المستهدف للإنتاج: `v1.0.0`.


## ⚙️ متغيرات البيئة (Environment Variables)

يجب ضبط المتغيرات التالية في بيئة التشغيل (`appsettings.Production.json` أو كمتغيرات بيئة للنظام):

| المتغير | الوصف | مثال |
| :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | سلسلة الاتصال بـ SQL Server | `Server=.;Database=KharasanaDb;Trusted_Connection=True;` |
| `Jwt__Key` | مفتاح تشفير JWT (32 حرفاً كحد أدنى) | `Your_Super_Secret_Key_For_JWT_Validation_123!` |
| `Jwt__Issuer` | مُصدر التوكن | `https://api.kharasana.local` |
| `Jwt__Audience` | جمهور التوكن | `https://kharasana.local` |
| `Cors__Origins` | النطاقات المسموح لها بالوصول | `["https://app.kharasana.local"]` |
| `ApiSettings__BaseUrl` | رابط الـ API الأساسي (للويب) | `https://api.kharasana.local` |

---

## 🔗 نقاط نهاية API الرئيسية

يمكن استعراض وتجربة كافة الـ Endpoints عبر واجهة **Swagger** المدمجة عند تشغيل التطبيق محلياً: `https://localhost:5000/swagger`

أهم العمليات:

*   **المصادقة:** `POST /api/v1/Auth/login`
*   **الطلبات:** `GET /api/v1/Orders`, `POST /api/v1/Orders`
*   **المخزون:** `GET /api/v1/RawMaterials`, `PUT /api/v1/RawMaterials/{id}/stock`
*   **الإنتاج:** `GET /api/v1/ProductionBatches`, `POST /api/v1/ProductionBatches/order/{orderId}`
*   **الصحة:** `GET /health`

---

## 🗺️ خارطة الطريق والحالة الحالية

| الميزة | الحالة | ملاحظات |
| :--- | :--- | :--- |
| إدارة الطلبات | مكتملة | دورة حياة كاملة مع تحكم Concurrency |
| إدارة المخزون | مكتملة | (CRUD & Stock Updates) |
| إدارة الإنتاج | مكتملة | (Batches & Status Management) |
| إدارة المصانع والعملاء | مكتملة | (Tenant Isolation) |
| الربط مع WhatsApp | مكتملة | إشعارات للمستخدمين |
| محاسبة المخزون التلقائية | **قيد التطوير** | الخصم التلقائي عند الإنتاج |
| تقارير متقدمة | **قيد التطوير** | مخططات بيانية (Dashboards) |

## 🔗 المشاريع المرتبطة

- **Kharasana App (Flutter / Dart)**
  - المستودع: [https://github.com/noormohamed7763-a11y/kharasana_app](https://github.com/noormohamed7763-a11y/kharasana_app)
  - الغرض: تطبيق محمول للعملاء والسائقين يتصل بالنظام الرئيسي عبر الـ API.

---

## 📜 الترخيص
هذا المشروع مرخص بموجب [MIT License](LICENSE).

<div dir="rtl" align="center">

**تم التطوير بواسطة [Mohamed Alnoor](https://github.com/noormohamed7763-a11y)** — نظام إدارة مصانع الخرسانة الجاهزة
</div>
