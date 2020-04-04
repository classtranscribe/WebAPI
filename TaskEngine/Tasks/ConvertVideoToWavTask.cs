using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task converts the video file to an audio file using ffmpeg via the NodeRpcServer.
    /// </summary>
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
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                // Get the video object
                var video = await _context.Videos.FindAsync(videoId);
                _logger.LogInformation("Consuming" + video);
                // Make RPC call to produce audio file.
                var file = await _rpcClient.NodeServerClient.ConvertVideoToWavRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video1.VMPath
                });
                var fileRecord = new FileRecord(file.FilePath);

                // Check if a valid file was returned and the file has a length greater than 1000 bytes.
                if (fileRecord.Path.Length > 0 && File.Exists(fileRecord.Path) && new FileInfo(fileRecord.Path).Length > 1000)
                {
                    // Get the latest video object, in case it has changed
                    var videoLatest = await _context.Videos.FindAsync(video.Id);

                    // If there is no Audio file present, then update.
                    if (videoLatest.Audio == null)
                    {
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
