using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.MSTranscription;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons;
using Newtonsoft.Json.Linq;

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
        private readonly CaptionQueries _captionQueries;
        private readonly CTDbContext _context;

        public TranscriptionTask(RabbitMQConnection rabbitMQ, MSTranscriptionService msTranscriptionService,
            GenerateVTTFileTask generateVTTFileTask, SceneDetectionTask sceneDetectionTask, ILogger<TranscriptionTask> logger, CaptionQueries captionQueries, CTDbContext context)
            : base(rabbitMQ, TaskType.Transcribe, logger)
        {
            _msTranscriptionService = msTranscriptionService;
            _generateVTTFileTask = generateVTTFileTask;
            _sceneDetectionTask = sceneDetectionTask;
            _captionQueries = captionQueries;
            _context = context;
        }

        /// <summary>
        /// [2020.7.7] For the purpose of resuming failed transcriptions, all the captions of the failed transcription would now be stored in the database.
        /// Before resuming, all the old captions would be queried out from the database and stored in a dictionary, which will be passed to the
        /// transcription function along with the resume time point.
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="taskParameters"></param>
        /// <returns></returns>
        protected async override Task OnConsume(string videoId, TaskParameters taskParameters)
        {
            Video video;
            video = await _context.Videos.Include(v => v.Video1).Where(v => v.Id == videoId).FirstAsync();
            Key key = TaskEngineGlobals.KeyProvider.GetKey(video.Id);

            // creat Dictionary and pass it to the recognition function
            Dictionary<string, List<Caption>> captions = new Dictionary<string, List<Caption>>();
            // check if query return empty List the first time
            captions[Languages.ENGLISH] = await _captionQueries.GetCaptionsAsync(video.Id, Languages.ENGLISH);
            captions[Languages.SIMPLIFIED_CHINESE] = await _captionQueries.GetCaptionsAsync(video.Id, Languages.SIMPLIFIED_CHINESE);
            captions[Languages.KOREAN] = await _captionQueries.GetCaptionsAsync(video.Id, Languages.KOREAN);
            captions[Languages.SPANISH] = await _captionQueries.GetCaptionsAsync(video.Id, Languages.SPANISH);
            captions[Languages.FRENCH] = await _captionQueries.GetCaptionsAsync(video.Id, Languages.FRENCH);

            var lastSuccessTime = TimeSpan.Zero;
            if (video.JsonMetadata != null && video.JsonMetadata["LastSuccessfulTime"] != null)
            {
                lastSuccessTime = TimeSpan.Parse(video.JsonMetadata["LastSuccessfulTime"].ToString());
            }

            var result = await _msTranscriptionService.RecognitionWithVideoStreamAsync(video.Video1, key, captions, lastSuccessTime);

            if (video.JsonMetadata == null)
            {
                video.JsonMetadata = new JObject();
            }

            video.JsonMetadata["LastSuccessfulTime"] = result.LastSuccessTime.ToString();

            await _context.SaveChangesAsync();
            TaskEngineGlobals.KeyProvider.ReleaseKey(key, video.Id);
            List<Transcription> transcriptions = new List<Transcription>();
            foreach (var language in result.Captions)
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

            var latestVideo = await _context.Videos.FindAsync(video.Id);
            if (latestVideo.Transcriptions != null && latestVideo.Transcriptions.Any())
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
            await _context.SaveChangesAsync();
            transcriptions.ForEach(t => _generateVTTFileTask.Publish(t.Id));
            _sceneDetectionTask.Publish(video.Id);
            latestVideo.TranscriptionStatus = result.ErrorCode;
            latestVideo.TranscribingAttempts += 1;
            await _context.SaveChangesAsync();
        }
    }
}

