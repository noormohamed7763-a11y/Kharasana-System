using Kharasana.API.Middlewares;
using Kharasana.Application;
using Kharasana.Application.Common.Logging;
using Kharasana.Infrastructure;
using Kharasana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

namespace Kharasana.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // تسجيل دائم في ملفات (Logs/) — يحافظ على أخطاء الإنتاج بعد وقوعها
        builder.Logging.AddFileLogging();

        // ============================================================
        // 1. CORS — سياسة موثوقة من إعدادات التطبيق
        //    - في الإنتاج: أسماء النطاقات المُسموحة فقط من Cors:Origins (متغير بيئة/ملف خارجي)
        //    - في التطوير: السماح بكل المصادر (لتسهيل عمل Flutter محلياً)
        // ============================================================
        var corsOrigins = builder.Configuration
            .GetSection("Cors:Origins")
            .Get<string[]>() ?? Array.Empty<string>();

        // إنتاج بدون إعداد صريح = إيقاف فوري (fail-fast) — لا سماح مؤقت صامت
        if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "يجب ضبط Cors:Origins في الإنتاج. حدّده في appsettings.Production.json أو عبر متغيرات البيئة.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (corsOrigins.Length > 0)
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // وضع التطوير فقط: لكل المصادر بدون بيانات اعتماد
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        // Register Layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // ============================================================
        // 2. JWT Authentication — مع فحص صارم على المفتاح المُ졌ّر
        // ============================================================
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "مفتاح توقيع JWT غير مُعرّف (Jwt:Key). حددّه عبر appsettings أو متغير البيئة Jwt__Key.");
        }

        // تشديد: مفتاح أقصر من 32 حرفاً يعطي عشوائية ضعيفة وسهل الكسر
        if (jwtKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"Jwt:Key يجب ألا يقل عن 32 حرفاً لضمان عشوائية كافية (الطول الحالي: {jwtKey.Length}).");
        }

        var jwt = builder.Configuration.GetSection("Jwt");
        if (string.IsNullOrWhiteSpace(jwt["Issuer"]) ||
            string.IsNullOrWhiteSpace(jwt["Audience"]))
        {
            throw new InvalidOperationException(
                "يجب ضبط Jwt:Issuer و Jwt:Audience للتطبيق.");
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },

                    // صرامة أعلى: السماحة بالفرق الزمني 30 ثانية بدل الافتراضي (5 دقائق)
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();

        // ============================================================
        // 3. Rate Limiting — حماية نقاط الدخول الحساسة
        // ============================================================
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // حاجز عام: 100 طلب في الدقيقة لكل عنوان IP — يُطبَّق على كل النقاط قبل
            // السياسات المسماة، فتبقى نقاط الدخول الحساسة (login) بمعدّل أصغر
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // فلتر تسجيل الدخول: 10 محاولات كحد أقصى في دقيقة واحدة لكل عنوان IP
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        // ============================================================
        // 4. Health Checks — التحقق من صحة التطبيق
        // ============================================================
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<KharasanaDbContext>();

        // Controllers
        builder.Services.AddControllers();

        // API Explorer
        builder.Services.AddEndpointsApiExplorer();

        // Swagger
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Kharasana ERP API | واجهة برمجة التطبيقات",
                Version = "v1",
                Description = "واجهة برمجة تطبيقات نظام خرسانة للطلب المسبق للخرسانة الجاهزة — " +
                              "إدارة المصانع والأنواع والطلبات والتقارير. " +
                              "المصادقة عبر JWT Bearer: اضغط زر Authorize وأدخل التوكن بصيغة “Bearer {token}” ثم جرّب النقاط المحمية.",
                Contact = new OpenApiContact
                {
                    Name = "فريق تطوير خرسانة"
                }
            });

            // دمج تعليقات التوثيق العربية (XML) الخاصة بكل نقطة نهاية في وصف Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // ترقيم النقاط حسب اسم الـ Controller ثم مسارها لعرضٍ منظّم بدل الترتيب العشوائي
            options.TagActionsBy(api => new[] { api.ActionDescriptor.RouteValues["controller"] ?? "General" });
            options.OrderActionsBy(api => api.RelativePath ?? string.Empty);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "أدخل JWT Token بهذا الشكل:\n\nBearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kharasana ERP API v1");
                options.DocumentTitle = "Kharasana ERP API | واجهة برمجة التطبيقات";
                options.EnableTryItOutByDefault();      // تفعيل زر "Try it out" تلقائياً
                options.EnablePersistAuthorization();   // حفظ ما يُدخله المستخدم من توكن
                options.DisplayRequestDuration();       // عرض مدّة كل طلب بالمللي ثانية
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });
        }

        // ============================================================
        // 5. تفعيل CORS (يجب أن يكون قبل UseAuthentication)
        // ============================================================
        app.UseCors();

        // ============================================================
        // 6. Forwarded Headers — لتشغيل Rate Limiter و HTTPS بشكل صحيح خلف وسيط (Proxy)
        //    تحذير: في الإنتاج يجب ضبط KnownProxies / KnownNetworks لقبول
        //    X-Forwarded-* من مصادر موثوقة فقط — وإلا أمكن تزوير عنوان IP.
        // ============================================================
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto
        });

        app.UseRateLimiter();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
