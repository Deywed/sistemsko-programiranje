using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ML;
using ReactiveNewsServer.MLModels;
using ReactiveNewsServer.Models;

namespace ReactiveNewsServer.Services
{
    public class ReactiveNewServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _newsApiKey;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;
        private readonly HttpClient _httpClient;
        public ReactiveNewServer(string prefix, string newsApiKey)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _newsApiKey = newsApiKey;
            _predictionEngine = ModelHelper.CreatePredictionEngine();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ReactiveNewsServer/1.0");
            Console.WriteLine($"Server inicijalizovan na: {prefix}");
        }

        private string RemoveHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Uklanja HTML
            var cleanText = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", " ");

            // Uklanja visak razmaka
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, "\\s+", " ");

            return cleanText.Trim();
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server pokrenut");

            // Kreiraj Observable stream od HTTP zahteva
            var requestObservable = Observable.Create<HttpListenerContext>(observer =>
            {
                var cancellationDisposable = new CancellationDisposable();
                var ct = cancellationDisposable.Token;

                Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var context = await _listener.GetContextAsync();
                            observer.OnNext(context);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                }, ct);

                return cancellationDisposable;
            });

            // Obradi zahteve sa TaskPool Schedulerom za multithreading
            var processingSubscription = requestObservable
                .ObserveOn(TaskPoolScheduler.Default)
                .Select(context => Observable.FromAsync(() => ProcessRequestAsync(context)))
                .Merge() // Obradi zahteve paralelno
                .Subscribe(
                    context => LogSuccess(context),
                    ex => Console.WriteLine($"Greška u obradi zahteva: {ex.Message}"),
                    () => Console.WriteLine($"Server zaustavljen")
                );
            _disposables.Add(processingSubscription);
        }

        private async Task<HttpListenerContext> ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            LogRequest(request);
            
            try
            {
                // Proveri da li zahtev ima keyword parametar
                string? keyword = request.QueryString["keyword"];
                if (string.IsNullOrEmpty(keyword))
                {
                    await SendErrorResponseAsync(response, HttpStatusCode.BadRequest, "Missing 'keyword' parameter");
                    LogError(context, "Missing 'keyword' parameter");
                    return context;
                }

                // Prikupi članke sa News API-ja
                var articles = await FetchArticlesAsync(keyword);
                LogInfo(context, $"Pronađeno {articles.Count} članaka za ključnu reč: {keyword}");
                
                // Analiziraj sentiment za svaki članak
                var analysisResult = AnalyzeArticlesSentiment(keyword, articles);
                LogInfo(context, $"Analiza sentimenta završena. Pozitivnih: {CountPositiveArticles(analysisResult)}/{articles.Count}");
                
                // Pošalji odgovor
                await SendJsonResponseAsync(response, analysisResult);
                LogSuccess(context, "Zahtev uspešno obrađen");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Greška pri obradi zahteva: {ex.Message}");
                await SendErrorResponseAsync(response, HttpStatusCode.InternalServerError, $"Internal server error: {ex.Message}");
                LogError(context, ex.Message);
            }

            return context;
        }

        private async Task<List<Article>> FetchArticlesAsync(string keyword)
        {
            var url = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(keyword)}&apiKey={_newsApiKey}";
            
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "ReactiveNewsServer/1.0");
            
            var json = await client.GetStringAsync(url);
            var apiResponse = JsonSerializer.Deserialize<NewsApiResponse>(json);
            
            if (apiResponse?.Status?.ToLower() != "ok")
            {
                throw new Exception($"News API error: {apiResponse?.Status}");
            }

            return apiResponse.Articles ?? new List<Article>();
        }

        private AnalysisResult AnalyzeArticlesSentiment(string keyword, List<Article> articles)
        {
            var sentimentResults = new List<SentimentResult>();

            foreach (var article in articles)
            {
                // Očistite HTML tagove iz naslova i sadržaja
                string cleanTitle = RemoveHtmlTags(article.Title);
                string cleanContent = RemoveHtmlTags(article.Content);
                
                // Kombinuj naslov i sadržaj za analizu
                var textToAnalyze = $"{cleanTitle}. {cleanContent}";
                
                // Ograniči dužinu teksta radi performansi
                if (textToAnalyze.Length > 1000)
                {
                    textToAnalyze = textToAnalyze.Substring(0, 1000);
                }
                
                // Koristi ML.NET za analizu sentimenta
                var sentimentScore = AnalyzeSentiment(textToAnalyze);
                
                sentimentResults.Add(new SentimentResult
                {
                    Title = cleanTitle,
                    ContentSnippet = cleanContent?.Length > 100 
                        ? cleanContent.Substring(0, 100) + "..." 
                        : cleanContent!,
                    SentimentScore = sentimentScore
                });
            }

            return new AnalysisResult
            {
                Keyword = keyword,
                TotalArticles = articles.Count,
                Articles = sentimentResults
            };
        }

        private float AnalyzeSentiment(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0.5f;
            
            var prediction = _predictionEngine.Predict(new SentimentData { Text = text });
            return prediction.Probability;
        }

        private int CountPositiveArticles(AnalysisResult result)
        {
            int count = 0;
            foreach (var article in result.Articles)
            {
                if (article.SentimentScore > 0.5f) count++;
            }
            return count;
        }

        private async Task SendJsonResponseAsync(HttpListenerResponse response, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var buffer = Encoding.UTF8.GetBytes(json);
            
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task SendErrorResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            var errorResponse = new { error = message };
            var json = JsonSerializer.Serialize(errorResponse);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            response.StatusCode = (int)statusCode;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void LogRequest(HttpListenerRequest request)
        {
            Console.WriteLine($"Zahtev primljen: {request.HttpMethod} {request.Url}");
        }

        private void LogSuccess(HttpListenerContext context, string message = "Zahtev uspešno obrađen")
        {
            Console.WriteLine($"USPEH: {message} - Status: {context.Response.StatusCode} - URL: {context.Request.Url}");
        }

        private void LogError(HttpListenerContext context, string error)
        {
            Console.WriteLine($"GREŠKA: {error} - Status: {context.Response.StatusCode} - URL: {context.Request.Url}");
        }

        private void LogInfo(HttpListenerContext context, string info)
        {
            Console.WriteLine($" INFO: {info} - URL: {context.Request.Url}");
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Server zaustavljen");
        }

        public void Dispose()
        {
            Stop();
            _disposables.Dispose();
        }
    }
}