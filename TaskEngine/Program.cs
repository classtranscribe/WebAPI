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
                .AddDbContext<CTDbContext>()
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

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();
            logger.LogDebug("Starting application");

            int choice = Convert.ToInt32(Console.ReadLine());
            switch (choice)
            {
                case 0:
                    RabbitMQ rabbitMQ = serviceProvider.GetService<RabbitMQ>();
                    CTDbContext context = serviceProvider.GetService<CTDbContext>();
                    RunProgramRunExample(rabbitMQ, context).GetAwaiter().GetResult();
                    break;
                case 1:
                    serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume();
                    break;
                case 2:
                    //DownloadMediaTask _downloadMediaTask = serviceProvider.GetService<DownloadMediaTask>();
                    //CTDbContext _context = serviceProvider.GetService<CTDbContext>();
                    //(_context.Medias.Where(m => m.Videos.Count() == 0 && m.SourceType == SourceType.Echo360).Take(2).ToList()).ForEach(m => _downloadMediaTask.Publish(m));
                    serviceProvider.GetService<DownloadMediaTask>().Consume();
                    break;
                case 3:
                    serviceProvider.GetService<ConvertVideoToWavTask>().Consume();
                    break;
                case 4:
                    //TranscriptionTask t = serviceProvider.GetService<TranscriptionTask>();
                    //CTDbContext _context = serviceProvider.GetService<CTDbContext>();
                    //_context.Videos.Where(v => v.AudioPath != null).Take(2).ToList().ForEach(v => t.Publish(v));
                    serviceProvider.GetService<TranscriptionTask>().Consume();
                    break;
            }



            logger.LogDebug("All done!");

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();
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
                        .WithIntervalInMinutes(1)
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
