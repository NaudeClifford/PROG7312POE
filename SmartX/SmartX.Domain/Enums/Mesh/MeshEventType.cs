namespace SmartX.Domain.Enums.Mesh;

public enum MeshEventType
{
    NodeConnected,
    NodeDisconnected,
    LinkEstablished,
    LinkLost,
    PacketSent,
    PacketReceived,
    PacketForwarded,
    PacketDropped,
    RouteDiscovered,
    RouteChanged,
    RouteRecovered,
    GatewayConnected,
    GatewayDisconnected,
    NetworkTopologyUpdated,
    NodeHeartbeatMissed,
    NodeRecovered
}