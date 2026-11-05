# إصلاح الاستعادة والبناء — 2026-11-05

## سبب NU1301

كان مصدر NuGet الافتراضي في هذا الاتصال يشير إلى `https://api.nuget.org/v3/index.json`. فشل الوصول إلى هذا المضيف على المنفذ 443، مع أن الاتصال بـ `www.nuget.org` وCDN الحزم كان متاحًا. لذلك لم تكن المشكلة انقطاع الإنترنت بالكامل، بل تعذر الوصول إلى مضيف فهرس v3 تحديدًا. نجحت استعادة الحل عند استخدام NuGet v2 عبر `https://www.nuget.org/api/v2/`.

أُضيف ملف `NuGet.Config` على مستوى المستودع ليستخدم مصدر v2 المتاح. تحقق الأمر `dotnet restore Kharasana.slnx` من اختيار المصدر وإتمام الاستعادة دون تمرير مصدر يدوي.

## إصلاحات التجميع والعقود

- إزالة DTO قديم باسم `RawMaterial` كان يتعارض مع مساحة أسماء DTO الحديثة، وإضافة مراجع `IUnitOfWork` الصحيحة في خدمتي المواد الخام ودفعات الإنتاج.
- إضافة مساحات الأسماء الناقصة لـ `ProductionStatus` و`GetCallerContext` في متحكمات الإنتاج والمواد الخام.
- توحيد عقد تسجيل المصنع: تعيد الخدمة معرّف الطلب بعد حفظه، ويرجع API استجابة `201 Created` قياسية تحتوي المعرّف، ويحوّل عميل الويب الغلاف إلى `ServiceResult` كما يتوقعه مستهلكه.
- استخدام كيان `FactoryRegistrationRequest` في قراءات API، بما يطابق ما تعيده الخدمة ويستهلكه عميل الويب.
- إصلاح استيراد `OrderDetailsDto` وترتيب وسيط مصنع الاختبار في اختبارات الطلبات.

## التحقق

- `dotnet restore Kharasana.slnx` — ناجح باستخدام مصدر v2 في إعداد المستودع.
- `dotnet build Kharasana.slnx --configuration Release` — ناجح لجميع المشاريع.
- `dotnet test Kharasana.Tests/Kharasana.Tests.csproj --no-build --configuration Release` — 323 اختبارًا ناجحًا، بلا إخفاقات.
- `dotnet test Kharasana.ContractTests/Kharasana.ContractTests.csproj --no-build --configuration Release` — 3 اختبارات ناجحة، بلا إخفاقات.
- `dotnet test Kharasana.IntegrationTests/Kharasana.IntegrationTests.csproj --no-build --configuration Release` — 8 اختبارات SQL Server متخطاة لعدم إعداد `KHARASANA_TEST_SQLSERVER`؛ لا إخفاقات.
