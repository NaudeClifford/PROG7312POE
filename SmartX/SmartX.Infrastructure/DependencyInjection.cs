using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure.Persistence.Mongo;
using SmartX.Infrastructure.Repositories;
using SmartX.Infrastructure.Authentication.Firebase;

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
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();


        return services;
    }
}