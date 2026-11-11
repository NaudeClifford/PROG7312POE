using SmartX.Shared.DTOs;
using System.Collections.ObjectModel;

namespace SmartX.WPF.ViewModels.Alerts;

public sealed class PredictiveWarningViewModel
{
    public ObservableCollection<PredictiveSuggestionDto>
        Suggestions
    { get; } = [];

    public void SetWarnings(
        IEnumerable<PredictiveSuggestionDto> warnings)
    {
        Suggestions.Clear();

        foreach (var warning in warnings)
            Suggestions.Add(warning);
    }

    public void Clear()
    {
        Suggestions.Clear();
    }
}