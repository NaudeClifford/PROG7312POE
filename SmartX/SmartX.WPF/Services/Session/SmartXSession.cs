using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.Services.Session;

public class SmartXSession : INotifyPropertyChanged
{
    private Guid _userId;
    private Guid _companyId;
    private Guid? _gatewayId;

    private Guid _selectedCompanyId;
    private string? _selectedCompanyName;

    private string? _firebaseUid;
    private string? _email;
    private string? _displayName;
    private string? _userName;
    private string? _companyName;
    private UserRole? _role;

    private string? _idToken;
    private string? _refreshToken;

    private bool _isAuthenticated;
    private bool _isOnline = true;
    private bool _isGuest;

    private string? _gatewayName;

    private bool _isOnboardingComplete;
    private bool _isOnboarding;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public Guid SelectedCompanyId
    {
        get => _selectedCompanyId;
        private set => SetField(ref _selectedCompanyId, value);
    }

    public string? SelectedCompanyName
    {
        get => _selectedCompanyName;
        private set => SetField(ref _selectedCompanyName, value);
    }

    public Guid EffectiveCompanyId =>
        Role == UserRole.SuperAdmin
            ? SelectedCompanyId
            : CompanyId;

    public bool HasCompany =>
        EffectiveCompanyId != Guid.Empty;

    public bool HasSelectedCompany =>
        SelectedCompanyId != Guid.Empty;

    public bool IsSuperAdmin =>
        Role == UserRole.SuperAdmin;

    public bool IsAdministrator =>
        Role == UserRole.Administrator;

    public bool CanManageUsers =>
        Role is UserRole.SuperAdmin or UserRole.Administrator;

    public bool CanSelectCompany =>
        Role == UserRole.SuperAdmin;

    public bool HasSelectedGateway =>
        GatewayId.HasValue &&
        GatewayId.Value != Guid.Empty;

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

    public string? UserName
    {
        get => _userName;
        private set => SetField(ref _userName, value);
    }

