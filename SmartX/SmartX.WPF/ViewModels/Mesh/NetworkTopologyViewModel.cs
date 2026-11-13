
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

public class NetworkTopologyViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;

    private Guid gatewayId;

    private MeshNodeDto? selectedNode;
    private MeshLinkDto? selectedLink;

    private string statusMessage = "Ready.";

    public Guid GatewayId
    {
        get => gatewayId;
        set => SetProperty(ref gatewayId, value);
    }

    public MeshNodeDto? SelectedNode
    {
        get => selectedNode;
        set => SetProperty(ref selectedNode, value);
    }

    public MeshLinkDto? SelectedLink
    {
        get => selectedLink;
        set => SetProperty(ref selectedLink, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public ObservableCollection<MeshNodeDto> Nodes { get; } = [];

    public ObservableCollection<MeshLinkDto> Links { get; } = [];

    public int TotalNodes => Nodes.Count;

    public int TotalLinks => Links.Count;

    public int ActiveLinks => Links.Count(x => x.IsAvailable);

    public int UnavailableLinks => Links.Count(x => !x.IsAvailable);

    public ICommand LoadTopologyCommand { get; }

    public ICommand DiscoverCommand { get; }

    public ICommand SynchronizeCommand { get; }

    public NetworkTopologyViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        IConnectivityService connectivityService)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;

        GatewayId = session.GatewayId ?? Guid.Empty;

        LoadTopologyCommand =
            new RelayCommand(async _ => await LoadTopologyAsync());

        DiscoverCommand =
            new RelayCommand(async _ => await DiscoverAsync());

        SynchronizeCommand =
            new RelayCommand(async _ => await SynchronizeAsync());
    }

    public async Task InitializeAsync()
    {
        GatewayId = Session.GatewayId ?? Guid.Empty;

        if (!ValidateGateway())
            return;

        await LoadTopologyAsync();
    }

    public async Task LoadTopologyAsync()
    {
        if (!ValidateGateway())
            return;

        await ExecuteAsync(LoadTopologyCoreAsync);
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

            await LoadTopologyCoreAsync();
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

            await LoadTopologyCoreAsync();
        });
    }

    private async Task LoadTopologyCoreAsync()
    {
        var topology = await _apiClient.GetMeshTopologyAsync(GatewayId);

        Nodes.Clear();

        foreach (var node in topology.Nodes)
            Nodes.Add(node);

        Links.Clear();

        foreach (var link in topology.Links)
            Links.Add(link);

        NotifyStatistics();

        SelectedNode = null;
        SelectedLink = null;

        StatusMessage = "Network topology loaded successfully.";

        UpdateTopologyCharts();
    }

    private void NotifyStatistics()
    {
        OnPropertyChanged(nameof(TotalNodes));
        OnPropertyChanged(nameof(TotalLinks));
        OnPropertyChanged(nameof(ActiveLinks));
        OnPropertyChanged(nameof(UnavailableLinks));
        UpdateTopologyCharts();
    }

    private bool ValidateGateway()
    {
        if (GatewayId != Guid.Empty)
            return true;

        ErrorMessage = "Enter a valid Gateway ID.";
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
            Debug.WriteLine($"[TOPOLOGY API ERROR] {ex}");
        }
        catch (TimeoutException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Request timed out.";
        }
        catch (OperationCanceledException ex)
        {
            ErrorMessage = "The operation was cancelled." + ex;
            StatusMessage = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Topology operation failed.";
            Debug.WriteLine($"[TOPOLOGY ERROR] {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    public ISeries[] TopologyLinkSeries { get; private set; } = [];

    public ISeries[] TopologyNodeSeries { get; private set; } = [];

    public Axis[] TopologyXAxis { get; } =
    [
        new Axis
    {
        Labels = ["Active", "Unavailable"],
        MinLimit = 0
    }
    ];

    public Axis[] TopologyYAxis { get; } =
    [
        new Axis
    {
        MinLimit = 0
    }
    ];

    public void UpdateTopologyCharts()
    {
        TopologyLinkSeries =
        [
            new ColumnSeries<int>
        {
            Name = "Connections",
            Values = [ActiveLinks, UnavailableLinks],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(37, 99, 235))
        }
        ];

        TopologyNodeSeries =
        [
            new PieSeries<int>
        {
            Name = "Nodes",
            Values = [TotalNodes],
            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                new SkiaSharp.SKColor(14, 165, 233))
        }
        ];

        OnPropertyChanged(nameof(TopologyLinkSeries));
        OnPropertyChanged(nameof(TopologyNodeSeries));
    }

}
