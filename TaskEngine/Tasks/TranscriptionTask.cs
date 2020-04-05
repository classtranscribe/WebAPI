using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.MSTranscription;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task produces the transcriptions for a Video item.
    /// </summary>
    class TranscriptionTask : RabbitMQTask<string>
    {
        private readonly MSTranscriptionService _msTranscriptionService;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly SceneDetectionTask _sceneDetectionTask;

        public TranscriptionTask(RabbitMQConnection rabbitMQ, MSTranscriptionService msTranscriptionService,
            GenerateVTTFileTask generateVTTFileTask, SceneDetectionTask sceneDetectionTask, ILogger<TranscriptionTask> logger)
            : base(rabbitMQ, TaskType.Transcribe, logger)
        {
            _msTranscriptionService = msTranscriptionService;
            _generateVTTFileTask = generateVTTFileTask;
            _sceneDetectionTask = sceneDetectionTask;
        }
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters)
        {
            Video video;
            using (var _context = CTDbContext.CreateDbContext())
            {
                video = await _context.Videos.FindAsync(videoId);
            }

            if (!File.Exists(video.Audio.Path) || new FileInfo(video.Audio.Path).Length < 1000)
            {
                // As file does not exist remove record of it.
                using (var context = CTDbContext.CreateDbContext())
                {
                    var tempAudio = await context.FileRecords.FindAsync(video.AudioId);
                    context.FileRecords.Remove(tempAudio);
                    await context.SaveChangesAsync();
                }
                throw new FileNotFoundException("Wav file not found.", video.Audio.Path);
            }
            var result = await _msTranscriptionService.RecognitionWithAudioStreamAsync(video);
            List<Transcription> transcriptions = new List<Transcription>();
            foreach (var language in result.Item1)
            {
                if (language.Value.Count > 0)
                {
                    transcriptions.Add(new Transcription
                    {
                        Language = language.Key,
                        VideoId = video.Id,
                        Captions = language.Value
                    });
                }
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                var latestVideo = await _context.Videos.FindAsync(video.Id);
                if (result.Item2 == "NoError")
                {
                    if (latestVideo.TranscriptionStatus != "NoError")
                    {
                        // If any present, remove them.
                        if (latestVideo.Transcriptions.Any())
                        {
                            var oldTranscriptions = latestVideo.Transcriptions;
                            var oldCaptions = latestVideo.Transcriptions.SelectMany(t => t.Captions);
                            _context.Captions.RemoveRange(oldCaptions);
                            await _context.SaveChangesAsync();
                            _context.Transcriptions.RemoveRange(oldTranscriptions);
                            await _context.SaveChangesAsync();
                        }

                        // Add the latest transcriptions.
                        await _context.Transcriptions.AddRangeAsync(transcriptions);
                        transcriptions.ForEach(t => _generateVTTFileTask.Publish(t.Id));
                        _sceneDetectionTask.Publish(video.Id);
                    }
                    latestVideo.TranscriptionStatus = result.Item2;
                    latestVideo.TranscribingAttempts += 1;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Transcription failed" + result.Item2);
                }
            }
        }
    }
}
