---
name: factory-registration-workflow
description: توثيق سير عمل تسجيل المصانع من الطلب حتى القبول أو الرفض
metadata:
  type: project
---

# سير عمل تسجيل المصانع (Factory Registration Workflow)

تم الانتهاء من بناء التدفق الكامل لتسجيل المصانع، ويتكون من المراحل التالية:

### 1. طلب التسجيل (Public Registration)
- **المسار:** `/Registration/Create`
- **المنطق:** يتم إنشاء طلب (`FactoryRegistrationRequest`) بحالة `Pending`.
- **الهدف:** إتاحة الفرصة للمصانع الجديدة بالتقديم عبر نموذج عام.

### 2. مراجعة الأدمن (Admin Dashboard)
- **المسار:** `/FactoryRegistrationRequests/Index` (يعرض الطلبات بـ `Status == Pending` فقط).
- **التفاصيل:** `/FactoryRegistrationRequests/Details/{id}`.
- **الإجراءات:**
    - **قبول (Approve):** يقوم بإنشاء المصنع، إنشاء مستخدم (Owner)، تعيين دور `FactoryAdmin` له، وتوليد `ActivationToken` لتفعيل الحساب. العملية تتم داخل `Transaction` لضمان تكامل البيانات.
    - **رفض (Reject):** تغيير حالة الطلب إلى `Rejected` مع تسجيل سبب الرفض.

### 3. الجسور البرمجية (Integration)
- **API:** تم إنشاء `FactoryRegistrationRequestsController` في `Kharasana.API` لإدارة الطلبات.
- **Service:** تم تحديث `FactoryRegistrationRequestService` لدعم منطق القبول والرفض الآمن.
- **Web:** تم تحديث `FactoryRegistrationRequestApiService` لربط واجهات الويب بمسارات الـ API.

**كيفية التطبيق:**
- النظام الآن يعمل بالكامل: أي طلب جديد يظهر للأدمن، وبضغطة زر يتم إنشاء البنية التحتية للمصنع والمستخدم وتفعيل التدفق الأمني.
- التوثيق مرتبط بـ [[kharasana-pending-defects-after-review]].
