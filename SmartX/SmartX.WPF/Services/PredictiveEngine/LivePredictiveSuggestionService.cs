using SmartX.Application.Services.Alerts;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Mapping;
using SmartX.WPF.ViewModels.Telemetry;
using System.Collections.ObjectModel;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class LivePredictiveSuggestionService
{
    private readonly IPredictiveEngine _predictiveEngine;
    private readonly IMapper _mapper;
    private readonly AlertQueue _alertQueue;
    private readonly TelemetryCollection _telemetryCollection;
    public ObservableCollection<PredictiveSuggestion> Suggestions { get; }
        = new();

    public LivePredictiveSuggestionService(
        IPredictiveEngine predictiveEngine,
        IMapper mapper,
        AlertQueue alertQueue, TelemetryCollection telemetryCollection)
    {
        _predictiveEngine = predictiveEngine;
        _mapper = mapper;
        _alertQueue = alertQueue;
        _telemetryCollection = telemetryCollection;
    }

    public void Refresh(IEnumerable<Telemetry> telemetry)
    {
        var telemetryDtos =
            _mapper.Map<List<TelemetryDto>>(telemetry);

        var generated =
            _predictiveEngine.GenerateSuggestions(
                telemetryDtos,
                _alertQueue.GetAlerts());

        Suggestions.Clear();

        foreach (var suggestion in generated)
        {
            Suggestions.Add(suggestion);
        }
    }

    public void Clear()
    {
        Suggestions.Clear();
    }
}