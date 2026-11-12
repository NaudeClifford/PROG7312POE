using SmartX.Application.Requests.Company;
using SmartX.Application.Requests.Gateway;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Requests.Telemetry;
using SmartX.Application.Requests.User;
using SmartX.Application.Requests.UserBehavior;
using SmartX.Application.Services.Registration;
using SmartX.Application.Services.Sensors.Command;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Mesh;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using SmartX.Shared.Models;
using SmartX.WPF.Services.Session;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    public async Task<bool> DeleteCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return false;

        AddAuthenticationHeader();

        var response = await _httpClient.DeleteAsync(
            $"api/Companies/{companyId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to delete company " +
                $"({(int)response.StatusCode} {response.StatusCode}): {body}");
        }

        var result = JsonSerializer.Deserialize<Result<bool>>(
            body, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "The API returned an empty response.");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                result.Error ??
                "Unable to delete company.");
        }

        return result.Data;
    }

    public async Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/health");

            request.Headers.Authorization = null;

            using var response = await _httpClient.SendAsync(
                request, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/Sensors");

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<SensorDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", "api/Sensors");

        return result.Data ?? [];
    }

    public async Task<SensorDto?> GetSensorByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Sensors/{id}");

        try
        {
            return await SendAndReadAsync<SensorDto>(
                request,
                cancellationToken);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Guid> CreateSensorAsync(
        CreateSensorRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Sensors")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<Guid>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "POST", "api/Sensors");

        return result.Data;
    }

    public async Task<bool> UpdateSensorAsync(
        UpdateSensorRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id == Guid.Empty)
            return false;

        AddAuthenticationHeader();

        var endpoint = $"api/Sensors/{request.Id}";

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            endpoint)
        {
            Content = JsonContent.Create(request)
        };

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                httpRequest,
                cancellationToken);

            EnsureApiResult(result, "PUT", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteSensorAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        AddAuthenticationHeader();

        var endpoint = $"api/Sensors/{id}";

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            endpoint);

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "DELETE", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<SensorLogFileDto>>
        GetSensorLogFilesAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return [];

        AddAuthenticationHeader();

        var endpoint = $"api/SensorLogFiles/sensor/{sensorId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        try
        {
            var result = await SendAndReadAsync<
                Result<IReadOnlyList<SensorLogFileDto>>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", endpoint);

            return result.Data ?? [];
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    public async Task<SensorLogFileUploadResultDto>
        UploadSensorLogFileAsync(
            Guid sensorId, string fileName, Stream fileStream,
            string contentType, Guid userId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException(
                "Sensor ID is required.",
                nameof(sensorId));

        ArgumentNullException.ThrowIfNull(fileStream);

        AddAuthenticationHeader();

        using var content = new MultipartFormDataContent();

        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        content.Add(fileContent, "file", fileName);

        content.Add(
            new StringContent(sensorId.ToString()),
            "sensorId");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/SensorLogFiles")
        {
            Content = content
        };

        try
        {
            return await SendAndReadAsync<
                SensorLogFileUploadResultDto>(
                request,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new SensorLogFileUploadResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
        catch (InvalidOperationException ex)
        {
            return new SensorLogFileUploadResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<TelemetryDto?>
        GetTelemetryByIdAsync(
            Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        AddAuthenticationHeader();

        var endpoint = $"api/Telemetry/{id}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.CacheControl =
            new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };

        try
        {
            var result = await SendAndReadAsync<
                Result<TelemetryDto?>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TelemetryDto>>
        GetTelemetryBySensorIdAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return [];

        AddAuthenticationHeader();

        var endpoint = $"api/Telemetry/sensor/{sensorId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.CacheControl =
            new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<TelemetryDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", endpoint);

        return result.Data ?? [];
    }

    public async Task<TelemetryDto?>
        GetLatestTelemetryBySensorIdAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return null;

        AddAuthenticationHeader();

        var endpoint =
            $"api/Telemetry/sensor/{sensorId}/latest";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.CacheControl =
            new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };

        try
        {
            var result = await SendAndReadAsync<
                Result<TelemetryDto?>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TelemetryDto>>
        GetTelemetryByDateRangeAsync(
            Guid sensorId, DateTime from, DateTime to,
            CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return [];

        AddAuthenticationHeader();

        var endpoint =
            $"api/Telemetry/sensor/{sensorId}/history" +
            $"?from={Uri.EscapeDataString(from.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("O"))}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.CacheControl =
            new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<TelemetryDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", endpoint);

        return result.Data ?? [];
    }

    public async Task<Guid>
        CreateTelemetryAsync(
            CreateTelemetryRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Telemetry")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<Guid>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "POST", "api/Telemetry");

        return result.Data;
    }

    public async Task<IReadOnlyList<UserDto>>
        GetUsersAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        const string endpoint = "api/Users";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<UserDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", endpoint);

        return result.Data ?? [];
    }

    public async Task<IReadOnlyList<UserDto>>
        GetUsersByCompanyIdAsync(
            Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return [];

        var endpoint = $"api/Users/company/{companyId}";

        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<UserDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", endpoint);

        return result.Data ?? [];
    }

    public async Task<UserDto?>
        GetUserByIdAsync(
            Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        AddAuthenticationHeader();

        var endpoint = $"api/Users/{id}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        try
        {
            var result = await SendAndReadAsync<Result<UserDto>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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

        var endpoint =
            $"api/Users/firebase/{Uri.EscapeDataString(firebaseUid)}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                idToken);

        try
        {
            var result = await SendAndReadAsync<Result<UserDto>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", endpoint);

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Guid>
        CreateUserAsync(
            CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Users")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<Guid>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "POST", "api/Users");

        return result.Data;
    }

    public async Task<bool> UpdateUserAsync(
        UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/Users/{request.Id}")
        {
            Content = JsonContent.Create(request)
        };

        try
        {
            return await SendAndReadAsync<Result<bool>>(
                httpRequest,
                cancellationToken) is var result &&
                result.Success
                    ? result.Data
                    : throw new InvalidOperationException(
                        "Failed to update user.");
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/Users/{id}");

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "DELETE",
                $"api/Users/{id}");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<RegistrationResultDto>
        RegisterCompanyAsync(
            RegisterCompanyRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.IdToken))
            throw new InvalidOperationException(
                "Firebase ID token is empty.");

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/Companies/register");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                request.IdToken);

        httpRequest.Content =
            JsonContent.Create(request);

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Registration API failed " +
                $"({(int)response.StatusCode} {response.StatusCode}). " +
                $"Response: {body}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(
                "Registration API returned an empty response.");
        }

        RegistrationResultDto? registration;

        try
        {
            var result =
                JsonSerializer.Deserialize<
                    Result<RegistrationResultDto>>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result is null)
            {
                throw new InvalidOperationException(
                    "Registration API returned invalid JSON.");
            }

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    result.Error ??
                    "Registration failed.");
            }

            registration = result.Data;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Registration API returned invalid JSON. " +
                $"Response: {body}",
                ex);
        }

        if (registration is null)
        {
            throw new InvalidOperationException(
                "Registration succeeded but returned no registration data.");
        }

        return registration;
    }

    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/Companies");

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<CompanyDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", "api/Companies");

        return result.Data ?? [];
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Companies/{id}");

        try
        {
            var result = await SendAndReadAsync<Result<CompanyDto>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "GET",
                $"api/Companies/{id}");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Guid> CreateCompanyAsync(
     CreateCompanyRequest request,
     CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Companies")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<Guid>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "POST", "api/Companies");

        return result.Data;
    }

    public async Task<bool> UpdateCompanyAsync(
        UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/Companies/{request.Id}")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<bool>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "PUT", $"api/Companies/{request.Id}");

        return result.Data;
    }

    public async Task<bool> RequestCompanyDeletionAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/Companies/{companyId}/deletion-request");

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "POST",
                $"api/Companies/{companyId}/deletion-request");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "A deletion request already exists.",
                ex);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "Company not found.",
                ex);
        }
    }

    public async Task<bool> CancelDeletionAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/Companies/{companyId}/deletion-request/cancel");

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "POST",
                $"api/Companies/{companyId}/deletion-request/cancel");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<CompanyConfigurationDto?>
        GetCompanyConfigurationAsync(
            Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Companies/{companyId}/configuration");

        try
        {
            var result = await SendAndReadAsync<
                Result<CompanyConfigurationDto>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "GET",
                $"api/Companies/{companyId}/configuration");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> UpdateCompanyConfigurationAsync(
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/Companies/{request.CompanyId}/configuration")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<bool>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(
            result,
            "PUT",
            $"api/Companies/{request.CompanyId}/configuration");

        return result.Data;
    }

    public async Task<bool> CompleteOnboardingAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/Companies/{companyId}/onboarding/complete");

        var result = await SendAndReadAsync<Result<bool>>(
            request,
            cancellationToken);

        EnsureApiResult(
            result,
            "POST",
            $"api/Companies/{companyId}/onboarding/complete");

        return result.Data;
    }

    public async Task<bool> CompleteCompanyOnboardingAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/Companies/{companyId}/onboarding/complete");

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<GatewayDto>>
        GetGatewaysAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/Gateways");

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<GatewayDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(result, "GET", "api/Gateways");

        return result.Data ?? [];
    }

    public async Task<IReadOnlyList<GatewayDto>>
        GetGatewaysByCompanyIdAsync(
            Guid companyId, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Gateways/company/{companyId}");

        var result = await SendAndReadAsync<
            Result<IReadOnlyList<GatewayDto>>>(
            request,
            cancellationToken);

        EnsureApiResult(
            result,
            "GET",
            $"api/Gateways/company/{companyId}");

        return result.Data ?? [];
    }

    public async Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/Gateways/{id}");

        try
        {
            var result = await SendAndReadAsync<Result<GatewayDto>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "GET", $"api/Gateways/{id}");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Guid> CreateGatewayAsync(
        CreateGatewayRequest request, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Gateways")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAndReadAsync<Result<Guid>>(
            httpRequest,
            cancellationToken);

        EnsureApiResult(result, "POST", "api/Gateways");

        return result.Data;
    }

    public async Task<bool> UpdateGatewayAsync(
        UpdateGatewayRequest request, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"api/Gateways/{request.Id}")
        {
            Content = JsonContent.Create(request)
        };

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                httpRequest,
                cancellationToken);

            EnsureApiResult(
                result,
                "PUT",
                $"api/Gateways/{request.Id}");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteGatewayAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/Gateways/{id}");

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(result, "DELETE", $"api/Gateways/{id}");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteGuestCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return false;

        AddAuthenticationHeader();

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/Companies/{companyId}/guest");

        try
        {
            var result = await SendAndReadAsync<Result<bool>>(
                request,
                cancellationToken);

            EnsureApiResult(
                result,
                "DELETE",
                $"api/Companies/{companyId}/guest");

            return result.Data;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<SensorDto>>
        GetSensorsByGatewayIdAsync(
            Guid gatewayId, CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        AddAuthenticationHeader();

        using var response =
            await _httpClient.GetAsync(
                $"api/Sensors/gateway/{gatewayId}",
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];

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
                "Failed to retrieve gateway sensors.");

        return result.Data ?? [];
    }

    public async Task<SensorCommandResult>
        SendSensorCommandAsync(
            Guid sensorId, string command,
            CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException(
                "Sensor ID is required.",
                nameof(sensorId));

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException(
                "Command is required.",
                nameof(command));

        AddAuthenticationHeader();

        var request =
            new SendSensorCommandRequest
            {
                SensorId = sensorId,
                Command = command
            };

        using var response =
            await _httpClient.PostAsJsonAsync(
                $"api/Sensors/{sensorId}/command",
                request,
                cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                SensorCommandResult>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty command response.");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                result.ErrorMessage ??
                $"Sensor command failed ({(int)response.StatusCode}).",
                null,
                response.StatusCode);

        return result;
    }

    //UserBehavior
    public async Task<IReadOnlyList<UserBehaviorDto>>
        GetUserBehaviorsAsync(
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var response =
            await _httpClient.GetAsync(
                "api/UserBehavior",
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<UserBehaviorDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve user behaviors.");

        return result.Data ?? [];
    }

    public async Task<IReadOnlyList<UserBehaviorDto>>
        GetUserBehaviorsBySensorIdAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return [];

        AddAuthenticationHeader();

        using var response =
            await _httpClient.GetAsync(
                $"api/UserBehavior/sensor/{sensorId}",
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                Result<IReadOnlyList<UserBehaviorDto>>>(
                cancellationToken);

        if (result is null)
            throw new InvalidOperationException(
                "The API returned an empty response.");

        if (!result.Success)
            throw new InvalidOperationException(
                result.Error ??
                "Failed to retrieve sensor user behaviors.");

        return result.Data ?? [];
    }

    public async Task<Guid>
        CreateUserBehaviorAsync(
            CreateUserBehaviorRequest request,
            CancellationToken cancellationToken = default)
    {
        AddAuthenticationHeader();

        using var response =
            await _httpClient.PostAsJsonAsync(
                "api/UserBehavior",
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
                "Failed to create user behavior.");
        }

        return result.Data;
    }

    public async Task<IReadOnlyList<PredictiveSuggestionDto>>
        GetPredictiveSuggestionsAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException("Invalid sensor ID.");

        AddAuthenticationHeader();

        var endpoint =
            $"api/predictive/sensors/{sensorId}/suggestions";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        var result = await SendAndReadAsync<
            List<PredictiveSuggestionDto>>(
                request,
                cancellationToken);

        return result;
    }

    public async Task<AnomalyRecommendationDto?>
        AnalyzeSensorAnomalyAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException("Invalid sensor ID.");

        AddAuthenticationHeader();

        var endpoint =
            $"api/predictive/sensors/{sensorId}/anomaly";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<
            AnomalyRecommendationDto>(
                cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PredictiveSuggestionDto>>
        GetPredictiveWarningsAsync(
            Guid gatewayId,
            CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException("Invalid gateway ID.");

        AddAuthenticationHeader();

        var endpoint =
            $"api/Predictive/gateway/{gatewayId}/warnings";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        return await SendAndReadAsync<
            List<PredictiveSuggestionDto>>(
                request,
                cancellationToken);
    }

    public async Task<SensorCommandResult>
        UndoLastSensorCommandAsync(
            Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            return SensorCommandResult.Fail(
                "Sensor ID is required.");

        AddAuthenticationHeader();

        var endpoint =
            $"api/Sensors/{sensorId}/command/undo";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        var result = await SendAndReadAsync<
            SensorCommandResult>(
                request,
                cancellationToken);

        return result;
    }

    public async Task<List<SensorAlert>>
        GetAlertsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<SensorAlert>>(
            "api/alert",
            cancellationToken) ?? [];
    }

    public async Task<int>
        GetPendingAlertCountAsync(
            CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<int>(
            "api/alert/count",
            cancellationToken);
    }

    public async Task<SensorAlert?>
        ProcessNextAlertAsync(
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            "api/alert/process-next",
            content: null,
            cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SensorAlert>(
            cancellationToken: cancellationToken);
    }

    public async Task<List<SensorAlert>>
        ProcessAllAlertsAsync(
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            "api/alert/process-all",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<SensorAlert>>(
            cancellationToken: cancellationToken) ?? [];
    }

    public async Task ClearAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            "api/alert",
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    public async Task EnqueueAlertAsync(
        SensorAlert alert,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/alert",
            alert,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    // MESH DISCOVERY
    public async Task<MeshDiscoveryDto> DiscoverMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/discovery/{gatewayId}");

        return await SendAndReadAsync<MeshDiscoveryDto>(
            request,
            cancellationToken);
    }

    public async Task<MeshOperationResultDto> SynchronizeMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/discovery/{gatewayId}/synchronize");

        var result = await SendAndReadAsync<JsonElement>(
            request,
            cancellationToken);

        return result.TryGetProperty("changes", out var changes)
            ? new MeshOperationResultDto { Changes = changes.GetInt32() }
            : new MeshOperationResultDto { Changes = 0 };
    }

    // MESH TOPOLOGY
    public async Task<MeshTopologyDto> GetMeshTopologyAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/topology");

        return await SendAndReadAsync<MeshTopologyDto>(
            request,
            cancellationToken);
    }

    public async Task<Guid> RegisterMeshNodeAsync(
        MeshNodeDto node,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/mesh/nodes")
        {
            Content = JsonContent.Create(node)
        };

        var result = await SendAndReadAsync<MeshNodeDto>(
            request,
            cancellationToken);

        return result.Id;
    }

    public async Task<bool> UpdateMeshNodeAsync(
        MeshNodeDto node,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "api/mesh/nodes")
        {
            Content = JsonContent.Create(node)
        };

        await SendWithoutReadAsync(request, cancellationToken);

        return true;
    }

    public async Task<bool> RemoveMeshNodeAsync(
        Guid gatewayId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/mesh/{gatewayId}/nodes/{nodeId}");

        await SendWithoutReadAsync(request, cancellationToken);

        return true;
    }

    public async Task<Guid> RegisterMeshLinkAsync(
        MeshLinkDto link,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/mesh/links")
        {
            Content = JsonContent.Create(link)
        };

        var result = await SendAndReadAsync<MeshLinkDto>(
            request,
            cancellationToken);

        return result.Id;
    }

    public async Task<bool> UpdateMeshLinkAsync(
        MeshLinkDto link,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "api/mesh/links")
        {
            Content = JsonContent.Create(link)
        };

        await SendWithoutReadAsync(request, cancellationToken);

        return true;
    }

    public async Task<bool> RemoveMeshLinkAsync(
        Guid gatewayId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/mesh/{gatewayId}/links/{linkId}");

        await SendWithoutReadAsync(request, cancellationToken);

        return true;
    }

    // MESH HEALTH
    public async Task<MeshHealthReportDto> GetMeshHealthAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/health");

        return await SendAndReadAsync<MeshHealthReportDto>(
            request,
            cancellationToken);
    }

    // MESH MONITORING

    public async Task<MeshOperationResultDto> RefreshMeshMonitoringAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/{gatewayId}/monitoring/refresh");

        await SendWithoutReadAsync(request, cancellationToken);

        return new MeshOperationResultDto { Success = true };
    }

    public async Task<MeshOperationResultDto> CheckMeshNodeStatusAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/{gatewayId}/monitoring/nodes");

        await SendWithoutReadAsync(request, cancellationToken);

        return new MeshOperationResultDto { Success = true };
    }

    public async Task<MeshOperationResultDto> RecalculateMeshRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/{gatewayId}/monitoring/routes");

        await SendWithoutReadAsync(request, cancellationToken);

        return new MeshOperationResultDto { Success = true };
    }

    // MESH LINK QUALITY
    public async Task<MeshOperationResultDto> EvaluateGatewayLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/{gatewayId}/links/evaluate");

        await SendWithoutReadAsync(request, cancellationToken);

        return new MeshOperationResultDto { Success = true };
    }

    public async Task<MeshOperationResultDto> EvaluateMeshLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/mesh/links/{linkId}/evaluate");

        await SendWithoutReadAsync(request, cancellationToken);

        return new MeshOperationResultDto { Success = true };
    }

    // MESH ROUTING
    public async Task<MeshRouteDto> FindMeshShortestPathAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default)
    {
        var endpoint =
            $"api/mesh/{gatewayId}/routing/shortest-path" +
            $"?sourceNodeId={sourceNodeId}" +
            $"&destinationNodeId={destinationNodeId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        return await SendAndReadAsync<MeshRouteDto>(
            request,
            cancellationToken);
    }

    // MESH TRAVERSAL
    public async Task<IReadOnlyList<Guid>> MeshBreadthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/traversal/bfs/{startNodeId}");

        return await SendAndReadAsync<List<Guid>>(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> MeshDepthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/traversal/dfs/{startNodeId}");

        return await SendAndReadAsync<List<Guid>>(
            request,
            cancellationToken);
    }

    public async Task<MeshRouteDto> FindMeshBfsRouteAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default)
    {
        var endpoint =
            $"api/mesh/{gatewayId}/traversal/bfs-route" +
            $"?sourceNodeId={sourceNodeId}" +
            $"&destinationNodeId={destinationNodeId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        return await SendAndReadAsync<MeshRouteDto>(
            request,
            cancellationToken);
    }

    public async Task<MeshRouteDto> FindMeshDfsRouteAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default)
    {
        var endpoint =
            $"api/mesh/{gatewayId}/traversal/dfs-route" +
            $"?sourceNodeId={sourceNodeId}" +
            $"&destinationNodeId={destinationNodeId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        return await SendAndReadAsync<MeshRouteDto>(
            request,
            cancellationToken);
    }

    // MESH SPANNING TREE
    public async Task<IReadOnlyList<MeshConnectionDto>>
        GetMeshMinimumSpanningTreeAsync(
            Guid gatewayId,
            Guid startNodeId,
            CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/spanning-tree/{startNodeId}");

        var result = await SendAndReadAsync<JsonElement>(
            request,
            cancellationToken);

        if (!result.TryGetProperty(
                "connections",
                out var connections))
        {
            throw new InvalidOperationException(
                "API response did not contain spanning tree connections.");
        }

        return connections.Deserialize<List<MeshConnectionDto>>(
            JsonOptions) ?? [];
    }

    // MESH LOGGING
    public async Task<IReadOnlyList<MeshLogDto>> GetMeshGatewayLogsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/logs");

        return await SendAndReadAsync<List<MeshLogDto>>(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshLogDto>> GetMeshNodeLogsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/nodes/{nodeId}/logs");

        return await SendAndReadAsync<List<MeshLogDto>>(
            request,
            cancellationToken);
    }


    public async Task<MeshTraversalResultDto> BreadthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/traversal/bfs/{startNodeId}");

        return await SendAndReadAsync<MeshTraversalResultDto>(
            request,
            cancellationToken);
    }


    public async Task<MeshTraversalResultDto> DepthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/traversal/dfs/{startNodeId}");

        return await SendAndReadAsync<MeshTraversalResultDto>(
            request,
            cancellationToken);
    }

    public async Task<MeshSpanningTreeDto> GetMeshSpanningTreeAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/mesh/{gatewayId}/spanning-tree/{startNodeId}");

        return await SendAndReadAsync<MeshSpanningTreeDto>(
            request,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshConnectionDto>> GetMeshConnectionsAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<
            List<MeshConnectionDto>>(
            $"api/mesh/{gatewayId}/connections",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<MeshRouteDto>> GetMeshRoutesAsync(
    Guid gatewayId,
    CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        var result = await _httpClient.GetFromJsonAsync<
            List<MeshRouteDto>>(
            $"api/mesh/{gatewayId}/routes",
            cancellationToken);

        return result ?? [];
    }


    private async Task SendWithoutReadAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken = default)
    {
        var method = request.Method.Method;

        var endpoint = request.RequestUri?.ToString()
                       ?? "Unknown endpoint";

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            System.Diagnostics.Debug.WriteLine(
                $"[MESH API REQUEST] {method} {endpoint}");

            System.Diagnostics.Debug.WriteLine(
                $"[MESH API STATUS] {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"[MESH API RESPONSE] {body}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Mesh API request failed.\n" +
                    $"Method: {method}\n" +
                    $"Endpoint: {endpoint}\n" +
                    $"Status: {(int)response.StatusCode} {response.StatusCode}\n" +
                    $"Response: {body}",
                    null,
                    response.StatusCode);
            }
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Mesh API request timed out.\n" +
                $"Endpoint: {endpoint}",
                ex);
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MESH API ERROR] {method} {endpoint}: {ex}");

            throw;
        }
    }

    private async Task<T> SendAndReadAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        var method = request.Method.Method;
        var endpoint = request.RequestUri?.ToString() ?? "Unknown endpoint";

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            System.Diagnostics.Debug.WriteLine(
                $"[API REQUEST] {method} {endpoint}");

            System.Diagnostics.Debug.WriteLine(
                $"[API STATUS] {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"[API RESPONSE] {body}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"API request failed.\n" +
                    $"Method: {method}\n" +
                    $"Endpoint: {endpoint}\n" +
                    $"Status: {(int)response.StatusCode} {response.StatusCode}\n" +
                    $"Response: {body}",
                    null,
                    response.StatusCode);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidOperationException(
                    $"API returned an empty response.\n" +
                    $"Method: {method}\n" +
                    $"Endpoint: {endpoint}");
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(
                    body,
                    JsonOptions);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        $"API returned null JSON.\n" +
                        $"Method: {method}\n" +
                        $"Endpoint: {endpoint}");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"JSON deserialization failed.\n" +
                    $"Method: {method}\n" +
                    $"Endpoint: {endpoint}\n" +
                    $"Response: {body}",
                    ex);
            }
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"API request timed out.\n" +
                $"Method: {method}\n" +
                $"Endpoint: {endpoint}",
                ex);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[API NETWORK ERROR] {method} {endpoint}: {ex}");

            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[API ERROR] {method} {endpoint}: {ex}");

            throw;
        }
    }

    private static void EnsureApiResult<T>(
        Result<T> result,
        string method,
        string endpoint)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"API operation failed.\n" +
                $"Method: {method}\n" +
                $"Endpoint: {endpoint}\n" +
                $"API Error: {result.Error ?? "No error message returned."}");
        }
    }


}
