# توثيق: تسجيل تغييرات حالة الطلب (Order Status History Logging)

## ملخص
تم دمج نظام مركزي لتسجيل جميع تغييرات حالة الطلب في جدول `OrderStatusHistory` كجزء من تحسينات مراقبة الأعمال والشفافية.

## التغييرات المنفذة

### 1. بنية البيانات (Data Structure)

#### ملف جديد: `Kharasana.Application/Interfaces/Repositories/IOrderStatusHistoryRepository.cs`
```csharp
public interface IOrderStatusHistoryRepository : IGenericRepository<OrderStatusHistory>
{
}
```

#### ملف جديد: `Kharasana.Infrastructure/Repositories/OrderStatusHistoryRepository.cs`
```csharp
public class OrderStatusHistoryRepository : GenericRepository<OrderStatusHistory>, IOrderStatusHistoryRepository
{
    public OrderStatusHistoryRepository(KharasanaDbContext context)
        : base(context)
    {
    }
}
```

#### تعديل: `Kharasana.Application/Interfaces/IUnitOfWork.cs`
- إضافة خاصية `IOrderStatusHistoryRepository OrderStatusHistories { get; }`

#### تعديل: `Kharasana.Infrastructure/Persistence/UnitOfWork.cs`
- إنشاء instance من `OrderStatusHistoryRepository` في المُنشئ.

#### تعديل: `Kharasana.Application/DTOs/Order/UpdateOrderStatusDto.cs`
- إضافة حقل `string? Reason` لتوثيق سبب التغيير.

### 2. الخدمة التنفيذية

#### ملف: `Kharasana.Application/Services/OrderWorkflowService.cs`

تم إضافة دالة مساعدة:
```csharp
private async Task LogStatusChangeAsync(Order order, OrderStatus previousStatus, OrderStatus newStatus, int userId, string? reason = null)
```

تم استدعاء هذه الدالة في كل من:
- `SetPriceAsync` - تسجيل عند تغيير السعر (ملاحظة: الحالة قد تبقى Pending أو تتغير حسب المنطق)
- `ApproveOrderAsync` - Pending → Approved
- `RejectOrderAsync` - Pending → Rejected (مع تسجيل السبب)
- `CancelOrderAsync` - أي حالة → Cancelled
- `StartDeliveryAsync` - Approved → OnTheWay
- `DeliverOrderAsync` - OnTheWay → Delivered
- `CloseOrderAsync` - Delivered → Closed
- `UpdateStatusAsync` - أي تغيير حالة عام (مع تسجيل السبب)

### 3. الكائنات (Entities)

#### ملف موجود: `Kharasana.Domain/Entities/OrderStatusHistory.cs`
| الحقل | الوصف |
|--------|-------|
| Id | المعرف الأساسي |
| OrderId | معرف الطلب المُغير |
| PreviousStatus | الحالة قبل التغيير |
| NewStatus | الحالة الجديدة |
| ChangedByUserId | معرف المستخدم الذي قام بالتغيير |
| ChangedAt | توقيت التغيير (UTC) |
| Reason | السبب اختياري (مثل سبب الرفض أو الإلغاء) |

### 4. الفوترة (Configuration)

#### ملف موجود: `Kharasana.Infrastructure/Configurations/OrderStatusHistoryConfiguration.cs`
- تم إنشاءه مسبقاً ويُطبق تلقائياً عبر `ApplyConfigurationsFromAssembly`.

### 5. إصلاحات الاختبارات

#### ملف: `Kharasana.Tests/Tests/AdminAccountSeederTests.cs`
- إضافة `OrderStatusHistories` إلى `UnreachableUnitOfWork` لتوافق مع واجهة `IUnitOfWork`.

## فوائد النظام
1. **شفافية كاملة**: جميع تغييرات حالة الطلب موثقة مع توقيت دقيق.
2. **تحليل تاريخ الطلب**: يمكن استخدام البيانات لبناء تقارير أداء وتحليل سلوك العملاء.
3. **مراجعة دقيقة**: إمكانية مراجعة من يغير الحالة ولماذا.
4. **نقطة أمان**: سجل كامل يمكن استخدامه في حالات النزاع أو الأخطاء.

## ملاحظات
- جدول `OrderStatusHistory` يستخدم `DeleteBehavior.Cascade` على الطلب، أي أن تاريخ الحالة يُحذف مع الحذف الناعم للطلب.
- الحقل `Reason` اختياري، لكنه موثّق عند الرفض والإلغاء.