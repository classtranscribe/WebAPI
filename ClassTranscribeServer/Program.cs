using ClassTranscribeDatabase;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            var v = WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(c => c.AddOptions().Configure<AppSettings>(CTDbContext.GetConfigurations()));
            
            // TODO: This filter could be a environment variable setting
            // However we are still building the configuration at this point (is AppSettings configured here?)
                v.ConfigureLogging((context, logging) => {
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                });
            return v.UseStartup<Startup>();
        }
    }
}
