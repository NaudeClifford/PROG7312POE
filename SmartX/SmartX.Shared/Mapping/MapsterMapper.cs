using Mapster;

namespace SmartX.Shared.Mapping;

public sealed class MapsterMapper : IMapper
{
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Adapt<TDestination>();
    }

    public TDestination Map<TSource, TDestination>(
        TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Adapt<TDestination>();
    }

    public void Map<TSource, TDestination>(
        TSource source,
        TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        source.Adapt(destination);
    }
}
