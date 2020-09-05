using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using CTCommons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using static ClassTranscribeDatabase.CommonUtils;
using System.Diagnostics.CodeAnalysis;

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class QueueAwakerTask : RabbitMQTask<JObject>
    {
        private readonly DownloadPlaylistInfoTask _downloadPlaylistInfoTask;
        private readonly DownloadMediaTask _downloadMediaTask;
        // private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly TranscriptionTask _transcriptionTask;
        private readonly GenerateVTTFileTask _generateVTTFileTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly SceneDetectionTask _scenedDetectionTask;
        private readonly CreateBoxTokenTask _createBoxTokenTask;
        private readonly UpdateBoxTokenTask _updateBoxTokenTask;
        private readonly SlackLogger _slackLogger;

        public QueueAwakerTask() { }

        public QueueAwakerTask(RabbitMQConnection rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask,
            DownloadMediaTask downloadMediaTask,
            TranscriptionTask transcriptionTask, ProcessVideoTask processVideoTask,
            GenerateVTTFileTask generateVTTFileTask, SceneDetectionTask scenedDetectionTask,
            CreateBoxTokenTask createBoxTokenTask, UpdateBoxTokenTask updateBoxTokenTask,
            ILogger<QueueAwakerTask> logger, SlackLogger slackLogger)
            : base(rabbitMQ, TaskType.QueueAwaker, logger)
        {
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _downloadMediaTask = downloadMediaTask;
            //_convertVideoToWavTask = convertVideoToWavTask;
            _transcriptionTask = transcriptionTask;
            _generateVTTFileTask = generateVTTFileTask;
            _processVideoTask = processVideoTask;
            _scenedDetectionTask = scenedDetectionTask;
            _createBoxTokenTask = createBoxTokenTask;
            _updateBoxTokenTask = updateBoxTokenTask;
            _slackLogger = slackLogger;
        }

        private async Task FindPendingJobs()
        {
            using (var context = CTDbContext.CreateDbContext())
            {
                // Medias for which no videos have downloaded
                var toDownloadMediaIds = await context.Medias.Where(m => m.Video == null).Select(m =>
                    new TaskItem
                    {
                        UniqueId = m.Id,
                        ResultData = new JObject(),
                        TaskParameters = new JObject(),
                        TaskType = TaskType.DownloadMedia,
                        Attempts = 0
                    }).ToListAsync();

                // Videos which haven't been converted to wav 
                var toConvertVideoIds = await context.Videos.Where(v => v.Medias.Any() && v.Audio == null).Select(v =>
                    new TaskItem
                    {
                        UniqueId = v.Id,
                        ResultData = new JObject(),
                        TaskParameters = new JObject(),
                        TaskType = TaskType.ConvertMedia,
                        Attempts = 0
                    }).ToListAsync();

                // Transcribe pending videos.
                var toTranscribeVideoIds = await context.Videos.Where(v => v.TranscribingAttempts < 3 && 
                                                                           v.TranscriptionStatus != "NoError" && 
                                                                           v.Medias.Any() && v.Audio != null).Select(v =>
                                                                           new TaskItem
                                                                           {
                                                                               UniqueId = v.Id,
                                                                               ResultData = new JObject(),
                                                                               TaskParameters = new JObject(),
                                                                               TaskType = TaskType.Transcribe,
                                                                               Attempts = 0
                                                                           }).ToListAsync();

                // Completed Transcriptions which haven't generated vtt files
                var toGenerateVTTsTranscriptionIds = await context.Transcriptions.Where(t => t.Captions.Count > 0 && t.File == null)
                                                                                .Select(t =>
                                                                                new TaskItem
                                                                                {
                                                                                    UniqueId = t.Id,
                                                                                    ResultData = new JObject(),
                                                                                    TaskParameters = new JObject(),
                                                                                    TaskType = TaskType.GenerateVTTFile,
                                                                                    Attempts = 0
                                                                                }).ToListAsync();

                var allTaskItems = new List<TaskItem>();
                allTaskItems.AddRange(toDownloadMediaIds);
                allTaskItems.AddRange(toConvertVideoIds);
                allTaskItems.AddRange(toTranscribeVideoIds);
                allTaskItems.AddRange(toGenerateVTTsTranscriptionIds);

                foreach(var taskItem in allTaskItems)
                {
                    if(!await context.TaskItems.AnyAsync(t => t.TaskType == taskItem.TaskType && t.UniqueId == taskItem.UniqueId))
                    {
                        await context.TaskItems.AddAsync(taskItem);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        private async Task PendingJobs()
        {
            // Update Box Token every few hours
            _updateBoxTokenTask.Publish("");
            using (var context = CTDbContext.CreateDbContext())
            {
                // Medias for which no videos have downloaded
                (await context.Medias.Where(m => m.Video == null).ToListAsync()).ForEach(m => _downloadMediaTask.Publish(m.Id));
                //// Videos which haven't been converted to wav 
                //(await context.Videos.Where(v => v.Medias.Any() && v.Audio == null).ToListAsync()).ForEach(v => _convertVideoToWavTask.Publish(v.Id));
                // Videos which have failed in transcribing
                (await context.Videos.Where(v => v.TranscribingAttempts < 3 && v.TranscriptionStatus != "NoError" && v.Medias.Any())
                    .ToListAsync()).ForEach(v => _transcriptionTask.Publish(v.Id));
                // Completed Transcriptions which haven't generated vtt files
                (await context.Transcriptions.Where(t => t.Captions.Count > 0 && t.File == null)
                    .ToListAsync())
                    .ForEach(t => _generateVTTFileTask.Publish(t.Id));
            }
        }

        private async Task DownloadAllPlaylists()
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var period = DateTime.Now.AddMonths(-6);
                var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(p.Id));
            }
        }

        protected async override Task OnConsume(JObject jObject, TaskParameters taskParameters)
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
                    _downloadPlaylistInfoTask.Publish(playlist.Id);
                }
                else if (type == TaskType.GenerateVTTFile.ToString())
                {
                    var transcriptionId = jObject["TranscriptionId"].ToString();
                    var transcription = await _context.Transcriptions.FindAsync(transcriptionId);
                    _generateVTTFileTask.Publish(transcription.Id);
                }
                else if (type == TaskType.SceneDetection.ToString())
                {
                    var mediaId = jObject["mediaId"].ToString();
                    var media = _context.Medias.Find(mediaId);
                    _scenedDetectionTask.Publish(media.Video.Id);
                }
                else if (type == TaskType.CreateBoxToken.ToString())
                {
                    var authCode = jObject["authCode"].ToString();
                    _createBoxTokenTask.Publish(authCode);
                }
                else if (type == TaskType.DownloadMedia.ToString())
                {
                    var mediaId = jObject["mediaId"].ToString();
                    var media = await _context.Medias.FindAsync(mediaId);
                    _downloadMediaTask.Publish(media.Id);
                }
                //else if (type == TaskType.ConvertMedia.ToString())
                //{
                //    var videoId = jObject["videoId"].ToString();
                //    var video = await _context.Videos.FindAsync(videoId);
                //    _convertVideoToWavTask.Publish(video.Id);
                //}
                else if (type == TaskType.Transcribe.ToString())
                {
                    var videoId = jObject["videoId"].ToString();
                    var video = await _context.Videos.FindAsync(videoId);
                    _transcriptionTask.Publish(video.Id);
                }
                else if (type == TaskType.UpdateOffering.ToString())
                {
                    var offeringId = jObject["offeringId"].ToString();
                    (await _context.Playlists.Where(o => o.OfferingId == offeringId).ToListAsync())
                        .ForEach(p => _downloadPlaylistInfoTask.Publish(p.Id));
                }
                else if (type == TaskType.ReTranscribePlaylist.ToString())
                {
                    var playlistId = jObject["PlaylistId"].ToString();

                    // Get all videos 
                    var videos = await _context.Playlists.Where(p => p.Id == playlistId).SelectMany(p => p.Medias).Select(m => m.Video)
                        .ToListAsync();
                    // Delete all captions
                    var captions = videos.SelectMany(v => v.Transcriptions).SelectMany(t => t.Captions).ToList();
                    _context.Captions.RemoveRange(captions);
                    // Delete all Transcriptions
                    var transcriptions = videos.SelectMany(v => v.Transcriptions).ToList();
                    _context.Transcriptions.RemoveRange(transcriptions);

                    videos.ForEach(v =>
                    {
                        v.TranscribingAttempts = 0;
                        v.TranscriptionStatus = null;
                    });

                    await _context.SaveChangesAsync();

                    videos.ForEach(v =>
                    {
                        _transcriptionTask.Publish(v.Id);
                    });
                }
            }
        }
    }
}
