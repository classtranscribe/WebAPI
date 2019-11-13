using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class EPubGeneratorTask : RabbitMQTask<EPub>
    {
        private RpcClient _rpcClient;
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.GenerateEPubFile, "_1");
        }

        public EPubGeneratorTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
        }

        protected async override Task OnConsume(EPub epub)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var query = new CaptionQueries(_context);
                var captions = await query.GetCaptionsAsync(epub.VideoId, epub.Language);
                StringBuilder sb = new StringBuilder();
                captions.ForEach(c => sb.Append(c.Text + " "));
                string allText = sb.ToString();
                var filePath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, epub.VideoId + "_" + epub.Language + ".epub");
                var result = await _rpcClient.NodeServerClient.CreateEPubRPCAsync(new CTGrpc.EPubData
                {
                    File = filePath,
                    Text = allText,
                    Author = "ClassTranscribe",
                    Title = "Demo Title",
                    Publisher = "ClassTranscribe"
                });
                epub.File = new FileRecord(filePath);
                await _context.SaveChangesAsync();
            }
        }
    }
}
