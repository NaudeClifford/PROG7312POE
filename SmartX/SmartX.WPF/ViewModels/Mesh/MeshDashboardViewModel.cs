
using SmartX.Shared.DTOs.Mesh;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Connectivity;
using SmartX.WPF.Services.Session;
using SmartX.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Input;

namespace SmartX.WPF.ViewModels.Mesh;

public class MeshDashboardViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;

    private Guid gatewayId;
    private MeshHealthReportDto? health;

    private ObservableCollection<MeshNodeDto> nodes = [];
    private ObservableCollection<MeshLinkDto> links = [];
    private ObservableCollection<MeshLogDto> logs = [];

    private bool isBusy;
    private string statusMessage = "Ready.";
    private string errorMessage = string.Empty;

    // PROPERTIES
    public Guid GatewayId
    {
        get => gatewayId;
        set => SetProperty(ref gatewayId, value);
    }

    public MeshHealthReportDto? Health
    {
        get => health;
        set
        {
            if (SetProperty(ref health, value))
            {
                OnPropertyChanged(nameof(TotalNodes));
                OnPropertyChanged(nameof(OnlineNodes));
                OnPropertyChanged(nameof(OfflineNodes));
                OnPropertyChanged(nameof(DegradedNodes));
                OnPropertyChanged(nameof(TotalLinks));
                OnPropertyChanged(nameof(AvailableLinks));
                OnPropertyChanged(nameof(NetworkAvailability));
                OnPropertyChanged(nameof(HealthStatus));
            }
        }
    }

    public ObservableCollection<MeshNodeDto> Nodes
    {
        get => nodes;
        set => SetProperty(ref nodes, value);
    }

    public ObservableCollection<MeshLinkDto> Links
    {
        get => links;
        set => SetProperty(ref links, value);
    }

    public ObservableCollection<MeshLogDto> Logs
    {
        get => logs;
        set => SetProperty(ref logs, value);
    }

    public bool IsNotBusy => !IsBusy;

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    // DASHBOARD STATISTICS
    public int TotalNodes => Health?.TotalNodes ?? 0;

    public int OnlineNodes => Health?.OnlineNodes ?? 0;

    public int OfflineNodes => Health?.OfflineNodes ?? 0;

    public int DegradedNodes => Health?.DegradedNodes ?? 0;

    public int TotalLinks => Health?.TotalLinks ?? 0;

    public int AvailableLinks => Health?.AvailableLinks ?? 0;

    public double NetworkAvailability =>
        Health?.NetworkAvailability ?? 0;

    public string HealthStatus =>
        Health?.Status ?? "Unknown";

    // COMMANDS
    public ICommand LoadDashboardCommand { get; }
    public ICommand DiscoverCommand { get; }
    public ICommand SynchronizeCommand { get; }
    public ICommand RefreshMonitoringCommand { get; }
    public ICommand EvaluateLinksCommand { get; }
    public ICommand LoadLogsCommand { get; }

    // CONSTRUCTOR

    public MeshDashboardViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        IConnectivityService connectivityService)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;

        LoadDashboardCommand =
            new RelayCommand(async _ => await LoadDashboardAsync());

        DiscoverCommand =
            new RelayCommand(async _ => await DiscoverAsync());

        SynchronizeCommand =
            new RelayCommand(async _ => await SynchronizeAsync());

        RefreshMonitoringCommand =
            new RelayCommand(async _ => await RefreshMonitoringAsync());

        EvaluateLinksCommand =
            new RelayCommand(async _ => await EvaluateLinksAsync());

        LoadLogsCommand =
            new RelayCommand(async _ => await LoadLogsAsync());
    }

    // LOAD DASHBOARD

    public async Task LoadDashboardAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(LoadDashboardCoreAsync);
    }

    private async Task LoadDashboardCoreAsync()
    {
        var topology = await _apiClient.GetMeshTopologyAsync(
            GatewayId);

        var healthResult = await _apiClient.GetMeshHealthAsync(
            GatewayId);

        var logsResult = await _apiClient.GetMeshGatewayLogsAsync(
            GatewayId);

        Nodes = new ObservableCollection<MeshNodeDto>(
            topology.Nodes);

        Links = new ObservableCollection<MeshLinkDto>(
            topology.Links);

        Logs = new ObservableCollection<MeshLogDto>(
            logsResult);

        Health = healthResult;

        StatusMessage = "Mesh dashboard loaded successfully.";
    }

    // DISCOVERY

    public async Task DiscoverAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _apiClient.DiscoverMeshAsync(
                GatewayId);

            StatusMessage =
                $"Discovery completed. " +
                $"Nodes: {result.DiscoveredNodes.Count}, " +
                $"Links: {result.DiscoveredLinks.Count}.";

            await LoadDashboardCoreAsync();
        });
    }

    // SYNCHRONIZATION
    public async Task SynchronizeAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _apiClient.SynchronizeMeshAsync(
                GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    // MONITORING
    public async Task RefreshMonitoringAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.RefreshMeshMonitoringAsync(
                    GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    // LINK EVALUATION
    public async Task EvaluateLinksAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.EvaluateGatewayLinksAsync(
                    GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    // LOGGING
    public async Task LoadLogsAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.GetMeshGatewayLogsAsync(
                    GatewayId);

            Logs = new ObservableCollection<MeshLogDto>(
                result);

            StatusMessage = "Mesh logs loaded successfully.";
        });
    }

    // VALIDATION
    private bool ValidateGateway()
    {
        if (GatewayId != Guid.Empty)
            return true;

        ErrorMessage = "Select a valid gateway.";
        StatusMessage = "Gateway selection required.";

        return false;
    }

    // ERROR HANDLING
    private async Task ExecuteAsync(Func<Task> operation)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await operation();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "API request failed.";

            Debug.WriteLine(
                $"[MESH API ERROR] {ex}");
        }
        catch (TimeoutException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "API request timed out.";

            Debug.WriteLine(
                $"[MESH TIMEOUT] {ex}");
        }
        catch (OperationCanceledException ex)
        {
            ErrorMessage = "The operation was cancelled.";
            StatusMessage = "Operation cancelled.";

            Debug.WriteLine(
                $"[MESH CANCELLED] {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unexpected mesh operation error.";

            Debug.WriteLine(
                $"[MESH ERROR] {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
