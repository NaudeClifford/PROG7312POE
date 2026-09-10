using SmartX.Application.Authentication;
using SmartX.Application.Requests.User;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;

namespace SmartX.WPF.ViewModels.Pages.Users;

public class UsersViewModel :
    ViewModelBase,
    INavigationAware
{
    private readonly ILocalUserCache _userCache;
    private readonly ISmartXApiClient _apiClient;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isSynchronizingCompanySelection;

    public enum UserMode
    {
        List,
        Create,
        Edit
    }

    private UserMode _mode = UserMode.List;

    public UserMode Mode
    {
        get => _mode;

        private set
        {
            if (!SetProperty(ref _mode, value))
                return;

            OnPropertyChanged(nameof(IsListMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(IsEditMode));

            RaiseCommandStates();
        }
    }

    public bool IsListMode =>
        Mode == UserMode.List;

    public bool IsCreateMode =>
        Mode == UserMode.Create;

    public bool IsEditMode =>
        Mode == UserMode.Edit;

    private Guid? _editingUserId;

    public Guid? EditingUserId
    {
        get => _editingUserId;

        private set
        {
            if (!SetProperty(ref _editingUserId, value))
                return;

            RaiseCommandStates();
        }
    }

    private UserDto? _selectedUser;

    public UserDto? SelectedUser
    {
        get => _selectedUser;

        set
        {
            if (!SetProperty(ref _selectedUser, value))
                return;

            if (value is not null)
                LoadUserIntoForm(value);

            RaiseCommandStates();
        }
    }

    private string _formDisplayName = string.Empty;
    private string _formEmail = string.Empty;
    private string _formPassword = string.Empty;
    private UserRole _formRole;
    private bool _formIsActive = true;

    public string FormDisplayName
    {
        get => _formDisplayName;

        set
        {
            if (!SetProperty(ref _formDisplayName, value))
                return;

            RaiseCommandStates();
        }
    }

    public string FormEmail
    {
        get => _formEmail;

        set
        {
            if (!SetProperty(ref _formEmail, value))
                return;

            RaiseCommandStates();
        }
    }

    public string FormPassword
    {
        get => _formPassword;

        set
        {
            if (!SetProperty(ref _formPassword, value))
                return;

            RaiseCommandStates();
        }
    }

    public UserRole FormRole
    {
        get => _formRole;

        set
        {
            if (!SetProperty(ref _formRole, value))
                return;

            RaiseCommandStates();
        }
    }

    public bool FormIsActive
    {
        get => _formIsActive;

        set
        {
            if (!SetProperty(ref _formIsActive, value))
                return;

            RaiseCommandStates();
        }
    }

    public ObservableCollection<UserRole> AvailableRoles { get; } =
        [];

    public ObservableCollection<string> RoleFilters { get; } =
    [
        "All",
        "SuperAdmin",
        "Administrator",
        "Technician",
        "Viewer"
    ];

    public ObservableCollection<string> StatusFilters { get; } =
    [
        "All",
        "Active",
        "Inactive"
    ];

    public bool IsSuperAdmin =>
        Session.Role == UserRole.SuperAdmin;

    public bool IsAdministrator =>
        Session.Role == UserRole.Administrator;

    public bool HasCompany =>
        EffectiveCompanyId != Guid.Empty;

    public bool HasSelectedCompany =>
        Session.SelectedCompanyId != Guid.Empty;

    public Guid EffectiveCompanyId =>
        Session.Role == UserRole.SuperAdmin
            ? Session.SelectedCompanyId
            : Session.CompanyId;

    private ObservableCollection<CompanyDto> _companies = [];

    public ObservableCollection<CompanyDto> Companies =>
        _companies;

    private CompanyDto? _selectedCompany;

    public CompanyDto? SelectedCompany
    {
        get => _selectedCompany;

        set
        {
            if (!SetProperty(
                    ref _selectedCompany,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedCompany));
            OnPropertyChanged(nameof(EffectiveCompanyId));
            OnPropertyChanged(nameof(HasCompany));

            RaiseCommandStates();

            if (Session.Role != UserRole.SuperAdmin ||
                _isSynchronizingCompanySelection)
            {
                return;
            }

            if (value is not null)
            {
                Session.SelectCompany(
                    value.Id,
                    value.Name);
            }
            else
            {
                Session.ClearSelectedCompany();

                Users.Clear();
                FilteredUsers.Clear();

                RaiseCounts();
            }
        }
    }



    private string _nameFilter = string.Empty;
    private string _emailFilter = string.Empty;
    private string _selectedRoleFilter = "All";
    private string _statusFilter = "All";

    public string NameFilter
    {
        get => _nameFilter;

        set
        {
            if (!SetProperty(
                    ref _nameFilter,
                    value))
            {
                return;
            }

            ApplyFilters();
        }
    }

    public string EmailFilter
    {
        get => _emailFilter;

        set
        {
            if (!SetProperty(
                    ref _emailFilter,
                    value))
            {
                return;
            }

            ApplyFilters();
        }
    }

    public string SelectedRoleFilter
    {
        get => _selectedRoleFilter;

        set
        {
            if (!SetProperty(
                    ref _selectedRoleFilter,
                    value))
            {
                return;
            }

            ApplyFilters();
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;

        set
        {
            if (!SetProperty(
                    ref _statusFilter,
                    value))
            {
                return;
            }

            ApplyFilters();
        }
    }

    public ObservableCollection<UserDto> Users { get; } =
        [];

    public ObservableCollection<UserDto> FilteredUsers { get; } =
        [];

    public int TotalUsers =>
        FilteredUsers.Count;

    public int ActiveUsers =>
        FilteredUsers.Count(x => x.IsActive);

    public int TechnicianCount =>
        FilteredUsers.Count(x =>
            x.Role == UserRole.Technician);

    public int ViewerCount =>
        FilteredUsers.Count(x =>
            x.Role == UserRole.Viewer);

    public int AdministratorCount =>
        FilteredUsers.Count(x =>
            x.Role == UserRole.Administrator);

    public AsyncRelayCommand AddUserCommand { get; }

    public AsyncRelayCommand EditUserCommand { get; }

    public AsyncRelayCommand DeleteUserCommand { get; }

    public AsyncRelayCommand SaveUserCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand ClearFiltersCommand { get; }

    public UsersViewModel(
        ILocalUserCache userCache,
        ISmartXApiClient apiClient,
        SmartXSession session,
        ICacheSyncService cacheSyncService,
        IConnectivityService connectivityService,
        IAuthenticationService authenticationService)
        : base(
            connectivityService,
            session)
    {
        _userCache = userCache
            ?? throw new ArgumentNullException(nameof(userCache));

        _apiClient = apiClient
            ?? throw new ArgumentNullException(nameof(apiClient));

        _cacheSyncService = cacheSyncService
            ?? throw new ArgumentNullException(nameof(cacheSyncService));

        _authenticationService = authenticationService
            ?? throw new ArgumentNullException(nameof(authenticationService));

        UpdateAvailableRoles();

        AddUserCommand =
            new AsyncRelayCommand(
                AddUserAsync,
                CanAddUser);

        EditUserCommand =
            new AsyncRelayCommand(
                EditUserAsync,
                CanEditUser);

        DeleteUserCommand =
            new AsyncRelayCommand(
                DeleteUserAsync,
                CanDeleteUser);

        SaveUserCommand =
            new AsyncRelayCommand(
                SaveUserAsync,
                CanSaveUser);

        CancelCommand =
            new AsyncRelayCommand(
                CancelAsync,
                CanCancel);

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshAsync,
                CanRefresh);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ClearFiltersAsync);
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Guid userId)
        {
            Mode = UserMode.Edit;
            EditingUserId = userId;

            _ = LoadUserForEditAsync(userId);

            return;
        }

        if (parameter is string mode &&
            mode.Equals(
                "Create",
                StringComparison.OrdinalIgnoreCase))
        {
            Mode = UserMode.Create;

            ResetForm();

            _ = LoadCreateModeAsync();

            return;
        }

        Mode = UserMode.List;
        EditingUserId = null;

        _ = LoadAsync();
    }

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            IsLoaded = false;
            ErrorMessage = string.Empty;

            Users.Clear();
            FilteredUsers.Clear();

            RaiseCounts();

            if (Session.Role == UserRole.SuperAdmin)
            {           
                await LoadCompaniesAsync(cancellationToken);

                await LoadSuperAdminAsync(cancellationToken);
            }
            else
                await LoadUsersFromCacheAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;

            RaiseCommandStates();
        }
    }

    private async Task LoadSuperAdminAsync(
        CancellationToken cancellationToken)
    {
        if (!IsOnline)
        {
            ErrorMessage =
                "You must be online to manage users as SuperAdmin.";

            return;
        }

        if (Session.SelectedCompanyId == Guid.Empty)
        {
            ErrorMessage =
                "Select a company before managing users.";

            return;
        }

        var users =
            await _apiClient.GetUsersByCompanyIdAsync(
                Session.SelectedCompanyId,
                cancellationToken);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Users.Add(user);
        }

        ApplyFilters();
    }

    private bool CanRefresh()
    {
        return !IsBusy &&
               IsOnline &&
               EffectiveCompanyId != Guid.Empty;
    }

    private async Task RefreshAsync()
    {
        if (!CanRefresh())
            return;

        await LoadAsync();
    }

    private void ApplyFilters()
    {
        FilteredUsers.Clear();

        IEnumerable<UserDto> query = Users;

        if (!string.IsNullOrWhiteSpace(NameFilter))
        {
            var filter = NameFilter.Trim();

            query =
                query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.DisplayName) &&
                    x.DisplayName.Contains(
                        filter,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(EmailFilter))
        {
            var filter = EmailFilter.Trim();

            query =
                query.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Email) &&
                    x.Email.Contains(
                        filter,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedRoleFilter) &&
            SelectedRoleFilter != "All")
        {
            if (Enum.TryParse<UserRole>(
                    SelectedRoleFilter,
                    true,
                    out var selectedRole))
            {
                query =
                    query.Where(x =>
                        x.Role == selectedRole);
            }
        }

        if (StatusFilter == "Active")
        {
            query =
                query.Where(x =>
                    x.IsActive);
        }
        else if (StatusFilter == "Inactive")
        {
            query =
                query.Where(x =>
                    !x.IsActive);
        }

        foreach (var user in query)
        {
            FilteredUsers.Add(user);
        }

        RaiseCounts();
    }

    private Task ClearFiltersAsync()
    {
        NameFilter = string.Empty;
        EmailFilter = string.Empty;
        SelectedRoleFilter = "All";
        StatusFilter = "All";

        ApplyFilters();

        return Task.CompletedTask;
    }

    private async Task LoadCreateModeAsync()
    {
        try
        {
            IsBusy = true;
            IsLoaded = false;
            ErrorMessage = string.Empty;

            if (EffectiveCompanyId == Guid.Empty)
            {
                ErrorMessage =
                    IsSuperAdmin
                        ? "Select a company before creating a user."
                        : "No company is associated with this account.";

                return;
            }

            if (!IsOnline)
            {
                ErrorMessage =
                    "You are offline. Creating users is unavailable.";

                return;
            }

            UpdateAvailableRoles();

            if (AvailableRoles.Count == 0)
            {
                ErrorMessage =
                    "You do not have permission to create users.";

                return;
            }

            if (!AvailableRoles.Contains(FormRole))
            {
                FormRole = AvailableRoles[0];
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;

            RaiseCommandStates();
        }
    }

    private async Task LoadUserForEditAsync(
        Guid userId)
    {
        try
        {
            IsBusy = true;
            IsLoaded = false;
            ErrorMessage = string.Empty;

            if (Session.Role == UserRole.SuperAdmin)
            {
                await LoadSuperAdminUserForEditAsync(userId);
            }
            else
            {
                await LoadUserForEditFromCacheAsync(userId);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;

            RaiseCommandStates();
        }
    }

    private async Task LoadSuperAdminUserForEditAsync(
        Guid userId)
    {
        if (Session.SelectedCompanyId == Guid.Empty)
        {
            ErrorMessage =
                "Select a company before editing users.";

            return;
        }

        if (!IsOnline)
        {
            ErrorMessage =
                "You must be online to edit users as SuperAdmin.";

            return;
        }

        var user =
            await _apiClient.GetUserByIdAsync(
                userId);

        if (user is null)
        {
            ErrorMessage =
                "The selected user could not be found.";

            return;
        }

        if (user.CompanyId != Session.SelectedCompanyId)
        {
            ErrorMessage =
                "The selected user does not belong to the selected company.";

            return;
        }

        SelectedUser = user;
        EditingUserId = user.Id;

        LoadUserIntoForm(user);
    }

    private async Task LoadCompaniesAsync(
     CancellationToken cancellationToken = default)
    {
        if (Session.Role != UserRole.SuperAdmin)
            return;

        if (!IsOnline)
            return;

        var companies =
            await _apiClient.GetCompaniesAsync(
                cancellationToken);

        Companies.Clear();

        foreach (var company in companies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Companies.Add(company);
        }

        _isSynchronizingCompanySelection = true;

        try
        {
            SelectedCompany =
                Session.SelectedCompanyId == Guid.Empty
                    ? null
                    : Companies.FirstOrDefault(
                        x => x.Id == Session.SelectedCompanyId);
        }
        finally
        {
            _isSynchronizingCompanySelection = false;
        }
    }


    private bool CanAddUser()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               HasUserWritePermission() &&
               EffectiveCompanyId != Guid.Empty;
    }

    private async Task AddUserAsync()
    {
        if (!CanAddUser())
            return;

        EditingUserId = null;

        ResetForm();

        Mode = UserMode.Create;

        await LoadCreateModeAsync();
    }

    private bool CanEditUser()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               SelectedUser is not null &&
               EffectiveCompanyId != Guid.Empty &&
               HasUserWritePermission();
    }

    private Task EditUserAsync()
    {
        if (!CanEditUser())
            return Task.CompletedTask;

        if (SelectedUser is null)
            return Task.CompletedTask;

        EditingUserId = SelectedUser.Id;
        Mode = UserMode.Edit;
        LoadUserIntoForm(SelectedUser);

        return Task.CompletedTask;
    }


    private bool CanDeleteUser()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               SelectedUser is not null &&
               EffectiveCompanyId != Guid.Empty &&
               HasUserWritePermission();
    }

    private async Task RefreshUsersAfterCrudAsync()
    {
        var companyId = EffectiveCompanyId;

        if (companyId == Guid.Empty)
            return;

        try
        {
            ErrorMessage = string.Empty;

            Users.Clear();
            FilteredUsers.Clear();

            if (Session.Role == UserRole.SuperAdmin)
            {
                var users =
                    await _apiClient.GetUsersByCompanyIdAsync(
                        companyId,
                        CancellationToken.None);

                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            else
            {
                await _cacheSyncService.SyncUsersAsync(
                    companyId);

                var users =
                    await _userCache.GetByCompanyIdAsync(
                        companyId,
                        CancellationToken.None);

                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }

            ApplyFilters();
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to refresh the users list.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }


    private async Task DeleteUserAsync()
    {
        if (!CanDeleteUser())
            return;

        if (SelectedUser is null)
            return;

        var result =
            MessageBox.Show(
                $"Are you sure you want to delete '{SelectedUser.DisplayName}'?\n\nThis action cannot be undone.",
                "Delete User",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var deleted =
                await _apiClient.DeleteUserAsync(
                    SelectedUser.Id);

            if (!deleted)
            {
                ErrorMessage =
                    "Unable to delete the user.";

                return;
            }

            SelectedUser = null;

            await RefreshUsersAfterCrudAsync();

        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the SmartX API.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    private bool CanSaveUser()
    {
        if (!IsOnline ||
            IsBusy ||
            EffectiveCompanyId == Guid.Empty ||
            !HasUserWritePermission())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormDisplayName) ||
            string.IsNullOrWhiteSpace(FormEmail))
        {
            return false;
        }

        if (IsCreateMode)
        {
            return !string.IsNullOrWhiteSpace(FormPassword);
        }

        return IsEditMode &&
               EditingUserId.HasValue;
    }

    private async Task SaveUserAsync()
    {
        if (!CanSaveUser())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (IsCreateMode)
            {
                var firebaseResult =
                    await _authenticationService.SignUpAsync(
                        FormEmail.Trim(),
                        FormPassword);

                if (!firebaseResult.Success)
                {
                    ErrorMessage =
                        firebaseResult.ErrorMessage ??
                        "Unable to create the Firebase account.";

                    return;
                }

                if (string.IsNullOrWhiteSpace(
                        firebaseResult.UserId))
                {
                    ErrorMessage =
                        "Firebase did not return a valid user ID.";

                    return;
                }

                var request =
                    new CreateUserRequest
                    {
                        CompanyId =
                            EffectiveCompanyId,

                        FirebaseUid =
                            firebaseResult.UserId,

                        Email =
                            FormEmail.Trim(),

                        DisplayName =
                            FormDisplayName.Trim(),

                        Role =
                            FormRole,

                        IsActive =
                            true
                    };

                var userId =
                    await _apiClient.CreateUserAsync(
                        request);

                if (userId == Guid.Empty)
                {
                    ErrorMessage =
                        "The API did not return a valid user ID.";

                    return;
                }

                ResetForm();

                Mode = UserMode.List;

                await RefreshUsersAfterCrudAsync();

                return;
            }

            if (!EditingUserId.HasValue)
                return;

            if (SelectedUser is null)
            {
                ErrorMessage =
                    "The selected user could not be found.";

                return;
            }

            if (EffectiveCompanyId == Guid.Empty)
            {
                ErrorMessage =
                    "Select a company before updating a user.";

                return;
            }

            if (SelectedUser.CompanyId != EffectiveCompanyId)
            {
                ErrorMessage =
                    "The selected user does not belong to the selected company.";

                return;
            }

            var updateRequest =
                new UpdateUserRequest
                {
                    Id =
                        EditingUserId.Value,

                    CompanyId =
                        SelectedUser.CompanyId,

                    FirebaseUid =
                        SelectedUser.FirebaseUid,

                    DisplayName =
                        FormDisplayName.Trim(),

                    Role =
                        FormRole,

                    IsActive =
                        FormIsActive
                };

            var updated =
                await _apiClient.UpdateUserAsync(
                    updateRequest);

            if (!updated)
            {
                ErrorMessage =
                    "The user could not be updated.";

                return;
            }

            ResetForm();

            Mode = UserMode.List;

            await RefreshUsersAfterCrudAsync();

        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the SmartX API.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    private bool CanCancel()
    {
        return !IsBusy &&
               !IsListMode;
    }

    private async Task CancelAsync()
    {
        if (!CanCancel())
            return;

        ResetForm();

        Mode = UserMode.List;

        await LoadAsync();
    }

    private void LoadUserIntoForm(
        UserDto user)
    {
        FormDisplayName =
            user.DisplayName;

        FormEmail =
            user.Email;

        FormRole =
            user.Role;

        FormIsActive =
            user.IsActive;

        FormPassword =
            string.Empty;
    }

    private void ResetForm()
    {
        EditingUserId = null;
        SelectedUser = null;

        FormDisplayName =
            string.Empty;

        FormEmail =
            string.Empty;

        FormPassword =
            string.Empty;

        FormIsActive =
            true;

        UpdateAvailableRoles();

        if (AvailableRoles.Count > 0)
        {
            FormRole =
                AvailableRoles[0];
        }
    }

    private bool HasUserWritePermission()
    {
        return Session.Role is
            UserRole.SuperAdmin or
            UserRole.Administrator;
    }

    private void UpdateAvailableRoles()
    {
        AvailableRoles.Clear();

        if (Session.Role == UserRole.SuperAdmin)
        {
            foreach (var role in
                     Enum.GetValues<UserRole>())
            {
                AvailableRoles.Add(role);
            }

            return;
        }

        if (Session.Role == UserRole.Administrator)
        {
            AvailableRoles.Add(
                UserRole.Administrator);

            AvailableRoles.Add(
                UserRole.Technician);

            AvailableRoles.Add(
                UserRole.Viewer);
        }
    }

    private async Task LoadUsersFromCacheAsync(
        CancellationToken cancellationToken)
    {
        if (EffectiveCompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this account.";

            return;
        }

        if (IsOnline)
        {
            try
            {
                await _cacheSyncService.SyncUsersAsync(
                    EffectiveCompanyId,
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API. Showing cached users.";
            }
        }

        var users =
            await _userCache.GetByCompanyIdAsync(
                EffectiveCompanyId,
                cancellationToken);

        Users.Clear();

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Users.Add(user);
        }

        ApplyFilters();
    }

    private async Task LoadUserForEditFromCacheAsync(
        Guid userId)
    {
        if (EffectiveCompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this account.";

            return;
        }

        if (IsOnline)
        {
            try
            {
                await _cacheSyncService.SyncUserAsync(
                    userId);
            }
            catch (HttpRequestException)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API. Showing cached data.";
            }
        }

        var user =
            await _userCache.GetByIdAsync(
                userId);

        if (user is null)
        {
            ErrorMessage =
                "The selected user could not be found.";

            return;
        }

        if (user.CompanyId != EffectiveCompanyId)
        {
            ErrorMessage =
                "The selected user does not belong to the selected company.";

            return;
        }

        SelectedUser = user;

        EditingUserId = user.Id;

        LoadUserIntoForm(user);
    }

    private async Task LoadUsersForSelectedCompanyAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Users.Clear();
            FilteredUsers.Clear();

            RaiseCounts();

            if (Session.SelectedCompanyId == Guid.Empty)
            {
                return;
            }

            if (!IsOnline)
            {
                ErrorMessage =
                    "You must be online to manage users as SuperAdmin.";

                return;
            }

            var users =
                await _apiClient.GetUsersByCompanyIdAsync(
                    Session.SelectedCompanyId,
                    CancellationToken.None);

            foreach (var user in users)
            {
                Users.Add(user);
            }

            ApplyFilters();
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the SmartX API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    protected override async void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
        base.OnSessionPropertyChanged(e);

        if (e.PropertyName ==
            nameof(SmartXSession.SelectedCompanyId))
        {
            OnPropertyChanged(nameof(IsSuperAdmin));
            OnPropertyChanged(nameof(IsAdministrator));
            OnPropertyChanged(nameof(EffectiveCompanyId));
            OnPropertyChanged(nameof(HasCompany));
            OnPropertyChanged(nameof(HasSelectedCompany));

            UpdateAvailableRoles();

            RaiseCommandStates();

            if (Session.Role == UserRole.SuperAdmin)
            {
                ResetForm();
                await LoadUsersForSelectedCompanyAsync();
            }

            return;
        }

        if (e.PropertyName ==
            nameof(SmartXSession.CompanyId))
        {
            OnPropertyChanged(nameof(EffectiveCompanyId));
            OnPropertyChanged(nameof(HasCompany));

            RaiseCommandStates();

            if (Session.Role != UserRole.SuperAdmin)
                await LoadAsync();

            return;
        }

        if (e.PropertyName ==
            nameof(SmartXSession.Role))
        {
            OnPropertyChanged(nameof(IsSuperAdmin));
            OnPropertyChanged(nameof(IsAdministrator));
            OnPropertyChanged(nameof(EffectiveCompanyId));
            OnPropertyChanged(nameof(HasCompany));
            OnPropertyChanged(nameof(HasSelectedCompany));

            UpdateAvailableRoles();

            RaiseCommandStates();

            await LoadAsync();
        }
    }

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(TotalUsers));
        OnPropertyChanged(nameof(ActiveUsers));
        OnPropertyChanged(nameof(TechnicianCount));
        OnPropertyChanged(nameof(ViewerCount));
        OnPropertyChanged(nameof(AdministratorCount));
    }

    protected override void RaiseCommandStates()
    {
        AddUserCommand?.RaiseCanExecuteChanged();
        EditUserCommand?.RaiseCanExecuteChanged();
        DeleteUserCommand?.RaiseCanExecuteChanged();
        SaveUserCommand?.RaiseCanExecuteChanged();
        CancelCommand?.RaiseCanExecuteChanged();
        RefreshCommand?.RaiseCanExecuteChanged();
        ClearFiltersCommand?.RaiseCanExecuteChanged();
    }

    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }

    protected override void OnBusyStateChanged()
    {
        OnPropertyChanged(nameof(IsBusyVisibility));

        RaiseCommandStates();
    }
}
