﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using System;
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
    /// This task converts the video file to an audio file. It is not currently used.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class ConvertVideoToWavTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly LocalTranscriptionTask _localTranscriptionTask;

        public ConvertVideoToWavTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, LocalTranscriptionTask localTranscriptionTask, ILogger<ConvertVideoToWavTask> logger)
            : base(rabbitMQ, TaskType.ConvertMedia, logger)
        {
            _rpcClient = rpcClient;
            _localTranscriptionTask = localTranscriptionTask;
        }

        protected override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId);
            
            throw new Exception("ConvertVideoToWavTask No longer used. Videoid= " + videoId);
        }

        /// <summary>
        /// Original implementation of OnConsume. This code may be deleted if it is no longer useful. It is left as available for now as a template
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="taskParameters"></param>
        /// <returns></returns>
        private async Task OldOnConsumeNotUsed(string videoId) 
        { 
            using (var _context = CTDbContext.CreateDbContext())
            {
                // Get the video object
                var video = await _context.Videos.FindAsync(videoId);
                string subdir = ToCourseOfferingSubDirectory(_context, video);
                GetLogger().LogInformation("Consuming" + video);
                // Make RPC call to produce audio file.
                var file = await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(new CTGrpc.FileForConversion
                {
                    File = new CTGrpc.File{ FilePath = video.Video1.VMPath }
                });


                // Check if a valid file was returned.
                if (FileRecord.IsValidFile(file.FilePath))
                {
                    var fileRecord = await FileRecord.GetNewFileRecordAsync(file.FilePath, file.Ext, subdir);
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
                            _localTranscriptionTask.Publish(videoLatest.Id);
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
