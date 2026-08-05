using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Queries.Sensors;
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
        services.AddScoped<UpdateSensorHandler>();
        services.AddScoped<DeleteSensorHandler>();

        return services;
    }
}