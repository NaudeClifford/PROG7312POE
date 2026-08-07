using AutoMapper;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Sensors;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartX.Application.Mapping;

public class SensorMappingProfile : Profile
{
    public SensorMappingProfile()
    {
        CreateMap<Sensor, SensorDto>()
            .ForMember(
                dest => dest.Category,
                opt => opt.MapFrom(src => (int)src.Category));
    }
}