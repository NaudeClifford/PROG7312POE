using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using DomainSensor = SmartX.Domain.Entities.Sensor;

namespace SmartX.WPF.ViewModels.Pages.Sensor;

public class SensorViewModel : ViewModelBase
{
    public ICommand OpenTelemetryCommand { get; }

    private readonly ILocalSensorCache _sensorCache;
    private readonly SmartXSession _session;
    private readonly INavigationService _navigationService;

    public ICommand AddSensorCommand { get; }
    public ICommand EditSensorCommand { get; }
    public ICommand DeleteSensorCommand { get; }

    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private readonly ISmartXApiClient _apiClient;

    private DomainSensor? _selectedSensor;

    public DomainSensor? SelectedSensor
    {
        get => _selectedSensor;
        set => SetProperty(ref _selectedSensor, value);
    }

    public ObservableCollection<DomainSensor> Sensors { get; } = [];

    public int TotalSensors => Sensors.Count;

    public int OnlineSensors =>
        Sensors.Count(x => x.IsActive);

    public int OfflineSensors =>
        Sensors.Count(x => !x.IsActive);

    public int ActiveAlerts => 0;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    private bool CanEditSensor()
    {
        return SelectedSensor is not null && !IsBusy;
    }

    private bool CanDeleteSensor()
    {
        return SelectedSensor is not null && !IsBusy;
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public SensorViewModel(
        ILocalSensorCache sensorCache,
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        SmartXSession session)
    {
        _sensorCache = sensorCache;
        _apiClient = apiClient;
        _navigationService = navigationService;
        _session = session;

        AddSensorCommand =
            new AsyncRelayCommand(AddSensorAsync);

        EditSensorCommand =
            new AsyncRelayCommand(EditSensorAsync, CanEditSensor);

        DeleteSensorCommand =
            new AsyncRelayCommand(DeleteSensorAsync, CanDeleteSensor);


    OpenTelemetryCommand = new RelayCommand(
            sensor => OpenTelemetry(sensor));
    }

    public async Task LoadAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Sensors.Clear();

            var sensors = await _sensorCache.GetByCompanyIdAsync(
                _session.CompanyId,
                cancellationToken);

            foreach (var sensor in sensors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Sensors.Add(sensor);
            }

            OnPropertyChanged(nameof(TotalSensors));
            OnPropertyChanged(nameof(OnlineSensors));
            OnPropertyChanged(nameof(OfflineSensors));
            OnPropertyChanged(nameof(ActiveAlerts));
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
    private void OpenTelemetry(object? parameter)
    {
        if (parameter is not DomainSensor sensor)
            return;

        _navigationService.NavigateTo<TelemetryPage>(
            sensor.Id);
    }

    private async Task AddSensorAsync()
    {
        // Open sensor editor
        // Collect sensor information
        // Call API
        // Update SQLite cache
        // Reload sensors
    }

    private async Task EditSensorAsync()
    {
        if (SelectedSensor is null)
            return;

        // Open editor with SelectedSensor
        // Call API
        // Update SQLite cache
        // Reload sensors
    }

    private async Task DeleteSensorAsync()
    {
        if (SelectedSensor is null)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var sensorId = SelectedSensor.Id;

            var deleted = await _apiClient.DeleteSensorAsync(sensorId);

            if (!deleted)
            {
                ErrorMessage = "Unable to delete the sensor.";
                return;
            }

            await _sensorCache.DeleteAsync(sensorId);

            Sensors.Remove(SelectedSensor);

            SelectedSensor = null;

            OnPropertyChanged(nameof(TotalSensors));
            OnPropertyChanged(nameof(OnlineSensors));
            OnPropertyChanged(nameof(OfflineSensors));
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the SmartX API.";
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