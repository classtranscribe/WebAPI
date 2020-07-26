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

            // Seed the database, with some initial data.
            Seeder seeder = serviceProvider.GetService<Seeder>();
            seeder.Seed();

            _logger.LogInformation("Starting application");


            // Delete any pre-existing queues on rabbitMQ.
            RabbitMQConnection rabbitMQ = serviceProvider.GetService<RabbitMQConnection>();
            rabbitMQ.DeleteAllQueues();

            // Start consuming from all queues.
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume();
            serviceProvider.GetService<DownloadMediaTask>().Consume();
            serviceProvider.GetService<ConvertVideoToWavTask>().Consume();
            serviceProvider.GetService<TranscriptionTask>().Consume();
            serviceProvider.GetService<QueueAwakerTask>().Consume();
            serviceProvider.GetService<GenerateVTTFileTask>().Consume();
            serviceProvider.GetService<ProcessVideoTask>().Consume();
            serviceProvider.GetService<SceneDetectionTask>().Consume();
            serviceProvider.GetService<UpdateBoxTokenTask>().Consume();
            serviceProvider.GetService<CreateBoxTokenTask>().Consume();

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
