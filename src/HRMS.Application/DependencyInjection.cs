using FluentValidation;
using HRMS.Application.Interfaces;
using HRMS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IHrUserService, HrUserService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        // Registers every AbstractValidator<T> found in this assembly (see Validators/*).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
