// using Newtonsoft.Json.Linq;
// using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
// using System;
// using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using static ClassTranscribeDatabase.CommonUtils;



#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DescribeImageTask : RabbitMQTask<string>
    {
        private readonly DescribeImageTask _describeImageTask;
 
        public DescribeImageTask(RabbitMQConnection rabbitMQ, ILogger<DescribeImageTask> logger)
            : base(rabbitMQ, TaskType.DescribeImage, logger)
        {
           
        }
        /// <summary>Extracts scene descriptions for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string imagePath, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, imagePath); // may throw AlreadyInProgress exception
            GetLogger().LogInformation($"DescribeImageTask({imagePath}): Consuming Task");
            string captionId = taskParameters.Metadata["captionId"].ToString();

            using (var _context = CTDbContext.CreateDbContext())
            {
                Caption c= await _context.Captions.FindAsync(captionId);
                if (c == null || c.HasPlaceHolderText()) {
                    GetLogger().LogInformation($"Describe Image {imagePath}: Caption Text changed or caption missing");
                    return;
                }
                string result = $"A very interesting lecture slide ({captionId})";
                c.Text = result;
                _context.Update(c);
                await _context.SaveChangesAsync();
            }
            GetLogger().LogInformation($"DescribeImageTask({imagePath}): Complete- end of task");
        }
    }
}
