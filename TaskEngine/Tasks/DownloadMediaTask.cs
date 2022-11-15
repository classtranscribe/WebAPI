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
            string subdir;
            using (var _context = CTDbContext.CreateDbContext())
            {
                media = await _context.Medias.Where(m => m.Id == mediaId)
                    .Include(m => m.Playlist).FirstAsync();
                GetLogger().LogInformation($"Downloading media id=({media.Id}), UniqueMediaIdentifier={media.UniqueMediaIdentifier}");
                subdir = ToCourseOfferingSubDirectory(_context, media); // e.g. "/data/2203-abcd"
            }
            Video video = new Video();
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
                            await _context.SaveChangesAsync(); // now video has an Id

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
                            if( video.Video2 != null && existingVideo.Video2 == null) {
                                var v2 = video.Video2;
                                await _context.FileRecords.AddAsync(v2);
                                await _context.SaveChangesAsync(); // now v2 has an Id
                                // Special case;
                                // add video2 to existing video
                                GetLogger().LogInformation($"Adding video2 ({v2.Id}) to video ({existingVideo.Id})");

                                existingVideo.Video2Id =  v2.Id;
                                video.Video2= null; // otherwise DeleteVideo will delete the file
                            }
                            await _context.SaveChangesAsync();
                            GetLogger().LogInformation($"Media ({media.Id}): Existing Video found. (Deleting New) video.Id=" + video.Id);

                            // Deleting downloaded video as it's duplicate. Don't start scene detection
                            await video.DeleteVideoAsync(_context);
                        }
                    }
                }
            }
        }

        public async Task<Video> DownloadKalturaVideo(string subdir, Media media)
        {
            var mediaResponse = await _rpcClient.PythonServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
            {
                VideoUrl = media.JsonMetadata["downloadUrl"].ToString()
            });

            Video video;
            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {
                video = new Video
                {
                    Video1 = await FileRecord.GetNewFileRecordAsync(mediaResponse.FilePath, mediaResponse.Ext, subdir)
                };
                try {
                    if ( media.JsonMetadata["child"]!= null && media.JsonMetadata["child"]["downloadUrl"] != null) {
                        GetLogger().LogInformation($"Media ({media.Id}): Downloading child video");

                        var childMediaR = await _rpcClient.PythonServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
                        {
                            VideoUrl = media.JsonMetadata["child"]["downloadUrl"].ToString()
                        });
                        if(FileRecord.IsValidFile(childMediaR.FilePath)) {
                            video.Video2 =  await FileRecord.GetNewFileRecordAsync(childMediaR.FilePath, childMediaR.Ext, subdir);
                        }
                    }
                } catch(Exception  ignored) {
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
            bool video1Success = false, video2Success = false;

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
                    video.Video2 = await  FileRecord.GetNewFileRecordAsync(mediaResponse2.FilePath, mediaResponse.Ext, subdir);
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
                    
                    video.Video2 = await  FileRecord.GetNewFileRecordAsync(video2Path, Path.GetExtension(video2Path), subdir);
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
