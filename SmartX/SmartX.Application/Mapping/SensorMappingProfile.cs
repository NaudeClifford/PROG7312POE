using AutoMapper;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Mapping;

public class SensorMappingProfile : Profile
{
    public SensorMappingProfile()
    {
        CreateMap<Sensor, SensorDto>()
            .ForMember(
                dest => dest.Category,
                opt => opt.MapFrom(src => (int)src.Category));

        CreateMap<Telemetry, TelemetryDto>();

    }
}