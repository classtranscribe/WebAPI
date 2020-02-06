using ClassTranscribeDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
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
                             ("", LogLevel.Trace);
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
                .AddSingleton<Box>()
                .AddScoped<Seeder>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            Globals.logger = logger;

            //configure console logging
            if (configuration.GetValue<string>("DEV_ENV", "NULL") == "DOCKER")
            {
                logger.LogInformation("Sleeping");
                Thread.Sleep(15000);
                logger.LogInformation("Waking up");
            }

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);


            RabbitMQConnection rabbitMQ = serviceProvider.GetService<RabbitMQConnection>();

            Seeder seeder = serviceProvider.GetService<Seeder>();
            seeder.Seed();

            CTDbContext context = CTDbContext.CreateDbContext();

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
            RunProgramRunExample(rabbitMQ).GetAwaiter().GetResult();


            DownloadPlaylistInfoTask downloadPlaylistInfoTask = serviceProvider.GetService<DownloadPlaylistInfoTask>();
            DownloadMediaTask downloadMediaTask = serviceProvider.GetService<DownloadMediaTask>();
            ConvertVideoToWavTask convertVideoToWavTask = serviceProvider.GetService<ConvertVideoToWavTask>();
            TranscriptionTask transcriptionTask = serviceProvider.GetService<TranscriptionTask>();
            GenerateVTTFileTask generateVTTFileTask = serviceProvider.GetService<GenerateVTTFileTask>();
            ProcessVideoTask processVideoTask = serviceProvider.GetService<ProcessVideoTask>();
            EPubGeneratorTask ePubGeneratorTask = serviceProvider.GetService<EPubGeneratorTask>();
            UpdateBoxTokenTask updateBoxTokenTask = serviceProvider.GetService<UpdateBoxTokenTask>();
            CreateBoxTokenTask createBoxTokenTask = serviceProvider.GetService<CreateBoxTokenTask>();

            RpcClient rpcClient = serviceProvider.GetService<RpcClient>();
            logger.LogInformation("All done!");

            Console.WriteLine("Press any key to close the application");

            while (true)
            {
                Console.Read();
            };
        }

        private static async Task RunProgramRunExample(RabbitMQConnection rabbitMQ)
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                // and start it off
                await scheduler.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<QueueAwakerTask>()
                    .WithIdentity("job1", "group1")
                    .Build();

                job.JobDataMap.Put("rabbitMQ", rabbitMQ);

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInHours(6)
                        .RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                // some sleep to show what's happening
                await Task.Delay(TimeSpan.FromSeconds(60));

                // and last shut down the scheduler when you are ready to close your program
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Globals.logger.LogInformation(se, "Exception in quartz");
            }
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Globals.logger.LogError(e, "Unhandled Exception Caught");
        }
    }
}
