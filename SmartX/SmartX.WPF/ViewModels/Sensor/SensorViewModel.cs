using Microsoft.Win32;
using SmartX.Application.Commands.Sensors;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.SensorLog;
using SmartX.WPF.Navigation;
using SmartX.WPF.Repositories.Local;
using SmartX.WPF.Services;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;
using SmartX.WPF.ViewModels.Base;
using SmartX.WPF.Views.Pages.Gateway;
using SmartX.WPF.Views.Pages.Sensor;
using SmartX.WPF.Views.Pages.Telemetry;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

using DomainSensor = SmartX.Domain.Entities.Sensor;

namespace SmartX.WPF.ViewModels.Pages.Sensor;

public class SensorViewModel :
    ViewModelBase,
    INavigationAware
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly ILocalSensorCache _sensorCache;
    private readonly ISmartXApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly SmartXSession _session;

    // =========================================================
    // MODE
    // =========================================================

    public enum SensorMode
    {
        List,
        Create,
        Edit
    }

    private SensorMode _mode = SensorMode.List;

    public SensorMode Mode
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
        Mode == SensorMode.List;

    public bool IsCreateMode =>
        Mode == SensorMode.Create;

    public bool IsEditMode =>
        Mode == SensorMode.Edit;

    // =========================================================
    // STATE
    // =========================================================

    private bool _isLoaded;
    private bool _isBusy;
    private bool _isOnline;

    private string _errorMessage = string.Empty;

    private Guid? _editingSensorId;

    private DomainSensor? _selectedSensor;

    // =========================================================
    // FORM
    // =========================================================

    private string _name = string.Empty;
    private string _deviceIdentifier = string.Empty;
    private SensorCategory _category;
    private string? _location;
    private string? _description;
    private bool _isActive = true;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public SensorViewModel(
        ILocalSensorCache sensorCache,
        ISmartXApiClient apiClient,
        INavigationService navigationService,
        SmartXSession session,
        ICacheSyncService cacheSyncService)
    {
        _sensorCache = sensorCache;
        _apiClient = apiClient;
        _navigationService = navigationService;
        _session = session;
        _cacheSyncService = cacheSyncService;

        // -----------------------------------------------------
        // LIST / CRUD
        // -----------------------------------------------------

        AddSensorCommand =
            new AsyncRelayCommand(
                AddSensorAsync,
                CanAddSensor);

        EditSensorCommand =
            new AsyncRelayCommand(
                EditSensorAsync,
                CanEditSensor);

        DeleteSensorCommand =
            new AsyncRelayCommand(
                DeleteSensorAsync,
                CanDeleteSensor);

        // -----------------------------------------------------
        // CREATE / EDIT
        // -----------------------------------------------------

        SaveSensorCommand =
            new AsyncRelayCommand(
                SaveSensorAsync,
                CanSaveSensor);

        CancelCommand =
            new AsyncRelayCommand(
                CancelAsync,
                CanCancel);

        // -----------------------------------------------------
        // OTHER
        // -----------------------------------------------------

        BackToGatewaysCommand =
            new AsyncRelayCommand(
                BackToGatewaysAsync);

        AddLogFileCommand =
            new AsyncRelayCommand(
                AddLogFileAsync,
                CanAddLogFile);

        OpenTelemetryCommand =
            new RelayCommand(
                OpenTelemetry);

        _session.PropertyChanged +=
            Session_PropertyChanged;
    }

    // =========================================================
    // SENSOR PROPERTIES
    // =========================================================

    public string Name
    {
        get => _name;

        set
        {
            if (!SetProperty(ref _name, value))
                return;

            RaiseCommandStates();
        }
    }

    public string DeviceIdentifier
    {
        get => _deviceIdentifier;

        set
        {
            if (!SetProperty(ref _deviceIdentifier, value))
                return;

            RaiseCommandStates();
        }
    }

    public SensorCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string? Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public ObservableCollection<SensorCategory> Categories { get; } =
        new(Enum.GetValues<SensorCategory>());

    // =========================================================
    // COLLECTION
    // =========================================================

    public ObservableCollection<DomainSensor> Sensors { get; } = [];

    // =========================================================
    // LOG FILES
    // =========================================================

    public ObservableCollection<SensorLogFileDto> LogFiles { get; } = [];

    public bool HasLogFiles =>
        LogFiles.Count > 0;

    // =========================================================
    // GATEWAY
    // =========================================================

    public string CurrentGatewayName =>
        _session.GatewayName ??
        "No Gateway Selected";

    public Guid? SelectedGatewayId =>
        _session.GatewayId;

    public string? SelectedGatewayName =>
        _session.GatewayName;

    public bool HasSelectedGateway =>
        _session.GatewayId.HasValue;

    // =========================================================
    // SELECTED SENSOR
    // =========================================================

    public DomainSensor? SelectedSensor
    {
        get => _selectedSensor;

        set
        {
            if (!SetProperty(ref _selectedSensor, value))
                return;

            if (value is not null)
            {
                Name = value.Name;
                DeviceIdentifier = value.DeviceIdentifier;
                Category = value.Category;
                Location = value.Location;
                Description = value.Description;
                IsActive = value.IsActive;
            }

            RaiseCommandStates();
        }
    }

    // =========================================================
    // EDITING
    // =========================================================

    public Guid? EditingSensorId
    {
        get => _editingSensorId;

        private set
        {
            if (!SetProperty(ref _editingSensorId, value))
                return;

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
            if (!SetProperty(ref _isBusy, value))
                return;

            RaiseCommandStates();
        }
    }

    public bool IsOnline
    {
        get => _isOnline;

        private set
        {
            if (!SetProperty(ref _isOnline, value))
                return;

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
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand AddSensorCommand { get; }

    public AsyncRelayCommand EditSensorCommand { get; }

    public AsyncRelayCommand DeleteSensorCommand { get; }

    public AsyncRelayCommand SaveSensorCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    public AsyncRelayCommand BackToGatewaysCommand { get; }

    public AsyncRelayCommand AddLogFileCommand { get; }

    public ICommand OpenTelemetryCommand { get; }

    // =========================================================
    // NAVIGATION
    // =========================================================

    public void OnNavigatedTo(object parameter)
    {
        // -----------------------------------------------------
        // EDIT
        // -----------------------------------------------------

        if (parameter is Guid sensorId)
        {
            Mode = SensorMode.Edit;

            EditingSensorId = sensorId;

            _ = LoadSensorForEditAsync(sensorId);

            return;
        }

        // -----------------------------------------------------
        // CREATE
        // -----------------------------------------------------

        if (parameter is string mode &&
            mode.Equals(
                "Create",
                StringComparison.OrdinalIgnoreCase))
        {
            Mode = SensorMode.Create;

            ResetForm();

            _ = LoadCreateModeAsync();

            return;
        }

        // -----------------------------------------------------
        // LIST
        // -----------------------------------------------------

        Mode = SensorMode.List;

        EditingSensorId = null;

        _ = LoadAsync();
    }

    // =========================================================
    // CREATE MODE LOAD
    // =========================================================

    private async Task LoadCreateModeAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (_session.CompanyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                return;
            }

            if (!_session.GatewayId.HasValue ||
                _session.GatewayId.Value == Guid.Empty)
            {
                ErrorMessage =
                    "No gateway is currently selected.";

                return;
            }

            IsOnline =
                await _apiClient.IsAvailableAsync();

            if (!IsOnline)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }
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
    // LOAD SENSOR LIST
    // =========================================================

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _isLoaded = true;

            IsBusy = true;
            ErrorMessage = string.Empty;

            Sensors.Clear();
            SelectedSensor = null;

            if (_session.CompanyId == Guid.Empty)
            {
                ErrorMessage =
                    "No company is associated with this session.";

                return;
            }

            if (!_session.GatewayId.HasValue ||
                _session.GatewayId.Value == Guid.Empty)
            {
                ErrorMessage =
                    "No gateway selected.";

                return;
            }

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
            // IMPORTANT:
            // Synchronise API -> local cache first.
            // -------------------------------------------------

            await _cacheSyncService.SyncSensorsAsync();

            var sensors =
                await _sensorCache.GetByGatewayIdAsync(
                    _session.GatewayId.Value,
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
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    // =========================================================
    // CREATE
    // =========================================================

    private bool CanAddSensor()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               HasSelectedGateway &&
               HasSensorWritePermission();
    }

    private async Task AddSensorAsync()
    {
        if (!CanAddSensor())
            return;

        // Same VM, different page.
        _navigationService.NavigateTo<SensorSetupPage>(
            "Create");

        await Task.CompletedTask;
    }

    private bool CanCreateSensor()
    {
        return IsCreateMode &&
               IsOnline &&
               !IsBusy &&
               HasSelectedGateway &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(DeviceIdentifier) &&
               HasSensorWritePermission();
    }

    // =========================================================
    // SAVE / CREATE
    // =========================================================

    private bool CanSaveSensor()
    {
        if (IsCreateMode)
            return CanCreateSensor();

        return IsEditMode &&
               IsOnline &&
               !IsBusy &&
               SelectedSensor is not null &&
               HasSelectedGateway &&
               HasSensorWritePermission() &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(DeviceIdentifier);
    }

    private async Task SaveSensorAsync()
    {
        if (!CanSaveSensor())
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // =================================================
            // CREATE
            // =================================================

            if (IsCreateMode)
            {
                if (!_session.GatewayId.HasValue)
                {
                    ErrorMessage =
                        "No gateway is currently selected.";

                    return;
                }

                var command =
                    new CreateSensorCommand
                    {
                        Name = Name.Trim(),

                        DeviceIdentifier =
                            DeviceIdentifier.Trim(),

                        Location =
                            string.IsNullOrWhiteSpace(Location)
                                ? string.Empty
                                : Location.Trim(),

                        Category = Category,

                        Description =
                            string.IsNullOrWhiteSpace(Description)
                                ? string.Empty
                                : Description.Trim(),

                        GatewayId =
                            _session.GatewayId.Value
                    };

                var sensorId =
                    await _apiClient.CreateSensorAsync(
                        command);

                if (sensorId == Guid.Empty)
                {
                    ErrorMessage =
                        "The API did not return a valid sensor ID.";

                    return;
                }

                // -------------------------------------------------
                // REFRESH CACHE
                // -------------------------------------------------

                await _cacheSyncService.SyncSensorsAsync();

                MessageBox.Show(
                    $"Sensor '{Name}' has been created successfully.",
                    "Sensor Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // -------------------------------------------------
                // RETURN TO LIST
                // LoadAsync() will sync again and display it.
                // -------------------------------------------------

                _navigationService.NavigateTo<SensorsPage>();

                return;
            }

            // =================================================
            // UPDATE
            // =================================================

            if (SelectedSensor is null)
                return;

            var updateCommand =
                new UpdateSensorCommand
                {
                    Id = SelectedSensor.Id,

                    Name = Name.Trim(),

                    DeviceIdentifier =
                        DeviceIdentifier.Trim(),

                    Category = Category,

                    Location =
                        string.IsNullOrWhiteSpace(Location)
                            ? string.Empty
                            : Location.Trim(),

                    Description =
                        string.IsNullOrWhiteSpace(Description)
                            ? string.Empty
                            : Description.Trim(),

                    IsActive = IsActive
                };

            var updated =
                await _apiClient.UpdateSensorAsync(
                    updateCommand);

            if (!updated)
            {
                ErrorMessage =
                    "The sensor could not be updated.";

                return;
            }

            // -------------------------------------------------
            // REFRESH CACHE
            // -------------------------------------------------

            await _cacheSyncService.SyncSensorsAsync();

            // -------------------------------------------------
            // RETURN TO LIST
            // -------------------------------------------------

            _navigationService.NavigateTo<SensorsPage>();
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

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

    // =========================================================
    // EDIT
    // =========================================================

    private bool CanEditSensor()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               SelectedSensor is not null &&
               HasSelectedGateway &&
               HasSensorWritePermission();
    }

    private async Task EditSensorAsync()
    {
        if (!CanEditSensor())
            return;

        if (SelectedSensor is null)
            return;

        _navigationService.NavigateTo<SensorEditPage>(
            SelectedSensor.Id);

        await Task.CompletedTask;
    }

    // =========================================================
    // LOAD EDIT
    // =========================================================

    private async Task LoadSensorForEditAsync(
        Guid sensorId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            IsOnline =
                await _apiClient.IsAvailableAsync();

            if (!IsOnline)
            {
                ErrorMessage =
                    "Unable to connect to the SmartX API.";

                return;
            }

            // Refresh first so edit uses current data.
            await _cacheSyncService.SyncSensorsAsync();

            var sensor =
                await _sensorCache.GetByIdAsync(
                    sensorId);

            if (sensor is null)
            {
                ErrorMessage =
                    "The selected sensor could not be found.";

                return;
            }

            SelectedSensor = sensor;

            Name = sensor.Name;
            DeviceIdentifier = sensor.DeviceIdentifier;
            Category = sensor.Category;
            Location = sensor.Location;
            Description = sensor.Description;
            IsActive = sensor.IsActive;

            await LoadLogFilesAsync(sensorId);
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

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

    // =========================================================
    // DELETE
    // =========================================================

    private bool CanDeleteSensor()
    {
        return IsListMode &&
               IsOnline &&
               !IsBusy &&
               SelectedSensor is not null &&
               HasSelectedGateway &&
               HasSensorWritePermission();
    }

    private async Task DeleteSensorAsync()
    {
        if (!CanDeleteSensor())
            return;

        if (SelectedSensor is null)
            return;

        var result =
            MessageBox.Show(
                $"Are you sure you want to delete '{SelectedSensor.Name}'?\n\nThis action cannot be undone.",
                "Delete Sensor",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var sensorId =
                SelectedSensor.Id;

            var deleted =
                await _apiClient.DeleteSensorAsync(
                    sensorId);

            if (!deleted)
            {
                ErrorMessage =
                    "Unable to delete the sensor.";

                return;
            }

            // -------------------------------------------------
            // REFRESH CACHE
            // -------------------------------------------------

            await _cacheSyncService.SyncSensorsAsync();

            // -------------------------------------------------
            // RELOAD LIST
            // -------------------------------------------------

            await LoadAsync();
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
    // CANCEL
    // =========================================================

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

        _navigationService.NavigateTo<SensorsPage>();

        await Task.CompletedTask;
    }

    // =========================================================
    // RESET FORM
    // =========================================================

    private void ResetForm()
    {
        EditingSensorId = null;
        SelectedSensor = null;

        Name = string.Empty;
        DeviceIdentifier = string.Empty;
        Category = default;
        Location = string.Empty;
        Description = string.Empty;
        IsActive = true;

        LogFiles.Clear();

        OnPropertyChanged(nameof(HasLogFiles));
    }

    // =========================================================
    // LOG FILES
    // =========================================================

    private async Task LoadLogFilesAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files =
                await _apiClient.GetSensorLogFilesAsync(
                    sensorId,
                    cancellationToken);

            LogFiles.Clear();

            foreach (var file in files)
                LogFiles.Add(file);

            OnPropertyChanged(nameof(HasLogFiles));
        }
        catch (HttpRequestException)
        {
            IsOnline = false;

            ErrorMessage =
                "Unable to load the sensor log files.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task AddLogFileAsync()
    {
        if (!CanAddLogFile())
            return;

        if (SelectedSensor is null)
            return;

        var dialog =
            new OpenFileDialog
            {
                Title = "Select Sensor Log File",
                Filter = "Text files (*.txt)|*.txt",
                Multiselect = false,
                CheckFileExists = true
            };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var fileInfo =
                new FileInfo(dialog.FileName);

            if (!fileInfo.Exists)
            {
                ErrorMessage =
                    "The selected file does not exist.";

                return;
            }

            await using var stream =
                File.OpenRead(fileInfo.FullName);

            var result =
                await _apiClient.UploadSensorLogFileAsync(
                    SelectedSensor.Id,
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

            await LoadLogFilesAsync(
                SelectedSensor.Id);
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

    private bool CanAddLogFile()
    {
        return IsEditMode &&
               IsOnline &&
               !IsBusy &&
               SelectedSensor is not null &&
               HasSelectedGateway &&
               HasSensorWritePermission();
    }

    // =========================================================
    // TELEMETRY
    // =========================================================

    private void OpenTelemetry(object? parameter)
    {
        if (parameter is not DomainSensor sensor)
            return;

        _navigationService.NavigateTo<TelemetryPage>(
            sensor.Id);
    }

    // =========================================================
    // BACK
    // =========================================================

    private async Task BackToGatewaysAsync()
    {
        _navigationService.NavigateTo<GatewayPage>();

        await Task.CompletedTask;
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

    // =========================================================
    // SESSION
    // =========================================================

    private async void Session_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName !=
                nameof(SmartXSession.GatewayId) &&
            e.PropertyName !=
                nameof(SmartXSession.GatewayName))
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedGatewayId));
        OnPropertyChanged(nameof(SelectedGatewayName));
        OnPropertyChanged(nameof(CurrentGatewayName));
        OnPropertyChanged(nameof(HasSelectedGateway));

        RaiseCommandStates();

        if (!_isLoaded)
            return;

        if (!_session.GatewayId.HasValue)
        {
            Sensors.Clear();
            SelectedSensor = null;

            RaiseSensorCounts();

            ErrorMessage =
                "No gateway selected.";

            return;
        }

        if (e.PropertyName ==
            nameof(SmartXSession.GatewayId))
        {
            await LoadAsync();
        }
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
    // COMMAND STATE
    // =========================================================

    private void RaiseCommandStates()
    {
        AddSensorCommand?.RaiseCanExecuteChanged();
        EditSensorCommand?.RaiseCanExecuteChanged();
        DeleteSensorCommand?.RaiseCanExecuteChanged();
        SaveSensorCommand?.RaiseCanExecuteChanged();
        CancelCommand?.RaiseCanExecuteChanged();
        AddLogFileCommand?.RaiseCanExecuteChanged();
        BackToGatewaysCommand?.RaiseCanExecuteChanged();
    }
}