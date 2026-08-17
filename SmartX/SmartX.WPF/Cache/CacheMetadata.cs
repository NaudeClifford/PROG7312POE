namespace SmartX.WPF.Cache
{
    public class CacheMetadata
    {
        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public DateTime LastSyncedAt { get; set; }
    }
}
