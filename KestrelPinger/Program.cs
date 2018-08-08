using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelPinger
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromHours(12)
            };

            while (true)
            {
                try
                {
                    var response = await httpClient.SendAsync(new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(args[0])
                    }, HttpCompletionOption.ResponseHeadersRead);

                    Console.WriteLine("Ping: " + await response.Content.ReadAsStringAsync());
                }
                catch (Exception)
                {
                    Console.WriteLine("Ping failed");
                }

                Thread.Sleep(1000);
            }
        }
    }
}
