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

## 📦 النشر (Production)

للنشر في بيئة الإنتاج:

1.  **إعدادات البيئة:** تأكد من ضبط `Connection String`، `Jwt:Key`، و `CORS Origins` عبر متغيرات البيئة (Environment Variables) وليس في ملفات الإعدادات.
2.  **الأمان:** يجب تفعيل `HTTPS` حصراً واستخدام `Reverse Proxy` (مثل Nginx أو Caddy) لتعزيز الأمان.
3.  **رابط الـ API:**
    - في بيئة الإنتاج، يجب ضبط `ApiSettings:BaseUrl` داخل ملف `appsettings.Production.json` (أو متغيرات البيئة) ليشير إلى النطاق (Domain) الفعلي الذي تم عليه نشر الـ API.
    - تجنب استخدام روابط لوحات تحكم الاستضافة كـ Base URL.

---

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
