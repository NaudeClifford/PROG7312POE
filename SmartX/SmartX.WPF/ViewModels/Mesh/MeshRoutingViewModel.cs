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

    private bool isBusy;
    private string statusMessage = "Ready.";
    private string errorMessage = string.Empty;
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
        set => SetProperty(ref routingResult, value);
    }

    public MeshTraversalResultDto? TraversalResult
    {
        get => traversalResult;
        set => SetProperty(ref traversalResult, value);
    }

    public MeshSpanningTreeDto? SpanningTree
    {
        get => spanningTree;
        set => SetProperty(ref spanningTree, value);
    }

    public ObservableCollection<Guid> RouteNodes { get; } = [];

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

        ShortestPathCommand = new RelayCommand(
            async _ => await FindShortestPathAsync());

        BreadthFirstSearchCommand = new RelayCommand(
            async _ => await RunBfsAsync());

        DepthFirstSearchCommand = new RelayCommand(
            async _ => await RunDfsAsync());

        FindBfsRouteCommand = new RelayCommand(
            async _ => await FindBfsRouteAsync());

        FindDfsRouteCommand = new RelayCommand(
            async _ => await FindDfsRouteAsync());

        SpanningTreeCommand = new RelayCommand(
            async _ => await CalculateSpanningTreeAsync());
    }

    public async Task FindShortestPathAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateRoute();

            RoutingResult = await _apiClient.FindMeshShortestPathAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            ResultMessage = RoutingResult.Message;

            StatusMessage = RoutingResult.Success
                ? "Shortest path calculated successfully."
                : "No shortest path found.";
        });
    }

    public async Task RunBfsAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateStartNode();

            TraversalResult = await _apiClient.BreadthFirstSearchAsync(
                GatewayId,
                StartNodeId);

            ResultMessage = TraversalResult.Message;

            StatusMessage = "Breadth-first traversal completed.";
        });
    }

    public async Task RunDfsAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateStartNode();

            TraversalResult = await _apiClient.DepthFirstSearchAsync(
                GatewayId,
                StartNodeId);

            ResultMessage = TraversalResult.Message;

            StatusMessage = "Depth-first traversal completed.";
        });
    }

    public async Task FindBfsRouteAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateRoute();

            RoutingResult = await _apiClient.FindMeshBfsRouteAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            ResultMessage = RoutingResult.Message;

            StatusMessage = RoutingResult.Success
                ? "BFS route found."
                : "BFS route not found.";
        });
    }

    public async Task FindDfsRouteAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateRoute();

            RoutingResult = await _apiClient.FindMeshDfsRouteAsync(
                GatewayId,
                SourceNodeId,
                DestinationNodeId);

            ResultMessage = RoutingResult.Message;

            StatusMessage = RoutingResult.Success
                ? "DFS route found."
                : "DFS route not found.";
        });
    }

    public async Task CalculateSpanningTreeAsync()
    {
        await ExecuteAsync(async () =>
        {
            ValidateGateway();
            ValidateStartNode();

            SpanningTree = await _apiClient.GetMeshSpanningTreeAsync(
                GatewayId,
                StartNodeId);

            StatusMessage = "Minimum spanning tree calculated.";

            ResultMessage =
                $"Connections: {SpanningTree.TotalConnections}\n" +
                $"Total cost: {SpanningTree.TotalCost:F2}\n" +
                $"Energy cost: {SpanningTree.TotalEnergyCost:F2}";
        });
    }

    private void ValidateGateway()
    {
        if (GatewayId == Guid.Empty)
            throw new InvalidOperationException(
                "Enter a valid Gateway ID.");
    }

    private void ValidateStartNode()
    {
        if (StartNodeId == Guid.Empty)
            throw new InvalidOperationException(
                "Enter a valid starting Node ID.");
    }

    private void ValidateRoute()
    {
        if (SourceNodeId == Guid.Empty)
            throw new InvalidOperationException(
                "Enter a valid source Node ID.");

        if (DestinationNodeId == Guid.Empty)
            throw new InvalidOperationException(
                "Enter a valid destination Node ID.");

        if (SourceNodeId == DestinationNodeId)
            throw new InvalidOperationException(
                "Source and destination nodes must be different.");
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
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
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
}
