using Mapster;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Shared.Mapping;

public static class MappingConfiguration
{
    public static void Configure()
    {
        // Sensor
        TypeAdapterConfig<Sensor, SensorDto>
            .NewConfig()
            .Map(
                dest => dest.Category,
                src => (int)src.Category);

        // Telemetry
        TypeAdapterConfig<Telemetry, TelemetryDto>
            .NewConfig();

        // User
        TypeAdapterConfig<User, UserDto>
            .NewConfig();

        // Company
        TypeAdapterConfig<Company, CompanyDto>
            .NewConfig();

        // Gateway
        TypeAdapterConfig<Gateway, GatewayDto>
            .NewConfig();

        // Sensor log file
        TypeAdapterConfig<SensorLogFile, SensorLogFileDto>
            .NewConfig();

        // Company DTO -> Entity
        TypeAdapterConfig<CompanyDto, Company>
            .NewConfig();

        // Company configuration
        TypeAdapterConfig<CompanyConfiguration, CompanyConfigurationDto>
            .NewConfig();
    }
}
