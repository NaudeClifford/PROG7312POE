using SmartX.Application.Authentication;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SmartX.WPF.Authentication;

public class FirebaseAuthService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FirebaseAuthService(
        HttpClient httpClient,
        FirebaseOptions options)
    {
        _httpClient = httpClient;
        _apiKey = options.ApiKey;
    }

    public async Task<AuthenticationResult> SignInAsync(
        string email,
        string password)
    {
        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";

        var request = new FirebaseLoginRequest
        {
            Email = email,
            Password = password,
            ReturnSecureToken = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            url,
            request);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        var firebaseResult =
            await response.Content
                .ReadFromJsonAsync<FirebaseLoginResponse>();

        if (firebaseResult is null)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Firebase returned an empty response."
            };
        }

        return new AuthenticationResult
        {
            Success = true,
            UserId = firebaseResult.LocalId,
            Email = firebaseResult.Email
        };
    }
    public class FirebaseLoginResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public string ExpiresIn { get; set; } = string.Empty;

        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class FirebaseLoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }
} 