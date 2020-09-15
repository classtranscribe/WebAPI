using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using CTCommons.Grpc;
using CTCommons.MSTranscription;
using TaskEngine.Tasks;
using CTCommons;
using System.Threading;
using Newtonsoft.Json.Linq;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine
{
    public static class TaskEngineGlobals
    {
        public static KeyProvider KeyProvider { get; set; }
    }
    class Program
    {
        public static ServiceProvider _serviceProvider;
        public static ILogger<Program> _logger;
        public static void Main()
        {
            var configuration = CTDbContext.GetConfigurations();

            // This project relies on Dependency Injection to configure its various services,
            // For more info, https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1
            // All the services used are configured using the service provider.
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
                .AddSingleton<CaptionQueries>()
                .AddSingleton<DownloadPlaylistInfoTask>()
                .AddSingleton<DownloadMediaTask>()
                .AddSingleton<ConvertVideoToWavTask>()
                .AddSingleton<TranscriptionTask>()
                .AddSingleton<QueueAwakerTask>()
                .AddSingleton<GenerateVTTFileTask>()
                .AddSingleton<RpcClient>()
                .AddSingleton<ProcessVideoTask>()
                .AddSingleton<MSTranscriptionService>()
                .AddSingleton<SceneDetectionTask>()
                .AddSingleton<UpdateBoxTokenTask>()
                .AddSingleton<CreateBoxTokenTask>()
                .AddSingleton<BoxAPI>()
                .AddScoped<Seeder>()
                .AddScoped<SlackLogger>()
                .AddSingleton<TempCode>()
                .BuildServiceProvider();

            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);
            _logger.LogInformation("Seeding database");

            // Seed the database, with some initial data.
            Seeder seeder = serviceProvider.GetService<Seeder>();
            seeder.Seed();

            _logger.LogInformation("Starting TaskEngine");


            // Delete any pre-existing queues on rabbitMQ.
            RabbitMQConnection rabbitMQ = serviceProvider.GetService<RabbitMQConnection>();

            _logger.LogInformation("RabbitMQ - deleting all queues");

            rabbitMQ.DeleteAllQueues();
            //Todo. Recreate all of the queues before starting them below

            // Create and start consuming from all queues.
            
            ushort noConcurrency = 1;
            ushort minimalConcurrency = 2;
            ushort maxConcurrency = 4; //  Convert.ToUInt16(Globals.appSettings.RABBITMQ_PREFETCH_COUNT ?? "4");

            serviceProvider.GetService<DownloadMediaTask>().Consume(minimalConcurrency);
            serviceProvider.GetService<TranscriptionTask>().Consume(minimalConcurrency); // extracts audio

            serviceProvider.GetService<QueueAwakerTask>().Consume(maxConcurrency); //TODO TOREVIEW: noConcurrency?

            serviceProvider.GetService<GenerateVTTFileTask>().Consume(maxConcurrency);
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume(maxConcurrency);

            // We dont want concurrency for these tasks
            serviceProvider.GetService<UpdateBoxTokenTask>().Consume(noConcurrency);
            serviceProvider.GetService<CreateBoxTokenTask>().Consume(noConcurrency);
            // These are too heavy and low priority
            serviceProvider.GetService<ProcessVideoTask>().Consume(noConcurrency);
            serviceProvider.GetService<SceneDetectionTask>().Consume(noConcurrency);


            //nolonger used serviceProvider.GetService<nope ConvertVideoToWavTask>().Consume();

            bool hacktest = false;
            if (hacktest)
            {
                TempCode tempCode = serviceProvider.GetService<TempCode>();
                tempCode.Temp();
                return;
            }
            _logger.LogInformation("All done!");

            QueueAwakerTask queueAwakerTask = serviceProvider.GetService<QueueAwakerTask>();

            var timeInterval = new TimeSpan(5, 0, 0);

            // Check for new tasks every "timeInterval".
            while (true)
            {
                queueAwakerTask.Publish(new JObject
                {
                    { "Type", TaskType.PeriodicCheck.ToString() }
                });
                Thread.Sleep(timeInterval);
            };
        }

        // Catch all unhandled exceptions.
        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            _logger.LogError(e, "Unhandled Exception Caught");
        }
    }
}
