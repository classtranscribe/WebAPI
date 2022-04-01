using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;


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
            registerTask(cleanup,mediaId); // may throw AlreadyInProgress exception

            Media media;
            using (var _context = CTDbContext.CreateDbContext())
            {
                media = await _context.Medias.Where(m => m.Id == mediaId)
                    .Include(m => m.Playlist).FirstAsync();
            }
            GetLogger().LogInformation($"Downloading media id=({media.Id}), UniqueMediaIdentifier={media.UniqueMediaIdentifier}");
            Video video = new Video();
            switch (media.SourceType)
            {
                case SourceType.Echo360: video = await DownloadEchoVideo(media); break;
                case SourceType.Youtube: video = await DownloadYoutubeVideo(media); break;
                case SourceType.Local: video = await DownloadLocalPlaylist(media); break;
                case SourceType.Kaltura: video = await DownloadKalturaVideo(media); break;
                case SourceType.Box: video = await DownloadBoxVideo(media); break;
            }
            // If no valid video1, or if a video2 object exists but not a valid file - fail the task.
            if (video == null || video.Video1 == null || !video.Video1.IsValidFile()
                || (video.Video2 != null && !video.Video2.IsValidFile()))
            {
                throw new Exception($"DownloadMediaTask failed for mediaId ({media.Id})");
            }

            using (var _context = CTDbContext.CreateDbContext())
            {
                var latestMedia = await _context.Medias.FindAsync(media.Id);
                GetLogger().LogInformation($"Media ({media.Id}): latestMedia.Video == null is {latestMedia.Video == null}");

                // Don't add video if there are already videos for the given media.
                if (latestMedia.Video == null)
                {
                    // Check if Video already exists, if yes link it with this media item.
                    var file = _context.FileRecords.Where(f => f.Hash == video.Video1.Hash).ToList();
                    if (!file.Any())
                    {
                        GetLogger().LogInformation($"Media ({media.Id}): FileRecord with matching hash NOT found");
                        // Create new video Record
                        await _context.Videos.AddAsync(video);
                        await _context.SaveChangesAsync();
                        latestMedia.VideoId = video.Id;
                        await _context.SaveChangesAsync();
                        GetLogger().LogInformation($"Downloaded (new) video.Id={video.Id}" );
                        _sceneDetectionTask.Publish(video.Id);
                        //_processVideoTask.Publish(video.Id); //TODO - re- add this code
                    }
                    else
                    {
                        GetLogger().LogInformation($"Media ({media.Id}): FileRecord with matching hash found");
                        var existingVideos = await _context.Videos.Where(v => v.Video1Id == file.First().Id).ToListAsync();
                        // If file exists but video doesn't.
                        if (!existingVideos.Any())
                        {
                            GetLogger().LogInformation($"Media ({media.Id}): FileRecord but no Video; deleting FileRecord. Creating Video entity");

                            // Delete existing file Record
                            await file.First().DeleteFileRecordAsync(_context);

                            // Create new video Record
                            await _context.Videos.AddAsync(video);
                            await _context.SaveChangesAsync();
                            latestMedia.VideoId = video.Id;
                            await _context.SaveChangesAsync();
                            GetLogger().LogInformation($"Media ({media.Id}):Downloaded (file existed) new video.Id={video.Id}");
                            //_transcriptionTask.Publish(video.Id);
                            _sceneDetectionTask.Publish(video.Id);
                            //_processVideoTask.Publish(video.Id); //TODO - re- add this code
                        }
                        // If video and file both exist.
                        else
                        {
                            GetLogger().LogInformation($"Media ({media.Id}): FileRecord and existing Video found; deleting newly downloaded video");

                            var existingVideo = await _context.Videos.Where(v => v.Video1Id == file.First().Id).FirstAsync();
                            latestMedia.VideoId = existingVideo.Id;
                            await _context.SaveChangesAsync();
                            GetLogger().LogInformation($"Media ({media.Id}): Existing Video found. (Deleting New) video.Id=" + video.Id);

                            // Deleting downloaded video as it's duplicate. Don't start scene detection
                            await video.DeleteVideoAsync(_context);
                        }
                    }
                }
            }
        }

        public async Task<Video> DownloadKalturaVideo(Media media)
        {
            var mediaResponse = await _rpcClient.PythonServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["downloadUrl"].ToString()
            });

            Video video;
            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {
                var co = GetRelatedCourseOffering(media);
                video = new Video
                {
                    Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, co)
                };
            }
            else
            {
                throw new Exception("DownloadKalturaVideo Failed + " + media.Id);
            }

            return video;
        }

        public async Task<Video> DownloadEchoVideo(Media media)
        {
            Video video = new Video();
            bool video1Success = false, video2Success = false;

            var mediaResponse = await _rpcClient.PythonServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["videoUrl"].ToString(),
                AdditionalInfo = media.Playlist.JsonMetadata["downloadHeader"].ToString()
            });

            video1Success = FileRecord.IsValidFile(mediaResponse.FilePath);
            if (video1Success)
            {
                var co = GetRelatedCourseOffering(media);
                video.Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, co);
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
                    var co = GetRelatedCourseOffering(media);
                    video.Video2 = await  FileRecord.GetNewFileRecordAsync(mediaResponse2.FilePath, mediaResponse.Ext, co);
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

        public async Task<Video> DownloadYoutubeVideo(Media media)
        {
            var mediaResponse = await _rpcClient.PythonServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });
            // can be much later ie. be careful-  the database context may have been disposed.

            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {
                
                using (var context = CTDbContext.CreateDbContext())
                {
                    // reload media so we can do lazy tranversal
                    media = await context.Medias.Where(m => m.Id == media.Id).FirstAsync();
                    var co = GetRelatedCourseOffering(media); // may use database, so need fresh instance of the media
                    Video video = new Video
                    {
                        Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, co)
                    };
                    return video;
                }                
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

        public async Task<Video> DownloadLocalPlaylist(Media media)
        {
            try
            {
                Video video = new Video();
                if (media.JsonMetadata.ContainsKey("video1Path"))
                {
                    var video1Path = media.JsonMetadata["video1Path"].ToString();
                    var co = GetRelatedCourseOffering(media);
                    video.Video1 = await FileRecord.GetNewFileRecordAsync(video1Path, Path.GetExtension(video1Path), co);
                }
                if (media.JsonMetadata.ContainsKey("video2Path"))
                {
                    var video2Path = media.JsonMetadata["video2Path"].ToString();
                    var co = GetRelatedCourseOffering(media);
                    video.Video2 = await  FileRecord.GetNewFileRecordAsync(video2Path, Path.GetExtension(video2Path), co);
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

        public async Task<Video> DownloadBoxVideo(Media media)
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
                    var co = GetRelatedCourseOffering(media);
                    Video video = new Video
                    {
                        Video1 = await FileRecord.GetNewFileRecordAsync(newPath, Path.GetExtension(newPath), co)
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
                GetLogger().LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
        }
    }
}
