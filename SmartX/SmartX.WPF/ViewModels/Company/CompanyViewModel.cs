using AutoMapper;
using DomainCompany = SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Users;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Media;

namespace SmartX.WPF.ViewModels.Pages.Company;

public class CompanyViewModel :
    ViewModelBase,
    INavigationAware
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly SmartXSession _session;

    // =========================================================
    // FIELDS
    // =========================================================

    private bool _isLoaded;
    private bool _isBusy;
    private bool _isOnline;

    private string _errorMessage = string.Empty;

    private DomainCompany.Company? _selectedCompany;

    // Current company details
    private Guid _companyId;
    private string _companyName = string.Empty;
    private string _description = string.Empty;
    private bool _isActive;
    private DateTime _updatedAt;

    // =========================================================
    // COLLECTION
    // =========================================================

    public ObservableCollection<DomainCompany.Company> Companies { get; }
        = [];

    // =========================================================
    // SELECTED COMPANY
    // =========================================================

    public DomainCompany.Company? SelectedCompany
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

            RaiseCommandStates();
        }
    }

    // =========================================================
    // CURRENT COMPANY
    // =========================================================

    public Guid CompanyId
    {
        get => _companyId;

        private set => SetProperty(
            ref _companyId,
            value);
    }

    public string CompanyName
    {
        get => _companyName;

        private set => SetProperty(
            ref _companyName,
            value);
    }

    public string Description
    {
        get => _description;

        private set => SetProperty(
            ref _description,
            value);
    }

    public bool IsActive
    {
        get => _isActive;

        private set
        {
            if (!SetProperty(
                    ref _isActive,
                    value))
            {
                return;
            }

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;

        private set => SetProperty(
            ref _updatedAt,
            value);
    }

    public string StatusText =>
        IsActive
            ? "Active"
            : "Inactive";

    public Brush StatusColor =>
        IsActive
            ? Brushes.Green
            : Brushes.Red;

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

            OnPropertyChanged(
                nameof(IsBusyVisibility));

            RaiseCommandStates();
        }
    }

    public System.Windows.Visibility IsBusyVisibility =>
        IsBusy
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

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
    // COUNTS
    // =========================================================

    public int TotalCompanies =>
        Companies.Count;

    public int ActiveCompanies =>
        Companies.Count(x => x.IsActive);

    public int InactiveCompanies =>
        Companies.Count(x => !x.IsActive);

    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand BackCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand OpenUsersCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CompanyViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        SmartXSession session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _session = session;

        BackCommand =
            new AsyncRelayCommand(
                BackAsync);

        RefreshCommand =
            new AsyncRelayCommand(
                () => LoadAsync(),
                CanRefresh);

        OpenUsersCommand =
            new AsyncRelayCommand(
                OpenUsersAsync,
                CanOpenUsers);

        _session.PropertyChanged +=
            Session_PropertyChanged;
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    public void OnNavigatedTo(object parameter)
    {
        /*
         * No parameter:
         *     Load company list.
         *
         * Guid:
         *     Load a specific company.
         *
         * This allows multiple pages to use this same
         * ViewModel.
         */

        if (parameter is Guid companyId)
        {
            _ = LoadCompanyAsync(companyId);
            return;
        }

        _ = LoadAsync();
    }

    // =========================================================
    // LOAD COMPANIES
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            _isLoaded = true;

            IsBusy = true;
            ErrorMessage = string.Empty;

            Companies.Clear();
            SelectedCompany = null;

            // -------------------------------------------------
            // ROLE
            // -------------------------------------------------

            if (_session.Role != UserRole.SuperAdmin)
            {
                ErrorMessage =
                    "You do not have permission to view companies.";

                RaiseCounts();

                return;
            }

            // -------------------------------------------------
            // API
            // -------------------------------------------------

            IsOnline =
                await _apiClient.IsAvailableAsync(
                    cancellationToken);

            if (!IsOnline)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }

            // -------------------------------------------------
            // LOAD COMPANIES
            // -------------------------------------------------

            var companyDtos =
                await _apiClient.GetCompaniesAsync(
                    cancellationToken);

            foreach (var companyDto in companyDtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var company = new DomainCompany.Company
                {
                    Id = companyDto.Id,
                    Name = companyDto.Name,
                    Description = companyDto.Description,
                    IsActive = companyDto.IsActive,
                    UpdatedAt = companyDto.UpdatedAt
                };

                Companies.Add(company);

                Companies.Add(company);
            }

            RaiseCounts();
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

    // =========================================================
    // LOAD CURRENT / SELECTED COMPANY
    // =========================================================

    public async Task LoadCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // -------------------------------------------------
            // AUTHENTICATION
            // -------------------------------------------------

            if (!_session.IsAuthenticated)
            {
                ErrorMessage =
                    "You are not authenticated.";

                return;
            }

            // -------------------------------------------------
            // COMPANY
            // -------------------------------------------------

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company was specified.";

                return;
            }

            // -------------------------------------------------
            // API
            // -------------------------------------------------

            IsOnline =
                await _apiClient.IsAvailableAsync(
                    cancellationToken);

            if (!IsOnline)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }

            var company =
                await _apiClient.GetCompanyByIdAsync(
                    companyId,
                    cancellationToken);

            if (company is null)
            {
                ErrorMessage =
                    "The company could not be found.";

                return;
            }

            // -------------------------------------------------
            // UPDATE VIEWMODEL
            // -------------------------------------------------

            CompanyId =
                company.Id;

            CompanyName =
                company.Name ?? string.Empty;

            Description =
                company.Description ?? string.Empty;

            IsActive =
                company.IsActive;

            UpdatedAt =
                company.UpdatedAt;

            // -------------------------------------------------
            // UPDATE SELECTED COMPANY
            // -------------------------------------------------

            var existing =
                Companies.FirstOrDefault(
                    x => x.Id == company.Id);

            if (existing != null)
            {
                SelectedCompany = existing;
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

    // =========================================================
    // OPEN USERS
    // =========================================================

    private bool CanOpenUsers()
    {
        return !IsBusy &&
               IsOnline &&
               _session.Role == UserRole.SuperAdmin &&
               SelectedCompany != null;
    }

    private async Task OpenUsersAsync()
    {
        if (!CanOpenUsers())
            return;

        if (SelectedCompany is null)
            return;

        _session.SelectCompany(
            SelectedCompany.Id,
            SelectedCompany.Name);

        _navigationService
            .NavigateTo<UsersPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private bool CanRefresh()
    {
        return !IsBusy &&
               _session.Role == UserRole.SuperAdmin;
    }

    // =========================================================
    // BACK
    // =========================================================

    private async Task BackAsync()
    {
        _navigationService
            .NavigateTo<HomePage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // COUNTS
    // =========================================================

    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(TotalCompanies));
        OnPropertyChanged(nameof(ActiveCompanies));
        OnPropertyChanged(nameof(InactiveCompanies));
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

        OpenUsersCommand?
            .RaiseCanExecuteChanged();
    }

    // =========================================================
    // SESSION
    // =========================================================

    private void Session_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.CompanyId))
        {
            RaiseCommandStates();
        }
    }
}