using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NyTimesListServer.Models;
using NyTimesListServer.Services;

namespace NyTimesListServer
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
        private static SimpleLogger logger;
        private static NytApiService nytApiService;
        private static readonly string NytApiKey = "API_KEY_HERE";

        static async Task Main(string[] args)
        {
            logger = new SimpleLogger("logs/server.log");
            nytApiService = new NytApiService(httpClient, NytApiKey);
            
            const string prefix = "http://localhost:8080/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try { listener.Start(); }
            catch (HttpListenerException hlex)
            {
                logger.Error($"HttpListener error: {hlex.Message}");
                return;
            }

            logger.Info($"Server startovan na {prefix}");

            var cleanupTimer = new Timer(_ => CleanupCache(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleContextAsync(context));
                }
                catch (Exception ex)
                {
                    logger.Error($"Greška pri prihvatanju zahteva: {ex.Message}");
                }
            }

            cleanupTimer.Dispose();
            listener.Close();
        }

        private static async Task HandleContextAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var resp = context.Response;
            string category = req.QueryString["category"] ?? "";
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string cacheKey = $"date:{date}|category:{category.Trim().ToLowerInvariant()}";

            if (req.HttpMethod != "GET")
            {
                await WriteStringResponseAsync(resp, 400, JsonSerializer.Serialize(new { error = "Invalid request" }), "application/json");
                return;
            }

            if (cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                await WriteStringResponseAsync(resp, 200, entry.Payload, "application/json");
                return;
            }

            try
            {
                var json = await nytApiService.GetBooksListAsync(category, date);
                var doc = await nytApiService.ParseJsonResponseAsync(json);

                if (nytApiService.HasBooks(doc))
                {
                    var books = nytApiService.GetBooks(doc);
                    var payload = JsonSerializer.Serialize(new
                    {
                        date,
                        category,
                        count = books.GetArrayLength(),
                        books
                    });

                    cache[cacheKey] = new CacheEntry { Payload = payload, ExpiresAt = DateTime.UtcNow.Add(CacheTtl) };
                    await WriteStringResponseAsync(resp, 200, payload, "application/json");
                }
                else
                {
                    await WriteStringResponseAsync(resp, 404, JsonSerializer.Serialize(new { error = "No books found" }), "application/json");
                }
                
                doc.Dispose();
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                await WriteStringResponseAsync(resp, 404, JsonSerializer.Serialize(new { error = "Category not found" }), "application/json");
            }
            catch (HttpRequestException ex)
            {
                logger.Error($"API greška: {ex.Message}");
                await WriteStringResponseAsync(resp, 502, JsonSerializer.Serialize(new { error = "Upstream API error" }), "application/json");
            }
            catch (Exception ex)
            {
                logger.Error($"Greška u obradi zahteva: {ex.Message}");
                await WriteStringResponseAsync(resp, 500, JsonSerializer.Serialize(new { error = "Internal Server Error" }), "application/json");
            }
            finally
            {
                try { resp.Close(); } catch { }
            }
        }

        private static async Task WriteStringResponseAsync(HttpListenerResponse resp, int statusCode, string body, string contentType)
        {
            try
            {
                resp.StatusCode = statusCode;
                resp.ContentType = contentType;
                byte[] data = Encoding.UTF8.GetBytes(body);
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
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