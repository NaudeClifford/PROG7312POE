using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Authentication;
using SmartX.Application.Interfaces.Mesh;
using SmartX.Application.Interfaces.Sensors;
using SmartX.Application.Services.Mesh.Discovery;
using SmartX.Application.Services.Sensors;
using SmartX.Domain.Interfaces;
using SmartX.Domain.Interfaces.Mesh;
using SmartX.Infrastructure.Authentication.Firebase;
using SmartX.Infrastructure.Persistence.Mongo;
using SmartX.Infrastructure.Repositories;
using SmartX.Infrastructure.Repositories.Mesh;
using SmartX.Infrastructure.Sensors;

namespace SmartX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Firebase configuration
        services.Configure<FirebaseOptions>(
            configuration.GetSection("Firebase"));

        // Infrastructure services
        services.AddSingleton<MongoContext>();

        services.AddSingleton<FirebaseAuthService>();

        services.AddSingleton<IFirebaseTokenService>(
            sp => sp.GetRequiredService<FirebaseAuthService>());

        // Repositories
        services.AddScoped<ISensorRepository, JsonSensorRepository>();
        services.AddScoped<ITelemetryRepository, JsonTelemetryRepository>();
        services.AddScoped<IUserRepository, JsonUserRepository>();
        services.AddScoped<ICompanyRepository, JsonCompanyRepository>();
        services.AddScoped<IGatewayRepository, JsonGatewayRepository>();
        services.AddScoped<ISensorLogFileRepository, JsonSensorLogFileRepository>();
        services.AddScoped<IAuditLogRepository, JsonAuditLogRepository>();
        services.AddScoped<ICompanyConfigurationRepository, JsonCompanyConfigurationRepository>();
        services.AddScoped<IUserBehaviorRepository, JsonUserBehaviorRepository>();
        services.AddScoped<ISensorConnectionService, SensorConnectionManager>();
        services.AddHostedService<SensorConnectionWorker>();

        services.AddSingleton<ISensorProtocolAdapter, SimulationSensorAdapter>();

        //Mesh
        services.AddSingleton<IMeshNodeRepository, JsonMeshNodeRepository>();
        services.AddSingleton<IMeshLinkRepository, JsonMeshLinkRepository>();
        services.AddSingleton<IMeshConnectionRepository, JsonMeshConnectionRepository>();
        services.AddSingleton<IMeshRouteRepository, JsonMeshRouteRepository>();
        services.AddSingleton<IMeshLogRepository, JsonMeshLogRepository>();
        services.AddScoped<IMeshDiscoveryProvider, MeshDiscoveryProvider>();
        services.AddScoped<IMeshGatewayDiscoveryClient, MeshGatewayDiscoveryClient>();

        return services;
    }
}
