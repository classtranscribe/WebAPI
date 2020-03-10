using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class ConvertVideoToWavTask : RabbitMQTask<Video>
    {
        private readonly RpcClient _rpcClient;
        private readonly TranscriptionTask _transcriptionTask;

        public ConvertVideoToWavTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, TranscriptionTask transcriptionTask, ILogger<ConvertVideoToWavTask> logger)
            : base(rabbitMQ, TaskType.ConvertMedia, logger)
        {
            _rpcClient = rpcClient;
            _transcriptionTask = transcriptionTask;
        }
        protected async override Task OnConsume(Video v)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var video = await _context.Videos.FindAsync(v.Id);
                _logger.LogInformation("Consuming" + video);
                var file = await _rpcClient.NodeServerClient.ConvertVideoToWavRPCAsync(new CTGrpc.File
                {
                    FilePath = video.Video1.VMPath
                });
                var fileRecord = new FileRecord(file.FilePath);
                if (fileRecord.Path.Length > 0 && new FileInfo(fileRecord.Path).Length > 1000)
                {
                    var videoLatest = await _context.Videos.FindAsync(video.Id);
                    if (videoLatest.Audio == null)
                    {
                        videoLatest.Audio = fileRecord;
                        await _context.SaveChangesAsync();
                        _transcriptionTask.Publish(videoLatest);
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
