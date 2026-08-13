using SmartX.Domain.Entities;
using SmartX.Shared.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace SmartX.WPF.Services.Api;

public class SmartXApiClient(
    HttpClient httpClient) : ISmartXApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<Sensor>> GetSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Sensors",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<Sensor>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensors.");

        return result.Data ?? [];
    }

    public async Task<Sensor?> GetSensorByIdAsync(
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
                Result<Sensor>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve sensor.");

        return result.Data;
    }
}