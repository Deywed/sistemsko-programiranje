using System;
using ReactiveNewsServer.Services;

namespace ReactiveNewsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Zamenite sa vašim News API ključem
            string newsApiKey = "c42ad60cac984181a27911de3c49c15d";
            
            // Port na kom server osluškuje
            string url = "http://localhost:8080/";
            
            // Kreiraj i pokreni server
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