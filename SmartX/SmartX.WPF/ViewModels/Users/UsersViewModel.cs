using AutoMapper;
using SmartX.Application.Commands.Users;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Company;
using SmartX.WPF.Views.Pages.Gateway;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;

namespace SmartX.WPF.ViewModels.Pages.Users;

public class UsersViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly SmartXSession _session;
    private readonly IMapper _mapper;
    private readonly ILocalUserCache _userCache;
    private readonly ICacheSyncService _cacheSyncService;

    private bool _isLoaded;
    private bool _isBusy;
    private bool _isOnline;
    private bool _isEditing;

    private string _errorMessage = string.Empty;
    private string _currentCompanyName = "Current Company";

    private User? _selectedUser;

    // =========================================================
    // FORM STATE
    // =========================================================

    private string _formEmail = string.Empty;
    private string _formDisplayName = string.Empty;
    private UserRole _formRole = UserRole.Viewer;
    private bool _formIsActive = true;

    // =========================================================
    // COLLECTION
    // =========================================================

    public ObservableCollection<User> Users { get; } = [];

    // =========================================================
    // COMPANY
    // =========================================================

    public Guid EffectiveCompanyId =>
        _session.Role == UserRole.SuperAdmin
            ? _session.SelectedCompanyId
            : _session.CompanyId;

    public Guid CompanyId =>
        EffectiveCompanyId;

    public bool HasCompany =>
        EffectiveCompanyId != Guid.Empty;

    public string CurrentCompanyName
    {
        get => _currentCompanyName;

        private set => SetProperty(
            ref _currentCompanyName,
            value);
    }

    // =========================================================
    // SELECTED USER
    // =========================================================

    public User? SelectedUser
    {
        get => _selectedUser;

        set
        {
            if (!SetProperty(
                    ref _selectedUser,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    // =========================================================
    // STATE
    // =========================================================

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (!SetProperty(
                    ref _isBusy,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public bool IsOnline
    {
        get => _isOnline;

        private set
        {
            if (!SetProperty(
                    ref _isOnline,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;

        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    // =========================================================
    // FORM
    // =========================================================

    public bool IsEditing
    {
        get => _isEditing;

        private set
        {
            if (!SetProperty(
                    ref _isEditing,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsCreating));

            RaiseCommandStates();
        }
    }

    public bool IsCreating =>
        !IsEditing;

    public string FormEmail
    {
        get => _formEmail;

        set
        {
            if (!SetProperty(
                    ref _formEmail,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public string FormDisplayName
    {
        get => _formDisplayName;

        set
        {
            if (!SetProperty(
                    ref _formDisplayName,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public UserRole FormRole
    {
        get => _formRole;

        set => SetProperty(
            ref _formRole,
            value);
    }

    public bool FormIsActive
    {
        get => _formIsActive;

        set => SetProperty(
            ref _formIsActive,
            value);
    }

    public Array UserRoles =>
        Enum.GetValues<UserRole>();

    // =========================================================
    // COUNTS
    // =========================================================

    public int TotalUsers =>
        Users.Count;

    public int ActiveUsers =>
        Users.Count(x => x.IsActive);

    public int InactiveUsers =>
        Users.Count(x => !x.IsActive);

    public int AdministratorCount =>
        Users.Count(x =>
            x.Role == UserRole.Administrator);

    public int TechnicianCount =>
        Users.Count(x =>
            x.Role == UserRole.Technician);

    public int ViewerCount =>
        Users.Count(x =>
            x.Role == UserRole.Viewer);

    public int SuperAdminCount =>
        Users.Count(x =>
            x.Role == UserRole.SuperAdmin);

    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand BackCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand AddUserCommand { get; }

    public AsyncRelayCommand EditUserCommand { get; }

    public AsyncRelayCommand SaveUserCommand { get; }

    public AsyncRelayCommand CancelEditCommand { get; }

    public AsyncRelayCommand DeleteUserCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public UsersViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        SmartXSession session,
        IMapper mapper,
        ILocalUserCache userCache,
        ICacheSyncService cacheSyncService)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _session = session;
        _mapper = mapper;
        _userCache = userCache;
        _cacheSyncService = cacheSyncService;

        // -----------------------------------------------------
        // BACK
        // -----------------------------------------------------

        BackCommand =
            new AsyncRelayCommand(
                BackAsync);

        // -----------------------------------------------------
        // REFRESH
        // -----------------------------------------------------

        RefreshCommand =
            new AsyncRelayCommand(
                () => LoadAsync(),
                CanRefresh);

        // -----------------------------------------------------
        // ADD
        // -----------------------------------------------------

        AddUserCommand =
            new AsyncRelayCommand(
                AddUserAsync,
                CanModifyUsers);

        // -----------------------------------------------------
        // EDIT
        // -----------------------------------------------------

        EditUserCommand =
            new AsyncRelayCommand(
                EditUserAsync,
                CanEditUser);

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        SaveUserCommand =
            new AsyncRelayCommand(
                SaveUserAsync,
                CanSaveUser);

        // -----------------------------------------------------
        // CANCEL
        // -----------------------------------------------------

        CancelEditCommand =
            new AsyncRelayCommand(
                CancelEditAsync,
                CanCancelEdit);

        // -----------------------------------------------------
        // DELETE
        // -----------------------------------------------------

        DeleteUserCommand =
            new AsyncRelayCommand(
                DeleteUserAsync,
                CanEditUser);

        // -----------------------------------------------------
        // SESSION
        // -----------------------------------------------------

        _session.PropertyChanged +=
            Session_PropertyChanged;
    }

    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _isLoaded = true;

            IsBusy = true;
            ErrorMessage = string.Empty;

            Users.Clear();
            SelectedUser = null;

            CancelForm();

            OnPropertyChanged(
                nameof(CompanyId));

            OnPropertyChanged(
                nameof(EffectiveCompanyId));

            OnPropertyChanged(
                nameof(HasCompany));

            var companyId =
                EffectiveCompanyId;

            // =================================================
            // NO COMPANY
            // =================================================

            if (companyId == Guid.Empty)
            {
                CurrentCompanyName =
                    "No Company Selected";

                ErrorMessage =
                    "No company is selected.";

                RaiseCounts();

                return;
            }

            // =================================================
            // CHECK API
            // =================================================

            try
            {
                IsOnline =
                    await _apiClient.IsAvailableAsync(
                        cancellationToken);
            }
            catch (HttpRequestException)
            {
                IsOnline = false;
            }

            // =================================================
            // ONLINE
            // =================================================

            if (IsOnline)
            {
                try
                {
                    // -----------------------------------------
                    // SYNC COMPANY
                    // -----------------------------------------

                    await _cacheSyncService.SyncCompanyAsync(
                        companyId,
                        cancellationToken);

                    // -----------------------------------------
                    // SYNC USERS
                    // -----------------------------------------

                    await _cacheSyncService.SyncUsersAsync(
                        companyId,
                        cancellationToken);
                }
                catch (HttpRequestException)
                {
                    IsOnline = false;

                    ErrorMessage =
                        "Unable to synchronize with the SmartX API.";
                }
            }

            // =================================================
            // COMPANY NAME FROM CACHE/API
            // =================================================

            if (IsOnline)
            {
                var company =
                    await _apiClient.GetCompanyByIdAsync(
                        companyId,
                        cancellationToken);

                CurrentCompanyName =
                    company?.Name ??
                    "Current Company";
            }

            // =================================================
            // READ USERS FROM LOCAL CACHE
            // =================================================

            var cachedUsers =
                await _userCache.GetByCompanyIdAsync(
                    companyId,
                    cancellationToken);

            foreach (var user in cachedUsers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Users.Add(user);
            }

            RaiseCounts();

            // =================================================
            // OFFLINE WITH NO CACHE
            // =================================================

            if (!IsOnline &&
                Users.Count == 0)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API and no cached users are available.";
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage =
                "Unable to connect to the SmartX API.";

            // -----------------------------------------------
            // FALLBACK TO CACHE
            // -----------------------------------------------

            try
            {
                var companyId =
                    EffectiveCompanyId;

                if (companyId != Guid.Empty)
                {
                    var cachedUsers =
                        await _userCache.GetByCompanyIdAsync(
                            companyId,
                            cancellationToken);

                    Users.Clear();

                    foreach (var user in cachedUsers)
                    {
                        Users.Add(user);
                    }

                    RaiseCounts();
                }
            }
            catch
            {
                // Keep original connection error.
            }
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // =========================================================
    // SESSION CHANGE
    // =========================================================

    private async void Session_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
                nameof(SmartXSession.CompanyId) &&
            e.PropertyName !=
                nameof(SmartXSession.SelectedCompanyId))
        {
            return;
        }

        OnPropertyChanged(
            nameof(CompanyId));

        OnPropertyChanged(
            nameof(EffectiveCompanyId));

        OnPropertyChanged(
            nameof(HasCompany));

        if (!_isLoaded)
            return;

        await LoadAsync();
    }

    // =========================================================
    // PERMISSIONS
    // =========================================================

    private bool HasUserWritePermission()
    {
        return _session.Role ==
                   UserRole.Administrator ||
               _session.Role ==
                   UserRole.SuperAdmin;
    }

    private bool CanRefresh()
    {
        return !IsBusy &&
               HasCompany;
    }

    private bool CanModifyUsers()
    {
        return IsOnline &&
               !IsBusy &&
               HasCompany &&
               HasUserWritePermission();
    }

    private bool CanEditUser()
    {
        return IsOnline &&
               !IsBusy &&
               HasCompany &&
               SelectedUser != null &&
               HasUserWritePermission();
    }

    private bool CanSaveUser()
    {
        return IsOnline &&
               !IsBusy &&
               HasCompany &&
               HasUserWritePermission() &&
               !string.IsNullOrWhiteSpace(
                   FormEmail) &&
               !string.IsNullOrWhiteSpace(
                   FormDisplayName);
    }

    private bool CanCancelEdit()
    {
        return !IsBusy &&
               IsEditing;
    }

    // =========================================================
    // ADD USER
    // =========================================================

    private async Task AddUserAsync()
    {
        if (!CanModifyUsers())
            return;

        SelectedUser = null;

        FormEmail =
            string.Empty;

        FormDisplayName =
            string.Empty;

        FormRole =
            UserRole.Viewer;

        FormIsActive =
            true;

        IsEditing = false;

        ErrorMessage =
            string.Empty;

        await Task.CompletedTask;

        RaiseCommandStates();
    }

    // =========================================================
    // EDIT USER
    // =========================================================

    private async Task EditUserAsync()
    {
        if (!CanEditUser())
            return;

        if (SelectedUser is null)
            return;

        FormEmail =
            SelectedUser.Email;

        FormDisplayName =
            SelectedUser.DisplayName;

        FormRole =
            SelectedUser.Role;

        FormIsActive =
            SelectedUser.IsActive;

        IsEditing = true;

        ErrorMessage =
            string.Empty;

        await Task.CompletedTask;

        RaiseCommandStates();
    }

    // =========================================================
    // SAVE USER
    // =========================================================

    private async Task SaveUserAsync()
    {
        if (!CanSaveUser())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var companyId =
                EffectiveCompanyId;

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is selected.";

                return;
            }

            // =================================================
            // UPDATE
            // =================================================

            if (IsEditing)
            {
                if (SelectedUser is null)
                    return;

                var command =
                    new UpdateUserCommand
                    {
                        Id =
                            SelectedUser.Id,

                        CompanyId =
                            SelectedUser.CompanyId,

                        Email =
                            FormEmail.Trim(),

                        DisplayName =
                            FormDisplayName.Trim(),

                        Role =
                            FormRole,

                        IsActive =
                            FormIsActive
                    };

                var updated =
                    await _apiClient.UpdateUserAsync(
                        command);

                if (!updated)
                {
                    ErrorMessage =
                        "Unable to update the user.";

                    return;
                }

                // ---------------------------------------------
                // API SUCCESS
                // ---------------------------------------------

                await _cacheSyncService.SyncUserAsync(
                    SelectedUser.Id);

                // ---------------------------------------------
                // RELOAD FROM CACHE
                // ---------------------------------------------

                await ReloadUsersFromCacheAsync(
                    companyId);

                CancelForm();

                return;
            }

            // =================================================
            // CREATE
            // =================================================

            var createCommand =
                new CreateUserCommand
                {
                    CompanyId =
                        companyId,

                    Email =
                        FormEmail.Trim(),

                    DisplayName =
                        FormDisplayName.Trim(),

                    Role =
                        FormRole,

                    IsActive =
                        FormIsActive
                };

            var userId =
                await _apiClient.CreateUserAsync(
                    createCommand);

            // ---------------------------------------------
            // SYNC CREATED USER INTO LOCAL CACHE
            // ---------------------------------------------

            await _cacheSyncService.SyncUserAsync(
                userId);

            // ---------------------------------------------
            // RELOAD FROM CACHE
            // ---------------------------------------------

            await ReloadUsersFromCacheAsync(
                companyId);

            SelectedUser =
                Users.FirstOrDefault(
                    x => x.Id == userId);

            CancelForm();
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage =
                "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // =========================================================
    // DELETE USER
    // =========================================================

    private async Task DeleteUserAsync()
    {
        if (!CanEditUser())
            return;

        if (SelectedUser is null)
            return;

        // =====================================================
        // PREVENT SELF DELETE
        // =====================================================

        if (SelectedUser.Id ==
            _session.UserId)
        {
            ErrorMessage =
                "You cannot delete your own account.";

            return;
        }

        // =====================================================
        // SUPER ADMIN PROTECTION
        // =====================================================

        if (SelectedUser.Role ==
                UserRole.SuperAdmin &&
            _session.Role !=
                UserRole.SuperAdmin)
        {
            ErrorMessage =
                "Only a SuperAdmin can delete a SuperAdmin.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var userId =
                SelectedUser.Id;

            var deleted =
                await _apiClient.DeleteUserAsync(
                    userId);

            if (!deleted)
            {
                ErrorMessage =
                    "Unable to delete the user.";

                return;
            }

            // =================================================
            // REMOVE FROM LOCAL CACHE
            // =================================================

            await _userCache.DeleteAsync(
                userId);

            // =================================================
            // RELOAD FROM CACHE
            // =================================================

            await ReloadUsersFromCacheAsync(
                EffectiveCompanyId);

            SelectedUser = null;

            CancelForm();
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage =
                "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // =========================================================
    // CACHE RELOAD
    // =========================================================

    private async Task ReloadUsersFromCacheAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        Users.Clear();

        var cachedUsers =
            await _userCache.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

        foreach (var user in cachedUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Users.Add(user);
        }

        RaiseCounts();
    }

    // =========================================================
    // CANCEL
    // =========================================================

    private async Task CancelEditAsync()
    {
        if (!CanCancelEdit())
            return;

        CancelForm();

        await Task.CompletedTask;

        RaiseCommandStates();
    }

    private void CancelForm()
    {
        IsEditing = false;

        FormEmail =
            string.Empty;

        FormDisplayName =
            string.Empty;

        FormRole =
            UserRole.Viewer;

        FormIsActive =
            true;
    }

    // =========================================================
    // BACK
    // =========================================================

    private async Task BackAsync()
    {
        if (_session.Role ==
            UserRole.SuperAdmin)
        {
            _navigationService
                .NavigateTo<CompaniesPage>();
        }
        else
        {
            _navigationService
                .NavigateTo<GatewayPage>();
        }

        await Task.CompletedTask;
    }

    // =========================================================
    // COUNTS
    // =========================================================

    private void RaiseCounts()
    {
        OnPropertyChanged(
            nameof(TotalUsers));

        OnPropertyChanged(
            nameof(ActiveUsers));

        OnPropertyChanged(
            nameof(InactiveUsers));

        OnPropertyChanged(
            nameof(AdministratorCount));

        OnPropertyChanged(
            nameof(TechnicianCount));

        OnPropertyChanged(
            nameof(ViewerCount));

        OnPropertyChanged(
            nameof(SuperAdminCount));
    }

    // =========================================================
    // COMMAND STATES
    // =========================================================

    private void RaiseCommandStates()
    {
        BackCommand?
            .RaiseCanExecuteChanged();

        RefreshCommand?
            .RaiseCanExecuteChanged();

        AddUserCommand?
            .RaiseCanExecuteChanged();

        EditUserCommand?
            .RaiseCanExecuteChanged();

        SaveUserCommand?
            .RaiseCanExecuteChanged();

        CancelEditCommand?
            .RaiseCanExecuteChanged();

        DeleteUserCommand?
            .RaiseCanExecuteChanged();
    }
}