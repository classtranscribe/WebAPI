using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using TaskEngine.Grpc;
using TaskEngine.MSTranscription;
using TaskEngine.Tasks;

namespace TaskEngine
{
    public static class TaskEngineGlobals
    {
        public static KeyProvider KeyProvider { get; set; }
    }
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
                .AddSingleton<RabbitMQConnection>()
                .AddSingleton<DownloadPlaylistInfoTask>()
                .AddSingleton<DownloadMediaTask>()
                .AddSingleton<ConvertVideoToWavTask>()
                .AddSingleton<TranscriptionTask>()
                .AddSingleton<QueueAwakerTask>()
                .AddSingleton<GenerateVTTFileTask>()
                .AddSingleton<RpcClient>()
                .AddSingleton<ProcessVideoTask>()
                .AddSingleton<MSTranscriptionService>()
                .AddSingleton<EPubGeneratorTask>()
                .AddSingleton<UpdateBoxTokenTask>()
                .AddSingleton<CreateBoxTokenTask>()
                .AddSingleton<BoxAPI>()
                .AddScoped<Seeder>()
                .AddScoped<SlackLogger>()
                .AddSingleton<TempCode>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            Globals.logger = logger;

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);


            RabbitMQConnection rabbitMQ = serviceProvider.GetService<RabbitMQConnection>();

            Seeder seeder = serviceProvider.GetService<Seeder>();
            seeder.Seed();


            logger.LogInformation("Starting application");
            rabbitMQ.DeleteAllQueues();
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume();
            serviceProvider.GetService<DownloadMediaTask>().Consume();
            serviceProvider.GetService<ConvertVideoToWavTask>().Consume();
            serviceProvider.GetService<TranscriptionTask>().Consume();
            serviceProvider.GetService<QueueAwakerTask>().Consume();
            serviceProvider.GetService<GenerateVTTFileTask>().Consume();
            serviceProvider.GetService<ProcessVideoTask>().Consume();
            serviceProvider.GetService<EPubGeneratorTask>().Consume();
            serviceProvider.GetService<UpdateBoxTokenTask>().Consume();
            serviceProvider.GetService<CreateBoxTokenTask>().Consume();

            TempCode tempCode = serviceProvider.GetService<TempCode>();

            tempCode.CronJob();
            // tempCode.Temp();

            logger.LogInformation("All done!");
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Globals.logger.LogError(e, "Unhandled Exception Caught");
        }
    }
}
