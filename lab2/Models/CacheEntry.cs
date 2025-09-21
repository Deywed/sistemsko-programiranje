namespace NyTimesListServer.Models
{
    public class CacheEntry
    {
        public string Payload { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}