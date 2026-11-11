using SmartX.Shared.DTOs;

public interface IPredictiveSuggestionProvider
{
    Task<IReadOnlyList<PredictiveSuggestionDto>>
        GetSuggestionsAsync(
            Guid sensorId,
            Guid userId,
            CancellationToken cancellationToken = default);
}