using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaskEngine.MSTranscription;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class TranscriptionTask : RabbitMQTask<JobObject<Video>>
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
        protected async override Task OnConsume(JobObject<Video> j)
        {
            var video = j.Data;
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
                if (latestVideo.TranscriptionStatus != "NoError")
                {
                    await _context.Transcriptions.AddRangeAsync(transcriptions);
                    latestVideo.TranscriptionStatus = result.Item2;
                    latestVideo.TranscribingAttempts += 1;
                    await _context.SaveChangesAsync();
                    transcriptions.ForEach(t => _generateVTTFileTask.Publish(new JobObject<Transcription>
                    {
                        Data = t
                    }));
                }
            }
            _sceneDetectionTask.Publish(new JobObject<Video>
            {
                Data = video
            });
        }
    }
}
