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
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.ConvertMedia, "_1");
        }
        public ConvertVideoToWavTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, TranscriptionTask transcriptionTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _transcriptionTask = transcriptionTask;
        }
        protected async override Task OnConsume(Video video)
        {
            Console.WriteLine("Consuming" + video);
            var file = await _rpcClient.NodeServerClient.ConvertVideoToWavRPCAsync(new CTGrpc.File
            {
                FilePath = video.Video1.VMPath
            });
            if (file.FilePath.Length > 0)
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    var videoLatest = await _context.Videos.FindAsync(video.Id);
                    if (videoLatest.Audio == null)
                    {
                        videoLatest.Audio = new FileRecord(file.FilePath);
                        await _context.SaveChangesAsync();
                        _transcriptionTask.Publish(videoLatest);
                    }
                }
            }
            else
            {
                throw new Exception("ConvertVideoToWavTask Failed + " + video.Id);
            }
        }
    }
}
