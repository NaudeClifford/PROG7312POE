using SmartX.Application.Requests.Company;
using SmartX.Application.Requests.Gateway;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Requests.Telemetry;
using SmartX.Application.Requests.User;
using SmartX.Application.Requests.UserBehavior;
using SmartX.Application.Services.Registration;
using SmartX.Application.Services.Sensors.Command;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Mesh;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.DTOs.Telemetry;
using System.IO;

namespace SmartX.WPF.Services.Api;

public interface ISmartXApiClient
{

    // AVAILABILITY
    Task<bool> IsAvailableAsync(
        CancellationToken cancellationToken = default);


    // SENSORS - CRUD
    Task<IReadOnlyList<SensorDto>> GetSensorsAsync(
        CancellationToken cancellationToken = default);

    Task<SensorDto?> GetSensorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateSensorAsync(
        CreateSensorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateSensorAsync(
        UpdateSensorRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSensorAsync(
        Guid id,
        CancellationToken cancellationToken = default);


    // SENSOR LOG FILES
    Task<IReadOnlyList<SensorLogFileDto>> GetSensorLogFilesAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<SensorLogFileUploadResultDto> UploadSensorLogFileAsync(
        Guid sensorId,
        string fileName,
        Stream fileStream,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default);


    // TELEMETRY - CQRS
    Task<TelemetryDto?> GetTelemetryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryDto>> GetTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<TelemetryDto?> GetLatestTelemetryBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryDto>> GetTelemetryByDateRangeAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateTelemetryAsync(
    CreateTelemetryRequest request,
    CancellationToken cancellationToken = default);
    // USERS - CRUD
    Task<UserDto?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByFirebaseUidAsync(
        string firebaseUid, string idToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RegistrationResultDto> RegisterCompanyAsync(
        RegisterCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteCompanyOnboardingAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    // COMPANIES - CRUD
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCompanyAsync(
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RequestCompanyDeletionAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    Task<bool> DeleteGuestCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);


    Task<bool> CancelDeletionAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);


    Task<bool> CompleteOnboardingAsync(
    Guid companyId,
    CancellationToken cancellationToken = default);

    // COMPANY CONFIGURATION

    Task<CompanyConfigurationDto?> GetCompanyConfigurationAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCompanyConfigurationAsync(
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken = default);


    // GATEWAYS - CRUD
    Task<IReadOnlyList<GatewayDto>> GetGatewaysAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GatewayDto>> GetGatewaysByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<GatewayDto?> GetGatewayByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateGatewayAsync(
        CreateGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateGatewayAsync(
        UpdateGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGatewayAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SensorDto>> GetSensorsByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    // SENSOR COMMANDS
    Task<SensorCommandResult> SendSensorCommandAsync(
        Guid sensorId,
        string command,
        CancellationToken cancellationToken = default);

    Task<SensorCommandResult> UndoLastSensorCommandAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    // USER BEHAVIOUR
    Task<IReadOnlyList<UserBehaviorDto>> GetUserBehaviorsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserBehaviorDto>> GetUserBehaviorsBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserBehaviorAsync(
        CreateUserBehaviorRequest request,
        CancellationToken cancellationToken = default);

    // PREDICTIVE ENGINE
    Task<IReadOnlyList<PredictiveSuggestionDto>> GetPredictiveSuggestionsAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<AnomalyRecommendationDto?> AnalyzeSensorAnomalyAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PredictiveSuggestionDto>> GetPredictiveWarningsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    //Alerts
    Task<List<SensorAlert>> GetAlertsAsync(
        CancellationToken cancellationToken = default);

    Task<int> GetPendingAlertCountAsync(
        CancellationToken cancellationToken = default);

    Task<SensorAlert?> ProcessNextAlertAsync(
        CancellationToken cancellationToken = default);

    Task<List<SensorAlert>> ProcessAllAlertsAsync(
        CancellationToken cancellationToken = default);

    Task ClearAlertsAsync(
        CancellationToken cancellationToken = default);

    Task EnqueueAlertAsync(
    SensorAlert alert,
    CancellationToken cancellationToken = default);

    // MESH DISCOVERY
    Task<MeshDiscoveryDto> DiscoverMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<MeshOperationResultDto> SynchronizeMeshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    // MESH TOPOLOGY
    Task<MeshTopologyDto> GetMeshTopologyAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<Guid> RegisterMeshNodeAsync(
        MeshNodeDto node,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateMeshNodeAsync(
        MeshNodeDto node,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveMeshNodeAsync(
        Guid gatewayId,
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<Guid> RegisterMeshLinkAsync(
        MeshLinkDto link,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateMeshLinkAsync(
        MeshLinkDto link,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveMeshLinkAsync(
        Guid gatewayId,
        Guid linkId,
        CancellationToken cancellationToken = default);

    // MESH HEALTH
    Task<MeshHealthReportDto> GetMeshHealthAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    // MESH MONITORING
    Task<MeshOperationResultDto> RefreshMeshMonitoringAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<MeshOperationResultDto> CheckMeshNodeStatusAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<MeshOperationResultDto> RecalculateMeshRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    // MESH LINK QUALITY
    Task<MeshOperationResultDto> EvaluateGatewayLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<MeshOperationResultDto> EvaluateMeshLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default);

    // MESH ROUTING
    Task<MeshRouteDto> FindMeshShortestPathAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default);

    // MESH TRAVERSAL
    Task<IReadOnlyList<Guid>> MeshBreadthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> MeshDepthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    Task<MeshRouteDto> FindMeshBfsRouteAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default);

    Task<MeshRouteDto> FindMeshDfsRouteAsync(
        Guid gatewayId,
        Guid sourceNodeId,
        Guid destinationNodeId,
        CancellationToken cancellationToken = default);


    // MESH SPANNING TREE
    Task<IReadOnlyList<MeshConnectionDto>> GetMeshMinimumSpanningTreeAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    // MESH LOGGING
    Task<IReadOnlyList<MeshLogDto>> GetMeshGatewayLogsAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshLogDto>> GetMeshNodeLogsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    // MESH ROUTING

    Task<MeshTraversalResultDto> BreadthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    Task<MeshTraversalResultDto> DepthFirstSearchAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    Task<MeshSpanningTreeDto> GetMeshSpanningTreeAsync(
        Guid gatewayId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);
}
