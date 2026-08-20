using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;

namespace SmartX.WPF.Services;

public class SmartXSession
{
    public Guid UserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid? GatewayId { get; private set; }

    public string? FirebaseUid { get; private set; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public UserRole? Role { get; private set; }

    public string? IdToken { get; private set; }

    public string? RefreshToken { get; private set; }

    public bool IsAuthenticated { get; private set; }

    public bool IsGuest { get; private set; }

    public void SignIn(
        UserDto user,
        string idToken,
        string refreshToken)
    {
        UserId = user.Id;
        CompanyId = user.CompanyId;

        FirebaseUid = user.FirebaseUid;
        Email = user.Email;
        DisplayName = user.DisplayName;
        Role = user.Role;

        IdToken = idToken;
        RefreshToken = refreshToken;

        IsAuthenticated = true;
        IsGuest = false;
    }

    public void StartGuestSession(string name)
    {
        UserId = Guid.Empty;
        CompanyId = Guid.Empty;

        FirebaseUid = null;
        Email = null;
        DisplayName = name;
        Role = null;

        IdToken = null;
        RefreshToken = null;

        IsAuthenticated = false;
        IsGuest = true;
    }

    public void SignOut()
    {
        UserId = Guid.Empty;
        CompanyId = Guid.Empty;

        FirebaseUid = null;
        Email = null;
        DisplayName = null;
        Role = null;

        IdToken = null;
        RefreshToken = null;

        IsAuthenticated = false;
        IsGuest = false;
    }

    public void SelectGateway(Guid gatewayId)
    {
        GatewayId = gatewayId;
    }
}