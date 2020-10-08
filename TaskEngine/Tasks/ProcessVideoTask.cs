using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task converts a video to a common format using ffmpeg.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class ProcessVideoTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;

        public ProcessVideoTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ILogger<ProcessVideoTask> logger)
            : base(rabbitMQ, TaskType.ProcessVideo, logger)
        {
            _rpcClient = rpcClient;
        }
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {   
            registerTask(cleanup,videoId); // may throw AlreadyInProgress exception
            Video video;
            bool videoUpdated = false;
            using (var _context = CTDbContext.CreateDbContext())
            {
                video = await _context.Videos.Include(v => v.Video1)
                    .Include(v => v.Video2)
                    .Include(v => v.ProcessedVideo1)
                    .Include(v => v.ProcessedVideo2)
                    .Where(v => v.Id == videoId).FirstAsync();
            }
            _logger.LogInformation("Consuming" + video);
            if(video.Duration == null && video.Video1 != null)
            {
                var mediaInfoResult = await _rpcClient.PythonServerClient.GetMediaInfoRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video1.VMPath
                });

                var mediaJson = JObject.Parse(mediaInfoResult.Json);
                video.FileMediaInfo = mediaJson;
                video.UpdateMediaProperties();
                videoUpdated = true;
            }

            if (video.Video1 != null)
            {
                if (video.ProcessedVideo1 == null || taskParameters.Force)
                {
                    var file = await _rpcClient.PythonServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                    {
                        FilePath = video.Video1.VMPath
                    });
                    video.ProcessedVideo1 = await FileRecord.GetNewFileRecordAsync(file.FilePath, file.Ext);
                    videoUpdated = true;
                }
            }
            if (video.Video2 != null)
            {
                if (video.ProcessedVideo2 == null || taskParameters.Force)
                {
                    var file = await _rpcClient.PythonServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                    {
                        FilePath = video.Video2.VMPath
                    });
                    video.ProcessedVideo2 = await FileRecord.GetNewFileRecordAsync(file.FilePath, file.Ext);
                    videoUpdated = true;
                }
            }
            if (videoUpdated)
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    _context.Entry(video).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
