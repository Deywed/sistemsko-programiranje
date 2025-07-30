using System;
using System.Diagnostics;
using NasaProject;
class Program
{
    
    static void Main(string[] args)
    {
        Stopwatch stopwatch = new Stopwatch();
        Console.WriteLine("Krece glavna nit izvrsenja...");
        NasaImageRetrieval retriever = new(); //isto kao new NasaImageRetrieval()

        //pribavljanje slika bez niti
        //stopwatch.Start();
        //retriever.GetNasaImages();
        //stopwatch.Stop();
        //Console.WriteLine($"Vreme potrebno za pribavljanje slika - BEZ NITI: {stopwatch.Elapsed}");
        //stopwatch.Reset();

        ////pribavljanje slika sa nitima - rucno
        //stopwatch.Start();
        //retriever.GetNasaImagesThreads();
        //stopwatch.Stop();
        //Console.WriteLine($"Vreme potrebno za pribavljanje slika - NITI bez ThreadPool-a: {stopwatch.Elapsed}");
        //stopwatch.Reset();

        ////pribavljenje slika sa nitima - ThreadPool
        stopwatch.Start();
        retriever.GetNasaImagesThreadPool();
        stopwatch.Stop();
        Console.WriteLine($"Vreme potrebno za pribavljanje slika - NITI sa ThreadPool-om: {stopwatch.Elapsed}");
        Console.ReadLine();

        //*************REZULTATI*************
        //bez niti -> 00:00:03.2941545
        //sa nitima bez ThreadPool-a -> 00:00:01.9766154
        //sa nitima sa ThreadPool-om -> 00:00:01.8947047
    }
}






















