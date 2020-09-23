using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using System.Diagnostics.CodeAnalysis;



namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task converts the video file to an audio file. It is not currently used.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class ConvertVideoToWavTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly TranscriptionTask _transcriptionTask;

        public ConvertVideoToWavTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, TranscriptionTask transcriptionTask, ILogger<ConvertVideoToWavTask> logger)
            : base(rabbitMQ, TaskType.ConvertMedia, logger)
        {
            _rpcClient = rpcClient;
            _transcriptionTask = transcriptionTask;
        }
        
        #pragma warning disable 1998
        //Tasks/ConvertVideoToWavTask.cs(32,39): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread. [/src/TaskEngine/TaskEngine.csproj]

        protected override Task OnConsume(string videoId, TaskParameters taskParameters)
        {
            throw new Exception("ConvertVideoToWavTask No longer used. Videoid= " + videoId);
        }

        /// <summary>
        /// Original implementation of OnConsume. This code may be deleted if it is no longer useful. It is left as available for now as a template
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="taskParameters"></param>
        /// <returns></returns>
        private async Task OldOnConsumeNotUsed(string videoId, TaskParameters taskParameters) 
        { 
            using (var _context = CTDbContext.CreateDbContext())
            {
                // Get the video object
                var video = await _context.Videos.FindAsync(videoId);
                _logger.LogInformation("Consuming" + video);
                // Make RPC call to produce audio file.
                var file = await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(new CTGrpc.FileForConversion
                {
                    File = new CTGrpc.File{ FilePath = video.Video1.VMPath }
                });


                // Check if a valid file was returned.
                if (FileRecord.IsValidFile(file.FilePath))
                {
                    var fileRecord = FileRecord.GetNewFileRecord(file.FilePath, file.Ext);
                    // Get the latest video object, in case it has changed
                    var videoLatest = await _context.Videos.FindAsync(video.Id);

                    // If there is no Audio file present, then update.
                    if (videoLatest.Audio == null)
                    {
                        await _context.FileRecords.AddAsync(fileRecord);                        
                        videoLatest.Audio = fileRecord;
                        await _context.SaveChangesAsync();


                        // If no transcriptions present, produce transcriptions.
                        if (!videoLatest.Transcriptions.Any())
                        {
                            _transcriptionTask.Publish(videoLatest.Id);
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
}
