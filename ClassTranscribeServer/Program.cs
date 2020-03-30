using ClassTranscribeDatabase;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClassTranscribeServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
                })
                .ConfigureServices(c => c.AddOptions().Configure<AppSettings>(CTDbContext.GetConfigurations()))
                .UseStartup<Startup>();
        }
    }
}
