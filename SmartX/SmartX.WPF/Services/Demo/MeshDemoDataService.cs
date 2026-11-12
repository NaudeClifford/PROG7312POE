
using SmartX.Shared.DTOs.Mesh;

namespace SmartX.WPF.Services.Mesh;

public sealed class MeshDemoDataService
{
    public sealed class DemoNetwork
    {
        public Guid GatewayId { get; init; }
        public List<MeshNodeDto> Nodes { get; init; } = [];
        public List<MeshLinkDto> Links { get; init; } = [];
        public List<MeshLogDto> Logs { get; init; } = [];
        public MeshHealthReportDto Health { get; init; } = new();
    }

    public DemoNetwork CreateDemoNetwork(Guid? gatewayId = null)
    {
        var gateway = gatewayId is { } id && id != Guid.Empty
            ? id
            : Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var now = DateTime.UtcNow;

        var nodes = new List<MeshNodeDto>();

        var statuses = new[]
        {
            "Online", "Online", "Online", "Online",
            "Online", "Online", "Degraded", "Offline",
            "Online", "Online", "Degraded", "Online"
        };

        for (int i = 0; i < 12; i++)
        {
            nodes.Add(new MeshNodeDto
            {
                Id = Guid.Parse(
                    $"00000000-0000-0000-0000-{i + 1:000000000000}"),

                GatewayId = gateway,
                Name = i == 0 ? "Main Gateway" : $"Mesh Node {i:00}",
                DeviceIdentifier = $"SMARTX-NODE-{i + 1:000}",
                NodeType = i == 0 ? "Gateway" : i % 3 == 0
                    ? "Repeater"
                    : "Sensor",

                Status = i == 0 ? "Online" : statuses[i],
                IsGateway = i == 0,
                LastSeen = statuses[i] == "Offline"
                    ? now.AddMinutes(-35)
                    : now.AddSeconds(-i * 12),

                CreatedAt = now.AddDays(-30),
                UpdatedAt = now
            });
        }

        // Each connection joins two actual nodes.
        // The graph is intentionally connected through the gateway.
        var pairs = new (int Source, int Target)[]
        {
            (0, 1), (0, 2), (0, 3),
            (1, 4), (1, 5), (2, 5),
            (2, 6), (3, 7), (3, 8),
            (4, 9), (5, 9), (5, 10),
            (6, 10), (7, 11), (8, 11),
            (9, 11), (2, 8), (4, 6)
        };

        var links = new List<MeshLinkDto>();

        for (int i = 0; i < pairs.Length; i++)
        {
            var pair = pairs[i];

            var unavailable = i == 7 || i == 12;

            var quality = unavailable
                ? 0
                : 65 + (i * 7 % 35);

            links.Add(new MeshLinkDto
            {
                Id = Guid.NewGuid(),
                GatewayId = gateway,
                SourceNodeId = nodes[pair.Source].Id,
                TargetNodeId = nodes[pair.Target].Id,

                SignalStrengthDbm = unavailable
                    ? -100
                    : -35 - (i * 3 % 45),

                LatencyMilliseconds = unavailable
                    ? null
                    : 8 + (i * 7 % 65),

                EnergyCost = 1.5 + (i * 0.35),
                IsAvailable = !unavailable,
                LinkQuality = quality,

                LastSeenAt = now.AddSeconds(-i * 15),
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now
            });
        }

        var logs = new List<MeshLogDto>();

        var events = new[]
        {
            ("GatewayOnline", "Gateway connected successfully."),
            ("NodeDiscovered", "New mesh node discovered."),
            ("LinkEstablished", "Communication link established."),
            ("Monitoring", "Network monitoring completed."),
            ("NodeDegraded", "Signal quality below expected level."),
            ("LinkUnavailable", "Connection unavailable."),
            ("Synchronization", "Mesh synchronization completed."),
            ("NodeOnline", "Node returned to online status.")
        };

        for (int i = 0; i < events.Length; i++)
        {
            logs.Add(new MeshLogDto
            {
                Id = Guid.NewGuid(),
                GatewayId = gateway,
                NodeId = nodes[i].Id,
                EventType = events[i].Item1,
                Message = events[i].Item2,
                Timestamp = now.AddMinutes(-i * 12)
            });
        }

        return new DemoNetwork
        {
            GatewayId = gateway,
            Nodes = nodes,
            Links = links,
            Logs = logs,

            Health = new MeshHealthReportDto
            {
                GatewayId = gateway,
                GeneratedAt = now,
                Status = "Degraded",

                TotalNodes = 12,
                OnlineNodes = 9,
                OfflineNodes = 1,
                DegradedNodes = 2,
                UnreachableNodes = 1,

                TotalLinks = 18,
                AvailableLinks = 16,
                UnavailableLinks = 2,

                TotalRoutes = 24,
                SuccessfulRoutes = 21,
                FailedRoutes = 3,

                AverageLatency = 28.6,
                NetworkAvailability = 88.5,

                Warnings =
                [
                    "Two mesh connections are unavailable.",
                    "Two nodes have degraded signal quality.",
                    "One node is currently offline."
                ]
            }
        };
    }
}
