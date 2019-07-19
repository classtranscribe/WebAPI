using ClassTranscribeDatabase;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TaskEngine.Tasks;
using System.Collections.Specialized;
using Quartz.Impl;
using Quartz;
using TaskEngine.Grpc;
using TaskEngine.MSTranscription;
using Microsoft.Extensions.Options;

namespace TaskEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .Configure<AppSettings>(CTDbContext.GetConfigurations())
                .AddSingleton<RabbitMQ>()
                .AddSingleton<DownloadPlaylistInfoTask>()
                .AddSingleton<DownloadMediaTask>()
                .AddSingleton<ConvertVideoToWavTask>()
                .AddSingleton<TranscriptionTask>()
                .AddSingleton<RpcClient>()
                .AddSingleton<MSTranscriptionService>()
                .BuildServiceProvider();

            //configure console logging

            serviceProvider
                .GetService<ILoggerFactory>()
                .AddConsole(LogLevel.Debug);

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();
            logger.LogDebug("Starting application");
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume();
            serviceProvider.GetService<DownloadMediaTask>().Consume();
            serviceProvider.GetService<ConvertVideoToWavTask>().Consume();
            serviceProvider.GetService<TranscriptionTask>().Consume();
            RabbitMQ rabbitMQ = serviceProvider.GetService<RabbitMQ>();
            CTDbContext context = serviceProvider.GetService<CTDbContext>();
            RunProgramRunExample(rabbitMQ, context).GetAwaiter().GetResult();

            // MSTranscriptionService mSTranscriptionService = serviceProvider.GetService<MSTranscriptionService>();
            // mSTranscriptionService.RecognitionWithAudioStreamAsync("D:\\CT\\data\\256.wav").GetAwaiter().GetResult();

            // FileHasher.ComputeSha256HashForDirectory("C:\\Users\\chira\\Source\\Repos\\ClassTranscribeServer\\Data");

            logger.LogDebug("All done!");

            Console.WriteLine("Press any key to close the application");
            
             while (true) {
                Console.Read();
            };
        }

        private static async Task RunProgramRunExample(RabbitMQ rabbitMQ, CTDbContext context)
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
                IJobDetail job = JobBuilder.Create<DownloadPlaylistInfoTask>()
                    .WithIdentity("job1", "group1")
                    .Build();

                job.JobDataMap.Put("CTDbContext", context);
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
                Console.WriteLine(se);
            }
        }
    }
}
