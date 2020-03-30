using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class QueueAwakerTask : RabbitMQTask<JObject>
    {
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private readonly DownloadMediaTask _downloadMediaTask;
        private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly TranscriptionTask _transcriptionTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly SceneDetectionTask _scenedDetectionTask;
        private readonly CreateBoxTokenTask _createBoxTokenTask;
        private readonly UpdateBoxTokenTask _updateBoxTokenTask;
        private readonly SlackLogger _slackLogger;

        public QueueAwakerTask() { }

        public QueueAwakerTask(RabbitMQConnection rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask,
            DownloadMediaTask downloadMediaTask, ConvertVideoToWavTask convertVideoToWavTask,
            TranscriptionTask transcriptionTask, ProcessVideoTask processVideoTask,
            GenerateVTTFileTask generateVTTFileTask, SceneDetectionTask scenedDetectionTask,
            CreateBoxTokenTask createBoxTokenTask, UpdateBoxTokenTask updateBoxTokenTask,
            ILogger<QueueAwakerTask> logger, SlackLogger slackLogger)
            : base(rabbitMQ, TaskType.QueueAwaker, logger)
        {
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _downloadMediaTask = downloadMediaTask;
            _convertVideoToWavTask = convertVideoToWavTask;
            _transcriptionTask = transcriptionTask;
            _generateVTTFileTask = generateVTTFileTask;
            _processVideoTask = processVideoTask;
            _scenedDetectionTask = scenedDetectionTask;
            _createBoxTokenTask = createBoxTokenTask;
            _updateBoxTokenTask = updateBoxTokenTask;
            _slackLogger = slackLogger;
        }

        private async Task PendingJobs()
        {
            // Update Box Token every few hours
            _updateBoxTokenTask.Publish("");
            using (var context = CTDbContext.CreateDbContext())
            {
                // Medias for which no videos have downloaded
                (await context.Medias.Where(m => m.Video == null).ToListAsync()).ForEach(m => _downloadMediaTask.Publish(new JobObject<Media> { Data = m }));
                // Videos which haven't been converted to wav 
                (await context.Videos.Where(v => v.Medias.Any() && v.Audio == null).ToListAsync()).ForEach(v => _convertVideoToWavTask.Publish(new JobObject<Video> { Data = v }));
                // Videos which have failed in transcribing
                (await context.Videos.Where(v => v.TranscribingAttempts < 3 && v.TranscriptionStatus != "NoError" && v.Medias.Any() && v.Audio != null)
                    .ToListAsync()).ForEach(v => _transcriptionTask.Publish(new JobObject<Video> { Data = v }));
                // Completed Transcriptions which haven't generated vtt files
                (await context.Transcriptions.Where(t => t.Captions.Count > 0 && t.File == null)
                    .ToListAsync())
                    .ForEach(t => _generateVTTFileTask.Publish(new JobObject<Transcription> { Data = t }));
            }
        }

        private async Task DownloadAllPlaylists()
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var period = DateTime.Now.AddMonths(-6);
                var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(new JobObject<Playlist> { Data = p }));
            }
        }

        protected async override Task OnConsume(JObject jObject)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var type = jObject["Type"].ToString();
                if (type == TaskType.PeriodicCheck.ToString())
                {
                    await _slackLogger.PostMessageAsync("Periodic Check.");
                    _updateBoxTokenTask.Publish("");
                    await DownloadAllPlaylists();
                    await PendingJobs();
                }
                else if (type == TaskType.DownloadAllPlaylists.ToString())
                {
                    await DownloadAllPlaylists();
                }
                else if (type == TaskType.DownloadPlaylistInfo.ToString())
                {
                    var playlistId = jObject["PlaylistId"].ToString();
                    var playlist = await _context.Playlists.FindAsync(playlistId);
                    _downloadPlaylistInfoTask.Publish(new JobObject<Playlist> { Data = playlist });
                }
                else if (type == TaskType.GenerateVTTFile.ToString())
                {
                    var transcriptionId = jObject["TranscriptionId"].ToString();
                    var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                    _generateVTTFileTask.Publish(new JobObject<Transcription> { Data = transcription });
                }
                else if (type == TaskType.SceneDetection.ToString())
                {
                    var mediaId = jObject["mediaId"].ToString();
                    var media = _context.Medias.Find(mediaId);
                    _scenedDetectionTask.Publish(new JobObject<Video> { Data = media.Video });
                }
                else if (type == TaskType.CreateBoxToken.ToString())
                {
                    var authCode = jObject["authCode"].ToString();
                    _createBoxTokenTask.Publish(new JobObject<string> { Data = authCode });
                }
                else if (type == TaskType.DownloadMedia.ToString())
                {
                    var mediaId = jObject["mediaId"].ToString();
                    var media = await _context.Medias.FindAsync(mediaId);
                    _downloadMediaTask.Publish(new JobObject<Media> { Data = media });
                }
                else if (type == TaskType.ConvertMedia.ToString())
                {
                    var videoId = jObject["videoId"].ToString();
                    var video = await _context.Videos.FindAsync(videoId);
                    _convertVideoToWavTask.Publish(new JobObject<Video> { Data = video });
                }
                else if (type == TaskType.Transcribe.ToString())
                {
                    var videoId = jObject["videoId"].ToString();
                    var video = await _context.Videos.FindAsync(videoId);
                    _transcriptionTask.Publish(new JobObject<Video> { Data = video });
                }
                else if (type == TaskType.UpdateOffering.ToString())
                {
                    var offeringId = jObject["offeringId"].ToString();
                    (await _context.Playlists.Where(o => o.OfferingId == offeringId).ToListAsync())
                        .ForEach(p => _downloadPlaylistInfoTask.Publish(new JobObject<Playlist> { Data = p }));
                }
            }
        }
    }
}
