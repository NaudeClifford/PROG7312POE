using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
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

    private string statusMessage = "Ready.";

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
                NotifyStatistics();
            }
        }
    }

    public ObservableCollection<MeshNodeDto> Nodes
    {
        get => nodes;
        set
        {
            if (SetProperty(ref nodes, value))
                NotifyStatistics();
        }
    }

    public ObservableCollection<MeshLinkDto> Links
    {
        get => links;
        set
        {
            if (SetProperty(ref links, value))
                NotifyStatistics();
        }
    }

    public ObservableCollection<MeshLogDto> Logs
    {
        get => logs;
        set => SetProperty(ref logs, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public int TotalNodes => Health?.TotalNodes ?? Nodes.Count;
    public int OnlineNodes => Health?.OnlineNodes ?? Nodes.Count(x => x.Status == "Online");
    public int OfflineNodes => Health?.OfflineNodes ?? Nodes.Count(x => x.Status == "Offline");
    public int DegradedNodes => Health?.DegradedNodes ?? Nodes.Count(x => x.Status == "Degraded");

    public int TotalLinks => Health?.TotalLinks ?? Links.Count;
    public int AvailableLinks => Health?.AvailableLinks ?? Links.Count(x => x.IsAvailable);
    public int UnavailableLinks => Health?.UnavailableLinks ?? Links.Count(x => !x.IsAvailable);

    public int TotalRoutes => Health?.TotalRoutes ?? 0;
    public int SuccessfulRoutes => Health?.SuccessfulRoutes ?? 0;
    public int FailedRoutes => Health?.FailedRoutes ?? 0;

    public double NetworkAvailability => Health?.NetworkAvailability ?? 0;
    public double AverageLatency => Health?.AverageLatency ?? 0;

    public string HealthStatus => Health?.Status ?? "Unknown";

    public ObservableCollection<string> Warnings { get; } = [];

    public ICommand LoadDashboardCommand { get; }
    public ICommand DiscoverCommand { get; }
    public ICommand SynchronizeCommand { get; }
    public ICommand RefreshMonitoringCommand { get; }
    public ICommand EvaluateLinksCommand { get; }
    public ICommand LoadLogsCommand { get; }

    public MeshDashboardViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        IConnectivityService connectivityService)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;

        GatewayId = session.GatewayId ?? Guid.Empty;

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

    private void NotifyStatistics()
    {
        OnPropertyChanged(nameof(TotalNodes));
        OnPropertyChanged(nameof(OnlineNodes));
        OnPropertyChanged(nameof(OfflineNodes));
        OnPropertyChanged(nameof(DegradedNodes));
        OnPropertyChanged(nameof(TotalLinks));
        OnPropertyChanged(nameof(AvailableLinks));
        OnPropertyChanged(nameof(UnavailableLinks));
        OnPropertyChanged(nameof(TotalRoutes));
        OnPropertyChanged(nameof(SuccessfulRoutes));
        OnPropertyChanged(nameof(FailedRoutes));
        OnPropertyChanged(nameof(NetworkAvailability));
        OnPropertyChanged(nameof(AverageLatency));
        OnPropertyChanged(nameof(HealthStatus));
        UpdateDashboardCharts();
    }

    public async Task InitializeAsync()
    {
        GatewayId = Session.GatewayId ?? Guid.Empty;

        if (GatewayId == Guid.Empty)
            return;

        IsBusy = true;

        try
        {
            await DiscoverAsync();
            await SynchronizeAsync();
            await LoadDashboardAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadDashboardAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(LoadDashboardCoreAsync);
    }

    private async Task LoadDashboardCoreAsync()
    {
        var topology = await _apiClient.GetMeshTopologyAsync(GatewayId);

        var healthResult = await _apiClient.GetMeshHealthAsync(GatewayId);

        var logsResult = await _apiClient.GetMeshGatewayLogsAsync(GatewayId);

        Nodes = new ObservableCollection<MeshNodeDto>(topology.Nodes);
        Links = new ObservableCollection<MeshLinkDto>(topology.Links);
        Logs = new ObservableCollection<MeshLogDto>(logsResult);

        Health = healthResult;

        Warnings.Clear();

        foreach (var warning in healthResult.Warnings)
            Warnings.Add(warning);

        StatusMessage = "Mesh dashboard loaded successfully.";
    }

    public async Task DiscoverAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _apiClient.DiscoverMeshAsync(GatewayId);

            StatusMessage =
                $"Discovery completed. Nodes: {result.DiscoveredNodes.Count}, " +
                $"Links: {result.DiscoveredLinks.Count}.";

            await LoadDashboardCoreAsync();
        });
    }

    public async Task SynchronizeAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result = await _apiClient.SynchronizeMeshAsync(GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    public async Task RefreshMonitoringAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.RefreshMeshMonitoringAsync(GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    public async Task EvaluateLinksAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.EvaluateGatewayLinksAsync(GatewayId);

            StatusMessage = result.Message;

            await LoadDashboardCoreAsync();
        });
    }

    public async Task LoadLogsAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(async () =>
        {
            var result =
                await _apiClient.GetMeshGatewayLogsAsync(GatewayId);

            Logs = new ObservableCollection<MeshLogDto>(result);

            StatusMessage = "Mesh logs loaded successfully.";
        });
    }

    private bool ValidateGateway()
    {
        if (GatewayId != Guid.Empty)
            return true;

        ErrorMessage = "Select a valid gateway.";
        StatusMessage = "Gateway selection required.";

        return false;
    }

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
            Debug.WriteLine($"[MESH API ERROR] {ex}");
        }
        catch (TimeoutException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "API request timed out.";
            Debug.WriteLine($"[MESH TIMEOUT] {ex}");
        }
        catch (OperationCanceledException ex)
        {
            ErrorMessage = "The operation was cancelled.";
            StatusMessage = "Operation cancelled.";
            Debug.WriteLine($"[MESH CANCELLED] {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unexpected mesh operation error.";
            Debug.WriteLine($"[MESH ERROR] {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    public ISeries[] NodeHealthSeries { get; private set; } = [];

    public ISeries[] LinkHealthSeries { get; private set; } = [];

    public ISeries[] RoutingSeries { get; private set; } = [];

    public Axis[] DashboardXAxis { get; } =
    [
        new Axis
    {
        Labels = ["Successful", "Failed"],
        LabelsRotation = 0
    }
    ];

    public Axis[] DashboardYAxis { get; } =
    [
        new Axis
    {
        MinLimit = 0
    }
    ];


    public void UpdateDashboardCharts()
    {
        NodeHealthSeries =
        [
            new PieSeries<int>
        {
            Name = "Online",
            Values = [OnlineNodes],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(22, 163, 74))
        },

        new PieSeries<int>
        {
            Name = "Offline",
            Values = [OfflineNodes],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(239, 68, 68))
        },

        new PieSeries<int>
        {
            Name = "Degraded",
            Values = [DegradedNodes],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(245, 158, 11))
        }
        ];

        LinkHealthSeries =
        [
            new ColumnSeries<int>
        {
            Name = "Links",
            Values = [AvailableLinks, UnavailableLinks],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(37, 99, 235))
        }
        ];

        RoutingSeries =
        [
            new ColumnSeries<int>
        {
            Name = "Routes",
            Values = [SuccessfulRoutes, FailedRoutes],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(14, 165, 233))
        }
        ];

        OnPropertyChanged(nameof(NodeHealthSeries));
        OnPropertyChanged(nameof(LinkHealthSeries));
        OnPropertyChanged(nameof(RoutingSeries));
    }

}