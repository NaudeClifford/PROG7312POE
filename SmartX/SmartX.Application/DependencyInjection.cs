using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Commands.Users;
using SmartX.Application.Mapping;
using SmartX.Application.Queries.Sensors;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Queries.Users;
using SmartX.Application.Validators;

namespace SmartX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateSensorCommandValidator>();

        //Sensor handlers
        services.AddScoped<CreateSensorHandler>();
        services.AddScoped<GetSensorsHandler>();
        services.AddScoped<GetSensorByIdHandler>();
        services.AddScoped<UpdateSensorHandler>();
        services.AddScoped<DeleteSensorHandler>();

        //Telemetry handlers
        services.AddScoped<CreateTelemetryHandler>();
        services.AddScoped<GetTelemetryByIdHandler>();
        services.AddScoped<GetTelemetryBySensorHandler>();
        services.AddScoped<GetLatestTelemetryBySensorHandler>();
        services.AddScoped<GetTelemetryByDateRangeHandler>();

        //User handlers
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();

        services.AddScoped<GetUsersHandler>();
        services.AddScoped<GetUserByIdHandler>();

        //Auto Mapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return services;
    }
}