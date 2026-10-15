using MapsterMapper;
using SmartX.Domain.Entities;
using SmartX.WPF.Repositories.Local;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WPF.Services.PredictiveEngine
{
    public sealed class PredictiveSuggestionService(
        ILocalTelemetryCache telemetryCache,
        IPredictiveEngine predictiveEngine,
        IMapper mapper)
    {
        private readonly ILocalTelemetryCache _telemetryCache = telemetryCache;
        private readonly IPredictiveEngine _predictiveEngine = predictiveEngine;
        private readonly IMapper _mapper = mapper;

        public async Task<IReadOnlyList<PredictiveSuggestion>> GetWarningsAsync(
            CancellationToken cancellationToken = default)
        {

            var telemetry = await _telemetryCache.GetAllAsync(
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var dto = _mapper.Map<IReadOnlyList<PredictiveSuggestion>>(telemetry);

            return _predictiveEngine.GenerateWarnings(dto);
        }
    }
