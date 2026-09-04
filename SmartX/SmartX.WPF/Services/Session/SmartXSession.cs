using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.Services.Session;

public class SmartXSession : INotifyPropertyChanged
{
    // PRIVATE FIELDS

    private Guid _userId;
    private Guid _companyId;
    private Guid? _gatewayId;
    private bool _isOnboardingComplete;
    private bool _isOnboarding;


    private string? _firebaseUid;
    private string? _email;
    private string? _displayName;
    private UserRole? _role;

    private string? _idToken;
    private string? _refreshToken;

    private bool _isAuthenticated;
    private bool _isOnline = true;
    private string? _gatewayName;
    private bool _isGuest;

    private Guid _selectedCompanyId;
    private string? _selectedCompanyName;


    // PROPERTIES
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

    public void SetCompany(
    Guid companyId,
    string companyName)
    {
        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName;

        ClearGateway();
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

    public bool IsOnboardingComplete
    {
        get => _isOnboardingComplete;

        private set
        {
            if (_isOnboardingComplete == value)
                return;

            _isOnboardingComplete = value;

            OnPropertyChanged();
        }
    }

    public bool IsOnboarding
    {
        get => _isOnboarding;
        private set
        {
            if (_isOnboarding == value)
                return;

            _isOnboarding = value;

            OnPropertyChanged();
        }
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




    // PROPERTY CHANGED
    public event PropertyChangedEventHandler? PropertyChanged;

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
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    // SIGN IN
    public void SignIn(
        UserDto user,
        string idToken,
        string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException(
                "ID token cannot be empty.",
                nameof(idToken));

        UserId = user.Id;
        CompanyId = user.CompanyId;

        FirebaseUid = user.FirebaseUid;
        Email = user.Email;
        DisplayName = user.DisplayName;
        Role = user.Role;

        IdToken = idToken;
        RefreshToken = refreshToken;

        GatewayId = null;
        GatewayName = null;

        SelectedCompanyId = user.CompanyId;

        SelectedCompanyName = null;

        IsAuthenticated = true;
        IsGuest = false;
    }

    // GUEST SESSION
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

        SelectedCompanyId = Guid.Empty;
        SelectedCompanyName = null;

        IsAuthenticated = false;
        IsGuest = true;
    }

    // SIGN OUT
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

        SelectedCompanyId = Guid.Empty;
        SelectedCompanyName = null;

        IsAuthenticated = false;
        IsGuest = false;
    }


    // COMPANY
    public void SelectCompany(
        Guid companyId,
        string companyName)
    {
        SelectedCompanyId = companyId;
        SelectedCompanyName = companyName;

        ClearGateway();
    }

    public void ClearSelectedCompany()
    {
        SelectedCompanyId = Guid.Empty;
        SelectedCompanyName = null;

        ClearGateway();
    }


    // GATEWAY
    public void SelectGateway(
        Guid gatewayId,
        string gatewayName)
    {
        GatewayId = gatewayId;
        GatewayName = gatewayName;
    }

    public void ClearGateway()
    {
        GatewayId = null;
        GatewayName = null;
    }
}
