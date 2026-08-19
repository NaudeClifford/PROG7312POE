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

    // Sensors
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
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensors.");

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
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensor.");

        return result.Data;
    }

    // Telemetry
    public async Task<IReadOnlyList<TelemetryDto>>
        GetTelemetryBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Telemetry/sensor/{sensorId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<TelemetryDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve telemetry.");

        return result.Data ?? [];
    }

    public async Task<TelemetryDto?>
        GetLatestTelemetryBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Telemetry/sensor/{sensorId}/latest",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<TelemetryDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve latest telemetry.");

        return result.Data;
    }
    
    // Users
    public async Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/User/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<UserDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve user.");

        return result.Data;
    }

    public async Task<UserDto?> GetUserByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/User/{firebaseUid}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<UserDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve user.");

        return result.Data;
    }

    // Companies
    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Company",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<CompanyDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve companies.");

        return result.Data ?? [];
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Company/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<CompanyDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve company.");

        return result.Data;
    }

    // Gateways
    public async Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Gateway",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<GatewayDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve gateways.");

        return result.Data ?? [];
    }

    public async Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Gateway/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<GatewayDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve gateway.");

        return result.Data;
    }
}