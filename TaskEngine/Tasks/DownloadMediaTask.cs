using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using static ClassTranscribeDatabase.CommonUtils;

// #pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task fetches downloads the video file for a given media.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DownloadMediaTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly SceneDetectionTask _sceneDetectionTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly BoxAPI _box;
        private readonly SlackLogger _slack;

        public DownloadMediaTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient,
            SceneDetectionTask sceneDetectionTask, ProcessVideoTask processVideoTask, BoxAPI box,
            ILogger<DownloadMediaTask> logger, SlackLogger slack)
            : base(rabbitMQ, TaskType.DownloadMedia, logger)
        {
            _rpcClient = rpcClient;
            _processVideoTask = processVideoTask;
            _sceneDetectionTask = sceneDetectionTask;
            _box = box;
            _slack = slack;
        }

        protected override async Task OnConsume(string mediaId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, mediaId); // may throw AlreadyInProgress exception

            (Media media, string subdir) = await prepMediaForDownload(mediaId);
            if (media == null)
            {
                return;
            }

            Video? video = null;
            switch (media.SourceType)
            {
                case SourceType.Echo360: video = await DownloadEchoVideo(subdir, media); break;
                case SourceType.Youtube: video = await DownloadYoutubeVideo(subdir, media); break;
                case SourceType.Local: video = await DownloadLocalPlaylist(subdir, media); break;
                case SourceType.Kaltura: video = await DownloadKalturaVideo(subdir, media); break;
                case SourceType.Box: video = await DownloadBoxVideo(subdir, media); break;
            }
            // If no valid video1, or if a video2 object exists but not a valid file - fail the task.
            if (video == null || video.Video1 == null || !video.Video1.IsValidFile()
                || (video.Video2 != null && !video.Video2.IsValidFile()))
            {
                throw new Exception($"DownloadMediaTask failed for mediaId ({media.Id})");
            }

            var processNewVideo = await updateMediaWithVideo(mediaId, video);
            if (processNewVideo)
            {
                _sceneDetectionTask.Publish(video.Id);
                //_processVideoTask.Publish(video.Id); //TODO - re- add this code
            }
        }

        /* Print some useful log messages, check if media, playlist and courseoffing exist and determine the subdir for new files for this courseoffering
        Optionally updates the media options from the playlist if they have not been create yet
        */
        async Task<(Media, string)> prepMediaForDownload(string mediaId)
        {
            if (string.IsNullOrEmpty(mediaId))
            {
                GetLogger().LogInformation($"Download Media : mediaId is null or empty - skipping download");
                return (null, null);
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                var media = await _context.Medias.Include(m => m.Playlist).ThenInclude(p => p.Offering).Where(m => m.Id == mediaId).FirstOrDefaultAsync();
                var offeringName = media?.Playlist?.Offering?.CourseName ?? "no-offering";
                var playlistName = media?.Playlist?.Name ?? "no-playlist";
                GetLogger().LogInformation($"Download for media id=({mediaId}), (#{media?.Index}) of {offeringName}/ ({media?.Playlist?.Id}:{playlistName }). UniqueMediaIdentifier={media?.UniqueMediaIdentifier}");
                
                if (mediaId == null || media.Playlist == null || media.Playlist.Offering == null)
                {
                    GetLogger().LogInformation($"Media ({mediaId}): Media or Playlist or CourseOffering is null - perhaps it was deleted. Skipping download");
                    return (null, null);
                }

                // Clone media options (e.g. switch video streams) if needed
                if (string.IsNullOrEmpty(media.Options) && !string.IsNullOrEmpty(media.Playlist.Options))
                {
                    GetLogger().LogInformation($"Media ({media.Id}): Setting options based on playlist options ({media.Playlist.Options})");
                    media.Options = media.Playlist.Options;
                    _context.SaveChanges();
                }
                var subdir = ToCourseOfferingSubDirectory(_context, media.Playlist.Offering); // e.g. "/data/2203-abcd"
                return (media, subdir);
            }
        }
        protected async Task<bool> updateMediaWithVideo(string mediaId, Video newVideo)
        {
            // Sanity check
            if(mediaId == null || newVideo == null)
            {
                GetLogger().LogInformation($"Media ({mediaId}): mediaId or newVideo is null!");
                return false;
            }
            // We get the media again because downloading is very slow and perhaps the database has changed

            using (var _context = CTDbContext.CreateDbContext())
            {
                var media = await _context.Medias
                    .Include(m => m.Video).ThenInclude(v => v.Video2)
                    .Include(m => m.Video).ThenInclude(v => v.Video1)
                    .FirstOrDefaultAsync(m => m.Id == mediaId); // Find does not support Include
                if (media == null)
                { // should never happen... but if it does, clean up our newly downloaded video files
                    GetLogger().LogInformation($"Media ({mediaId}): media == null !? (deleting newly downloaded items)");
                    await newVideo.DeleteVideoAsync(_context);
                    return false;
                }
                GetLogger().LogInformation($"Media ({mediaId}): media.Video == null is {media.Video == null}");

                // Don't add video if there are already videos for the given media.
                if(newVideo.Id != null) {
                    GetLogger().LogError($"Media ({mediaId}): Huh? newVideo should not have an Id yet - that's my job!");
                }
                if (media.Video != null)
                {
                    GetLogger().LogInformation($"Media ({mediaId}): Surprise - media already has video set (race condition?)- no further processing required.Discarding new files");
                    await newVideo.DeleteVideoAsync(_context);
                    return false;
                }
                // Time to find out what we have in the database
                // Important idea: the newVideo and its filerecords are not yet part of the database.
                var matchingFiles = await _context.FileRecords.Where(f => f.Hash == newVideo.Video1.Hash).ToListAsync();
                var matchedFile = matchingFiles.FirstOrDefault(); // Expect 0 or 1

                var existingPrimaryVideos = matchedFile!= null ?  await _context.Videos.Where(v => v.Video1Id == matchedFile.Id).ToListAsync() : null;
                var existingPrimaryVideo = existingPrimaryVideos?.FirstOrDefault(); // If non null we expect 0 or 1

                GetLogger().LogInformation($"Media ({mediaId}): {matchingFiles.Count} FileRecord hash match found");
                GetLogger().LogInformation($"Media ({mediaId}): {existingPrimaryVideos?.Count ?? 0} existing Videos found");

                // cherrypick case (see comment below)
                if (existingPrimaryVideo != null)
                {
                    GetLogger().LogInformation($"Media ({mediaId}): FileRecord and existing Video!  deleting newly downloaded video");

                    media.VideoId = existingPrimaryVideo.Id;

                    // We now take any useful supplementary files from the newly downladed video and add them to the existing video
                    // Then delete the new video (which has now been cherrypicked for all of its valuable stuff
                    if (newVideo.Video2 != null && existingPrimaryVideo.Video2 == null)
                    {
                        var v2 = newVideo.Video2;
                        GetLogger().LogInformation($"Media ({mediaId}): Adding video2 ({v2.Id}) to video ({existingPrimaryVideo.Id})");
                        await _context.FileRecords.AddAsync(v2);
                        await _context.SaveChangesAsync(); // now v3 has an Id, so we can use below
                        existingPrimaryVideo.Video2Id = v2.Id;
                        newVideo.Video2 = null; // stop DeleteVideo beiow from deleting the file of video2 that we just added to existingPrimaryVideos
                    }
                    if (newVideo.ASLVideo != null && existingPrimaryVideo.ASLVideo == null)
                    {
                        var v3 = newVideo.ASLVideo;
                        GetLogger().LogInformation($"Media ({mediaId}): Adding ASL ({v3.Id}) to video ({existingPrimaryVideo.Id})");
                        await _context.FileRecords.AddAsync(v3);
                        await _context.SaveChangesAsync(); // now v3 has an Id, so we can use below
                        existingPrimaryVideo.ASLVideoId = v3.Id;
                        newVideo.ASLVideo = null; // stop DeleteVideo beiow from deleting the file of ASL that we just added to existingPrimaryVideo
                    }
                    await _context.SaveChangesAsync();
                    GetLogger().LogInformation($"Media ({media.Id}): Existing Video found. (Deleting New) video.Id=({newVideo.Id})");

                    // Deleting downloaded video as it is a duplicate. Don't start scene detection
                    await newVideo.DeleteVideoAsync(_context);
                    await _context.SaveChangesAsync();
                    return false; // no need to start scene detection etc
                }

                await _context.Videos.AddAsync(newVideo);
                await _context.SaveChangesAsync(); // now video has an Id (finally!), so we can use it for this media

                media.VideoId = newVideo.Id;
                await _context.SaveChangesAsync();
                GetLogger().LogInformation($"Media ({media.Id}): Assigned (new) video.Id={newVideo.Id} - done (no hash-matching FileRecords found)");

                // clean up orphaned FileRecords
                string maybeUnwantedId = matchedFile?.Id ?? "";
                if(! string.IsNullOrEmpty(maybeUnwantedId))
                {
                    
                    var isOrphanedFileRecord = ! await _context.Videos.Where(v => v.Video1Id == maybeUnwantedId || v.Video2Id == maybeUnwantedId || v.ASLVideoId == maybeUnwantedId).AnyAsync();
                    GetLogger().LogInformation($"Media ({media.Id}): fileRecordId= ({maybeUnwantedId}) isOrphanedFileRecord={isOrphanedFileRecord}");
                    // Delete existing file Record - no videos care about it
                    if (isOrphanedFileRecord)
                    {
                        GetLogger().LogInformation($"Media ({media.Id}): Deleting unnecessary FileRecord ${maybeUnwantedId} - no video entries need it");
                        // Is this a problem? An empty image/audio file filerecord could match an empty video (same hash) - which we then delete here
                        // Future Todo: limit deletes to just FileRecords created by this video process
                        // Future Todo II: It would be even better to occasionally run a task that finds all File orphans of all database fields and deletes them (or moves them to a "tobedeleted" folder)

                        // await matchedFile.DeleteFileRecordAsync(_context);
                    }
                }
                return true;
            }

        }


        public async Task<Video> DownloadKalturaVideo(string subdir, Media media)
        {
            GetLogger().LogInformation($"DownloadKalturaVideo ({media.Id}): started");
            string swapInfo = media.GetOptionsAsJson().GetValue("swapStreams")?.ToString() ?? "";
            bool swapStreams = swapInfo == "true" || swapInfo == "True";
            GetLogger().LogInformation($"DownloadKalturaVideo ({media.Id}): swap streams: {swapStreams};<{swapInfo}>");
            string? video2Url = null;
            string video1Url = media.JsonMetadata["downloadUrl"].ToString();
            try
            {
                video2Url = media.JsonMetadata["child"]["downloadUrl"].ToString();
                if (video2Url.Length > 0 && swapStreams)
                {
                    string temp = video1Url;
                    video1Url = video2Url;
                    video2Url = temp;
                }
            }
            catch (Exception) { };

            var mediaResponse = await _rpcClient.PythonServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = video1Url
            });

            Video video;
            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {
                video = new Video
                {
                    Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, subdir)
                };
                try
                {
                    if (media.JsonMetadata["child"] != null && media.JsonMetadata["child"]["downloadUrl"] != null)
                    {
                        GetLogger().LogInformation($"Media ({media.Id}): Downloading child video");

                        var childMediaR = await _rpcClient.PythonServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
                        {
                            VideoUrl = video2Url
                        });
                        if (FileRecord.IsValidFile(childMediaR.FilePath))
                        {
                            video.Video2 = await FileRecord.GetNewFileRecordAsync(childMediaR.FilePath, childMediaR.Ext, subdir);
                        }
                    }
                }
                catch (Exception ignored)
                {
                    GetLogger().LogInformation(ignored, $"Couldnt download second video for {media.Id}");
                }
            }
            else
            {
                throw new Exception("DownloadKalturaVideo Failed + " + media.Id);
            }

            return video;
        }

        public async Task<Video> DownloadEchoVideo(string subdir, Media media)
        {
            Video video = new Video();
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            bool video1Success = false, video2Success = false; // Keep these - occasionally useful for debugging
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            var mediaResponse = await _rpcClient.PythonServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["videoUrl"].ToString(),
                AdditionalInfo = media.Playlist.JsonMetadata["downloadHeader"].ToString()
            });

            video1Success = FileRecord.IsValidFile(mediaResponse.FilePath);
            if (video1Success)
            {
                video.Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, subdir);
            }

            if (!string.IsNullOrEmpty(media.JsonMetadata["altVideoUrl"].ToString()))
            {

                var mediaResponse2 = await _rpcClient.PythonServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
                {
                    VideoUrl = media.JsonMetadata["altVideoUrl"].ToString(),
                    AdditionalInfo = media.Playlist.JsonMetadata["downloadHeader"].ToString()
                });
                video2Success = FileRecord.IsValidFile(mediaResponse2.FilePath);
                if (video2Success)
                {
                    video.Video2 = await FileRecord.GetNewFileRecordAsync(mediaResponse2.FilePath, mediaResponse.Ext, subdir);
                }
            }
            else
            {
                // As there is no file to download, it's "successfull"
                video2Success = true;
            }

            if (video1Success && video2Success)
            {
                return video;
            }
            else
            {
                // Deleting media is fine if download failed as we can get it back from the echo playlist.
                GetLogger().LogError("DownloadEchoVideo failed. mediaId {0}, removing Media record", media.Id);
                using (var context = CTDbContext.CreateDbContext())
                {
                    context.Medias.Remove(media);
                    context.SaveChanges();
                }

                return null;
            }
        }

        public async Task<Video> DownloadYoutubeVideo(string subdir, Media media)
        {

            var mediaResponse = await _rpcClient.PythonServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });
            // can be much later ie. be careful-  the database context may have been disposed.

            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {

                Video video = new Video
                {
                    Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, subdir)
                };
                return video;
            }
            else
            {
                // Deleting media is fine if download failed as we can get it back from the youtube playlist.
                GetLogger().LogError("DownloadYoutubeVideo failed. mediaId {0}, removing Media record", media.Id);
                using (var context = CTDbContext.CreateDbContext())
                {
                    context.Medias.Remove(media);
                    context.SaveChanges();
                }

                return null;
            }
        }

        public async Task<Video> DownloadLocalPlaylist(string subdir, Media media)
        {
            try
            {
                Video video = new Video();
                if (media.JsonMetadata.ContainsKey("video1Path"))
                {
                    var video1Path = media.JsonMetadata["video1Path"].ToString();

                    video.Video1 = await FileRecord.GetNewFileRecordAsync(video1Path, Path.GetExtension(video1Path), subdir);
                }
                if (media.JsonMetadata.ContainsKey("video2Path"))
                {
                    var video2Path = media.JsonMetadata["video2Path"].ToString();

                    video.Video2 = await FileRecord.GetNewFileRecordAsync(video2Path, Path.GetExtension(video2Path), subdir);
                }

                return video;
            }
            catch (Exception e)
            {
                GetLogger().LogError(e, "DownloadLocalPlaylist failed. mediaId {0}", media.Id);
                using (var context = CTDbContext.CreateDbContext())
                {
                    context.Medias.Remove(media);
                    context.SaveChanges();
                }
                return null;
            }
        }

        public async Task<Video> DownloadBoxVideo(string subdir, Media media)
        {
            try
            {
                var guid = Guid.NewGuid().ToString();
                var newPath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, guid + ".mp4");
                var client = await _box.GetBoxClientAsync();
                var stream = await client.FilesManager.DownloadAsync(media.UniqueMediaIdentifier);
                using (var fileStream = File.Create(newPath))
                {
                    stream.CopyTo(fileStream);
                }
                if (FileRecord.IsValidFile(newPath))
                {
                    Video video = new Video
                    {
                        Video1 = await FileRecord.GetNewFileRecordAsync(newPath, Path.GetExtension(newPath), subdir)
                    };
                    return video;
                }
                else
                {
                    // Deleting media is fine if download failed as we can get it back from the youtube playlist.
                    GetLogger().LogError("DownloadBoxVideo failed. mediaId {0}, removing Media record", media.Id);
                    using (var context = CTDbContext.CreateDbContext())
                    {
                        context.Medias.Remove(media);
                        context.SaveChanges();
                    }
                    return null;
                }
            }
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                GetLogger().LogError(e, "Box Token Failure in DownloadMediaTask.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
        }
    }
}
