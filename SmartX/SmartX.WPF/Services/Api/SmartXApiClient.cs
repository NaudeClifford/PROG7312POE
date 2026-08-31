using SmartX.Application.Requests.Company;
using SmartX.Application.Requests.Gateway;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Requests.User;

using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using SmartX.WPF.Services.Session;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartX.WPF.Services.Api;

public class SmartXApiClient(
    HttpClient httpClient,
    SmartXSession session) : ISmartXApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly SmartXSession _session = session;
        


    // AUTHENTICATION

    private void AddAuthenticationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (string.IsNullOrWhiteSpace(_session.IdToken))
        {
            throw new InvalidOperationException(
                "Session IdToken is empty.");
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _session.IdToken);
    }



    // HEALTH

    public async Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            AddAuthenticationHeader();

            using var response =
                await _httpClient.GetAsync(
                    "api/health",
                    cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    // SENSORS - CRUD

    public async Task<IReadOnlyList<SensorDto>>
        GetSensorsAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
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
                result.Error ??
                "Failed to retrieve sensors.");

        return result.Data ?? [];
    }


    public async Task<SensorDto?>
        GetSensorByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Sensors/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

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
                result.Error ??
                "Failed to retrieve sensor.");

        return result.Data;
    }


    public async Task<Guid>
        CreateSensorAsync(
            CreateSensorRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/Sensors",
                request,
                cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to create sensor.");
        }

        return result.Data;
    }


    public async Task<bool>
        UpdateSensorAsync(
            UpdateSensorRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/Sensors/{request.Id}",
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to update sensor.");
        }

        return result.Data;
    }


    public async Task<bool>
        DeleteSensorAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.DeleteAsync(
                $"api/Sensors/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to delete sensor.");
        }

        return result.Data;
    }


    // SENSOR LOG FILES
    public async Task<IReadOnlyList<SensorLogFileDto>>
        GetSensorLogFilesAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/SensorLogFiles/sensor/{sensorId}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<SensorLogFileDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve sensor log files.");

        return result.Data ?? [];
    }


    public async Task<SensorLogFileUploadResultDto>
        UploadSensorLogFileAsync(
            Guid sensorId,
            string fileName,
            Stream fileStream,
            string contentType,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var content =
            new MultipartFormDataContent();

        using var fileContent =
            new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        content.Add(
            fileContent,
            "file",
            fileName);

        content.Add(
            new StringContent(sensorId.ToString()),
            "sensorId");

        var response =
            await _httpClient.PostAsync(
                "api/SensorLogFiles",
                content,
                cancellationToken);

        var responseText =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new SensorLogFileUploadResultDto
            {
                Success = false,
                Error = string.IsNullOrWhiteSpace(responseText)
                    ? $"API returned {(int)response.StatusCode}."
                    : responseText
            };
        }

        var result =
            System.Text.Json.JsonSerializer.Deserialize<
                SensorLogFileUploadResultDto>(
                    responseText,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

        return result ??
            new SensorLogFileUploadResultDto
            {
                Success = false,
                Error =
                    "The API returned an empty upload response."
            };
    }


    // TELEMETRY - CQRS
    public async Task<TelemetryDto?>
        GetTelemetryByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Telemetry/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<TelemetryDto?>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve telemetry.");

        return result.Data;
    }


    public async Task<IReadOnlyList<TelemetryDto>>
        GetTelemetryBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Telemetry/sensor/{sensorId}",
                cancellationToken);

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
                result.Error ??
                "Failed to retrieve telemetry.");

        return result.Data ?? [];
    }


    public async Task<TelemetryDto?>
        GetLatestTelemetryBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Telemetry/sensor/{sensorId}/latest",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<TelemetryDto?>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve latest telemetry.");

        return result.Data;
    }


    public async Task<IReadOnlyList<TelemetryDto>>
        GetTelemetryByDateRangeAsync(
            Guid sensorId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var url =
            $"api/Telemetry/sensor/{sensorId}/history" +
            $"?from={Uri.EscapeDataString(from.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("O"))}";

        var response =
            await _httpClient.GetAsync(
                url,
                cancellationToken);

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
                result.Error ??
                "Failed to retrieve telemetry history.");

        return result.Data ?? [];
    }

    // USERS - CRUD
    public async Task<IReadOnlyList<UserDto>>
        GetUsersAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
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
                result.Error ??
                "Failed to retrieve users.");

        return result.Data ?? [];
    }


    public async Task<IReadOnlyList<UserDto>>
        GetUsersByCompanyIdAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Users/company/{companyId}",
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
                result.Error ??
                "Failed to retrieve company users.");

        return result.Data ?? [];
    }


    public async Task<UserDto?>
        GetUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Users/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

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
                result.Error ??
                "Failed to retrieve user.");

        return result.Data;
    }


    public async Task<UserDto?>
        GetUserByFirebaseUidAsync(
            string firebaseUid, string idToken,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return null;

        if (string.IsNullOrWhiteSpace(idToken))
            throw new InvalidOperationException(
                "Firebase ID token is empty.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                idToken);

        var response =
            await _httpClient.GetAsync(
                $"api/Users/firebase/" +
                $"{Uri.EscapeDataString(firebaseUid)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            throw new InvalidOperationException(
                $"API returned {(int)response.StatusCode} " +
                $"{response.StatusCode}: {body}");
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<UserDto>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve user.");

        return result.Data;
    }


    public async Task<Guid>
        CreateUserAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/Users",
                request,
                cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to create user.");
        }

        return result.Data;
    }


    public async Task<bool>
        UpdateUserAsync(
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/Users/{request.Id}",
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to update user.");
        }

        return result.Data;
    }


    public async Task<bool>
        DeleteUserAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.DeleteAsync(
                $"api/Users/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to delete user.");
        }

        return result.Data;
    }


    // COMPANIES - CRUD
    public async Task<IReadOnlyList<CompanyDto>>
        GetCompaniesAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                "api/Companies",
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            JsonSerializer.Deserialize<
                Result<IReadOnlyList<CompanyDto>>>(
                    body);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve companies.");

        return result.Data ?? [];
    }



    public async Task<CompanyDto?>
        GetCompanyByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Companies/{id}",
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

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
                result.Error ??
                "Failed to retrieve company.");

        return result.Data;
    }


    public async Task<Guid>
        CreateCompanyAsync(
            CreateCompanyRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/Companies",
                request,
                cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to create company.");
        }

        return result.Data;
    }


    public async Task<bool>
        UpdateCompanyAsync(
            UpdateCompanyRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/Companies/{request.Id}",
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to update company.");
        }

        return result.Data;
    }


    public async Task<bool>
        DeleteCompanyAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.DeleteAsync(
                $"api/Companies/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to delete company.");
        }

        return result.Data;
    }


    // GATEWAYS - CRUD
    public async Task<IReadOnlyList<GatewayDto>>
        GetGatewaysAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
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
                result.Error ??
                "Failed to retrieve gateways.");

        return result.Data ?? [];
    }


    public async Task<IReadOnlyList<GatewayDto>>
        GetGatewaysByCompanyIdAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
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
                result.Error ??
                "Failed to retrieve gateways.");

        return result.Data ?? [];
    }


    public async Task<GatewayDto?>
        GetGatewayByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.GetAsync(
                $"api/Gateways/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

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
                result.Error ??
                "Failed to retrieve gateway.");

        return result.Data;
    }


    public async Task<Guid>
        CreateGatewayAsync(
            CreateGatewayRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/Gateways",
                request,
                cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<Guid>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to create gateway.");
        }

        return result.Data;
    }


    public async Task<bool>
        UpdateGatewayAsync(
            UpdateGatewayRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/Gateways/{request.Id}",
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to update gateway.");
        }

        return result.Data;
    }


    public async Task<bool>
        DeleteGatewayAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        var response =
            await _httpClient.DeleteAsync(
                $"api/Gateways/{id}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<bool>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!response.IsSuccessStatusCode ||
            !result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Failed to delete gateway.");
        }

        return result.Data;
    }
}
