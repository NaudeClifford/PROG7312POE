using SmartX.Application.Commands.Sensors;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.WPF.Navigation;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Sensor;

namespace SmartX.WPF.ViewModels.Pages.Sensor;

public class SensorSetupViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;

    private string _name = string.Empty;
    private string _deviceIdentifier = string.Empty;
    private string _location = string.Empty;
    private SmartX.Domain.Enums.SensorCategory _category;
    private string _description = string.Empty;

    private string _gatewayName = string.Empty;

    private bool _isBusy;
    private bool _isCreated;

    private string? _errorMessage;
    public Array SensorCategories => Enum.GetValues(typeof(
        SensorCategory));

    // CONSTRUCTOR
    public SensorSetupViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        INavigationService navigationService)
    {
        _apiClient = apiClient;
        _session = session;
        _navigationService = navigationService;

        CreateSensorCommand = new AsyncRelayCommand(
            CreateSensorAsync,
            CanCreateSensor);
    }

    // GATEWAY
    public string GatewayName
    {
        get => _gatewayName;

        private set => SetProperty(
            ref _gatewayName,
            value);
    }

    // SENSOR PROPERTIES
    public string Name
    {
        get => _name;

        set
        {
            if (!SetProperty(
                    ref _name,
                    value))
            {
                return;
            }

            CreateSensorCommand.RaiseCanExecuteChanged();
        }
    }

    public string DeviceIdentifier
    {
        get => _deviceIdentifier;

        set
        {
            if (!SetProperty(
                    ref _deviceIdentifier,
                    value))
            {
                return;
            }

            CreateSensorCommand.RaiseCanExecuteChanged();
        }
    }

    public string Location
    {
        get => _location;

        set => SetProperty(
            ref _location,
            value);
    }

    public SmartX.Domain.Enums.SensorCategory Category
    {
        get => _category;

        set => SetProperty(
            ref _category,
            value);
    }

    public string Description
    {
        get => _description;

        set => SetProperty(
            ref _description,
            value);
    }

    // STATE
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

            CreateSensorCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsCreated
    {
        get => _isCreated;

        private set
        {
            if (!SetProperty(
                    ref _isCreated,
                    value))
            {
                return;
            }

            CreateSensorCommand.RaiseCanExecuteChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set => SetProperty(
            ref _errorMessage,
            value);
    }

    // COMMAND
    public AsyncRelayCommand CreateSensorCommand { get; }

    // LOAD
    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (_session.CompanyId == Guid.Empty)
        {
            ErrorMessage = "No company is associated with this session.";

            return;
        }

        if (!_session.GatewayId.HasValue ||
            _session.GatewayId.Value == Guid.Empty)
        {
            ErrorMessage = "No gateway is currently selected.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var gateway = await _apiClient.GetGatewayByIdAsync(
                    _session.GatewayId.Value,
                    cancellationToken);

            if (gateway is null)
            {
                ErrorMessage = "The selected gateway could not be found.";

                return;
            }

            GatewayName = gateway.Name;
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
        }
    }

    // CAN CREATE 
    private bool CanCreateSensor()
    {
        return !IsBusy &&
               !IsCreated &&
               _session.GatewayId.HasValue &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(DeviceIdentifier);
    }

    // CREATE
    private async Task CreateSensorAsync()
    {
        if (!CanCreateSensor())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var command = new CreateSensorCommand
            {
                Name = Name.Trim(),

                DeviceIdentifier = DeviceIdentifier.Trim(),

                Location = string.IsNullOrWhiteSpace(Location)
                        ? string.Empty : Location.Trim(),

                Category = Category,

                Description = string.IsNullOrWhiteSpace(Description)
                    ? string.Empty : Description.Trim(),

                GatewayId = _session.GatewayId
            };

            var sensorId = await _apiClient.CreateSensorAsync(
                    command);

            if (sensorId == Guid.Empty)
            {
                ErrorMessage = "The sensor could not be created.";

                return;
            }

            IsCreated = true;

            _navigationService
                .NavigateTo<SensorsPage>();
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
        }
    }
}