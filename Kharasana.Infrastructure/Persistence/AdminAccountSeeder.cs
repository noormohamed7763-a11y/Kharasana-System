using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kharasana.Infrastructure.Persistence;

/// <summary>
/// تهيئة حساب مدير النظام الأول عند الإقلاع.
///
/// لا يوجد في النظام أي مسار لإنشاء مدير (تسجيل الحسابات العامة ينشئ عملاء فقط،
/// وإنشاء الموظفين يتطلب مديراً موجوداً أصلاً) — فقاعدة بيانات جديدة تبقى بلا أي
/// مدير، ولا يمكن لأي أحد الدخول لإنشاء المصانع والموظفين. هذا المُهيّئ يسدّ تلك الثغرة.
///
/// الإعداد (أحدهما يكفي؛ متغيرات البيئة بأسبقية على appsettings):
/// <code>
///   appsettings:  "AdminSeed": { "Email": "admin@kharasana.local", "Password": "..." }
///   متغيرات بيئة: AdminSeed__Email / AdminSeed__Password
/// </code>
///
/// ⚠️ عملية <b>مُتكرّرة بلا أثر</b> (idempotent): لا تُنشئ شيئاً إن وُجد أي حساب مدير،
/// ولا تُعدّل كلمة مرور قائمة إطلاقاً — فوجود متغيّر البيئة في الإنتاج لا يُعيد
/// تعيين كلمة مرور المدير بعد تغييرها.
/// </summary>
public static class AdminAccountSeeder
{
    /// <summary>الحد الأدنى لطول كلمة مرور المدير المُهيّأ.</summary>
    private const int MinimumPasswordLength = 8;

    /// <summary>
    /// إنشاء حساب المدير إن لزم. لا ترمي استثناءً أبداً: فشل التهيئة (قاعدة بيانات
    /// غير متاحة أو غير مُهاجَرة) يُسجَّل ويُتابع الإقلاع — الحالة تُبلَّغ عبر /health.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["AdminSeed:Email"]?.Trim();
        var password = configuration["AdminSeed:Password"];

        // لا إعدادات = سلوك مقصود (بيئة تطوير، أو قاعدة فيها مدير بالفعل) — تحذير فقط بلا فشل
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrEmpty(password))
        {
            logger.LogWarning(
                "تهيئة المدير: لم يُضبط AdminSeed:Email / AdminSeed:Password (أو AdminSeed__Email / AdminSeed__Password) — تم تخطّي التهيئة. " +
                "إن كانت هذه أول مرة تُشغّل فيها النظام فلن يوجد أي حساب مدير.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogError("تهيئة المدير: يجب ضبط البريد وكلمة المرور معاً — أحدهما فقط مُعرّف. تم تخطّي التهيئة.");
            return;
        }

        if (!email.Contains('@'))
        {
            logger.LogError("تهيئة المدير: البريد الإلكتروني غير صالح. تم تخطّي التهيئة.");
            return;
        }

        // كلمة مرور أقصر من الحد = حساب مدير ضعيف يمكن كسره — نرفض بدل إنشائه
        if (password.Length < MinimumPasswordLength)
        {
            logger.LogError(
                "تهيئة المدير: كلمة المرور أقصر من {Minimum} محارف. تم تخطّي التهيئة.",
                MinimumPasswordLength);
            return;
        }

        try
        {
            using var scope = services.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // ⚠️ الفحص على كل المديرين لا على البريد وحده: لو كان البريد المُهيّأ محجوزاً
            //    لحساب آخر فلا نُحاول الإنشاء أصلاً ونتجنّب خطأ تفرّد البريد.
            var anyAdminExists = await unitOfWork.Users.CountAsync(u => u.Role == UserRole.Admin) > 0;
            if (anyAdminExists)
            {
                logger.LogInformation("تهيئة المدير: يوجد حساب مدير بالفعل — لا تغيير.");
                return;
            }

            var existingWithEmail = await unitOfWork.Users.GetByEmailAsync(email);
            if (existingWithEmail != null)
            {
                logger.LogError(
                    "تهيئة المدير: البريد {Email} محجوز لحساب قائم بدور {Role} وليس مديراً — لن يُرقّى الحساب تلقائياً. " +
                    "اختر بريداً آخر أو أنشئ المدير يدوياً.",
                    email, existingWithEmail.Role);
                return;
            }

            await unitOfWork.Users.AddAsync(new User
            {
                FullName = "مدير النظام",
                Email = email,
                // تجزئة فقط — كلمة المرور الصريحة لا تُخزَّن ولا تُسجَّل في أي مكان
                PasswordHash = passwordHasher.Hash(password),
                Phone = null,
                Role = UserRole.Admin,
                FactoryId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("تهيئة المدير: أُنشئ حساب المدير {Email} بنجاح.", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "تهيئة المدير: فشلت المحاولة — سيُتابع الإقلاع بلا تهيئة.");
        }
    }
}
