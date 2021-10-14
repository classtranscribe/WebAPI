using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Services;
using ClassTranscribeDatabase.Services.MSTranscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevExperiments
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = CTDbContext.GetConfigurations();
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                             ("", LogLevel.Warning);
                    builder.AddApplicationInsights(configuration.GetValue<string>("APPLICATION_INSIGHTS_KEY"));
                })
                .AddOptions()
                .Configure<AppSettings>(configuration)
                .AddDbContext<CTDbContext>(options => options.UseLazyLoadingProxies().UseNpgsql(CTDbContext.ConnectionStringBuilder()))
                .AddScoped<SlackLogger>()
                .AddSingleton<MSTranscriptionService>()
                .AddSingleton<TempCode>()
                .AddSingleton<RpcClient>()
                .BuildServiceProvider();

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;

            TempCode tempCode = serviceProvider.GetService<TempCode>();
            tempCode.Temp();            
        }
    }
}
