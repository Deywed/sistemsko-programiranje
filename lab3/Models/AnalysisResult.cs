using System.Collections.Generic;

namespace ReactiveNewsServer.Models
{
    public class AnalysisResult
    {
        public required string Keyword { get; set; }
        public int TotalArticles { get; set; } = 0;
        public required List<SentimentResult> Articles { get; set; } = new List<SentimentResult>();
    }
}