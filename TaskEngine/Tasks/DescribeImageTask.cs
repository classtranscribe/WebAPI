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
using Newtonsoft.Json.Linq;
using System;
using System.Text;



// #pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DescribeImageTask : RabbitMQTask<string>
    {
     
 
        public DescribeImageTask(RabbitMQConnection rabbitMQ, ILogger<DescribeImageTask> logger)
            : base(rabbitMQ, TaskType.DescribeImage, logger)
        {
           
        }
        /// <summary>Extracts scene descriptions for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string id, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, id); // may throw AlreadyInProgress exception
            GetLogger().LogInformation($"DescribeImageTask({id}): Consuming Task");
            JObject meta = taskParameters.Metadata;
            string captionId = meta["CaptionId"].ToString();
            string imageFile = meta["ImageFile"].ToString();
            string ocrdata = meta["OCRText"].ToString();
            string ocrtext = "";
            try
            {
                JObject ocr = JObject.Parse(ocrdata);
                JArray texts = ocr["text"] as JArray;
                StringBuilder sb = new StringBuilder();
                foreach (var te in texts) {
                    string t = te.ToString();
                    if (string.IsNullOrWhiteSpace(t)) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(t);
                }
                ocrtext = sb.ToString();
            } catch(Exception ex)
            {
                GetLogger().LogError(ex, ex.Message);
            }
            GetLogger().LogInformation($"{captionId}: <{imageFile}> <{ocrtext}>");
            try
            {
                using var _context = CTDbContext.CreateDbContext();
                Caption c = await _context.Captions.FindAsync(captionId);

                if (c == null || !c.HasPlaceHolderText())
                {
                    GetLogger().LogInformation($"Describe Image {id}: Caption Text changed or caption missing");
                    return;
                }
                string result = $"MOCK AI output: An interesting lecture slide ({captionId}) for image {imageFile} and ocr (\"{ocrtext}\")";
                c.Text = result;
                _context.Update(c);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, ex.Message);
                throw;
            }
            GetLogger().LogInformation($"DescribeImageTask({id}): Complete - end of task");
        }
    }
}
