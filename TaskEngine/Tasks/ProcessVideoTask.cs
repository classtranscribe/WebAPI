using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class ProcessVideoTask : RabbitMQTask<JobObject<Video>>
    {
        private readonly RpcClient _rpcClient;

        public ProcessVideoTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<ProcessVideoTask> logger)
            : base(rabbitMQ, TaskType.ProcessVideo, logger)
        {
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(JobObject<Video> j)
        {
            var video = j.Data;
            _logger.LogInformation("Consuming" + video);
            if (video.Video1 != null)
            {
                if (video.ProcessedVideo1 != null || j.Force)
                {
                    var file = await _rpcClient.NodeServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                    {
                        FilePath = video.Video1.VMPath
                    });
                    video.ProcessedVideo1 = new FileRecord(file.FilePath);
                }
            }
            if (video.Video2 != null)
            {
                if (video.ProcessedVideo2 != null || j.Force)
                {
                    var file = await _rpcClient.NodeServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                    {
                        FilePath = video.Video2.VMPath
                    });
                    video.ProcessedVideo2 = new FileRecord(file.FilePath);
                }
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
