
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Commands.Sensor;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Interfaces.Mesh;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Queries.Users;
using SmartX.Application.Services;
using SmartX.Application.Services.Alerts;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Services.Mesh;
using SmartX.Application.Services.Mesh.Discovery;
using SmartX.Application.Services.Mesh.Graph;
using SmartX.Application.Services.Mesh.Logging;
using SmartX.Application.Services.Mesh.Monitoring;
using SmartX.Application.Services.Mesh.Quality;
using SmartX.Application.Services.Mesh.Routing;
using SmartX.Application.Services.Mesh.SpanningTree;
using SmartX.Application.Services.Mesh.Topology;
using SmartX.Application.Services.Mesh.Traversal;
using SmartX.Application.Services.PredictiveEngine;
using SmartX.Application.Services.Registration;
using SmartX.Application.Services.Sensors;
using SmartX.Application.Services.Sensors.Command;
using SmartX.Application.Services.Sensors.Interfaces;
using SmartX.Application.Services.Telemetry;
using SmartX.Application.Services.Telemetry.Structures;
using SmartX.Application.Validators;
using SmartX.Application.Validators.Company;
using SmartX.Application.Validators.Sensor;
using SmartX.Domain.Interfaces.Mesh;
using SmartX.Infrastructure.Mesh.Simulation;
using SmartX.Shared.Mapping;

namespace SmartX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateSensorValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateCompanyValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateGatewayValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateTelemetryValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateUserBehaviorValidator>();

        // CRUD services
        services.AddScoped<SensorCrudService>();
        services.AddScoped<GatewayCrudService>();
        services.AddScoped<CompanyCrudService>();
        services.AddScoped<UserCrudService>();
        services.AddScoped<SensorLogFileCrudService>();
        services.AddScoped<UserBehaviorCrudService>();

        // Telemetry
        services.AddSingleton<ITelemetryStream, TelemetryStream>();

        services.AddScoped<GetTelemetryBySensorHandler>();
        services.AddScoped<GetLatestTelemetryBySensorHandler>();
        services.AddScoped<GetTelemetryByDateRangeHandler>();
        services.AddScoped<GetTelemetryByIdHandler>();
        services.AddScoped<CreateTelemetryHandler>();
        services.AddScoped<TelemetryService>();

        // Telemetry indexes
        services.AddSingleton<TelemetryHistoryStore>();
        services.AddSingleton<TelemetryAvlTreeService>();
        services.AddHostedService<TelemetryHistoryInitializer>();

        // AVL query handler
        services.AddScoped<GetTelemetryAvlHistoryHandler>();

        // User handlers
        services.AddScoped<GetUserByFirebaseUidHandler>();
        services.AddScoped<GetUsersByCompanyIdHandler>();

        // General services
        services.AddScoped<AuditLogService>();
        services.AddScoped<RegistrationService>();

        // Sensor commands
        services.AddSingleton<ManualStack<SensorCommand>>();
        services.AddScoped<ISensorCommandService, SensorCommandService>();
        services.AddScoped<SendSensorCommandHandler>();
        services.AddScoped<SensorRegistry>();

        // Predictive engine
        services.AddScoped<IPredictiveEngine, PredictiveEngine>();
        services.AddScoped<IAnomalyRecommendationService, StatisticalAnomalyRecommendationService>();

        // Alerts
        services.AddSingleton<IAlertQueue, AlertQueue>();

        services.AddScoped<ISensorAlertProcessor, SensorAlertProcessor>();
        services.AddScoped<SensorProcessingService>();

        // Mapping
        services.AddSmartXMapping();

        //Mesh services
        services.AddScoped<MeshGraphBuilder>();
        services.AddScoped<MeshRoutingService>();
        services.AddScoped<MeshTraversalService>();
        services.AddScoped<MinimumSpanningTreeService>();
        services.AddScoped<MeshNetworkService>();
        services.AddScoped<MeshMonitoringService>();

        services.AddScoped<IMeshLinkQualityService, MeshLinkQualityService>();
        services.AddScoped<IMeshLogService, MeshLogService>();
        services.AddScoped<IMeshTopologyService, MeshTopologyService>();
        services.AddScoped<IMeshDiscoveryService, MeshDiscoveryService>();
        services.AddScoped<IMeshHealthService, MeshHealthService>();
        services.AddScoped<IMeshMonitoringService, MeshMonitoringService>();
        services.AddScoped<IMeshConnectionDiscoveryService, MeshConnectionDiscoveryService>();
        // services.AddScoped<IMeshGatewayConnectionClient, YourGatewayConnectionClient>();
        services.AddScoped<IMeshGatewayConnectionClient, SimulatedMeshGatewayConnectionClient>();
        services.AddScoped<IMeshConnectionDiscoveryService, MeshConnectionDiscoveryService>();
        services.AddScoped<IMeshConnectionDiscoveryProvider, SimulatedMeshConnectionDiscoveryProvider>();

        services.AddSingleton(
            new MeshSensorMonitoringOptions
            {
                HeartbeatTimeoutSeconds = 60,
                MonitoringIntervalSeconds = 15
            });

        services.AddScoped<IMeshSensorHeartbeatService, MeshSensorHeartbeatService>();
        services.AddScoped<IMeshConnectionDiscoveryService, MeshConnectionDiscoveryService>();

        services.AddSingleton(new MeshMonitoringOptions());

        services.AddHostedService<MeshSensorHeartbeatBackgroundService>();

        services.AddScoped<IMeshLinkMonitoringService, MeshLinkMonitoringService>();
        services.AddHostedService<SimulatedMeshHeartbeatBackgroundService>();
        return services;
    }
}
