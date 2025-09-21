using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;

namespace NyTimesListServer
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
        private static SimpleLogger logger;
        private static NytApiService nytApiService;
        private static readonly string NytApiKey = "YOUR_API_KEY_HERE";

        static void Main(string[] args)
        {
            logger = new SimpleLogger("logs/server.log");
            nytApiService = new NytApiService(httpClient, NytApiKey);
            
            const string prefix = "http://localhost:8080/";
            //ovo je server
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try { listener.Start(); }
            catch (HttpListenerException hlex)
            {
                logger.Error($"Ne mogu da startujem HttpListener: {hlex.Message}");
                return;
            }

            logger.Info($"Server startovan na {prefix}");


            //provera u kesu da li je info istekao
            var cleanupTimer = new Timer(_ => CleanupCache(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            while (listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(state => HandleContext((HttpListenerContext)state!), context);
                }
                catch (Exception ex)
                {
                    logger.Error($"Greška pri prihvatanju zahteva: {ex.Message}");
                }
            }

            cleanupTimer.Dispose();
            listener.Close();
        }

        private static void HandleContext(HttpListenerContext context)
        {
            var req = context.Request;
            var resp = context.Response;
            string category = req.QueryString["category"] ?? "";
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string cacheKey = $"date:{date}|category:{category.Trim().ToLowerInvariant()}";

            if (req.HttpMethod != "GET")
            {
                WriteStringResponse(resp, 400, JsonSerializer.Serialize(new { error = "Invalid request" }), "application/json");
                return;
            }

            if (cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                WriteStringResponse(resp, 200, entry.Payload, "application/json");
                return;
            }

            string nytUrl = $"https://api.nytimes.com/svc/books/v3/lists/{date}/{WebUtility.UrlEncode(category)}.json?api-key={NytApiKey}";

            try
            {
                var nytResponse = httpClient.GetAsync(nytUrl).GetAwaiter().GetResult();

                if (!nytResponse.IsSuccessStatusCode)
                {
                    WriteStringResponse(resp, 502, JsonSerializer.Serialize(new { error = "Upstream API error" }), "application/json");
                    return;
                }
                else if (nytResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    WriteStringResponse(resp, 404, JsonSerializer.Serialize(new { error = "Category not found" }), "application/json");
                    return;
                }

                string json = nytResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                //parsiramo json u documnet
                using var doc = JsonDocument.Parse(json);
                //document pretvaramo u objekat, pomocu getprop mozemo da pristupimo njegovim atributima
                var root = doc.RootElement;
                

                if (root.TryGetProperty("results", out var results) &&
                    results.TryGetProperty("books", out var books) &&
                    books.ValueKind == JsonValueKind.Array &&
                    books.GetArrayLength() > 0)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        date,
                        category,
                        count = books.GetArrayLength(),
                        books
                    });

                    cache[cacheKey] = new CacheEntry { Payload = payload, ExpiresAt = DateTime.UtcNow.Add(CacheTtl) };
                    WriteStringResponse(resp, 200, payload, "application/json");
                    return;
                }

                WriteStringResponse(resp, 404, JsonSerializer.Serialize(new { error = "No books found" }), "application/json");
            }
            catch
            {
                WriteStringResponse(resp, 500, JsonSerializer.Serialize(new { error = "Internal Server Error" }), "application/json");
            }
            finally
            {
                try { resp.Close(); } catch { }
            }
        }

        private static void WriteStringResponse(HttpListenerResponse resp, int statusCode, string body, string contentType)
        {
            try
            {
                resp.StatusCode = statusCode;
                resp.ContentType = contentType;
                byte[] data = Encoding.UTF8.GetBytes(body);
                resp.OutputStream.Write(data, 0, data.Length);
            }
            catch { }
        }

        private static void CleanupCache()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in cache)
                if (kv.Value.ExpiresAt <= now)
                    cache.TryRemove(kv.Key, out _);
        }
    }
}
