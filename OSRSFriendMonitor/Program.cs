using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using System.Net.WebSockets;

namespace OSRSFriendMonitor;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}


