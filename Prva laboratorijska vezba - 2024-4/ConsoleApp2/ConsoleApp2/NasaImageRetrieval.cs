using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NasaProject
{
    class NasaImageRetrieval
    {
        //za dobijanje API kljuca pristupiti adresi: https://api.nasa.gov/
        private readonly string api_key;
        //url na osnovu kojeg pristupamo podacima sa NASA sajta
        private readonly string url;

        //pravimo Http klijenta
        private HttpClient client;
        
        //potrebno je sacuvati pribavljene slike
        private Dictionary<int, string> slike = new Dictionary<int, string>();
        private Dictionary<int, Bitmap> cele_slike = new Dictionary<int, Bitmap>();

        //voditi racuna da lock mora da bude referentnog tipa -> koristiti lock samo za tu svrhu
        private static object locker = new object();

        private int br_slike = 0;

        public NasaImageRetrieval()
        {
            //inicijalizacija mrezne komunikacije
            api_key = "eu4FyaSnH5QszPzrEy893bh85xZ70bbZBsWfySpR";
            url = $"https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?earth_date=2015-6-3&api_key={api_key}";
            client = new HttpClient();
        }
        //funkcija za rad bez upotrebe niti + sinhroni pozivi (na sledecem terminu rad sa asinhronim pozivima)
        public void GetNasaImages()
        {
            try
            {
                var data = FetchNasaAPI();
                ImageDataProcess(data);
                MakeBitmaps();
            }
            catch (HttpRequestException e)
            {
                //kada radimo sa Http zahtevima, znamo koji tip izuzetka mozemo da ocekujemo
                //ukoliko niste sigurni, u redu je koristiti i samo Exception klasu
                Console.WriteLine($"Doslo je do pojave izuzetka prilikom pristuanju API-u : {e.Message}");
            }
        }
        public JObject FetchNasaAPI()
        {
            HttpResponseMessage response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            //voditi racuna da je ovo sinhrona operacija (... .Result;)
            string res_body = response.Content.ReadAsStringAsync().Result;
            var data = JObject.Parse(res_body);
            Console.WriteLine(data);
            return data;
        }
        public void ImageDataProcess(JObject data)
        {
            foreach (var slika in data["photos"])
            {
                int id = (int)slika["id"];
                string img_src = (string)slika["img_src"];
                Console.WriteLine("Identifikator slike i njen url:\n");
                Console.WriteLine($"{id} : {img_src} \n");
                //dodajemo u listu slika
                slike.Add(id, img_src);
            }
        }
        public void MakeBitmaps()
        {
            foreach (KeyValuePair<int, string> kvp in slike)
            {
                //u kvp.Value se nalazi url slike kojoj zelimo da pristupimo
                var res = client.GetAsync(kvp.Value).Result;
                Console.WriteLine(res);
                res.EnsureSuccessStatusCode();
                byte[] bajtovi = res.Content.ReadAsByteArrayAsync().Result;
                //na osnovu ovih bajtova pravimo bitmape
                using var mem_stream = new MemoryStream(bajtovi);
                Bitmap bmp = new(mem_stream);
                cele_slike.Add(br_slike++, bmp);
            }
        }
        public void MakeBitmapsThreads(string imgUrl)
        {
            var res = client.GetAsync(imgUrl).Result;
            Console.WriteLine(res);
            res.EnsureSuccessStatusCode();
            byte[] bajtovi = res.Content.ReadAsByteArrayAsync().Result;
            using var memStream = new MemoryStream(bajtovi);
            Bitmap bmp = new(memStream);
            lock (locker)
            {
                cele_slike.Add(br_slike++, bmp);
                Console.WriteLine($"Nit pristupa slici {br_slike}");
            }
        }
        public void GetNasaImagesThreads()
        {
            try
            {
                var data = FetchNasaAPI();
                ImageDataProcess(data);
                List<Thread> threads = new List<Thread>();
                int br_slike = 0;
                foreach (KeyValuePair<int, string> kvp in slike)
                {
                    var thread = new Thread(() => MakeBitmapsThreads(kvp.Value));
                    //dodajemo nit u listu niti
                    threads.Add(thread);
                    //obavezno pokrenuti nit pozivom Start metode
                    thread.Start();
                }
                foreach (Thread n in threads)
                {
                    n.Join();
                }

                Console.WriteLine("Niti su zavrsile pribavljanje slika");

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Doslo je do pojave izuzetka prilikom pristuanju API-u : {e.Message}");
            }
        }
        
        public void GetNasaImagesThreadPool()
        {
            try
            {
                var data = FetchNasaAPI();
                ImageDataProcess(data);
                int broj_niti = Environment.ProcessorCount;
                Console.WriteLine($"Dostupan broj niti je: {broj_niti}");
                int worker_niti, io_niti;
                ThreadPool.GetAvailableThreads(out worker_niti, out io_niti);
                var resetEvents = new List<ManualResetEvent>();
               
                foreach (KeyValuePair<int, string> kvp in slike)
                {
                    var resetEvent = new ManualResetEvent(false);
                    resetEvents.Add(resetEvent);
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            MakeBitmapsThreads(kvp.Value);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Doslo je do pojave izuzetka prilikom rada sa ThreadPool-om : {e.Message}");
                        }
                        finally
                        {
                            resetEvent.Set();
                        }
                    });
                }
                    
                foreach (var resetEvent in resetEvents)
                {
                    resetEvent.WaitOne();
                }
                Console.WriteLine("Niti iz ThreadPool-a su zavrsile pribavljanje slika");


            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Doslo je do pojave izuzetka prilikom pristuanju API-u : {e.Message}");
            }
          
        }

    }
}
