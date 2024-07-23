using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class FlashDetectionTask : RabbitMQTask<string>
    {
        public FlashDetectionTask(RabbitMQConnection rabbitMQ, ILogger<FlashDetectionTask> logger)
            : base(rabbitMQ, TaskType.FlashDetection, logger)
        {

        }
        /// <summary>Extracts ASL videos from online sources

        protected async override Task OnConsume(string sourceId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            // To suppress CS1998 warning
            await Task.CompletedTask;
        }
    }
}
