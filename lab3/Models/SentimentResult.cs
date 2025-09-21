using System;

namespace ReactiveNewsServer.Models
{
    public class SentimentResult
    {
        //ovo se serijalizuje u json objekat
        public required string Title { get; set; }
        public required string ContentSnippet { get; set; }
        public required float SentimentScore { get; set; }
        public string Sentiment => SentimentScore > 0.5f ? "Pozitivan" : "Negativan";
    }
}