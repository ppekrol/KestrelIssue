using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;

namespace KestreLIssue
{
    public class Program
    {
        private static IWebHost _webHost;

        private static readonly List<Process> _pingers = new List<Process>();

        public static async Task Main(string[] args)
        {
            _webHost = new WebHostBuilder()
                .CaptureStartupErrors(captureStartupErrors: true)
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .UseStartup<ServerStartup>()
                .UseShutdownTimeout(TimeSpan.FromSeconds(1))
                .ConfigureLogging(builder =>
                {
                    //builder.AddConsole();
                    //builder.AddDebug();
                    //builder.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

            _webHost.Start();

            var serverAddressesFeature = _webHost.ServerFeatures.Get<IServerAddressesFeature>();
            var url = serverAddressesFeature.Addresses.First();

            Console.WriteLine("Started server at: " + url);

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromHours(12)
            };

            Console.WriteLine("Starting first request...");

            var response = await httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            }, HttpCompletionOption.ResponseHeadersRead);

            Console.WriteLine("Finished first request...");

            if (response.IsSuccessStatusCode == false)
                throw new InvalidOperationException("Invalid response!");

            try
            {
                if (args.Length > 0)
                {
                    Console.WriteLine("Starting pingers...");

                    StartPingers(3, url); // comment this line and everything will work

                    Console.WriteLine("Started pingers...");
                }
                else
                {
                    Console.WriteLine("No pingers!");
                }

                Console.WriteLine("Disposing server...");

                _webHost.Dispose();

                Console.WriteLine("Disposed server...");

                ShowNetStat(url); // still listening when pingers are active!

                Console.WriteLine("Starting second request...");

                response = await httpClient.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                });

                Console.WriteLine("Finished second request...");

                if (response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Should not be successful!");

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
            finally
            {
                StopPingers();
            }
        }

        private static void StopPingers()
        {
            foreach (var pinger in _pingers)
            {
                try
                {
                    pinger?.Kill();
                }
                catch (Exception)
                {
                }
            }
        }

        private static void StartPingers(int count, string url)
        {
#if DEBUG
            var path = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\KestrelPinger\\bin\\Debug\\netcoreapp2.1\\KestrelPinger.dll");
#else
            var path = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\KestrelPinger\\bin\\Release\\netcoreapp2.1\\KestrelPinger.dll");
#endif

            var file = new FileInfo(path);

            if (file.Exists == false)
                throw new FileNotFoundException($"No pinger. Path: '{file.FullName}'", file.FullName);

            for (var i = 0; i < count; i++)
            {
                var processInfo = new ProcessStartInfo
                {
                    ArgumentList = { file.FullName, url },
                    FileName = "dotnet"
                };

                var process = Process.Start(processInfo);

                Console.WriteLine("Pinger started: " + process.Id);

                _pingers.Add(process);
            }

            Thread.Sleep(3000);
        }

        private static void ShowNetStat(string url)
        {
            var uri = new Uri(url, UriKind.Absolute);

            var processInfo = new ProcessStartInfo
            {
                ArgumentList = { "-ano" },
                FileName = "netstat",
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var process = Process.Start(processInfo);

            var sb = new StringBuilder();
            sb.AppendLine("NETSTAT:");

            try
            {
                var stream = process.StandardOutput;

                while (stream.EndOfStream == false)
                {
                    var line = stream.ReadLine();
                    if (line.Contains($":{uri.Port}", StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine(line);
                }
            }
            finally
            {
                try
                {
                    process?.Kill();
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
