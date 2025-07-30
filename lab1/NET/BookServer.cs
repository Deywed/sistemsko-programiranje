using System.Net;

namespace lab1._1
{
    public class BookServer
    {
        private static Dictionary<string, string> MemoryCache = new Dictionary<string, string>();
        private readonly string api_key;
        private HttpListener listener;
        private static object locker = new object();

        public BookServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            api_key = "JBHSQTvBdTNd6g0yJv8WJhdbhwvN0iOV";
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine("Server started. Listening for requests...");
            while (true)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest!, context);
            }
        }

        public void HandleRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var request = context.Request;
            var response = context.Response;
            Console.WriteLine($"Handling response: {response.StatusCode}");


            try
            {
                var availableThreads = ThreadPool.ThreadCount;
                Console.WriteLine($"Available threads: {availableThreads}");

                string category = request.QueryString["category"]!;
                if (string.IsNullOrEmpty(category))
                {
                    string error = "Category parameter is required.";
                    WriteResponse(response, error, 400);
                    return;
                }

                string cacheKey = category.ToLower();
                string result;

                lock (locker)
                {
                    if (MemoryCache.TryGetValue(cacheKey, out result!))
                    {
                        Console.WriteLine($"Already in cache: {category}");
                        WriteResponse(response, result, 200);
                        return;
                    }
                }

                using (var client = new HttpClient())
                {
                    string url = $"https://api.nytimes.com/svc/books/v3/lists/current/{category}.json?api-key={api_key}";

                    var apiResponse = client.GetAsync(url).Result;

                    if (!apiResponse.IsSuccessStatusCode)
                    {
                        string error = $"NYT API returned error: {apiResponse.StatusCode}";
                        Console.WriteLine($"Error {error}");
                        WriteResponse(response, error, (int)apiResponse.StatusCode);
                        return;
                    }

                    result = apiResponse.Content.ReadAsStringAsync().Result;

                    lock (locker)
                    {
                        MemoryCache[cacheKey] = result;
                    }

                    Console.WriteLine($"Category is stored in cache: {category}");
                    WriteResponse(response, result, 200);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                WriteResponse(response, "Server error", 500);
            }
        }


        private void WriteResponse(HttpListenerResponse response, string content, int statusCode)
        {

            //ovo sam morao da dodam jer browser nije hteo da posalje podatke u flutter

            //-----
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            //-----

            response.StatusCode = statusCode;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
        }
    }
}