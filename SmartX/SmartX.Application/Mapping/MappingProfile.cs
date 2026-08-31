using AutoMapper;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //Sensors
        CreateMap<Sensor, SensorDto>()
            .ForMember(
                dest => dest.Category,
                opt => opt.MapFrom(src => (int)src.Category));

        //Telemetry
        CreateMap<Telemetry, TelemetryDto>();

        //Users
        CreateMap<User, UserDto>();

        //Company
        CreateMap<Company, CompanyDto>();

        //Gateway
        CreateMap<Gateway, GatewayDto>();

        //SensorLogFile
        CreateMap<SensorLogFile, SensorLogFileDto>();


        CreateMap<CompanyDto, Company>();

    }
}