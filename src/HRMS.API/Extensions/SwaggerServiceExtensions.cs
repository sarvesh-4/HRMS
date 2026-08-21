using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace HRMS.API.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "HRMS API (POC)",
                Version = "v1",
                Description = "Human Resource Management System — proof of concept API. " +
                              "Flow: Register -> Login -> Create Organization (becomes Admin) -> " +
                              "Admin adds HR users -> HR manages employees."
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Paste ONLY the JWT token (no 'Bearer ' prefix — Swagger adds it automatically).",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtScheme, Array.Empty<string>() }
            });
        });

        return services;
    }
}

/// <summary>Small local alias so this file doesn't need an extra using for one constant.</summary>
internal static class JwtBearerDefaults
{
    public const string AuthenticationScheme = "Bearer";
}
