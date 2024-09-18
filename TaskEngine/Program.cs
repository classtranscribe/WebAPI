using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json.Linq;

using static ClassTranscribeDatabase.CommonUtils;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Services;
using ClassTranscribeDatabase.Services.MSTranscription;

using TaskEngine.Tasks;

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
            Console.WriteLine("TaskEngine.Main starting up -GetConfigurations..."); 
            try {
                SetupServices(); // should never return
                createTaskQueues();
                runQueueAwakerForever();
                
            } catch (Exception e) {
                // Some paranoia here; we *should* have a logger and exception handler in place
                // So this is only here to catch unexpected startup errors that otherwise might be silent
                Console.WriteLine($"Unhandled Exception Caught {e.Message}\n{e}\n");
                if(_logger !=null){
                    _logger.LogError(e, "Unhandled Exception Caught");
                }
            }
        }
        public static void SetupServices()
        {
            var configuration = CTDbContext.GetConfigurations();

            // This project relies on Dependency Injection to configure its various services,
            // For more info, https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.1
            // All the services used are configured using the service provider.
            Console.WriteLine("SetupServices() - starting");

            _serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                             ("", LogLevel.Warning);
                    // If we use A.I. in the future -
                    // Use the AddApplicationInsights() overload which accepts Action<TelemetryConfiguration> and set TelemetryConfiguration.ConnectionString. See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.
                    
                    // string insightKey = configuration.GetValue<string>("APPLICATION_INSIGHTS_KEY");
                    // if (!String.IsNullOrEmpty(insightKey) && insightKey.Trim().Length>1)
                    // {
                    //     builder.AddApplicationInsights(insightKey);
                    // }
                })
                .AddOptions()
                .Configure<AppSettings>(configuration)
                .AddDbContext<CTDbContext>(options => options.UseLazyLoadingProxies().UseNpgsql(CTDbContext.ConnectionStringBuilder()))
                .AddSingleton<RabbitMQConnection>()
                .AddSingleton<CaptionQueries>()
                .AddSingleton<DownloadPlaylistInfoTask>()
                .AddSingleton<DownloadMediaTask>()
                .AddSingleton<ConvertVideoToWavTask>()
                .AddSingleton<LocalTranscriptionTask>()
                .AddSingleton<AzureTranscriptionTask>()
                .AddSingleton<QueueAwakerTask>()
                // .AddSingleton<GenerateVTTFileTask>()
                .AddSingleton<RpcClient>()
                .AddSingleton<ProcessVideoTask>()
                .AddSingleton<MSTranscriptionService>()
                .AddSingleton<SceneDetectionTask>()
                .AddSingleton<PythonCrawlerTask>()
                .AddSingleton<DescribeVideoTask>()
                .AddSingleton<DescribeImageTask>()
               // .AddSingleton<UpdateBoxTokenTask>()
                .AddSingleton<CreateBoxTokenTask>()
                .AddSingleton<BuildElasticIndexTask>()
                .AddSingleton<ExampleTask>()
                .AddSingleton<CleanUpElasticIndexTask>()
                .AddSingleton<BoxAPI>()
                .AddScoped<Seeder>()
                .AddScoped<SlackLogger>()
                .AddSingleton<TempCode>()
                .BuildServiceProvider();

            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            Globals.appSettings = _serviceProvider.GetService<IOptions<AppSettings>>().Value;
            TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

            _logger.LogInformation("Seeding database");

            // Seed the database, with some initial data.
            Seeder seeder = _serviceProvider.GetService<Seeder>();
            seeder.Seed();
        }

        
        static void runQueueAwakerForever() {
             _logger.LogInformation("runQueueAwakerForever - start");
             QueueAwakerTask queueAwakerTask = _serviceProvider.GetService<QueueAwakerTask>();

            int periodicCheck = Math.Max(1,Convert.ToInt32(Globals.appSettings.PERIODIC_CHECK_EVERY_MINUTES));
            int initialPauseMinutes = Math.Max(1, Convert.ToInt32(Globals.appSettings.INITIAL_TASKENGINE_PAUSE_MINUTES));

            _logger.LogInformation("Periodic Check Every {0} minutes", periodicCheck);
            var timeInterval = new TimeSpan(0, periodicCheck, 0);
            
            var initialPauseInterval = new TimeSpan(0, initialPauseMinutes, 0);
            _logger.LogInformation("Pausing {0} minutes before first periodicCheck", initialPauseInterval);

            // Thread.Sleep(initialPauseInterval);
            Task.Delay(initialPauseInterval).Wait();
            // Check for new tasks every "timeInterval".
            // The periodic check will discover all undone tasks
            // TODO/REVIEW: However some tasks also publish the next items
            while (true)
            {
                try {
                    _logger.LogInformation("Periodic Check");
                    queueAwakerTask.Publish(new JObject
                    {
                        { "Type", TaskType.PeriodicCheck.ToString() }
                    });
                } catch (Exception e) {
                    _logger.LogError(e, "Error in Periodic Check");
                }
                // Thread.Sleep(timeInterval);
                Task.Delay(timeInterval).Wait();
                 _logger.LogInformation("Pausing {0} minutes before next periodicCheck", periodicCheck);
            };
        }
        static void createTaskQueues() {
            _logger.LogInformation("createTaskQueues() -starting");
            // Delete any pre-existing queues on rabbitMQ.
            RabbitMQConnection rabbitMQ = _serviceProvider.GetService<RabbitMQConnection>();

            // Active queues managed by C# (concurrency > 0) are now purged after the queue is created and before messages are processed

            ushort concurrent_videotasks = ToUInt16(Globals.appSettings.MAX_CONCURRENT_VIDEO_TASKS, NO_CONCURRENCY);
            ushort concurrent_synctasks = ToUInt16(Globals.appSettings.MAX_CONCURRENT_SYNC_TASKS, MIN_CONCURRENCY);
            ushort concurrent_transcriptions = ToUInt16(Globals.appSettings.MAX_CONCURRENT_TRANSCRIPTIONS, MIN_CONCURRENCY);
            ushort concurrent_describe_images = NO_CONCURRENCY;
            ushort concurrent_describe_videos = NO_CONCURRENCY;

            // Create and start consuming from all queues. If concurrency >=1 the queues are purged

            // Upstream Sync related
            _logger.LogInformation($"Creating DownloadPlaylistInfoTask & DownloadMediaTask consumers. Concurrency={concurrent_synctasks} ");
            _serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume(concurrent_synctasks);
            _serviceProvider.GetService<DownloadMediaTask>().Consume(concurrent_synctasks);

            // Transcription Related
            _logger.LogInformation($"Creating TranscriptionTask consumers. Concurrency={concurrent_transcriptions} ");

            _serviceProvider.GetService<LocalTranscriptionTask>().Consume(concurrent_transcriptions);

            // no more! - _serviceProvider.GetService<GenerateVTTFileTask>().Consume(concurrent_transcriptions);

            // Video Processing Related
            _logger.LogInformation($"Creating ProcessVideoTask consumer. Concurrency={concurrent_videotasks} ");
            _serviceProvider.GetService<ProcessVideoTask>().Consume(concurrent_videotasks);
            // Descriptions
            _serviceProvider.GetService<DescribeVideoTask>().Consume(concurrent_describe_videos);
            _serviceProvider.GetService<DescribeImageTask>().Consume(concurrent_describe_images);

            // SceneDetection now handled by native Python
            //    See https://github.com/classtranscribe/pyapi
            _serviceProvider.GetService<SceneDetectionTask>().Consume(DISABLED_TASK);

            // We dont want concurrency for these tasks
            _logger.LogInformation("Creating QueueAwakerTask and Box token tasks consumers.");
            _serviceProvider.GetService<QueueAwakerTask>().Consume(NO_CONCURRENCY); //TODO TOREVIEW: NO_CONCURRENCY?
            // does nothing at the moment _serviceProvider.GetService<UpdateBoxTokenTask>().Consume(NO_CONCURRENCY);
            _serviceProvider.GetService<CreateBoxTokenTask>().Consume(NO_CONCURRENCY); // calls _box.CreateAccessTokenAsync(authCode);

            // Elastic Search index should be built after TranscriptionTask
            _serviceProvider.GetService<BuildElasticIndexTask>().Consume(NO_CONCURRENCY);

            // Outdated Elastic Search index would be removed
            _serviceProvider.GetService<CleanUpElasticIndexTask>().Consume(NO_CONCURRENCY);

            _serviceProvider.GetService<ExampleTask>().Consume(NO_CONCURRENCY);

            _serviceProvider.GetService<PythonCrawlerTask>().Consume(DISABLED_TASK); 
            _logger.LogInformation("createTaskQueues() - Done creating task consumers");
        }
        // Catch all unhandled exceptions.
        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine($"Unhandled Exception Caught {e.Message}\n{e}\nSender:{sender ?? "null"}");
            if(_logger !=null){
                 _logger.LogError(e, "Unhandled Exception Caught");
            }
        }

        private static ushort ToUInt16(String val, ushort defaultVal)
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
