---
name: kharasana-factory-registration-workflow
description: Concept for factory registration workflow requiring Admin approval before creation.
metadata:
  type: project
---

فكرة النظام هي إضافة نظام تسجيل للمصانع الجديدة يتطلب موافقة الإدارة قبل إنشاء المصنع والمستخدم بشكل فعلي.

### سير العمل (Workflow)
1. **صاحب المصنع:** يرسل طلب تسجيل (`FactoryRegistrationRequest`) بحالة `Pending`. لا يتم إنشاء مصنع أو مستخدم في هذه المرحلة.
2. **Admin:** يراجع الطلبات في لوحة الإدارة الحالية (قسم جديد: "Factory Registration Requests").
3. **القرار:**
   - **Approve:** يقوم النظام بإنشاء `Factory` + `User` (بحالة `IsActive = false` وبدون `PasswordHash`) + `ActivationToken`.
   - **Reject:** يتم حفظ سبب الرفض وتغيير حالة الطلب إلى `Rejected`.
4. **التفعيل:** بعد الموافقة، يصل للمسؤول رابط تفعيل لتعيين كلمة المرور وتفعيل الحساب.

**Why:** لضمان تحكم الإدارة في دخول المصانع الجديدة وضمان إنشاء الحسابات بشكل آمن ومترابط.
**How to apply:** الالتزام بهذا السير عند تنفيذ الميزات المتعلقة بتسجيل المصانع، واستخدام نفس منطق الـ Admin الحالي.