namespace NyTimesListServer.Models
{
    public class CacheEntry
    {
        public string Payload { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}