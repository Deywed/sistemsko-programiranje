using System;
using System.Text.Json.Serialization;

namespace ReactiveNewsServer.Models
{
    public class Article
    {
        //ovo se deserijalizuje iz json objekta, da bi znao u kom
        //polju ide sta mi kazemo da atribut u jsonu title ide u property Title
        
        [JsonPropertyName("title")]
        public required string Title { get; set; }
        
        [JsonPropertyName("content")]
        public required string Content { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime PublishedAt { get; set; }
        
        [JsonPropertyName("url")]
        public required string Url { get; set; }
    }
}