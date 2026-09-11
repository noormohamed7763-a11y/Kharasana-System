using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Infrastructure.Repositories;
using Kharasana.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Repositories
        services.AddScoped<IFactoryRepository, FactoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConcreteTypeRepository, ConcreteTypeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Authentication Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();

        return services;
    }
}