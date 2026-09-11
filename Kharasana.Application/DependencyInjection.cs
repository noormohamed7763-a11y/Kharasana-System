using FluentValidation;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Application.Validators.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Kharasana.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IFactoryService, FactoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConcreteTypeService, ConcreteTypeService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>(); // ✅ تمت إضافة هذا السطر
        services.AddScoped<IReportService, ReportService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>();

        return services;
    }
}