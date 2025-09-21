using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReactiveNewsServer.Models
{
    public class NewsApiResponse
    {
        [JsonPropertyName("status")]
        public required string Status { get; set; }
        
        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }

        [JsonPropertyName("articles")]
        public List<Article> Articles { get; set; } = new List<Article>();
    }
}