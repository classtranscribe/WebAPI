using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class ExampleTask : RabbitMQTask<string>
    {
        public ExampleTask(RabbitMQConnection rabbitMQ,
            ILogger<ExampleTask> logger)
            : base(rabbitMQ, TaskType.ExampleTask, logger)
        {
        }
        protected async override Task OnConsume(string example, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, "ExampleTask"); // may throw AlreadyInProgress exception
            GetLogger().LogInformation("Example Task Starting");
            int captionCount = 0;
            int transcriptionCount = 0;
           
            using (var _context = CTDbContext.CreateDbContext())
            {
                CaptionQueries captionQueries = new CaptionQueries(_context);

                var transcriptions = await _context.Transcriptions.Take(30).ToListAsync();

                foreach (var transcription in transcriptions)
                {

                    var transcriptionId = transcription.Id;
                    var videoID = transcription.VideoId;
                    var captions = await captionQueries.GetCaptionsAsync(transcriptionId);

                    GetLogger().LogInformation($"{transcription.Id}: Caption count= {captions.Count}");
                    transcriptionCount++;
                }
            }

            GetLogger().LogInformation($"Example Task Done.  transcriptionCount={transcriptionCount} captionCount={captionCount}");
        }
    }
}