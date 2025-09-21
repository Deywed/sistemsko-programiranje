using System;
using ReactiveNewsServer.Services;

namespace ReactiveNewsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string newsApiKey = "YOUR_NEWS_API";
            
            string url = "http://localhost:8080/";
            
            using var server = new ReactiveNewServer(url, newsApiKey);
            
            try
            {
                server.Start();
                Console.WriteLine($"Server pokrenut. Pristupite putem: {url}?keyword=tehnologija");
                Console.WriteLine("Pritisnite Enter za zaustavljanje servera...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Došlo je do greške: {ex.Message}");
            }
        }
    }
}