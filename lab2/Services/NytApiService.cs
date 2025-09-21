using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace NyTimesListServer.Services
{
    public class NytApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NytApiService(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<string> GetBooksListAsync(string category, string date)
        {
            string nytUrl = $"https://api.nytimes.com/svc/books/v3/lists/{date}/{WebUtility.UrlEncode(category)}.json?api-key={_apiKey}";
            
            var response = await _httpClient.GetAsync(nytUrl);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<JsonDocument> ParseJsonResponseAsync(string json)
        {
            return await Task.Run(() => JsonDocument.Parse(json));
        }

        public bool HasBooks(JsonDocument doc)
        {
            var root = doc.RootElement;
            return root.TryGetProperty("results", out var results) &&
                   results.TryGetProperty("books", out var books) &&
                   books.ValueKind == JsonValueKind.Array &&
                   books.GetArrayLength() > 0;
        }

        public JsonElement GetBooks(JsonDocument doc)
        {
            return doc.RootElement.GetProperty("results").GetProperty("books");
        }
    }
}