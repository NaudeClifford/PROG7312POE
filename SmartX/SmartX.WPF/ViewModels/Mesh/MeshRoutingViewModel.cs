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

public class MeshRoutingViewModel : ViewModelBase
{
    private readonly ISmartXApiClient _apiClient;

    private Guid gatewayId;
    private Guid sourceNodeId;
    private Guid destinationNodeId;
    private Guid startNodeId;

    private string statusMessage = "Ready.";
    private string resultMessage = string.Empty;

    private MeshRouteDto? routingResult;
    private MeshTraversalResultDto? traversalResult;
    private MeshSpanningTreeDto? spanningTree;

    public Guid GatewayId
    {
        get => gatewayId;
        set => SetProperty(ref gatewayId, value);
    }

    public Guid SourceNodeId
    {
        get => sourceNodeId;
        set => SetProperty(ref sourceNodeId, value);
    }

    public Guid DestinationNodeId
    {
        get => destinationNodeId;
        set => SetProperty(ref destinationNodeId, value);
    }

    public Guid StartNodeId
    {
        get => startNodeId;
        set => SetProperty(ref startNodeId, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public string ResultMessage
    {
        get => resultMessage;
        set => SetProperty(ref resultMessage, value);
    }

    public MeshRouteDto? RoutingResult
    {
        get => routingResult;
        set
        {
            if (SetProperty(ref routingResult, value))
                OnPropertyChanged(nameof(RouteNodes));
        }
    }

    public MeshTraversalResultDto? TraversalResult
    {
        get => traversalResult;
        set
        {
            if (SetProperty(ref traversalResult, value))
                OnPropertyChanged(nameof(VisitedNodes));
        }
    }

    public MeshSpanningTreeDto? SpanningTree
    {
        get => spanningTree;
        set => SetProperty(ref spanningTree, value);
    }

    public ObservableCollection<Guid> RouteNodes { get; } = [];

    public ObservableCollection<Guid> VisitedNodes { get; } = [];

    public ICommand ShortestPathCommand { get; }
    public ICommand BreadthFirstSearchCommand { get; }
    public ICommand DepthFirstSearchCommand { get; }
    public ICommand FindBfsRouteCommand { get; }
    public ICommand FindDfsRouteCommand { get; }
    public ICommand SpanningTreeCommand { get; }

    public MeshRoutingViewModel(
        ISmartXApiClient apiClient,
        SmartXSession session,
        IConnectivityService connectivityService)
        : base(connectivityService, session)
    {
        _apiClient = apiClient;

        GatewayId = session.GatewayId ?? Guid.Empty;

        ShortestPathCommand =
            new RelayCommand(async _ => await FindShortestPathAsync());

        BreadthFirstSearchCommand =
            new RelayCommand(async _ => await RunBfsAsync());

        DepthFirstSearchCommand =
            new RelayCommand(async _ => await RunDfsAsync());

        FindBfsRouteCommand =
            new RelayCommand(async _ => await FindBfsRouteAsync());

        FindDfsRouteCommand =
            new RelayCommand(async _ => await FindDfsRouteAsync());

        SpanningTreeCommand =
            new RelayCommand(async _ => await CalculateSpanningTreeAsync());
    }

    public Task InitializeAsync()
    {
        GatewayId = Session.GatewayId ?? Guid.Empty;

        if (GatewayId == Guid.Empty)
        {
            ErrorMessage = "No valid gateway is selected.";
            StatusMessage = "Gateway selection required.";
        }

        return Task.CompletedTask;
    }

    public async Task FindShortestPathAsync()
    {
        if (!ValidateGateway() || !ValidateRoute())
            return;

        await ExecuteAsync(async () =>
        {
            RoutingResult = await _apiClient.FindMeshShortestPathAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            UpdateRouteNodes();

            ResultMessage = RoutingResult.IsSuccessful
                ? $"Shortest path found.\n" +
                  $"Hops: {RoutingResult.HopCount}\n" +
                  $"Total cost: {RoutingResult.TotalCost:F2}\n" +
                  $"Latency: {RoutingResult.TotalLatency:F2} ms"
                : "No shortest path found.";

            StatusMessage = RoutingResult.IsSuccessful
                ? "Shortest path calculated successfully."
                : "No shortest path found.";
        });
    }

    public async Task RunBfsAsync()
    {
        if (!ValidateGateway() || !ValidateStartNode())
            return;

        await ExecuteAsync(async () =>
        {
            TraversalResult = await _apiClient.BreadthFirstSearchAsync(
                GatewayId,
                StartNodeId);

            UpdateVisitedNodes();

            ResultMessage = TraversalResult.Message;
            StatusMessage = "Breadth-first traversal completed.";
        });
    }

    public async Task RunDfsAsync()
    {
        if (!ValidateGateway() || !ValidateStartNode())
            return;

        await ExecuteAsync(async () =>
        {
            TraversalResult = await _apiClient.DepthFirstSearchAsync(
                GatewayId,
                StartNodeId);

            UpdateVisitedNodes();

            ResultMessage = TraversalResult.Message;
            StatusMessage = "Depth-first traversal completed.";
        });
    }

    public async Task FindBfsRouteAsync()
    {
        if (!ValidateGateway() || !ValidateRoute())
            return;

        await ExecuteAsync(async () =>
        {
            RoutingResult = await _apiClient.FindMeshBfsRouteAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            UpdateRouteNodes();

            ResultMessage = RoutingResult.IsSuccessful
                ? $"BFS route found.\n" +
                  $"Hops: {RoutingResult.HopCount}\n" +
                  $"Path: {string.Join(" → ", RoutingResult.NodePath)}"
                : "No BFS route found.";

            StatusMessage = RoutingResult.IsSuccessful
                ? "BFS route found."
                : "BFS route not found.";
        });
    }

    public async Task FindDfsRouteAsync()
    {
        if (!ValidateGateway() || !ValidateRoute())
            return;

        await ExecuteAsync(async () =>
        {
            RoutingResult = await _apiClient.FindMeshDfsRouteAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            UpdateRouteNodes();

            ResultMessage = RoutingResult.IsSuccessful
                ? $"DFS route found.\n" +
                  $"Hops: {RoutingResult.HopCount}\n" +
                  $"Path: {string.Join(" → ", RoutingResult.NodePath)}"
                : "No DFS route found.";

            StatusMessage = RoutingResult.IsSuccessful
                ? "DFS route found."
                : "DFS route not found.";
        });
    }

    public async Task CalculateSpanningTreeAsync()
    {
        if (!ValidateGateway() || !ValidateStartNode())
            return;

        await ExecuteAsync(async () =>
        {
            SpanningTree = await _apiClient.GetMeshSpanningTreeAsync(
                GatewayId,
                StartNodeId);

            StatusMessage = "Minimum spanning tree calculated.";

            ResultMessage =
                $"Connections: {SpanningTree.TotalConnections}\n" +
                $"Total cost: {SpanningTree.TotalCost:F2}\n" +
                $"Energy cost: {SpanningTree.TotalEnergyCost:F2}";
        });

        UpdateRoutingCharts();
    }

    private void UpdateRouteNodes()
    {
        RouteNodes.Clear();

        if (RoutingResult != null)
        {
            foreach (var id in RoutingResult.NodePath)
                RouteNodes.Add(id);
        }

        UpdateRoutingCharts();
    }

    private void UpdateVisitedNodes()
    {
        VisitedNodes.Clear();

        if (TraversalResult != null)
        {
            foreach (var id in TraversalResult.VisitedNodeIds)
                VisitedNodes.Add(id);
        }

        UpdateRoutingCharts();
    }

    private bool ValidateGateway()
    {
        if (GatewayId != Guid.Empty)
            return true;

        ErrorMessage = "Enter a valid Gateway ID.";
        StatusMessage = "Gateway selection required.";

        return false;
    }

    private bool ValidateStartNode()
    {
        if (StartNodeId != Guid.Empty)
            return true;

        ErrorMessage = "Enter a valid starting Node ID.";
        return false;
    }

    private bool ValidateRoute()
    {
        if (SourceNodeId == Guid.Empty)
        {
            ErrorMessage = "Enter a valid source Node ID.";
            return false;
        }

        if (DestinationNodeId == Guid.Empty)
        {
            ErrorMessage = "Enter a valid destination Node ID.";
            return false;
        }

        if (SourceNodeId == DestinationNodeId)
        {
            ErrorMessage = "Source and destination nodes must be different.";
            return false;
        }

        return true;
    }

    private async Task ExecuteAsync(Func<Task> operation)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        ResultMessage = string.Empty;

        try
        {
            await operation();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "API request failed.";
            Debug.WriteLine($"[MESH ROUTING API ERROR] {ex}");
        }
        catch (TimeoutException ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Request timed out.";
        }
        catch (OperationCanceledException ex)
        {
            ErrorMessage = "The operation was cancelled.";
            StatusMessage = "Operation cancelled.";
            Debug.WriteLine($"[MESH ROUTING CANCELLED] {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Routing operation failed.";
            Debug.WriteLine($"[MESH ROUTING ERROR] {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    public ISeries[] RouteSeries { get; private set; } = [];

    public ISeries[] TraversalSeries { get; private set; } = [];

    public ISeries[] SpanningTreeSeries { get; private set; } = [];

    public Axis[] RoutingXAxis { get; } =
    [
        new Axis
    {
        Name = "Node Sequence",
        MinLimit = 0
    }
    ];

    public Axis[] RoutingYAxis { get; } =
    [
        new Axis
    {
        Name = "Sequence Position",
        MinLimit = 0
    }
    ];

    public void UpdateRoutingCharts()
    {
        RouteSeries =
        [
            new LineSeries<int>
        {
            Name = "Route",
            Values = RouteNodes
                .Select((_, index) => index + 1)
                .ToArray(),

            GeometrySize = 12,
            LineSmoothness = 0
        }
        ];

        TraversalSeries =
        [
            new ColumnSeries<int>
        {
            Name = "Visited Nodes",
            Values = VisitedNodes
                .Select((_, index) => index + 1)
                .ToArray()
        }
        ];

        if (SpanningTree != null)
        {
            SpanningTreeSeries =
            [
                new ColumnSeries<double>
            {
                Name = "Tree Cost",
                Values = [SpanningTree.TotalCost]
            },

            new ColumnSeries<double>
            {
                Name = "Energy Cost",
                Values = [SpanningTree.TotalEnergyCost]
            }
            ];
        }
        else
        {
            SpanningTreeSeries = [];
        }

        OnPropertyChanged(nameof(RouteSeries));
        OnPropertyChanged(nameof(TraversalSeries));
        OnPropertyChanged(nameof(SpanningTreeSeries));
    }

}