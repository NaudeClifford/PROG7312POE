using SmartX.Domain.Entities;
using SmartX.WPF.Services.PredictiveEngine;
using System.Collections.ObjectModel;

namespace SmartX.WPF.ViewModels.Alerts;

public sealed class PredictiveWarningViewModel
{
    private readonly LivePredictiveWarningService _warningService;

    public ObservableCollection<PredictiveSuggestion> Suggestions
        => _warningService.Warnings;

    public PredictiveWarningViewModel(
        LivePredictiveWarningService warningService)
    {
        _warningService = warningService;
    }

    public void Refresh()
    {
        _warningService.Refresh();
    }

    public void Clear()
    {
        _warningService.Clear();
    }
}