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
        // 1. Order Services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderCommandService, OrderCommandService>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
        services.AddScoped<IOrderHelperService, OrderHelperService>();

        // 2. Factory & Registration Services
        services.AddScoped<IFactoryService, FactoryService>();
        services.AddScoped<IFactoryRegistrationRequestService, FactoryRegistrationRequestService>();
        services.AddScoped<IConcreteTypeService, ConcreteTypeService>();

        // 3. User & Auth Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // 4. Other Services
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>();

        return services;
    }
}