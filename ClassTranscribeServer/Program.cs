using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace ClassTranscribeServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions().Configure<AppSettings>(CTDbContext.GetConfigurations());

                    string viewSQL = Environment.GetEnvironmentVariable("LogEntityFrameworkSQL") ?? "false";

                    if (viewSQL.Trim().ToUpperInvariant() != "TRUE")
                    {
                        services.AddLogging(logging =>
                        {
                            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                        });
                    }
                });
        }
    }
}
