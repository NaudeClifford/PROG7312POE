using SmartX.Application.Commands.Gateway;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartX.WPF.ViewModels.Gateway;

public class GatewaySetupViewModel : INotifyPropertyChanged
{
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _serialNumber;
    private string? _ipAddress;
    private bool _isBusy;
    private string? _errorMessage;
    private bool _isCreated;
    private Guid? _gatewayId;
    private readonly INavigationService _navigationService;

    public GatewaySetupViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService)
    {
        _apiClient = apiClient;
        _session = session;
        _navigationService = navigationService;

        CreateGatewayCommand = new AsyncRelayCommand(
            CreateGatewayAsync,
            CanCreateGateway);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;

            _name = value;
            OnPropertyChanged();

            CreateGatewayCommand.RaiseCanExecuteChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
                return;

            _description = value;
            OnPropertyChanged();
        }
    }

    public string? SerialNumber
    {
        get => _serialNumber;
        set
        {
            if (_serialNumber == value)
                return;

            _serialNumber = value;
            OnPropertyChanged();
        }
    }

    public string? IpAddress
    {
        get => _ipAddress;
        set
        {
            if (_ipAddress == value)
                return;

            _ipAddress = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();

            CreateGatewayCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsCreated
    {
        get => _isCreated;
        private set
        {
            if (_isCreated == value)
                return;

            _isCreated = value;
            OnPropertyChanged();

            CreateGatewayCommand.RaiseCanExecuteChanged();
        }
    }

    public Guid? GatewayId
    {
        get => _gatewayId;
        private set
        {
            if (_gatewayId == value)
                return;

            _gatewayId = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public AsyncRelayCommand CreateGatewayCommand { get; }

    private bool CanCreateGateway()
    {
        return !IsBusy &&
               !IsCreated &&
               !string.IsNullOrWhiteSpace(Name);
    }

    private async Task CreateGatewayAsync()
    {
        if (_session.CompanyId == Guid.Empty)
        {
            ErrorMessage =
                "No company is associated with this session.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var command = new CreateGatewayCommand
            {
                CompanyId = _session.CompanyId,

                Name = Name.Trim(),

                Description = Description.Trim(),

                SerialNumber =
                    string.IsNullOrWhiteSpace(SerialNumber)
                        ? null
                        : SerialNumber.Trim(),

                IpAddress =
                    string.IsNullOrWhiteSpace(IpAddress)
                        ? null
                        : IpAddress.Trim(),

                IsActive = true
            };

            var gatewayId =
                await _apiClient.CreateGatewayAsync(command);

            if (gatewayId == Guid.Empty)
            {
                ErrorMessage = "The gateway could not be created.";
                return;
            }

            GatewayId = gatewayId;
            IsCreated = true;

            _navigationService.NavigateTo<GatewayPage>();
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}