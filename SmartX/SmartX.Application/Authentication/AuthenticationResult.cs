namespace SmartX.Application.Authentication
{
    public class AuthenticationResult
    {
            public bool Success { get; init; }

            public string? UserId { get; init; }

            public string? Email { get; init; }

            public string? IdToken { get; init; }

            public string? RefreshToken { get; init; }

            public string? ErrorMessage { get; init; }
        
    }
}