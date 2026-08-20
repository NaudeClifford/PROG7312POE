using SmartX.Application.Commands.Company;
using SmartX.Application.Commands.Gateway;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Commands.Users;
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


    // ============================================================
    // SENSORS
    // ============================================================

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

    public async Task<Guid> CreateSensorAsync(
        CreateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/Sensors",
            command,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to create sensor.");

        return result.Data;
    }

    public async Task<bool> UpdateSensorAsync(
        UpdateSensorCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Sensors/{command.Id}",
            command,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to update sensor.");

        return result.Data;
    }

    public async Task<bool> DeleteSensorAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/Sensors/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to delete sensor.");

        return result.Data;
    }


    // ============================================================
    // TELEMETRY
    // ============================================================

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


    // ============================================================
    // USERS
    // ============================================================

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Users",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<UserDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to retrieve users.");

        return result.Data ?? [];
    }

    public async Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Users/{id}",
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
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return null;

        var response = await _httpClient.GetAsync(
            $"api/Users/{Uri.EscapeDataString(firebaseUid)}",
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

    public async Task<Guid> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/Users",
            command,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to create user.");

        return result.Data;
    }

    public async Task<bool> UpdateUserAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Users/{command.Id}",
            command,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to update user.");

        return result.Data;
    }

    public async Task<bool> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/Users/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to delete user.");

        return result.Data;
    }


    // ============================================================
    // COMPANIES
    // ============================================================

    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Companies",
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
            $"api/Companies/{id}",
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

    public async Task<Guid> CreateCompanyAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/Companies",
            command,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to create company.");

        return result.Data;
    }

    public async Task<bool> UpdateCompanyAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Companies/{command.Id}",
            command,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to update company.");

        return result.Data;
    }

    public async Task<bool> DeleteCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/Companies/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to delete company.");

        return result.Data;
    }


    // ============================================================
    // GATEWAYS
    // ============================================================

    public async Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            "api/Gateways",
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
            $"api/Gateways/{id}",
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

    public async Task<IReadOnlyList<GatewayDto>>
        GetGatewaysByCompanyIdAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/Gateways/company/{companyId}",
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

    public async Task<Guid> CreateGatewayAsync(
        CreateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/Gateways",
            command,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to create gateway.");

        return result.Data;
    }

    public async Task<bool> UpdateGatewayAsync(
        UpdateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Gateways/{command.Id}",
            command,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to update gateway.");

        return result.Data;
    }

    public async Task<bool> DeleteGatewayAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/Gateways/{id}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode || !result.Success)
            throw new InvalidOperationException(
                result.Error ?? "Failed to delete gateway.");

        return result.Data;
    }
}