using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class ConvertVideoToWavTask : RabbitMQTask<Video>
    {
        private RpcClient _rpcClient;
        private void Init(RabbitMQ rabbitMQ, CTDbContext context)
        {
            _rabbitMQ = rabbitMQ;
            _context = context;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.ConvertMedia, "_1");
        }
        public ConvertVideoToWavTask(RabbitMQ rabbitMQ, CTDbContext context, RpcClient rpcClient)
        {
            Init(rabbitMQ, context);
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(Video video)
        {
            Console.WriteLine("Consuming" + video);
            var file = await _rpcClient.NodeServerClient.ConvertVideoToWavRPCAsync(new CTGrpc.File
            {
                FilePath = video.Video1Path
            });
            video.AudioPath = file.FilePath;
            _context.Videos.Update(video);
            await _context.SaveChangesAsync();
        }
    }
}
