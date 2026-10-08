---
name: kharasana-hide-pricing-refactor
description: نقل CallerContext وتطبيق إخفاء السعر (hidePricing) في مستوى المستودع (Repository) باستخدام SQL Case.
metadata:
  type: project
---

تم نقل `CallerContext` إلى طبقة `Application` لحل حلقة الاعتمادية، وتحديث `OrderRepository.GetPagedAsync` لتطبيق منطق إخفاء `TotalPrice` للسائقين مباشرة داخل استعلام SQL باستخدام `CASE` لضمان الأمان والكفاءة.

**Why:**
الاعتماد على إخفاء السعر في الطبقات العليا (Service/Controller) كان عرضة للتسريب؛ نقله إلى مستوى SQL (Repository) يضمن عدم قراءة البيانات الحساسة أصلاً من قاعدة البيانات للسائقين.

**How to apply:**
1. استخدام `CallerContext` عند استدعاء `GetPagedAsync`.
2. يتم فحص `caller.Role` داخل الـ Projection في المستودع لتعيين `TotalPrice = null` إذا كان السائق هو المستخدم.
[[kharasana-split-refactor-status]]
