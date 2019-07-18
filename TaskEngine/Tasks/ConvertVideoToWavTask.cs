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
        private TranscriptionTask _transcriptionTask;
        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.ConvertMedia, "_1");
        }
        public ConvertVideoToWavTask(RabbitMQ rabbitMQ, RpcClient rpcClient, TranscriptionTask transcriptionTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _transcriptionTask = transcriptionTask;
        }
        protected async override Task OnConsume(Video video)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                Console.WriteLine("Consuming" + video);
                var file = await _rpcClient.NodeServerClient.ConvertVideoToWavRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video1.VMPath
                });
                video.Audio = new FileRecord(file.FilePath);
                await _context.SaveChangesAsync();
                _transcriptionTask.Publish(video);
            }
        }
    }
}
