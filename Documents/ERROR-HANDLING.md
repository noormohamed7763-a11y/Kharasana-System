# دليل نظام معالجة الأخطاء - Kharasana

## نظرة عامة

يجمع هذا الدليل جميع آليات معالجة الأخطاء وتسجيلها وعرضها في المشروع، مع شرح كيفية استخدامها وتوحيدها.

---

## 1. تسجيل الأخطاء (Logging)

### ملفات السجل اليومية

السجل تلقائيًا في ملفات يومية بالصيغة: `Logs/log-YYYY-MM-dd.log`

**كيفية التشغيل:** يُفعّل في `Program.cs` لكل من Web و API:
```csharp
builder.Logging.AddFileLogging();
```

**الخيارات المتاحة:**
```csharp
builder.Logging.AddFileLogging(
    minimumLevel: LogLevel.Warning,          // مستوى الحد الأدنى
    logsDirectory: "/custom/path"            // مسار مخصص
);
```

**صيغة السجل:**
```
2026-09-08 14:32:15.123 [INFO  Karasana.Web.Services.ApiClient]
    GET https://api.example.com/factory/5 → 200 OK
    TraceId=abc123 TenantId=5
```

**المستويات:** `INFO` → `WARN` → `ERROR` → `FATAL`

**ملاحظة:** ملفات `*.log` مُستبعدة من Git عبر `.gitignore`

---

## 2. صفحة الخطأ الموحدة (Error Page)

### المسارات المعالجة

| الحالة | المسار | النتيجة |
|--------|--------|---------|
| استثناء غير متوقع | `UseExceptionHandler("/Home/Error")` | صفحة خطأ 500 |
| صفحة غير موجودة | `UseStatusCodePagesWithReExecute("/Home/Error")` | صفحة 404 |
| ممنوع الوصول | `UseStatusCodePagesWithReExecute("/Home/Error")` | صفحة 403 |

### تفاصيل الخطأ المعروضة

- **404:** "الصفحة غير موجودة" + أيقونة بوصلة + رمز info-blue
- **403:** "غير مصرّح لك" + أيقونة درع + رمز info-blue
- **500:** "حدث خطأ" + أيقونة تعجب + زر إعادة المحاولة
- **Request ID:** يظهر في كل صفحة خطأ — يُستخدم لتحديد السجل المقابل

### كود الاستجابة

```csharp
// في HomeController.cs
[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult Error(int? statusCode = null)
{
    var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    var viewModel = new ErrorViewModel { ShowRequestId = true, StatusCode = statusCode };
    // ... logs at appropriate level
    return View(viewModel);
}
```

---

## 3. التتبع والربط (TraceId)

### في ApiClient (Web Layer)

كل طلب API يُسجَّل مع:
- **TraceId**: معرّف التتبع الفريد (= RequestId المعروض في صفحة الخطأ)
- **TenantId**: معرّف المصنع الحالي من Session

```
TRACE: GET https://api.example.com/drivers → 200 OK. TraceId=abc123 TenantId=5
WARN:  POST https://api.example.com/orders → 400 Bad Request. TraceId=def456 TenantId=5
ERROR: GET https://api.example.com/reports → 500 InternalServerError. TraceId=ghi789 TenantId=5
```

**كيفية تتبع الخطأ:**
1. المستخدم يرى Request ID في صفحة الخطأ (أو في واجهة المستخدم)
2. المطور يبحث في ملف السجل عن `TraceId=<المعرّف>`
3. يظهر كامل تسلسل الأخطاء المرتبطة بذلك الطلب

---

## 4. ملفات السجل المُولّدة

### في Web Layer

**الموقع:** `Karasana.Web/Services/Api/ApiClient.cs`

