
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Commands.Sensor;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Queries.Users;
using SmartX.Application.Services;
using SmartX.Application.Services.Alerts;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Services.PredictiveEngine;
using SmartX.Application.Services.Registration;
using SmartX.Application.Services.Sensors;
using SmartX.Application.Services.Sensors.Command;
using SmartX.Application.Services.Sensors.Interfaces;
using SmartX.Application.Services.Telemetry;
using SmartX.Application.Validators;
using SmartX.Application.Validators.Company;
using SmartX.Application.Validators.Sensor;
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
        services.AddScoped<ITelemetryStream, TelemetryStream>();

        services.AddScoped<GetTelemetryBySensorHandler>();
        services.AddScoped<GetLatestTelemetryBySensorHandler>();
        services.AddScoped<GetTelemetryByDateRangeHandler>();
        services.AddScoped<GetTelemetryByIdHandler>();
        services.AddScoped<CreateTelemetryHandler>();
        services.AddScoped<TelemetryHistoryStore>();
        services.AddScoped<TelemetryService>();

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
        services.AddScoped<AlertQueue>();

        services.AddScoped<IAlertQueue>(sp => sp.GetRequiredService<AlertQueue>());

        services.AddScoped<ISensorAlertProcessor, SensorAlertProcessor>();
        services.AddScoped<SensorProcessingService>();

        // Mapping
        services.AddSmartXMapping();

        return services;
    }
}
