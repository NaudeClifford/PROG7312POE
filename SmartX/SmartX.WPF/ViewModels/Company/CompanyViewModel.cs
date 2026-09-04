using AutoMapper;
using SmartX.Application.Requests.Company;
using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.Users;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using DomainCompany = SmartX.Domain.Entities;

namespace SmartX.WPF.ViewModels.Pages.Company;

public class CompanyViewModel :
    ViewModelBase,
    INavigationAware
{
    // DEPENDENCIES

    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly IMapper _mapper;
    
    
    // FIELDS

    private DomainCompany.Company? _selectedCompany;

    // Current company details
    private Guid _companyId;
    private string _companyName = string.Empty;
    private string _description = string.Empty;
    private bool _isActive;
    private DateTime _updatedAt;

    // COLLECTION
    public ObservableCollection<DomainCompany.Company> Companies { get; }
        = [];

    // SELECTED COMPANY

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

    // CURRENT COMPANY
    public Guid CompanyId
    {
        get => _companyId;

        private set => SetProperty(
            ref _companyId,
            value);
    }


    private string _editCompanyName = string.Empty;
    private string _editDescription = string.Empty;

    public string EditCompanyName
    {
        get => _editCompanyName;
        set
        {
            if (!SetProperty(ref _editCompanyName, value))
                return;

            SaveCompanyCommand?.RaiseCanExecuteChanged();
        }
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(
            ref _editDescription,
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
    
    // COUNTS

    public int TotalCompanies =>
        Companies.Count;

    public int ActiveCompanies =>
        Companies.Count(x => x.IsActive);

    public int InactiveCompanies =>
        Companies.Count(x => !x.IsActive);

    // COMMANDS

    public AsyncRelayCommand BackCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand OpenUsersCommand { get; }

    public AsyncRelayCommand RequestDeletionCommand { get; }

    public AsyncRelayCommand DeleteCompanyCommand { get; }

    // CONSTRUCTOR

    public CompanyViewModel(
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        IMapper mapper,
        IConnectivityService connectivityService,
        SmartXSession session) : base(connectivityService, session)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
        _mapper = mapper;

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

        DeleteCompanyCommand =
    new AsyncRelayCommand(
        DeleteCompanyAsync,
        CanDeleteCompany);


        RequestDeletionCommand =
    new AsyncRelayCommand(
        RequestDeletionAsync,
        CanRequestDeletion);


        SaveCompanyCommand =
    new AsyncRelayCommand(
        SaveCompanyAsync,
        CanSaveCompany);

        CancelEditCommand =
            new AsyncRelayCommand(
                CancelEditAsync,
                () => !IsBusy);

    }


    public bool IsAdministrator =>
    Session.Role == UserRole.Administrator ||
            Session.Role == UserRole.SuperAdmin;

    public Visibility AdministratorEditVisibility =>
        IsAdministrator
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility ReadOnlyVisibility =>
        IsAdministrator
            ? Visibility.Collapsed
            : Visibility.Visible;

    public AsyncRelayCommand SaveCompanyCommand { get; }

    public AsyncRelayCommand CancelEditCommand { get; }

    // NAVIGATION

    public void OnNavigatedTo(object parameter)
    {

        if (parameter is Guid companyId)
        {
            _ = LoadCompanyAsync(companyId);
            return;
        }

        _ = LoadAsync();
    }

    // LOAD COMPANIES

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Companies.Clear();
            SelectedCompany = null;

            // ROLE

            if (Session?.Role != UserRole.SuperAdmin)
            {
                ErrorMessage =
                    "You do not have permission to view companies.";

                RaiseCounts();

                return;
            }

            // API

            if (!await CheckOnlineAsync(cancellationToken))
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }

            // LOAD COMPANIES

            var companyDtos =
                await _apiClient.GetCompaniesAsync(
                    cancellationToken);

            foreach (var companyDto in companyDtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Companies.Add( _mapper.Map<DomainCompany.Company>(companyDto));

            }

            RaiseCounts();
        }
        catch (OperationCanceledException)
        {
            throw;
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
        }
    }

    private bool CanDeleteCompany()
    {
        return !IsBusy &&
               IsOnline &&
               Session.Role == UserRole.SuperAdmin &&
               SelectedCompany is not null &&
               SelectedCompany.DeletionRequested;
    }

    private async Task DeleteCompanyAsync()
    {
        if (!CanDeleteCompany())
            return;

        if (SelectedCompany is null)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (!await RequireOnlineAsync())
                return;

            var companyId = SelectedCompany.Id;

            var success =
                await _apiClient.DeleteCompanyAsync(companyId);

            if (!success)
            {
                ErrorMessage =
                    "The company could not be deleted.";

                return;
            }

            Companies.Remove(SelectedCompany);

            SelectedCompany = null;

            CompanyId = Guid.Empty;

            CompanyName = string.Empty;
            Description = string.Empty;
            EditCompanyName = string.Empty;
            EditDescription = string.Empty;

            RaiseCounts();
        }
        catch (OperationCanceledException)
        {
            throw;
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


    // LOAD CURRENT / SELECTED COMPANY
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

            // AUTHENTICATION

            if (!Session.IsAuthenticated)
            {
                ErrorMessage =
                    "You are not authenticated.";

                return;
            }

            // COMPANY

            if (companyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company was specified.";

                return;
            }

            // API

            if (!await CheckOnlineAsync(cancellationToken))
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }

            var companyDTO =
                await _apiClient.GetCompanyByIdAsync(
                    companyId,
                    cancellationToken);

            if (companyDTO is null)
            {
                ErrorMessage =
                    "The company could not be found.";

                return;
            }

            // UPDATE VIEWMODEL
            var company = _mapper.Map<DomainCompany.Company>(companyDTO);
            CompanyId =
                company.Id;

            CompanyName =
                company.Name ?? string.Empty;

            Description =
                company.Description ?? string.Empty;

            EditCompanyName = CompanyName;
            EditDescription = Description;

            IsActive =
                company.IsActive;

            UpdatedAt =
                company.UpdatedAt;


            // UPDATE SELECTED COMPANY

            var existing =
                Companies.FirstOrDefault(
                    x => x.Id == company.Id);

            if (existing is not null)
            {
                SelectedCompany = existing;
            }
            else
            {
                Companies.Add(company);
                SelectedCompany = company;
            }
            RaiseCounts();

        }
        catch (OperationCanceledException)
        {
            throw;
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

    // OPEN USERS
    private bool CanOpenUsers()
    {
        return !IsBusy &&
               IsOnline &&
               Session.Role == UserRole.SuperAdmin &&
               SelectedCompany != null;
    }

    private async Task OpenUsersAsync()
    {
        if (!CanOpenUsers())
            return;

        if (SelectedCompany is null)
            return;

        Session.SelectCompany(
            SelectedCompany.Id,
            SelectedCompany.Name);

        _navigationService
            .NavigateTo<UsersPage>();

        await Task.CompletedTask;
    }

    // REFRESH
    private bool CanRefresh()
    {
        return !IsBusy &&
               Session.Role == UserRole.SuperAdmin;
    }

    // BACK
    private async Task BackAsync()
    {
        _navigationService
            .NavigateTo<HomePage>();

        await Task.CompletedTask;
    }

    // COUNTS
    private void RaiseCounts()
    {
        OnPropertyChanged(nameof(TotalCompanies));
        OnPropertyChanged(nameof(ActiveCompanies));
        OnPropertyChanged(nameof(InactiveCompanies));
    }

    // COMMAND STATES
    protected override void RaiseCommandStates()
    {
        BackCommand?
            .RaiseCanExecuteChanged();

        RefreshCommand?
            .RaiseCanExecuteChanged();

        OpenUsersCommand?
            .RaiseCanExecuteChanged();

        DeleteCompanyCommand?
    .RaiseCanExecuteChanged();


        SaveCompanyCommand?
    .RaiseCanExecuteChanged();

        RequestDeletionCommand?
    .RaiseCanExecuteChanged();

        CancelEditCommand?
            .RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(AdministratorEditVisibility));
        OnPropertyChanged(nameof(ReadOnlyVisibility));
        OnPropertyChanged(nameof(RequestDeletionVisibility));


    }

    public Visibility RequestDeletionVisibility =>
    Session.Role == UserRole.Administrator ||
    Session.Role == UserRole.SuperAdmin
        ? Visibility.Visible
        : Visibility.Collapsed;


    // SESSION
    protected override void OnSessionPropertyChanged(
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.SelectedCompanyId))
                RaiseCommandStates();
        
    }
    // CONNECTIVITY

    protected override void RaiseConnectivityState()
    {
        RaiseCommandStates();
    }


    private bool CanSaveCompany()
    {
        return !IsBusy &&
               IsOnline &&
               IsAdministrator &&
               CompanyId != Guid.Empty &&
               !string.IsNullOrWhiteSpace(EditCompanyName);
    }


    private async Task SaveCompanyAsync()
    {
        if (!CanSaveCompany())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (!await RequireOnlineAsync())
                return;

            var request = new UpdateCompanyRequest
            {
                Id = CompanyId,
                Name = EditCompanyName.Trim(),
                Description = EditDescription?.Trim() ?? string.Empty
            };


            var success =
                await _apiClient.UpdateCompanyAsync(request);

            if (!success)
            {
                ErrorMessage =
                    "The company could not be updated.";

                return;
            }

            // Update the current saved values
            CompanyName = request.Name;
            Description = request.Description;

            // Keep edit values synchronized
            EditCompanyName = CompanyName;
            EditDescription = Description;

            // Update selected company if necessary
            if (SelectedCompany is not null)
            {
                SelectedCompany.Name = CompanyName;
                SelectedCompany.Description = Description;
            }

            ErrorMessage = string.Empty;

        }
        catch (OperationCanceledException)
        {
            throw;
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

    private Task CancelEditAsync()
    {
        if (IsBusy)
            return Task.CompletedTask;

        EditCompanyName = CompanyName;
        EditDescription = Description;

        return Task.CompletedTask;
    }


    private bool CanRequestDeletion()
    {
        return !IsBusy &&
               IsOnline &&
               Session.IsAuthenticated &&
               CompanyId != Guid.Empty;
    }
    private async Task RequestDeletionAsync()
    {
        if (!CanRequestDeletion())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (!await RequireOnlineAsync())
                return;

            var success =
                await _apiClient.RequestCompanyDeletionAsync(
                    CompanyId);

            if (!success)
            {
                ErrorMessage =
                    "The company deletion request could not be submitted.";

                return;
            }

            ErrorMessage =
                "Your company deletion request has been submitted.";
        }
        catch (OperationCanceledException)
        {
            throw;
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

}