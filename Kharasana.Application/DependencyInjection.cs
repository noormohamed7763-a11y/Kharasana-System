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
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderCommandService, OrderCommandService>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
        services.AddScoped<IOrderHelperService, OrderHelperService>();
        services.AddScoped<IOrderHelperService, OrderHelperService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IFactoryRegistrationRequestService, FactoryRegistrationRequestService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<RegisterClientValidator>();

        return services;
    }
}