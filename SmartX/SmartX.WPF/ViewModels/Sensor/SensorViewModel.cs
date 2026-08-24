using SmartX.Domain.Enums;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows.Input;

using DomainSensor = SmartX.Domain.Entities.Sensor;

namespace SmartX.WPF.ViewModels.Pages.Sensor;

public class SensorViewModel : ViewModelBase
{
    private readonly ILocalSensorCache _sensorCache;
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly SmartXSession _session;

    private DomainSensor? _selectedSensor;

    private bool _isBusy;
    private bool _isOnline;

    private string _errorMessage = string.Empty;

    // =========================================================
    // COLLECTION
    // =========================================================

    public ObservableCollection<DomainSensor> Sensors { get; } = [];


    public string CurrentGatewayName =>
    _session.GatewayName ?? "No Gateway Selected";

    public bool HasGateway =>
        _session.GatewayId.HasValue;

    public AsyncRelayCommand BackToGatewaysCommand { get; }

    // =========================================================
    // SELECTED SENSOR
    // =========================================================

    public DomainSensor? SelectedSensor
    {
        get => _selectedSensor;

        set
        {
            if (!SetProperty(
                    ref _selectedSensor,
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
    // SESSION INFORMATION
    // =========================================================

    public Guid? SelectedGatewayId =>
        _session.GatewayId;

    public string? SelectedGatewayName =>
        _session.GatewayName;

    public bool HasSelectedGateway =>
        _session.GatewayId.HasValue;

    // =========================================================
    // COMMANDS
    // =========================================================

    public ICommand OpenTelemetryCommand { get; }

    public AsyncRelayCommand AddSensorCommand { get; }

    public AsyncRelayCommand EditSensorCommand { get; }

    public AsyncRelayCommand DeleteSensorCommand { get; }

    public AsyncRelayCommand AddLogFileCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

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

        BackToGatewaysCommand =
    new AsyncRelayCommand(
        BackToGatewaysAsync);

        AddSensorCommand =
            new AsyncRelayCommand(
                AddSensorAsync,
                CanAddSensor);

        EditSensorCommand =
            new AsyncRelayCommand(
                EditSensorAsync,
                CanModifySensor);

        DeleteSensorCommand =
            new AsyncRelayCommand(
                DeleteSensorAsync,
                CanModifySensor);

        AddLogFileCommand =
            new AsyncRelayCommand(
                AddLogFileAsync,
                CanAddLogFile);

        OpenTelemetryCommand =
            new RelayCommand(
                OpenTelemetry);

        // React when the selected gateway changes.
        _session.PropertyChanged += Session_PropertyChanged;
    }
    private async Task BackToGatewaysAsync()
    {
        _navigationService.NavigateTo<GatewayPage>();

        await Task.CompletedTask;
    }
    // =========================================================
    // SESSION CHANGE
    // =========================================================

    private void Session_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SmartXSession.GatewayId) ||
            e.PropertyName == nameof(SmartXSession.GatewayName))
        {
            OnPropertyChanged(nameof(SelectedGatewayId));
            OnPropertyChanged(nameof(SelectedGatewayName));
            OnPropertyChanged(nameof(HasSelectedGateway));

            RaiseCommandStates();
        }
    }

    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Sensors.Clear();
            SelectedSensor = null;

            OnPropertyChanged(nameof(SelectedGatewayId));
            OnPropertyChanged(nameof(SelectedGatewayName));
            OnPropertyChanged(nameof(HasSelectedGateway));

            // -------------------------------------------------
            // COMPANY
            // -------------------------------------------------

            if (_session.CompanyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                RaiseSensorCounts();

                return;
            }

            // -------------------------------------------------
            // GATEWAY
            // -------------------------------------------------

            if (!_session.GatewayId.HasValue ||
                _session.GatewayId.Value == Guid.Empty)
            {
                ErrorMessage =
                    "No gateway selected.";

                RaiseSensorCounts();

                return;
            }

            var gatewayId =
                _session.GatewayId.Value;

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
            // LOAD SENSORS FOR SELECTED GATEWAY
            // -------------------------------------------------

            var sensors =
                await _sensorCache.GetByGatewayIdAsync(
                    gatewayId,
                    cancellationToken);

            foreach (var sensor in sensors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Sensors.Add(sensor);
            }

            RaiseSensorCounts();
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
            IsOnline = false;

            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;

