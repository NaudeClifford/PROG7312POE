using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Services;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure.Authentication.Firebase;
using SmartX.Infrastructure.Persistence.Mongo;
using SmartX.Infrastructure.Repositories;

namespace SmartX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FirebaseOptions>(configuration.GetSection("Firebase"));

        services.AddSingleton<MongoContext>();
        services.AddSingleton<FirebaseAuthService>();

        services.AddScoped<ISensorRepository, JsonSensorRepository>();
        services.AddScoped<ITelemetryRepository, JsonTelemetryRepository>();
        services.AddScoped<IUserRepository, JsonUserRepository>();
        services.AddScoped<ICompanyRepository, JsonCompanyRepository>();
        services.AddScoped<IGatewayRepository, JsonGatewayRepository>();
        services.AddScoped<ISensorLogFileRepository, JsonSensorLogFileRepository>();
        services.AddScoped<IAuditLogRepository, JsonAuditLogRepository>();

        return services;
    }
}