| الموقع | المستوى | الوصف |
|--------|---------|-------|
| `AddAuthorizationHeader` | ERROR | فشل إضافة رمز التخزين |
| `HandleResponseAsync` | WARN | فشل استجابة API |
| `HandleResponseAsync` | INFO | مسح Session بسبب 401 |
| `HandleResponseAsync` | WARN | فشل تحليل JSON للخطأ |
| `HandleResponseAsync` | ERROR | خطأ معاد من API |
| `catch (Exception)` | ERROR | استثناء غير متوقع في الطلب |
| `catch (OperationCanceledException)` | WARN | انتهاء مهلة الطلب |

### في API Layer

**الموقع:** `Kharasana.API/Middleware/ExceptionMiddleware.cs`

| المستوى | الوصف |
|---------|-------|
| ERROR | استثناء تجاري (BusinessException) |
| ERROR | استثناء غير متوقع |

---

## 5. التنبيهات الموحدة (Unified Alerts)

### التنسيق المركزي

يُعرَّف في `_Alerts.cshtml` ويُستدعى في `_Layout.cshtml` قبل `@RenderBody()`:

```html
@await Html.PartialAsync("Components/_Alerts")
```

### المفاتيح المدعومة

| المفتاح | النوع | الأيقونة | اللون |
|---------|-------|---------|-------|
| `TempData["Success"]` أو `TempData["SuccessMessage"]` | نجاح | ✓ | أخضر |
| `TempData["Error"]` أو `TempData["ErrorMessage"]` | خطأ | ⚠ | أحمر |
| `TempData["Warning"]` أو `TempData["WarningMessage"]` | تحذير | ⚠ | برتقالي |
| `TempData["Info"]` أو `TempData["InfoMessage"]` | معلومات | ℹ | أزرق |

### الكونستانت في BaseController

```csharp
public const string TempDataSuccess = "Success";
public const string TempDataError   = "Error";
public const string TempDataInfo    = "Info";
public const string TempDataWarning = "Warning";
```

**يُنصح باستخدام الكونستانت في controllers الجديدة:**
```csharp
TempData[TempDataSuccess] = "تم الحفظ بنجاح";
// بدلاً من:
TempData["SuccessMessage"] = "تم الحفظ بنجاح";
```

---

## 6. Best Practices

### ✅ يُنصح بـ

- استخدام `TempData[TempDataSuccess]` في controllers الجديدة
- تسجيل `LogLevel.Error` للاستثناءات فقط
- استخدام `LogLevel.Information` للسجلات اليومية العادية
- تمرير `TraceId` في رسائل الخطأ للربط مع السجل
- اختبار صفحة الخطأ عبر زيارة `/nonexistent-page` (404) أو `/Home/Error?statusCode=500`

### ❌ يُحذَّر من

- تسجيل كلمات المرور أو البيانات الحساسة في السجل
- لا تستخدم `throw ex` — استخدم `throw` للحفاظ على التتبع
- لا تحاول تخصيص `_Alerts.cshtml` مباشرة — عدّل المفاتيح في Controller بدلاً من ذلك

---

## 7. تشخيص مشكلة سريعة

### إذا ظهر خطأ 500 في الإنتاج

1. افتح ملف `Logs/log-YYYY-MM-dd.log` (نفس تاريخ الخطأ)
2. ابحث عن `ERROR` مع `TraceId=<المعرّف من صفحة الخطأ>`
3. تحقق من الـ Stack Trace المُسجَّل
4. تحقق من `Kharasana.API` logs إن كان الخطأ من API

### إذا لم تظهر صفحة الخطأ المخصصة

1. تأكد من `UseExceptionHandler("/Home/Error")` موجود في `Program.cs`
2. تأكد من `UseStatusCodePagesWithReExecute` موجود في `Program.cs`
3. تأكد من `HomeController.Error()` action موجودة وتعمل

### إذا لم تعمل التنبيهات

1. تأكد من `@await Html.PartialAsync("Components/_Alerts")` موجود في `_Layout.cshtml`
2. تأكد من المفتاح في Controller يطابق أحد المفاتيح المدعومة
3. تأكد من `TempData` ليس `null` قبل التوجيه (`RedirectToAction`)

---

*تاريخ آخر تحديث: سبتمبر 2026*
