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
            Email = firebaseResult.Email,
            IdToken = firebaseResult.IdToken,
            RefreshToken = firebaseResult.RefreshToken
        };
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(
    string refreshToken)
    {
        var url =
            $"https://securetoken.googleapis.com/v1/token?key={_apiKey}";

        var request = new FirebaseRefreshRequest
        {
            GrantType = "refresh_token",
            RefreshToken = refreshToken
        };

        var response = await _httpClient.PostAsJsonAsync(
            url,
            request);

        if (!response.IsSuccessStatusCode)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "The saved login session has expired."
            };
        }

        var firebaseResult =
            await response.Content
                .ReadFromJsonAsync<FirebaseRefreshResponse>();

        if (firebaseResult is null)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage =
                    "Firebase returned an empty refresh response."
            };
        }

        return new AuthenticationResult
        {
            Success = true,
            UserId = firebaseResult.UserId,
            IdToken = firebaseResult.IdToken,
            RefreshToken = firebaseResult.RefreshToken
        };
    }

    public async Task<AuthenticationResult> SignUpAsync(
        string email,
        string password)
    {
        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";

        var request = new FirebaseSignUpRequest
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
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage =
                    "Unable to create the Firebase account."
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
                ErrorMessage =
                    "Firebase returned an empty response."
            };
        }

        return new AuthenticationResult
        {
            Success = true,
            UserId = firebaseResult.LocalId,
            Email = firebaseResult.Email,
            IdToken = firebaseResult.IdToken,
            RefreshToken = firebaseResult.RefreshToken
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

    public class FirebaseSignUpRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }

    public class FirebaseRefreshRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }


    public class FirebaseRefreshResponse
    {

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public string ExpiresIn { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;
    }

} 