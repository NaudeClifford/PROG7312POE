using Mapster;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Shared.Mapping;

public static class MapsterConfig
{
    private static bool _configured;

    public static void Register()
    {
        if (_configured)
            return;

        _configured = true;

        TypeAdapterConfig<User, UserDto>
            .NewConfig();

        TypeAdapterConfig<Company, CompanyDto>
            .NewConfig();

        TypeAdapterConfig<Gateway, GatewayDto>
            .NewConfig();

        TypeAdapterConfig<Sensor, SensorDto>
            .NewConfig();

        TypeAdapterConfig<Telemetry, TelemetryDto>
            .NewConfig();

        TypeAdapterConfig<SensorLogFile, SensorLogFileDto> .NewConfig();

        TypeAdapterConfig<CompanyConfiguration, CompanyConfigurationDto>
            .NewConfig();   


    }
}