    public string? CompanyName
    {
        get => _companyName;
        private set => SetField(ref _companyName, value);
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

    public bool IsOnline
    {
        get => _isOnline;
        set => SetField(ref _isOnline, value);
    }

    public string? GatewayName
    {
        get => _gatewayName;
        private set => SetField(ref _gatewayName, value);
    }

    public bool IsGuest
    {
        get => _isGuest;
        private set => SetField(ref _isGuest, value);
    }

    public bool IsOnboardingComplete
    {
        get => _isOnboardingComplete;
        private set => SetField(ref _isOnboardingComplete, value);
    }

    public bool IsOnboarding
    {
        get => _isOnboarding;
        private set => SetField(ref _isOnboarding, value);
    }

    public void SignIn(
        UserDto user,
        string companyName,
        string idToken,
        string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException(
                "ID token cannot be empty.",
                nameof(idToken));

        if (user.Id == Guid.Empty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(user));

        if (user.Role != UserRole.SuperAdmin &&
            user.CompanyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Company ID cannot be empty.",
                nameof(user));
        }

        UserId = user.Id;
        CompanyId = user.CompanyId;

        FirebaseUid = user.FirebaseUid;
        Email = user.Email;
        DisplayName = user.DisplayName;
        UserName = user.DisplayName;
        CompanyName = companyName;
        Role = user.Role;

        IdToken = idToken;
        RefreshToken = refreshToken;

        GatewayId = null;
        GatewayName = null;

        if (user.Role == UserRole.SuperAdmin)
        {
            SelectedCompanyId = Guid.Empty;
            SelectedCompanyName = null;
        }
        else
        {
            SelectedCompanyId = user.CompanyId;
            SelectedCompanyName =
                string.IsNullOrWhiteSpace(companyName)
                    ? null
                    : companyName.Trim();
        }

        IsAuthenticated = true;
        IsGuest = false;

        OnPropertyChanged(nameof(EffectiveCompanyId));
        OnPropertyChanged(nameof(HasCompany));
        OnPropertyChanged(nameof(HasSelectedCompany));
        OnPropertyChanged(nameof(IsSuperAdmin));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanSelectCompany));
        OnPropertyChanged(nameof(HasSelectedGateway));
    }

    public void SignIn(
        UserDto user,
        string idToken,
        string refreshToken)
    {
        SignIn(
            user,
            string.Empty,
            idToken,
            refreshToken);
    }

    public void SelectCompany(
        Guid companyId,
        string companyName)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException(
                "A user must be authenticated before selecting a company.");

        if (Role != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException(
                "Only SuperAdmin users can select a company.");

        if (companyId == Guid.Empty)
            throw new ArgumentException(
                "Company ID cannot be empty.",
                nameof(companyId));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException(
                "Company name cannot be empty.",
                nameof(companyName));

        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName.Trim();

        ClearGateway();

        OnPropertyChanged(nameof(EffectiveCompanyId));
        OnPropertyChanged(nameof(HasCompany));
        OnPropertyChanged(nameof(HasSelectedCompany));
    }

    public void ClearSelectedCompany()
    {
        if (Role != UserRole.SuperAdmin)
            return;

        SelectedCompanyId = Guid.Empty;
        SelectedCompanyName = null;

        ClearGateway();

        OnPropertyChanged(nameof(EffectiveCompanyId));
        OnPropertyChanged(nameof(HasCompany));
        OnPropertyChanged(nameof(HasSelectedCompany));
    }

    public void SetCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            SelectedCompanyName = null;
            CompanyName = null;
            return;
        }

        var trimmedName = companyName.Trim();

        SelectedCompanyName = trimmedName;
        CompanyName = trimmedName;
    }

    public void SelectGateway(
        Guid gatewayId,
        string gatewayName)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException(
                "A user must be authenticated before selecting a gateway.");

        if (EffectiveCompanyId == Guid.Empty)
            throw new InvalidOperationException(
                "A company must be selected before selecting a gateway.");

        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "Gateway ID cannot be empty.",
                nameof(gatewayId));

        if (string.IsNullOrWhiteSpace(gatewayName))
            throw new ArgumentException(
                "Gateway name cannot be empty.",
                nameof(gatewayName));

        GatewayId = gatewayId;
        GatewayName = gatewayName.Trim();

        OnPropertyChanged(nameof(HasSelectedGateway));
    }

    public void ClearGateway()
    {
        GatewayId = null;
        GatewayName = null;

        OnPropertyChanged(nameof(HasSelectedGateway));
    }

    public void BeginOnboarding()
    {
        IsOnboarding = true;
        IsOnboardingComplete = false;
    }

    public void CompleteOnboarding()
    {
        IsOnboarding = false;
        IsOnboardingComplete = true;
    }

    public void SetOnboardingCompleted(bool completed)
    {
        IsOnboardingComplete = completed;
        IsOnboarding = !completed;
    }

    public void SignOut()
    {
        UserId = Guid.Empty;
        CompanyId = Guid.Empty;

        FirebaseUid = null;
        Email = null;
        DisplayName = null;
        UserName = null;
        CompanyName = null;
        Role = null;

        IdToken = null;
        RefreshToken = null;

        GatewayId = null;
        GatewayName = null;

        SelectedCompanyId = Guid.Empty;
        SelectedCompanyName = null;

        IsAuthenticated = false;
        IsGuest = false;

        IsOnboarding = false;
        IsOnboardingComplete = false;

        OnPropertyChanged(nameof(EffectiveCompanyId));
        OnPropertyChanged(nameof(HasCompany));
        OnPropertyChanged(nameof(HasSelectedCompany));
        OnPropertyChanged(nameof(IsSuperAdmin));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanSelectCompany));
        OnPropertyChanged(nameof(HasSelectedGateway));
    }
    public void StartGuestSession(
    Guid userId,
    Guid companyId,
    string userName,
    string companyName,
    string firebaseUid,
    string idToken,
    string refreshToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "Guest user ID cannot be empty.",
                nameof(userId));

        if (companyId == Guid.Empty)
            throw new ArgumentException(
                "Guest company ID cannot be empty.",
                nameof(companyId));

        if (string.IsNullOrWhiteSpace(firebaseUid))
            throw new ArgumentException(
                "Firebase UID cannot be empty.",
                nameof(firebaseUid));

        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException(
                "Firebase ID token cannot be empty.",
                nameof(idToken));

        UserId = userId;
        CompanyId = companyId;

        UserName = userName;
        DisplayName = userName;
        CompanyName = companyName;

        Email = null;

        FirebaseUid = firebaseUid;

        Role = UserRole.Guest;

        IdToken = idToken;
        RefreshToken = refreshToken;

        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName;

        GatewayId = null;
        GatewayName = null;

        IsAuthenticated = true;
        IsGuest = true;

        IsOnboarding = false;
        IsOnboardingComplete = true;

        OnPropertyChanged(nameof(EffectiveCompanyId));
        OnPropertyChanged(nameof(HasCompany));
        OnPropertyChanged(nameof(HasSelectedCompany));
        OnPropertyChanged(nameof(IsSuperAdmin));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanSelectCompany));
        OnPropertyChanged(nameof(HasSelectedGateway));
    }


    protected virtual void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;

        OnPropertyChanged(propertyName);

        if (propertyName is
            nameof(CompanyId) or
            nameof(SelectedCompanyId) or
            nameof(Role))
        {
            OnPropertyChanged(nameof(EffectiveCompanyId));
            OnPropertyChanged(nameof(HasCompany));
            OnPropertyChanged(nameof(HasSelectedCompany));
        }

        if (propertyName == nameof(Role))
        {
            OnPropertyChanged(nameof(IsSuperAdmin));
            OnPropertyChanged(nameof(IsAdministrator));
            OnPropertyChanged(nameof(CanManageUsers));
            OnPropertyChanged(nameof(CanSelectCompany));
        }

        if (propertyName == nameof(GatewayId))
            OnPropertyChanged(nameof(HasSelectedGateway));

        return true;
    }


}