            RaiseCommandStates();
        }
    }

    // =========================================================
    // PERMISSIONS
    // =========================================================

    private bool HasSensorWritePermission()
    {
        return _session.Role is
            UserRole.Technician or
            UserRole.Administrator;
    }

    private bool CanAddSensor()
    {
        return IsOnline &&
               !IsBusy &&
               HasSelectedGateway &&
               HasSensorWritePermission();
    }

    private bool CanModifySensor()
    {
        return IsOnline &&
               !IsBusy &&
               HasSelectedGateway &&
               SelectedSensor != null &&
               HasSensorWritePermission();
    }

    private bool CanAddLogFile()
    {
        return IsOnline &&
               !IsBusy &&
               HasSelectedGateway &&
               SelectedSensor != null &&
               HasSensorWritePermission();
    }

    // =========================================================
    // ADD SENSOR
    // =========================================================

    private async Task AddSensorAsync()
    {
        if (!CanAddSensor())
            return;

        _navigationService
            .NavigateTo<SensorSetupPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // EDIT SENSOR
    // =========================================================

    private async Task EditSensorAsync()
    {
        if (!CanModifySensor())
            return;

        if (SelectedSensor is null)
            return;

        _navigationService.NavigateTo<SensorEditPage>(
            SelectedSensor.Id);

        await Task.CompletedTask;
    }

    // =========================================================
    // DELETE SENSOR
    // =========================================================

    private async Task DeleteSensorAsync()
    {
        if (!CanModifySensor())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var sensorId =
                SelectedSensor!.Id;

            var deleted =
                await _apiClient.DeleteSensorAsync(
                    sensorId);

            if (!deleted)
            {
                ErrorMessage =
                    "Unable to delete the sensor.";

                return;
            }

            await _sensorCache.DeleteAsync(
                sensorId);

            var sensor =
                Sensors.FirstOrDefault(
                    x => x.Id == sensorId);

            if (sensor != null)
                Sensors.Remove(sensor);

            SelectedSensor = null;

            RaiseSensorCounts();
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
        }
    }

    // =========================================================
    // ADD LOG FILE
    // =========================================================

    private async Task AddLogFileAsync()
    {
        if (!CanAddLogFile())
            return;

        var dialog =
            new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Sensor Log File",

                Filter =
                    "Text files (*.txt)|*.txt",

                Multiselect = false,

                CheckFileExists = true
            };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var filePath =
                dialog.FileName;

            var fileInfo =
                new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                ErrorMessage =
                    "The selected file does not exist.";

                return;
            }

            if (!string.Equals(
                    fileInfo.Extension,
                    ".txt",
                    StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage =
                    "Only .txt log files are allowed.";

                return;
            }

            await using var stream =
                File.OpenRead(filePath);

            var result =
                await _apiClient.UploadSensorLogFileAsync(
                    SelectedSensor!.Id,
                    fileInfo.Name,
                    stream,
                    "text/plain",
                    _session.UserId);

            if (!result.Success)
            {
                ErrorMessage =
                    result.Error ??
                    "Unable to upload the sensor log file.";

                return;
            }

            ErrorMessage = string.Empty;
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
        }
    }

    // =========================================================
    // TELEMETRY
    // =========================================================

    private void OpenTelemetry(
        object? parameter)
    {
        if (parameter is not DomainSensor sensor)
            return;

        _navigationService.NavigateTo<TelemetryPage>(
            sensor.Id);
    }

    // =========================================================
    // COUNTS
    // =========================================================

    private void RaiseSensorCounts()
    {
        OnPropertyChanged(nameof(TotalSensors));
        OnPropertyChanged(nameof(OnlineSensors));
        OnPropertyChanged(nameof(OfflineSensors));
        OnPropertyChanged(nameof(ActiveAlerts));
    }

    public int TotalSensors =>
        Sensors.Count;

    public int OnlineSensors =>
        Sensors.Count(x => x.IsActive);

    public int OfflineSensors =>
        Sensors.Count(x => !x.IsActive);

    public int ActiveAlerts =>
        0;

    // =========================================================
    // COMMAND STATES
    // =========================================================

    private void RaiseCommandStates()
    {
        AddSensorCommand?
            .RaiseCanExecuteChanged();

        EditSensorCommand?
            .RaiseCanExecuteChanged();

        DeleteSensorCommand?
            .RaiseCanExecuteChanged();

        AddLogFileCommand?
            .RaiseCanExecuteChanged();
    }
}