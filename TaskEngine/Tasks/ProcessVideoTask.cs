using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread


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
            RegisterTask(cleanup,videoId); // may throw AlreadyInProgress exception
            Video video;
            bool videoUpdated = false;
            string subdir;
            FileRecord video1Record = null;
            FileRecord video2Record = null;
            using (var _context = CTDbContext.CreateDbContext())
            {
                video = await _context.Videos.Where(v => v.Id == videoId).FirstAsync();
                subdir = ToCourseOfferingSubDirectory(_context, video); // needs to traverse from Video to CO

                GetLogger().LogInformation("ProcessVideo Task; Consuming video.Id=" + video.Id);

                if (video.Video1Id != null)
                {
                    video1Record = await _context.FileRecords.FindAsync(video.Video1Id);
                }
                if (video.Video2Id != null)
                {
                    video2Record = await _context.FileRecords.FindAsync(video.Video2Id);
                }
            }
            if(video.Duration == null &&  video1Record != null ) {
                var mediaInfoResult = await _rpcClient.PythonServerClient.GetMediaInfoRPCAsync(new CTGrpc.File
                {
                    FilePath = video1Record.VMPath
                }); ; 

                var mediaJson = JObject.Parse(mediaInfoResult.Json);
                video.FileMediaInfo = mediaJson;
                video.UpdateMediaProperties();
                videoUpdated = true;
            }
            bool runbrokencode = false;
            if (runbrokencode)
            {
                if (video.Video1Id != null)
                {
                    if (video.ProcessedVideo1 == null || taskParameters.Force)
                    {
                        var file = await _rpcClient.PythonServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                        {
                            FilePath = video1Record.VMPath
                        });

                        // TODO: Does this work now?
                        video.ProcessedVideo1 = await FileRecord.GetNewFileRecordAsync(file.FilePath, file.Ext, subdir);
                        videoUpdated = true;
                    }
                }
                if (video.Video2Id != null)
                {
                    if (video.ProcessedVideo2 == null || taskParameters.Force)
                    {
                        var file = await _rpcClient.PythonServerClient.ProcessVideoRPCAsync(new CTGrpc.File
                        {
                            FilePath = video2Record.VMPath
                        });

                        // TODO: Does this work now?
                        video.ProcessedVideo2 = await FileRecord.GetNewFileRecordAsync(file.FilePath, file.Ext, subdir);
                        videoUpdated = true;
                    }
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
