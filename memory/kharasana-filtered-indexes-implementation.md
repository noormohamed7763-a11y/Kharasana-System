---
name: kharasana-filtered-indexes-implementation
description: Implementation of Filtered Indexes for Orders and Users tables to optimize performance by excluding soft-deleted rows.
metadata:
  type: project
---

تم تطبيق فهارس مصفاة (Filtered Indexes) لجدولي `Orders` و `Users` لتحسين أداء الاستعلامات التي تستثني السجلات المحذوفة ناعمًا (`IsDeleted = 0`).

**التغييرات:**
1.  تعديل `OrderConfiguration` و `UserConfiguration` لإضافة فهارس مصفاة باستخدام `HasFilter("[IsDeleted] = 0")`.
2.  تطبيق Migration رقم `20261008002600_AddFilteredIndexes`.
3.  **الفهارس المضافة:**
    *   `IX_Users_FactoryId_Role_Filtered` على `Users`.
    *   `IX_Orders_FactoryId_Status_Filtered` على `Orders`.
4.  **الفهارس المحذوفة:**
    *   `IX_Users_Role_FactoryId`
    *   `IX_Orders_FactoryId_Status`

**النتيجة:**
تم تطبيق التغييرات بنجاح على قاعدة البيانات (SQL Server) وبناء المشروع دون أخطاء.
المرحلة القادمة: قياس وتحليل الأداء قبل وبعد (Performance Baseline & Verification).
