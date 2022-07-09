﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Services;
using ClassTranscribeDatabase.Services.MSTranscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using TaskEngine.Tasks;
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
        // Default concurrency (max jobs in parallel *PER QUEUE* (=Per task) if none are set in env
        private const ushort NO_CONCURRENCY = 1; // Some tasks should be serialized
        private const ushort MIN_CONCURRENCY = 2; // By definition minimal is two.

        private const ushort DISABLED_TASK = 0; // Task is disabled, expecting external task agent

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
                    //builder.AddApplicationInsights(configuration.GetValue<string>("APPLICATION_INSIGHTS_KEY"));
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
                .AddSingleton<BuildElasticIndexTask>()
                .AddSingleton<ExampleTask>()
                .AddSingleton<CleanUpElasticIndexTask>()
                .AddSingleton<BoxAPI>()
                .AddScoped<Seeder>()
                .AddScoped<SlackLogger>()
                .AddSingleton<TempCode>()
                .BuildServiceProvider();

            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            //TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

            _logger.LogInformation("Seeding database");

            // Seed the database, with some initial data.
            Seeder seeder = serviceProvider.GetService<Seeder>();
            seeder.Seed();

            _logger.LogInformation("Starting TaskEngine");


            // Delete any pre-existing queues on rabbitMQ.
            RabbitMQConnection rabbitMQ = serviceProvider.GetService<RabbitMQConnection>();

            // Active queues managed by C# (concurrency > 0) are now purged after the queue is created and before messages are processed

            ushort concurrent_videotasks = toUInt16(Globals.appSettings.MAX_CONCURRENT_VIDEO_TASKS, NO_CONCURRENCY);
            ushort concurrent_synctasks = toUInt16(Globals.appSettings.MAX_CONCURRENT_SYNC_TASKS, MIN_CONCURRENCY);
            ushort concurrent_transcriptions = toUInt16(Globals.appSettings.MAX_CONCURRENT_TRANSCRIPTIONS, MIN_CONCURRENCY);


            // Create and start consuming from all queues. If concurrency >=1 the queues are purged


            // Upstream Sync related
            _logger.LogInformation($"Creating DownloadPlaylistInfoTask & DownloadMediaTask consumers. Concurrency={concurrent_synctasks} ");
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume(concurrent_synctasks);
            serviceProvider.GetService<DownloadMediaTask>().Consume(concurrent_synctasks);

            // Transcription Related
            _logger.LogInformation($"Creating TranscriptionTask & GenerateVTTFileTask consumers. Concurrency={concurrent_transcriptions} ");

            serviceProvider.GetService<TranscriptionTask>().Consume(concurrent_transcriptions);
            serviceProvider.GetService<GenerateVTTFileTask>().Consume(concurrent_transcriptions);

            // Video Processing Related
            _logger.LogInformation($"Creating ProcessVideoTask consumer. Concurrency={concurrent_videotasks} ");
            serviceProvider.GetService<ProcessVideoTask>().Consume(concurrent_videotasks);
            
            // SceneDetection now handled by native Python
            //    See https://github.com/classtranscribe/pyapi
            serviceProvider.GetService<SceneDetectionTask>().Consume(DISABLED_TASK);

            // We dont want concurrency for these tasks
            _logger.LogInformation("Creating QueueAwakerTask and Box token tasks consumers.");
            serviceProvider.GetService<QueueAwakerTask>().Consume(NO_CONCURRENCY); //TODO TOREVIEW: NO_CONCURRENCY?
            serviceProvider.GetService<UpdateBoxTokenTask>().Consume(NO_CONCURRENCY);
            serviceProvider.GetService<CreateBoxTokenTask>().Consume(NO_CONCURRENCY);

            // Elastic Search index should be built after TranscriptionTask
            serviceProvider.GetService<BuildElasticIndexTask>().Consume(NO_CONCURRENCY);

            // Outdated Elastic Search index would be removed
            serviceProvider.GetService<CleanUpElasticIndexTask>().Consume(NO_CONCURRENCY);

            serviceProvider.GetService<ExampleTask>().Consume(NO_CONCURRENCY);
            
            _logger.LogInformation("Done creating task consumers");
            //nolonger used :
            // nope serviceProvider.GetService<nope ConvertVideoToWavTask>().Consume(concurrent_videotasks);

            bool hacktest = false;
            if (hacktest)
            {
                TempCode tempCode = serviceProvider.GetService<TempCode>();
                tempCode.Temp();
                return;
            }
            _logger.LogInformation("All done!");

            QueueAwakerTask queueAwakerTask = serviceProvider.GetService<QueueAwakerTask>();

            int periodicCheck = Math.Max(1,Convert.ToInt32(Globals.appSettings.PERIODIC_CHECK_EVERY_MINUTES));
            
            _logger.LogInformation("Periodic Check Every {0} minutes", periodicCheck);
            var timeInterval = new TimeSpan(0, periodicCheck, 0);

            // Check for new tasks every "timeInterval".
            // The periodic check will discover all undone tasks
            // TODO/REVIEW: However some tasks also publish the next items
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

        private static ushort toUInt16(String val, ushort defaultVal)
        {
            // ConvertToUInt16(String, int base) is not the droid you are looking for 
            if (val != null && val.Length > 0)
            {
                return Convert.ToUInt16(val); //May throw exception if val is not convertable
            }
            return defaultVal;
        }
    }
}
