using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task converts a video to a common format using ffmpeg.
    /// </summary>
    class ProcessVideoTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;

        public ProcessVideoTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<ProcessVideoTask> logger)
            : base(rabbitMQ, TaskType.ProcessVideo, logger)
        {
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters)
        {
            Video video;
            using (var _context = CTDbContext.CreateDbContext())
            {
                video = await _context.Videos.FindAsync(videoId);
            }
            _logger.LogInformation("Consuming" + video);
            if (video.Video1 != null)
            {
                if (video.ProcessedVideo1 != null || taskParameters.Force)
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
                if (video.ProcessedVideo2 != null || taskParameters.Force)
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
