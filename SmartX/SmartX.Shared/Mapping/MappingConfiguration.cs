using Mapster;
using SmartX.Domain.Entities;
using SmartX.Domain.Entities.Mesh;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Mesh;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Shared.Mapping;

public static class MappingConfiguration
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;

        // SENSOR

        TypeAdapterConfig<Sensor, SensorDto>
            .NewConfig()
            .Map(dest => dest.Category,
                 src => (int)src.Category);

        // TELEMETRY

        TypeAdapterConfig<Telemetry, TelemetryDto>
            .NewConfig();

        // USER

        TypeAdapterConfig<User, UserDto>
            .NewConfig();

        // COMPANY

        TypeAdapterConfig<Company, CompanyDto>
            .NewConfig();

        TypeAdapterConfig<CompanyDto, Company>
            .NewConfig();

        TypeAdapterConfig<CompanyConfiguration, CompanyConfigurationDto>
            .NewConfig();

        // GATEWAY

        TypeAdapterConfig<Gateway, GatewayDto>
            .NewConfig();

        // SENSOR LOG FILE

        TypeAdapterConfig<SensorLogFile, SensorLogFileDto>
            .NewConfig();

        // USER BEHAVIOUR

        TypeAdapterConfig<UserBehavior, UserBehaviorDto>
            .NewConfig();

        TypeAdapterConfig<UserBehaviorDto, UserBehavior>
            .NewConfig();

        // MESH

        TypeAdapterConfig<MeshNode, MeshNodeDto>
            .NewConfig();

        TypeAdapterConfig<MeshLink, MeshLinkDto>
            .NewConfig();

        TypeAdapterConfig<MeshConnection, MeshConnectionDto>
            .NewConfig();

        TypeAdapterConfig<MeshLog, MeshLogDto>
            .NewConfig();

        TypeAdapterConfig<MeshRoute, MeshRouteDto>
            .NewConfig();
    }
}