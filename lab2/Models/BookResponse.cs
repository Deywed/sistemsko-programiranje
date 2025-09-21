using System.Text.Json;
namespace NyTimesListServer.Models
{
    public class BookResponse
    {
        public string Date { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public JsonElement Books { get; set; }
    }
}