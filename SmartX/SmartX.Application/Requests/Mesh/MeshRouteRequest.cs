

namespace SmartX.Application.Requests.Mesh
{
    public class MeshRouteRequest
    {
        public Guid GatewayId { get; set; }
        public Guid SourceNodeId { get; set; }
        public Guid DestinationNodeId { get; set; }
    }
}
