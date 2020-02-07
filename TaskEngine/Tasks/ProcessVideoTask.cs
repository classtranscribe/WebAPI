using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class ProcessVideoTask : RabbitMQTask<Video>
    {
        private RpcClient _rpcClient;

        public ProcessVideoTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<ProcessVideoTask> logger)
            : base(rabbitMQ, TaskType.ProcessVideo, logger)
        {
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(Video video)
        {
            _logger.LogInformation("Consuming" + video);
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
