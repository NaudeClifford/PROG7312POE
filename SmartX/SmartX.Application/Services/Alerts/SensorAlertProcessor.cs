
using SmartX.Domain.Enums;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Alerts;

public sealed class SensorAlertProcessor : ISensorAlertProcessor
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IAlertQueue _alertQueue;

    public SensorAlertProcessor(
        ISensorRepository sensorRepository,
        IAlertQueue alertQueue)
    {
        _sensorRepository = sensorRepository;
        _alertQueue = alertQueue;
    }

    public async Task<bool> ProcessAsync(
        DomainTelemetry telemetry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        var sensor = await _sensorRepository.GetByIdAsync(
            telemetry.SensorId,
            cancellationToken);

        if (sensor is null)
            return false;

        var messages = new List<string>();

        if (telemetry.Temperature >= 35)
            messages.Add(
                $"Temperature critically high: {telemetry.Temperature:F1} °C");

        if (telemetry.Voltage >= 240)
            messages.Add(
                $"Voltage critically high: {telemetry.Voltage:F1} V");

        if (telemetry.Current >= 5)
            messages.Add(
                $"Current critically high: {telemetry.Current:F1} A");

        string? newMessage = messages.Count > 0
            ? string.Join("; ", messages)
            : null;

        bool alertChanged = !string.Equals(
            sensor.ActiveAlertMessage,
            newMessage,
            StringComparison.OrdinalIgnoreCase);

        if (!alertChanged)
            return newMessage is not null;

        sensor.HasActiveAlert = newMessage is not null;
        sensor.ActiveAlertMessage = newMessage;

        sensor.ActiveAlertSeverity = newMessage is not null
            ? AlertSeverity.Critical
            : null;

        sensor.UpdatedAt = DateTime.UtcNow;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        if (newMessage is not null)
        {
            _alertQueue.Enqueue(new SensorAlert
            {
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                Message = newMessage,
                Severity = AlertSeverity.Critical,
                Timestamp = telemetry.Timestamp
            });
        }

        return newMessage is not null;
    }
}
