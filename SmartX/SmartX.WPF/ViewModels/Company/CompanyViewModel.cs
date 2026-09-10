using SmartX.Application.Requests.Company;
using SmartX.Domain.Enums;
using SmartX.Shared.Mapping;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Company;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Home;
using SmartX.WPF.Views.Pages.SignUp;
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

    // Current company detail


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
            OnPropertyChanged(nameof(DeletionRequested));
            OnPropertyChanged(nameof(RequestDeletionVisibility));
            OnPropertyChanged(nameof(CancelDeletionVisibility));

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

    public bool DeletionRequested =>
    SelectedCompany?.DeletionRequested ?? false;

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
    public AsyncRelayCommand ForceDeleteCompanyCommand { get; }

    public AsyncRelayCommand CancelDeletionCommand { get; }
    public AsyncRelayCommand CompanyConfigurationCommand { get; }
    public AsyncRelayCommand ContinueToGatewaySetupCommand { get; }


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

        ContinueToGatewaySetupCommand =
    new AsyncRelayCommand(
        ContinueToGatewaySetupAsync,
        CanContinueToGatewaySetup);


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


        CancelDeletionCommand =
    new AsyncRelayCommand(
        CancelDeletionAsync,
        CanCancelDeletion);
        
        ForceDeleteCompanyCommand =
    new AsyncRelayCommand(
        ForceDeleteCompanyAsync,
        CanForceDeleteCompany);

        CompanyConfigurationCommand =
    new AsyncRelayCommand(
        OpenCompanyConfigurationAsync,
        CanOpenCompanyConfiguration);


    }
    private bool CanContinueToGatewaySetup()
    {
        return !IsBusy &&
               IsOnline &&
               Session.IsAuthenticated &&
               Session.Role == UserRole.Administrator &&
               CompanyId != Guid.Empty;
    }

    private async Task ContinueToGatewaySetupAsync()
    {
        if (!CanContinueToGatewaySetup())
            return;

        _navigationService.NavigateTo<GatewaySetupPage>();

        await Task.CompletedTask;
    }


    public bool IsAdministrator =>
    Session.Role == UserRole.Administrator ||
            Session.Role == UserRole.SuperAdmin;

    public Visibility AdministratorEditVisibility =>
        IsAdministrator
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility AdministratorOnlyVisibility =>
    Session.Role == UserRole.Administrator
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
        Guid companyId;

        if (parameter is Guid parameterCompanyId &&
            parameterCompanyId != Guid.Empty)
        {
            companyId = parameterCompanyId;
        }
        else
        {
            companyId = Session.SelectedCompanyId;
        }

        if (companyId == Guid.Empty)
        {
            ErrorMessage = "No current company is selected.";
            return;
        }

        _ = LoadCompanyAsync(companyId);
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

            if (!Session.IsAuthenticated)
            {
                ErrorMessage = "You are not authenticated.";
                return;
            }

            if (companyId == Guid.Empty)
            {
                ErrorMessage = "No company was specified.";
                return;
            }

            var companyDTO =
                await _apiClient.GetCompanyByIdAsync(
                    companyId,
                    cancellationToken);

            if (companyDTO is null)
            {
                ErrorMessage = "The company could not be found.";
                return;
            }

            var company =
                _mapper.Map<DomainCompany.Company>(companyDTO);

            // CURRENT COMPANY DETAILS

            CompanyId = company.Id;
            CompanyName = company.Name ?? string.Empty;
            Description = company.Description ?? string.Empty;
            IsActive = company.IsActive;
            UpdatedAt = company.UpdatedAt;

            // EDIT VALUES

            EditCompanyName = CompanyName;
            EditDescription = Description;

            // CURRENT SELECTED COMPANY

            SelectedCompany = company;
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


    private bool CanOpenCompanyConfiguration()
    {
        return !IsBusy &&
               IsOnline &&
               Session.IsAuthenticated &&
               Session.CompanyId != Guid.Empty &&
               Session.Role is UserRole.Administrator;
    }

    private async Task OpenCompanyConfigurationAsync()
    {
        if (!CanOpenCompanyConfiguration())
            return;

        _navigationService.NavigateTo<CompanyServicesPage>();

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

        CompanyConfigurationCommand?
    .RaiseCanExecuteChanged();



        SaveCompanyCommand?
    .RaiseCanExecuteChanged();

        RequestDeletionCommand?
    .RaiseCanExecuteChanged();
        
        ForceDeleteCompanyCommand?
    .RaiseCanExecuteChanged();

        CancelEditCommand?
            .RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(AdministratorOnlyVisibility));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(AdministratorEditVisibility));
        OnPropertyChanged(nameof(ReadOnlyVisibility));
        OnPropertyChanged(nameof(RequestDeletionVisibility));
        OnPropertyChanged(nameof(CancelDeletionVisibility));
        OnPropertyChanged(nameof(DeletionRequested));


    }

    public Visibility RequestDeletionVisibility =>
        Session.Role == UserRole.Administrator &&
        !DeletionRequested
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CancelDeletionVisibility =>
        Session.Role == UserRole.Administrator &&
        DeletionRequested
            ? Visibility.Visible
            : Visibility.Collapsed;


    // SESSION
    protected override void OnSessionPropertyChanged(
    PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.Role) ||
            e.PropertyName == nameof(SmartXSession.SelectedCompanyId))
        {
            OnPropertyChanged(nameof(CurrentCompanyId));
            RaiseCommandStates();

            if (e.PropertyName == nameof(SmartXSession.SelectedCompanyId) &&
                Session.SelectedCompanyId != Guid.Empty)
            {
                _ = LoadCompanyAsync(Session.SelectedCompanyId);
            }
        }
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

    private bool CanCancelDeletion()
    {
        return !IsBusy &&
               IsOnline &&
               Session.IsAuthenticated &&
               Session.Role == UserRole.Administrator &&
               CompanyId != Guid.Empty &&
               _selectedCompany?.DeletionRequested == true;
    }

    private async Task CancelDeletionAsync()
    {
        if (!CanCancelDeletion())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var success =
                await _apiClient.CancelDeletionAsync(
                    CompanyId);

            if (!success)
            {
                ErrorMessage =
                    "The company deletion request could not be cancelled.";

                return;
            }

            if (SelectedCompany is not null)
            {
                SelectedCompany.DeletionRequested = false;
            }

            OnPropertyChanged(nameof(DeletionRequested));
            OnPropertyChanged(nameof(RequestDeletionVisibility));
            OnPropertyChanged(nameof(CancelDeletionVisibility));
            RaiseCommandStates();

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

    private bool CanRequestDeletion()
    {
        return !IsBusy &&
               IsOnline &&
               Session.IsAuthenticated &&
               Session.Role == UserRole.Administrator &&
               CompanyId != Guid.Empty &&
               (SelectedCompany is null ||
                !SelectedCompany.DeletionRequested);
    }

    private async Task RequestDeletionAsync()
    {
        if (!CanRequestDeletion())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var success =
                await _apiClient.RequestCompanyDeletionAsync(
                    CompanyId);

            if (SelectedCompany is not null)
            {
                SelectedCompany.DeletionRequested = true;
            }

            OnPropertyChanged(nameof(DeletionRequested));
            OnPropertyChanged(nameof(RequestDeletionVisibility));
            OnPropertyChanged(nameof(CancelDeletionVisibility));
            RaiseCommandStates();

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

    private bool CanForceDeleteCompany()
    {
        return !IsBusy &&
               IsOnline &&
               Session.Role == UserRole.SuperAdmin &&
               SelectedCompany is not null;
    }

    private async Task ForceDeleteCompanyAsync()
    {
        if (!CanForceDeleteCompany())
            return;

        if (SelectedCompany is null)
            return;

        var companyName =
            SelectedCompany.Name;

        var firstConfirmation =
            MessageBox.Show(
                $"You are about to permanently delete '{companyName}'.\n\n" +
                "This will remove the company and ALL linked data, including:\n\n" +
                "• Users\n" +
                "• Sensors\n" +
                "• Gateways\n" +
                "• Other company-linked records\n\n" +
                "This action cannot be undone.\n\n" +
                "Are you sure you want to continue?",
                "FORCE DELETE COMPANY",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (firstConfirmation != MessageBoxResult.Yes)
            return;

        var secondConfirmation =
            MessageBox.Show(
                $"FINAL CONFIRMATION\n\n" +
                $"Permanently delete '{companyName}' and ALL linked users, " +
                "sensors, gateways, and other company data?\n\n" +
                "There is no undo for this operation.",
                "Confirm Permanent Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

        if (secondConfirmation != MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var companyId =
                SelectedCompany.Id;

            var success =
                await _apiClient.DeleteCompanyAsync(
                    companyId);

            if (!success)
            {
                ErrorMessage =
                    "The company could not be permanently deleted.";

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

}