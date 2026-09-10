using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace SmartX.Shared.Mapping;

public static class MappingServiceCollectionExtensions
{
    public static IServiceCollection AddSmartXMapping(
        this IServiceCollection services)
    {
        MappingConfiguration.Configure();

        services.AddSingleton<IMapper, MapsterMapper>();

        return services;
    }
}
