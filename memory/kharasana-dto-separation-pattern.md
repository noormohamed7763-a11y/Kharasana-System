---
name: kharasana-dto-separation-pattern
description: نمط الفصل التام بين DTOs طبقة التطبيق وطبقة الويب
metadata:
  type: project
---

تم اعتماد نمط **الفصل التام (Complete Separation)** بين نماذج نقل البيانات (DTOs) في طبقة التطبيق (Application) ونماذج العرض (ViewModels/DTOs) في طبقة الويب (Web).

**Why:**
- **عدم الاقتران (Loose Coupling):** التغييرات في متطلبات الويب لا تؤثر على طبقة التطبيق، والعكس صحيح.
- **الوضوح:** كل طبقة تمتلك نماذجها الخاصة التي تخدم غرضها (منطق عمل مقابل عرض).
- **الأداء:** تجنب العبء الإضافي لمكتبات التعيين التلقائي (مثل AutoMapper).

**How to apply:**
- أي DTO يُضاف في `Kharasana.Application.DTOs` يخدم منطق العمل فقط.
- أي نموذج بيانات يُستخدم في `Kharasana.Web.ViewModels` (حتى لو تشابهت أسماؤه مع الـ DTO في التطبيق) يجب أن يُعرف محلياً في الويب.
- **ممنوع منعاً باتاً:** استيراد `Kharasana.Application.DTOs.*` داخل أي `Controller` أو `ViewModel` في الويب.
- **التعيين اليدوي (Manual Mapping):** يتم التعيين يدوياً بين الـ DTO والـ ViewModel عند الحاجة داخل الخدمة (Service) أو المتحكم (Controller).

مرتبط بـ: [[orderdto-duplication]]
