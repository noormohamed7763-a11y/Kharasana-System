using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Infrastructure.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database — فحص صارم: يجب أن تكون ConnectionString مُعرّفة عند بدء التشغيل
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection String غير مُعرّف (DefaultConnection). حددّها عبر appsettings أو متغيرات البيئة.");
        }

        services.AddDbContext<KharasanaDbContext>(options =>
            options.UseSqlServer(connectionString));
            
        services.Configure<JwtSettings>(
    configuration.GetSection(JwtSettings.SectionName));

        // Unit Of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories — مطلوبة من قبل ClientStatementService
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Authentication Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddScoped<IImageCleanupService, ImageCleanupService>();

        services.AddHttpClient<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IRawMaterialService, RawMaterialService>();
        services.AddScoped<IProductionBatchService, ProductionBatchService>();

        return services;
    }
}