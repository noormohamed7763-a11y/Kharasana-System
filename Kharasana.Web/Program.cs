using Kharasana.Web.Configuration;
using Kharasana.Web.Filters;
using Kharasana.Web.Middlewares;
using Kharasana.Web.Services;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Application.Common.Logging;
using Kharasana.Web.Localization;
using Kharasana.Web.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Kharasana.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // تسجيل دائم في ملفات (Logs/) — يحافظ على أخطاء الإنتاج بعد وقوعها
            builder.Logging.AddFileLogging();

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
                options.Filters.Add<UnhandledExceptionFilter>());

            // المصادقة في هذا المشروع تعمل عبر Session + SessionAuthorizeAttribute،
            // ولا يوجد أي استخدام لـ SignInAsync — لذا لا نُسجّل Cookie Authentication (كود ميت)
            builder.Services.AddAuthentication();

            // Session
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // HttpContext Accessor
            builder.Services.AddHttpContextAccessor();

            // ============================================================
            // Rate Limiting — حماية نموذج تسجيل الدخول من محاولات التخمين
            // (نفس سياسة الـ API: 10 محاولات في الدقيقة لكل عنوان IP)
            // ============================================================
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("login", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    // طلبات تسجيل الدخول: نُعيد المستخدم للنموذج مع رسالة عربية
                    // عبر TempData (كوكي مشفّر — لا يتطلب جلسة) بدل صفحة 429 الخام.
                    if (context.HttpContext.Request.Method == HttpMethods.Post &&
                        context.HttpContext.Request.Path.StartsWithSegments("/Account/Login"))
                    {
                        var factory = context.HttpContext.RequestServices
                            .GetRequiredService<ITempDataDictionaryFactory>();
                        var tempData = factory.GetTempData(context.HttpContext);
                        tempData[BaseController.TempDataError] = AppMessages.Common.TooManyAttempts;
                        tempData.Save();

                        context.HttpContext.Response.Redirect("/Account/Login");
                        return;
                    }

                    // باقي الطلبات: رفض عادي مخزّن في الـ response
                    var response = context.HttpContext.Response;
                    response.ContentType = "application/json";
                    await response.WriteAsJsonAsync(
                        new { success = false, message = AppMessages.Common.TooManyAttempts },
                        cancellationToken);
                };
            });

            // API Configuration
            builder.Services.Configure<ApiSettings>(
                builder.Configuration.GetSection("ApiSettings"));

            // Http Client — مهلة معقولة بدل الافتراضية الطويلة حتى لا تعلق الواجهة
            builder.Services.AddHttpClient<ApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Services Registration
            builder.Services.AddScoped<IAuthApiService, AuthApiService>();
            builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
            builder.Services.AddScoped<IOrdersApiService, OrdersApiService>();
            builder.Services.AddScoped<ILookupApiService, LookupApiService>();
            builder.Services.AddScoped<IFactoryApiService, FactoryApiService>();
            builder.Services.AddScoped<IUserApiService, UserApiService>();
            builder.Services.AddScoped<IConcreteTypeApiService, ConcreteTypeApiService>();
            builder.Services.AddScoped<IConcreteCatalogService, ConcreteCatalogService>();
            builder.Services.AddScoped<IClientApiService, ClientApiService>();
            builder.Services.AddScoped<IDriverApiService, DriverApiService>();
            builder.Services.AddScoped<ISettingsApiService, SettingsApiService>();
            builder.Services.AddScoped<IReportsApiService, ReportsApiService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            // أول وسيط في الـ pipeline: يسجّل كل طلب مكتمل (مسار/حالة/مدة/TraceId)
            // ويلتف حول معالج الأخطاء فيسجّل الطلب الفاشل مرة واحدة بالحالة الحقيقية.
            app.UseMiddleware<RequestLoggingMiddleware>();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
                app.UseHsts();
            }

            // ترويسات الأمان — قبل الملفات الثابتة والتوجيه حتى تغطي كل استجابة
            app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Rate Limiting (قبل المصادقة حتى تعمل السياسات المسماة على كل الطلبات)
            app.UseRateLimiter();

            // Session
            app.UseSession();

            // Authentication & Authorization (يجب أن تكون المصادقة قبل الصلاحيات)
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}