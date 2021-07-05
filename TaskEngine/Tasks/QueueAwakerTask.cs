using ClassTranscribeDatabase;
using CTCommons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis; // Supports SuppressMessage decoration
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;



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
        private readonly SceneDetectionTask _sceneDetectionTask;
        private readonly CreateBoxTokenTask _createBoxTokenTask;
        private readonly UpdateBoxTokenTask _updateBoxTokenTask;
        private readonly BuildElasticIndexTask _buildElasticIndexTask;
        private readonly CleanUpElasticIndexTask _cleanUpElasticIndexTask;
        private readonly ExampleTask _exampleTask;
        private readonly SlackLogger _slackLogger;

        public QueueAwakerTask() { }

        public QueueAwakerTask(RabbitMQConnection rabbitMQ, DownloadPlaylistInfoTask downloadPlaylistInfoTask,
            DownloadMediaTask downloadMediaTask,
            TranscriptionTask transcriptionTask, ProcessVideoTask processVideoTask,
            GenerateVTTFileTask generateVTTFileTask, SceneDetectionTask sceneDetectionTask,
            CreateBoxTokenTask createBoxTokenTask, UpdateBoxTokenTask updateBoxTokenTask,
            BuildElasticIndexTask buildElasticIndexTask, CleanUpElasticIndexTask cleanUpElasticIndexTask,
            ExampleTask exampleTask,
            ILogger<QueueAwakerTask> logger, SlackLogger slackLogger)
            : base(rabbitMQ, TaskType.QueueAwaker, logger)
        {
            _downloadPlaylistInfoTask = downloadPlaylistInfoTask;
            _downloadMediaTask = downloadMediaTask;
            //_convertVideoToWavTask = convertVideoToWavTask;
            _transcriptionTask = transcriptionTask;
            _generateVTTFileTask = generateVTTFileTask;
            _processVideoTask = processVideoTask;
            _sceneDetectionTask = sceneDetectionTask;
            _createBoxTokenTask = createBoxTokenTask;
            _updateBoxTokenTask = updateBoxTokenTask;
            _buildElasticIndexTask = buildElasticIndexTask;
            _cleanUpElasticIndexTask = cleanUpElasticIndexTask;
            _exampleTask = exampleTask;
            _slackLogger = slackLogger;
        }

        /// <summary>Finds incomplete tasks and adds them all a TaskItem table. 
        /// This appears to be defunct and not yet used code - grep FindPendingJobs, found no callers of this function
        /// </summary>
        //       private async Task FindPendingJobs()
        // {
        //     using (var context = CTDbContext.CreateDbContext())
        //     {
        //         // Medias for which no videos have downloaded
        //         var toDownloadMediaIds = await context.Medias.Where(m => m.Video == null).Select(m =>
        //             new TaskItem
        //             {
        //                 UniqueId = m.Id,
        //                 ResultData = new JObject(),
        //                 TaskParameters = new JObject(),
        //                 TaskType = TaskType.DownloadMedia,
        //                 Attempts = 0
        //             }).ToListAsync();

        //         // Videos which haven't been converted to wav 
        //         var toConvertVideoIds = await context.Videos.Where(v => v.Medias.Any() && v.Audio == null).Select(v =>
        //             new TaskItem
        //             {
        //                 UniqueId = v.Id,
        //                 ResultData = new JObject(),
        //                 TaskParameters = new JObject(),
        //                 TaskType = TaskType.ConvertMedia,
        //                 Attempts = 0
        //             }).ToListAsync();

        //         // Transcribe pending videos.
        //         var toTranscribeVideoIds = await context.Videos.Where(v => v.TranscribingAttempts < 3 && 
        //                                                                    v.TranscriptionStatus != "NoError" && 
        //                                                                    v.Medias.Any() && v.Audio != null).Select(v =>
        //                                                                    new TaskItem
        //                                                                    {
        //                                                                        UniqueId = v.Id,
        //                                                                        ResultData = new JObject(),
        //                                                                        TaskParameters = new JObject(),
        //                                                                        TaskType = TaskType.Transcribe,
        //                                                                        Attempts = 0
        //                                                                    }).ToListAsync();

        //         // Completed Transcriptions which haven't generated vtt files
        //         var toGenerateVTTsTranscriptionIds = await context.Transcriptions.Where(t => t.Captions.Count > 0 && t.File == null)
        //                                                                         .Select(t =>
        //                                                                         new TaskItem
        //                                                                         {
        //                                                                             UniqueId = t.Id,
        //                                                                             ResultData = new JObject(),
        //                                                                             TaskParameters = new JObject(),
        //                                                                             TaskType = TaskType.GenerateVTTFile,
        //                                                                             Attempts = 0
        //                                                                         }).ToListAsync();

        //         var allTaskItems = new List<TaskItem>();
        //         allTaskItems.AddRange(toDownloadMediaIds);
        //         allTaskItems.AddRange(toConvertVideoIds);
        //         allTaskItems.AddRange(toTranscribeVideoIds);
        //         allTaskItems.AddRange(toGenerateVTTsTranscriptionIds);

        //         foreach(var taskItem in allTaskItems)
        //         {
        //             if(!await context.TaskItems.AnyAsync(t => t.TaskType == taskItem.TaskType && t.UniqueId == taskItem.UniqueId))
        //             {
        //                 await context.TaskItems.AddAsync(taskItem);
        //             }
        //         }
        //         await context.SaveChangesAsync();
        //     }
        // }
        /// <summary> Used by the PeriodicCheck to identify and enqueue missing tasks.
        /// This Task is started after all playlists are updated.
        /// </summary>
        private async Task PendingJobs()
        {

           
            // Update Box Token every few hours
            _updateBoxTokenTask.Publish("");

            //We will use these outside of the DB scope
            List<String> todoVTTs ;
            List<String> todoProcessVideos;
            List<String> todoTranscriptions;
            List<String> todoDownloads;
            List<String> todoSceneDetection;
            using (var context = CTDbContext.CreateDbContext())
            {
                // Most tasks are created directly from within a task when it normally completed. 
                // This code exists to detect missing items and to publish tasks to complete them
                // A redesigned taskengine should not have the direct coupling inside each task

                // Since downloading a video could also create a Video, it is better to do these with little time delay in-between and then publish all the tasks
                // I believe there is still a race condition: Prior to this, we've just polled all active playlists and at least one of these may have already completed
                // So let's only consider items that are older than 10 minutes
                // Okay this is bandaid on the current design until we redesign the taskengine
                // Ideas For the future: 
                // * Consider setting TTL on these messages to be 5 minutes short of thethe Periodic Refresh?
                // * If/when we drop the direct appoach consider: Random ordering. Most recent first (or randomly choosing either)

                // If an object was created during the middle of a periodic cycle, give it a full cycle to queue, and another cycle to complete its tasks

                
                int minutesCutOff =  Math.Max( 1, Convert.ToInt32(Globals.appSettings.PERIODIC_CHECK_OLDER_THAN_MINUTES));
               
                
                var tooRecentCutoff = DateTime.Now.AddMinutes(- minutesCutOff);
                // This is the first use of 'AsNoTracking' in this project; let's check it works in Production as expected

                // TODO/TOREVIEW: Does EF create the complete entity and then project out the ID column in dot Net, or does it request only the ID from the database?
                // TODO/TOREVIEW: Since this code  just pulls the IDs from the database, I expect this will be harmless no-op, however all DB reads should use AsNoTracking as a best practice
                // See https://code-maze.com/queries-in-entity-framework-core/
                // See https://docs.microsoft.com/en-us/ef/core/querying/tracking


                // Completed Transcriptions which haven't generated vtt files
                // TODO: Should also check dates too
                GetLogger().LogInformation($"Finding incomplete VTTs, Transcriptions and Downloads from before {tooRecentCutoff}, minutesCutOff=({minutesCutOff})");


                // Todo Could also check for secondary video too
                todoProcessVideos = await context.Videos.AsNoTracking().Where(
                   v=>(v.Duration == null && ! String.IsNullOrEmpty(v.Video1Id))
                   ).OrderByDescending(t => t.CreatedAt).Select(e => e.Id).ToListAsync();

                todoVTTs = await context.Transcriptions.AsNoTracking().Where(
                    t => t.Captions.Count > 0 && t.File == null && t.CreatedAt < tooRecentCutoff
                    ).OrderByDescending(t => t.CreatedAt).Select(e => e.Id).ToListAsync();

                todoSceneDetection = await context.Videos.AsNoTracking().Where( 
                        v=> v.PhraseHints == null &&
                        v.Medias.Any() && v.CreatedAt < tooRecentCutoff
                    ).OrderByDescending(t => t.CreatedAt).Select(e => e.Id).ToListAsync();

                todoTranscriptions = await context.Videos.AsNoTracking().Where( 
                        v=> v.PhraseHints != null &&
                        v.TranscribingAttempts < 41 && v.TranscriptionStatus != "NoError" && 
                        v.Medias.Any() && v.CreatedAt < tooRecentCutoff
                    ).OrderByDescending(t => t.CreatedAt).Select(e => e.Id).ToListAsync();

                // Medias for which no videos have downloaded
                todoDownloads = await context.Medias.AsNoTracking().Where(
                    m => m.Video == null && m.CreatedAt < tooRecentCutoff
                    ).OrderByDescending(t => t.CreatedAt).Select(e => e.Id).ToListAsync();
            }
            // We have a list of outstanding tasks
            // However some of these may already be in progress
            // So don't queue theses

            GetLogger().LogInformation($"Found {todoProcessVideos.Count},{todoVTTs.Count},{todoTranscriptions.Count},{todoDownloads.Count} counts before filtering");
            ClientActiveTasks currentProcessVideos = _processVideoTask.GetCurrentTasks();
            todoProcessVideos.RemoveAll(e => currentProcessVideos.Contains(e));


            ClientActiveTasks currentVTTs = _generateVTTFileTask.GetCurrentTasks();
            todoVTTs.RemoveAll(e => currentVTTs.Contains(e));

            
            ClientActiveTasks currentSceneDetection = _sceneDetectionTask.GetCurrentTasks();
            todoSceneDetection.RemoveAll(e => currentSceneDetection.Contains(e));

            ClientActiveTasks currentTranscription = _transcriptionTask.GetCurrentTasks();
            todoTranscriptions.RemoveAll(e => currentTranscription.Contains(e));

            ClientActiveTasks currentDownloads = _transcriptionTask.GetCurrentTasks();
            todoDownloads.RemoveAll(e => currentDownloads.Contains(e));

            GetLogger().LogInformation($"Current In progress  {currentProcessVideos.Count},{currentVTTs.Count},{currentTranscription.Count},{currentDownloads.Count} counts after filtering");
            GetLogger().LogInformation($"Found {todoProcessVideos.Count},{todoVTTs.Count},{todoTranscriptions.Count},{todoDownloads.Count} counts after filtering");


            // Now we have a list of new things we want to do
            GetLogger().LogInformation($"Publishing processingVideos ({String.Join(",", todoProcessVideos)})");

            todoProcessVideos.ForEach(t => _processVideoTask.Publish(t));


            
            GetLogger().LogInformation($"Publishing SceneDetects ({String.Join(",", todoSceneDetection)})");
            todoSceneDetection.ForEach(t => _sceneDetectionTask.Publish(t));

            GetLogger().LogInformation($"Publishing todoVTTs ({String.Join(",", todoVTTs)})");

            todoVTTs.ForEach(t => _generateVTTFileTask.Publish(t));

            GetLogger().LogInformation($"Publishing todoTranscriptions ({String.Join(",", todoTranscriptions)})");

            todoTranscriptions.ForEach(v => _transcriptionTask.Publish(v));

            GetLogger().LogInformation($"Publishing todoDownloads ({String.Join(",", todoDownloads)})");

            todoDownloads.ForEach(m => _downloadMediaTask.Publish(m));

            //// Not used Videos which haven't been converted to wav 
            /// Code Not deleted because one day we will just reuse the one wav file and use an offset into that file
            //(await context.Videos.Where(v => v.Medias.Any() && v.Audio == null).ToListAsync()).ForEach(v => _convertVideoToWavTask.Publish(v.Id));
            // Videos which have failed in transcribing
            GetLogger().LogInformation("Pending Jobs - completed");
        }
        /// Requests _downloadPlaylistInfoTask for all recent playlists
        private async Task DownloadAllPlaylists()
        {
           
            List <String> playlists;
            using (var _context = CTDbContext.CreateDbContext())
            {
                _downloadPlaylistInfoTask.PurgeQueue();

                var period = DateTime.Now.AddMonths(-6);
                //TODO/TOREVIEW: Suggest Term.EndDate < Today plus 2 weeks (but let's check the semester dates in the DB and document this in the frontend)
                playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).Select(p => p.Id).ToListAsync();
            }
            GetLogger().LogInformation($"DownloadAllPlaylists(); _downloadPlaylistInfoTask publishing {playlists.Count} tasks");
            playlists.ForEach(p => _downloadPlaylistInfoTask.Publish(p));
            
            GetLogger().LogInformation("DownloadAllPlaylists() - Complete");
        }

        protected async override Task OnConsume(JObject jObject, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
         
            using (var _context = CTDbContext.CreateDbContext())
            {
                var type = jObject["Type"].ToString();

                if (type == TaskType.PeriodicCheck.ToString())
                {
                    await _slackLogger.PostMessageAsync("Periodic Check.");
                    registerTask(cleanup, "PeriodicCheck");
                    _buildElasticIndexTask.Publish("");
                    _cleanUpElasticIndexTask.Publish("");
                    //_exampleTask.Publish("");

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
                    _sceneDetectionTask.Publish(media.Video.Id);
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
                else if(type==TaskType.SceneDetection.ToString())
                {
                    var id = jObject["videoMediaPlaylistId"].ToString();
                    bool deleteExisting = false;
                    try
                    {
                        deleteExisting = jObject["DeleteExisting"].Value<bool>();
                    }
                    catch (Exception) { }
                    GetLogger().LogInformation($"{type}:{id}");
                    var videos = await _context.Videos.Where(v=>v.Id ==id).ToListAsync();
                    if (videos.Count == 0)
                    {
                        videos = await _context.Medias.Where(m => (m.PlaylistId == id) || (m.Id == id)).Select(m => m.Video).ToListAsync();
                    }
                    foreach (var video in videos)
                    {
                        if (deleteExisting)
                        {
                            GetLogger().LogInformation($"{id}:Removing SceneDetection for video ({video.Id})");

                            video.SceneData = null;
                            
                            await _context.SaveChangesAsync();
                        }
                        _sceneDetectionTask.Publish(video.Id);

                    }

                }
                else if (type == TaskType.TranscribeVideo.ToString())
                {
                    var id = jObject["videoOrMediaId"].ToString();
                    

                    GetLogger().LogInformation($"{type}:{id}");
                    var video = await _context.Videos.FindAsync(id);
                    if(video == null)
                    {
                        var media = await _context.Medias.FindAsync(id);
                        if( media != null)
                        {
                            GetLogger().LogInformation($"{id}: media Found. videoID=({media.VideoId})");
                            video = media.Video;
                        }
                    }
                    if( video == null)
                    {
                        GetLogger().LogInformation($"No video found for video/mediaId ({id})");
                        return;

                     }
                    //TODO: These properties should not be literal strings
                    bool deleteExisting = false;
                    try
                    {
                        deleteExisting = jObject["DeleteExisting"].Value<bool>();
                    }
                    catch (Exception) { }
                    if (deleteExisting)
                    {
                        GetLogger().LogInformation($"{id}:Removing Transcriptions for video ({video.Id})");
                        
                        var transcriptions = video.Transcriptions;
                        _context.Transcriptions.RemoveRange(transcriptions);
                        video.TranscriptionStatus = "";
                        // Could also remove LastSuccessTime and reset attempts
                        
                        await _context.SaveChangesAsync();
                    }
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
                    var videos = await _context.Playlists.Where(p => p.Id == playlistId)
                        .SelectMany(p => p.Medias)
                        .Where(e=> e!=null)
                        .Select(m => m.Video)
                        .ToListAsync();
                    // Delete all captions. This caused a null pointer exception because some elements were null
                    // the above line and this line now have null filters
                    var captions =  videos.SelectMany(v => v.Transcriptions)
                        .Where(e => e != null)
                        .SelectMany(t => t.Captions).ToList();

                    _context.Captions.RemoveRange(captions);
                    // TODO/TOREVIEW: No need to create in captions. Their IDs should be sufficient

                    // Delete all Transcriptions
                    var transcriptions = videos.SelectMany(v => v.Transcriptions).Where(e => e != null).ToList();
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
