using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.Services;

public class SmartXSession : INotifyPropertyChanged
{
    private Guid _userId;
    private Guid _companyId;
    private Guid? _gatewayId;

    private string? _firebaseUid;
    private string? _email;
    private string? _displayName;
    private UserRole? _role;

    private string? _idToken;
    private string? _refreshToken;

    private bool _isAuthenticated;
    private bool _isGuest;

    private string? _gatewayName;

    public Guid UserId
    {
        get => _userId;
        private set => SetField(ref _userId, value);
    }

    public Guid CompanyId
    {
        get => _companyId;
        private set => SetField(ref _companyId, value);
    }

    public Guid? GatewayId
    {
        get => _gatewayId;
        private set => SetField(ref _gatewayId, value);
    }

    public string? FirebaseUid
    {
        get => _firebaseUid;
        private set => SetField(ref _firebaseUid, value);
    }

    public string? Email
    {
        get => _email;
        private set => SetField(ref _email, value);
    }

    public string? DisplayName
    {
        get => _displayName;
        private set => SetField(ref _displayName, value);
    }

    public UserRole? Role
    {
        get => _role;
        private set => SetField(ref _role, value);
    }

    public string? IdToken
    {
        get => _idToken;
        private set => SetField(ref _idToken, value);
    }

    public string? RefreshToken
    {
        get => _refreshToken;
        private set => SetField(ref _refreshToken, value);
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set => SetField(ref _isAuthenticated, value);
    }

    public bool IsGuest
    {
        get => _isGuest;
        private set => SetField(ref _isGuest, value);
    }

    public string? GatewayName
    {
        get => _gatewayName;
        private set => SetField(ref _gatewayName, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // =========================================================
    // SIGN IN
    // =========================================================

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

        // A new login starts without a selected gateway.
        GatewayId = null;
        GatewayName = null;

        IsAuthenticated = true;
        IsGuest = false;
    }

    // =========================================================
    // GUEST
    // =========================================================

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

        GatewayId = null;
        GatewayName = null;

        IsAuthenticated = false;
        IsGuest = true;
    }

    // =========================================================
    // SIGN OUT
    // =========================================================

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

        GatewayId = null;
        GatewayName = null;

        IsAuthenticated = false;
        IsGuest = false;
    }

    // =========================================================
    // GATEWAY
    // =========================================================

    public void SelectGateway(
        Guid gatewayId,
        string gatewayName)
    {
        if (gatewayId == Guid.Empty)
        {
            ClearGateway();
            return;
        }

        GatewayId = gatewayId;
        GatewayName = gatewayName;
    }

    public void ClearGateway()
    {
        GatewayId = null;
        GatewayName = null;
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    private void SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}