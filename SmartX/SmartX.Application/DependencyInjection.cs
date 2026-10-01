using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.API.Services.Sensors;
using SmartX.Application.Commands.Sensor;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Queries.Users;
using SmartX.Application.Services;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Services.Registration;
using SmartX.Application.Services.Sensors;
using SmartX.Application.Services.Telemetry;
using SmartX.Application.Validators;
using SmartX.Application.Validators.Company;
using SmartX.Application.Validators.Sensor;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Mapping;

namespace SmartX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {

        //Validators
        services.AddValidatorsFromAssemblyContaining<CreateSensorValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateCompanyValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateGatewayValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateTelemetryValidator>();


        // CRUD services
        services.AddScoped<SensorCrudService>();
        services.AddScoped<GatewayCrudService>();
        services.AddScoped<CompanyCrudService>();
        services.AddScoped<UserCrudService>();
        services.AddScoped<SensorLogFileCrudService>();

        //Telemetry stream
        services.AddSingleton<ITelemetryStream, TelemetryStream>();

        //Telemetry handlers
        services.AddScoped<GetTelemetryBySensorHandler>();
        services.AddScoped<GetLatestTelemetryBySensorHandler>();
        services.AddScoped<GetTelemetryByDateRangeHandler>();
        services.AddScoped<GetTelemetryByIdHandler>();
        services.AddScoped<CreateTelemetryHandler>();
        services.AddScoped<TelemetryHistoryStore>();
        services.AddScoped<ITelemetryStream, TelemetryStream>();


        //User handlers
        services.AddScoped<GetUserByFirebaseUidHandler>();
        services.AddScoped<GetUsersByCompanyIdHandler>();

        //Services
        services.AddScoped<AuditLogService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<ISensorCommandService, SensorCommandService>();
        services.AddScoped<SendSensorCommandHandler>();

        services.AddSmartXMapping();

        return services;
    }
}