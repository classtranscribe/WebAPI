using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class ProcessVideoTask : RabbitMQTask<Video>
    {
        private RpcClient _rpcClient;
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.ProcessVideo, "_1");
        }
        public ProcessVideoTask(RabbitMQ rabbitMQ, RpcClient rpcClient)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(Video video)
        {
            Console.WriteLine("Consuming" + video);
            if (video.Video1 != null)
            {
                var file = await _rpcClient.NodeServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video1.VMPath
                });
                video.ProcessedVideo1 = new FileRecord(file.FilePath);
            }
            if (video.Video2 != null)
            {
                var file = await _rpcClient.NodeServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video2.VMPath
                });
                video.ProcessedVideo2 = new FileRecord(file.FilePath);
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
