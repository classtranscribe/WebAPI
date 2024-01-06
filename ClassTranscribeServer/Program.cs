using ClassTranscribeDatabase;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace ClassTranscribeServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var v = WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(c => c.AddOptions().Configure<AppSettings>(CTDbContext.GetConfigurations()));
            
            // TTODO better code would use AppSettings

            string viewSQL = Environment.GetEnvironmentVariable("LogEntityFrameworkSQL") ?? "false";

            if( viewSQL.Trim().ToUpperInvariant() != "TRUE") {
                
                v.ConfigureLogging((context, logging) => {
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                });
            }
            return v.UseStartup<Startup>();
        }
    }
}
