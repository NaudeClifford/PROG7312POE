namespace SmartX.Shared.DTOs;

public class LoginResponse
{
    public string IdToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}