using SmartX.Domain.Entities;
using SmartX.Shared;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace SmartX.WPF.Services.Api;

public class SmartXApiClient(
    HttpClient httpClient) : ISmartXApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Sensors",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<SensorDto>>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensors.");
        }

        return result.Data ?? [];
    }

    public async Task<SensorDto?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Sensors/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<SensorDto>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensor.");
        }

        return result.Data;
    }

    public async Task<IReadOnlyList<TelemetryDto>> GetTelemetryBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/Telemetry/{sensorId}",
                    cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return [];

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<TelemetryDto>>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve Telemetry.");
        }

        return result.Data ?? [];
    }

    public async Task<TelemetryDto?> GetLatestTelemetryBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/Telemetry/Latest/{sensorId}",
                    cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<TelemetryDto>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve lastest Telemetry.");
        }

        return result.Data;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/User/{id}",
                    cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<UserDto>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve User.");
        }

        return result.Data;
    }

    public async Task<UserDto?> GetUserByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/user/{firebaseUid}",
                    cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<UserDto>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve User.");
        }

        return result.Data;
    }
}
