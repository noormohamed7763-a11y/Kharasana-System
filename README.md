# نظام خراسانة لإدارة مصانع الخرسانة الجاهزة

<p dir="rtl">
نظام ERP متكامل لإدارة مصانع الخرسانة الجاهزة ومتابعة الطلبات والمركبات والعملاء — مبني على مبادئ
<strong>Clean Architecture</strong> و <code>.NET 10</code>، بواجهة ويب عربية <strong>RTL</strong> بالكامل
مصممة للسوق اليمني.
</p>

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10-512BD4?logo=entityfusion&logoColor=white)
![Architecture](https://img.shields.io/badge/Clean%20Architecture-CQRS-blue)
![JWT](https://img.shields.io/badge/Auth-JWT-orange)
![Tests](https://img.shields.io/badge/Tests-40%20passing-green)
![License](https://img.shields.io/badge/License-MIT-green)

</div>

---

## 📋 نظرة عامة

نظام **خراسانة** يوفر لوحة تحكم موحّدة لإدارة مصانع الخرسانة الجاهزة في اليمن، تغطي دورة العمل الكاملة:

- إدارة المصانع والموظفين لكل مصنع
- إنشاء ومتابعة **طلبات الخرسانة** (الكمية، النوع، الطابق، المضخة، الموقع)
- إدارة الأنواع الخرسانية والعملاء والسائقين والمركبات
- لوحات معلومات وتقارير لحظية للمشرف والمصنع

النظام مقسوم إلى **واجهة API** مستقلة تستخدمها **واجهة ويب MVC** عبر `HttpClient`، مع فصل تام بين الطبقات.

---

## 🎭 الأدوار والصلاحيات

| الدور | الصلاحيات |
|--------|-----------|
| **Admin** (المشرف) | إدارة كاملة: المصانع، المستخدمون، الأنواع، العملاء، السائقون، التقارير، الإعدادات |
| **FactoryEmployee** (موظف المصنع) | إدارة طلبات مصنعه فقط (عزل تام بين المصانع)، بيانات المصنع وشعاره |
| **Client** (العميل) | تقديم الطلبات ومتابعتها |
| **Driver** (السائق) | عرض طلباته وتسليمها وتحديث حالتها |

> يتم فرض **عزل المصنع** في طبقة التطبيقات وطبقة الـ API معًا — لا يستطيع موظف مصنع الوصول لبيانات مصنع آخر.

---

## ✨ المميزات

- **لوحات معلومات**: إحصاءات الطلبات والمصانع والتوزيع للمشرف، وإحصاءات مصنعه لموظف المصنع
- **إدارة الطلبات**: إنشاء/تعديل/أرشفة طلبات الخرسانة مع تحقق شامل من الحالة والنوع والكمية
- **رفع شعار المصنع**: صورة توضع محليًا يُعرض لها مع بديل تلقائي بالأحرف الأولى عند غيابها
- **حذف ناعم (Soft Delete)**: أرشفة الكيانات عبر `IsDeleted` + فلاتر استعلام عامة دون فقدان البيانات
- **حماية**: JWT مع صلاحيات بالأدوار، تحديد معدل تسجيل الدخول، ترويسات أمان (CSP)، قفل حساب بعد محاولات فاشلة
- **تسجيل شامل**: سجل يومي في `Logs/` مع `TraceId` لكل طلب
- **دعم اليمن**: واجهة عربية RTL، تطبيع أرقام الهواتف اليمنية (967XXXXXXXXX)

---

## 🏗️ العمارة (Clean Architecture)

```
┌────────────────────────────────────────────────────┐
│                     Web (MVC)                      │  واجهة المستخدم
│              + API (REST + JWT)                    │
├────────────────────────────────────────────────────┤
│                 Application                        │  حالات الاستخدام وإدارة العمل
│          (Services — قواعد العمل والنقل)            │
├────────────────────────────────────────────────────┤
│                   Domain                           │  الكيانات والقواعد الأساسية
├────────────────────────────────────────────────────┤
│               Infrastructure                        │  EF Core / SQL Server / التخزين
└────────────────────────────────────────────────────┘
```

قاعدة الاعتماد: **الاعتماد يتجه نحو الداخل فقط** — لا تعتمد `Application` على `Infrastructure`، وتصل الواجهات فقط عبر الاتجاهات الخارجية.

---

## 🗂️ بنية المشروع

```
Kharasana.slnx
├── Kharasana.Domain/           الكيانات (Factory, Order, User, ConcreteType) والتعدادات
├── Kharasana.Application/      الخدمات، التحقق، الرسائل الموحّدة، الاستثناءات التجارية
├── Kharasana.Infrastructure/   DbContext، المستودعات، UnitOfWork، تخزين الصور، الهجرات
├── Kharasana.API/              واجهة REST: JWT، CORS، OpenAPI، التحكم بالمعدل
├── Kharasana.Web/              واجهة MVC العربية: صفحات، مكوّنات، ApiClient
└── Kharasana.Tests/            xUnit + FluentAssertions (40 اختبارًا)
```

---

## 🚀 البدء السريع

### المتطلبات

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server** (محلي أو LocalDB)
- Visual Studio 2022 / JetBrains Rider / VS Code

### 1) تشغيل الـ API

```bash
cd Kharasana.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Kharasana;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<مفتاح-سري-طويل-لا-يقل-عن-32-حرفاً>"
dotnet run
```

الخدمة تبدأ على `http://localhost:5000`، وواجهة Swagger على `http://localhost:5000/swagger/index.html`.

### 2) تشغيل الواجهة

```bash
cd Kharasana.Web
dotnet run
```

الواجهة على `http://localhost:5283` (HTTPS: `https://localhost:7195`)، وتتواصل مع الـ API عبر `ApiSettings.BaseUrl`.

### 3) قاعدة البيانات

تُنشأ الهجرات وتُطبَّق تلقائيًا عند أول تشغيل، أو يدويًا:

```bash
cd Kharasana.Infrastructure
dotnet ef database update
```

---

## ⚙️ الإعدادات

| الملف / الآلية | المسؤول عن |
|----------------|------------|
| `appsettings.json` | الإعدادات العامة (المنافذ، Cors، Jwt) |
| `user-secrets` | **التطوير**: سلسلة الاتصال + مفتاح JWT (لا تُرفع للريبو أبدًا) |
| `ApiSettings:BaseUrl` (Web) | عنوان الـ API الذي تتصل به الواجهة |
| `appsettings.Production*.json` | إعدادات النشر (تُوضع وقت النشر فقط) |

---

## 🧪 الاختبارات

```bash
dotnet test
```

**40 اختبارًا** عبر xUnit + FluentAssertions تشمل:

- الحذف الناعم والاسترداد (ملاحظة الأرشفة تمييز نهائي)
- المنطق الروتيني للطلبات والمصنع
- قفل الحساب (Lockout) وكشف المصنع المؤرشف
- تطبيع الأرقام والتحقق من النماذج

---

## 🔌 واجهة الـ API

| الوحدة | التحكمات |
|--------|----------|
| **Auth** | تسجيل الدخول والخروج وتجديد التوكن |
| **Factories** | إدارة المصانع وحساباتها وشعاراتها |
| **Orders** | تقديم الطلبات ومتابعتها وحالاتها |
| **ConcreteTypes** | إدارة الأنواع الخرسانية |
| **Users / Clients / Drivers** | إدارة المستخدمين والعملاء والسائقين |
| **Dashboard / Reports** | الإحصاءات والتقارير |
| **Settings** | إعدادات الحساب والشعار |

جميع النقاط محمية بـ JWT + تفويض بالأدوار، مع **تحديد معدل** (Rate Limiting) على تسجيل الدخول وعزل بيانات المصنع.

---

## 📦 النشر

- ملف نشر جاهز للاستضافة عبر **SiteAsp / runasp.net**:
  `Kharasana.API/Properties/PublishProfiles/site89235-WebDeploy.pubxml`
- بيانات الاعتماد محفوظة في `*.pubxml.user` **المتجاهلة** ولا تُرفع أبدًا
- التكوين الحساس (سلسلة الاتصال + مفتاح JWT) يُحقن وقت النشر فقط

---

## 🗂️ السجل ومعالجة الأخطاء

- سجل يومي تلقائي: `Logs/log-yyyy-MM-dd.log` مع مستوى قابل للتعديل
- كل طلب يحمل `TraceId` لربط الأخطاء عبر الطبقات
- رسائل خطأ عربية موحّدة من مصدر واحد: `Messages.cs` (112 رسالة)
- مرجع إضافي: [`Documents/ERROR-HANDLING.md`](Documents/ERROR-HANDLING.md)

---

<div dir="rtl" align="center">

**تم التطوير بواسطة [Mohamed Alnoor](https://github.com/noormohamed7763-a11y)** — مشروع نظام إدارة مصانع الخرسانة الجاهزة

</div>