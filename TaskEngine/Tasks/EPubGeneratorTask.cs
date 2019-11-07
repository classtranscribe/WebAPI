using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Collections.Generic;
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
                var captions = await query.GetCaptionsAsync(epub.VideoId);
                StringBuilder sb = new StringBuilder();
                captions.ForEach(c => sb.Append(c.Text + " "));
                string allText = sb.ToString();
                var result = await _rpcClient.NodeServerClient.CreateEPubRPCAsync(new CTGrpc.EPubData
                {
                    OutputPath = epub.File.VMPath,
                    Text = allText
                });
            }
        }
    }
}
