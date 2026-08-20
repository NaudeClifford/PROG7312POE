using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Commands.Gateway;
using SmartX.Application.Commands.Company;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Commands.Users;
using SmartX.Application.Mapping;
using SmartX.Application.Queries.Company;
using SmartX.Application.Queries.Gateway;
using SmartX.Application.Queries.Sensors;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Queries.Users;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Validators;
using SmartX.Application.Commands.Sensors;

namespace SmartX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateSensorCommandValidator>();

        // CRUD services
        services.AddScoped<SensorCrudService>();
        services.AddScoped<GatewayCrudService>();
        services.AddScoped<CompanyCrudService>();
        services.AddScoped<UserCrudService>();

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
        services.AddScoped<GetUserByFirebaseUidHandler>();


        //Company
        services.AddScoped<CreateCompanyHandler>();
        services.AddScoped<GetCompanysHandler>();
        services.AddScoped<GetCompanyByIdHandler>();
        services.AddScoped<UpdateCompanyHandler>();
        services.AddScoped<DeleteCompanyHandler>();

        //Gateway
        services.AddScoped<CreateGatewayHandler>();
        services.AddScoped<GetGatewaysHandler>();
        services.AddScoped<GetGatewayByIdHandler>();
        services.AddScoped<UpdateGatewayHandler>();
        services.AddScoped<DeleteGatewayHandler>();


        //Auto Mapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return services;
    }
}