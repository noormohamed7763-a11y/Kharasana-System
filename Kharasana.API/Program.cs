using Kharasana.API.Middlewares;
using Kharasana.Application;
using Kharasana.Application.Common.Logging;
using Kharasana.Infrastructure;
using Kharasana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowConfiguredOrigins", policy =>
            {
                if (corsOrigins.Length > 0)
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else if (builder.Environment.IsDevelopment())
                {
                    // وضع التطوير فقط
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    // إنتاج بدون إعداد صريح — تسجيل تحذير والسماح مؤقتاً لتجنب تعطل بدء التشغيل.
                    // المطور المسؤول يجب أن يحدد Cors:Origins عبر متغيرات البيئة عند النشر.
                    // لا نستخدم BuildServiceProvider() هنا لتجنب تحذير ASP0000 —
                    // نستخدم factory delegate داخل AddCors للوصول إلى ILogger لاحقاً.
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                    // سيتم تسجيل التحذير بعد بناء التطبيق عبر Middleware أو IStartupFilter.
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

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = builder.Configuration.GetSection("Jwt");

                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },

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
                Title = "Kharasana API",
                Version = "v1",
                Description = "Concrete Ordering System API"
            });

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

        // CORS: تسجيل تحذير إن لم يتم تحديد Origins في الإنتاج (بدون BuildServiceProvider)
        var corsOriginsCheck = app.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (corsOriginsCheck.Length == 0 && !app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning("CORS: لم يتم تحديد Cors:Origins في الإنتاج. سيتم السماح بكل المصادر مؤقتاً — أصلح هذا قبل الإطلاق.");
        }

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kharasana API v1");
            });
        }

        // ============================================================
        // 5. تفعيل CORS (يجب أن يكون قبل UseAuthentication)
        // ============================================================
        app.UseCors("AllowConfiguredOrigins");

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
