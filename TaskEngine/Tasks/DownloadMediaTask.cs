using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTCommons.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task fetches downloads the video file for a given media.
    /// </summary>
    class DownloadMediaTask : RabbitMQTask<string>
    {
        private readonly RpcClient _rpcClient;
        private readonly ConvertVideoToWavTask _convertVideoToWavTask;
        private readonly ProcessVideoTask _processVideoTask;
        private readonly BoxAPI _box;
        private readonly SlackLogger _slack;

        public DownloadMediaTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient,
            ConvertVideoToWavTask convertVideoToWavTask, ProcessVideoTask processVideoTask, BoxAPI box,
            ILogger<DownloadMediaTask> logger, SlackLogger slack)
            : base(rabbitMQ, TaskType.DownloadMedia, logger)
        {
            _rpcClient = rpcClient;
            _convertVideoToWavTask = convertVideoToWavTask;
            _processVideoTask = processVideoTask;
            _box = box;
            _slack = slack;
        }

        protected override async Task OnConsume(string mediaId, TaskParameters taskParameters)
        {
            Media media;
            using (var _context = CTDbContext.CreateDbContext())
            {
                media = await _context.Medias.Where(m => m.Id == mediaId)
                    .Include(m => m.Playlist).FirstAsync();
            }
            _logger.LogInformation("Consuming" + media);
            Video video = new Video();
            switch (media.SourceType)
            {
                case SourceType.Echo360: video = await DownloadEchoVideo(media); break;
                case SourceType.Youtube: video = await DownloadYoutubeVideo(media); break;
                case SourceType.Local: video = DownloadLocalPlaylist(media); break;
                case SourceType.Kaltura: video = await DownloadKalturaVideo(media); break;
                case SourceType.Box: video = await DownloadBoxVideo(media); break;
            }
            // If no valid video1, or if a video2 object exists but not a valid file - fail the task.
            if (video == null || video.Video1 == null || !video.Video1.IsValidFile()
                || (video.Video2 != null && !video.Video2.IsValidFile()))
            {
                throw new Exception("DownloadMediaTask failed for mediaId " + media.Id);
            }

            using (var _context = CTDbContext.CreateDbContext())
            {
                var latestMedia = await _context.Medias.FindAsync(media.Id);
                // Don't add video if there are already videos for the given media.
                if (latestMedia.Video == null)
                {
                    // Check if Video already exists, if yes link it with this media item.
                    var file = _context.FileRecords.Where(f => f.Hash == video.Video1.Hash).ToList();
                    if (!file.Any())
                    {
                        // Create new video Record
                        await _context.Videos.AddAsync(video);
                        await _context.SaveChangesAsync();
                        latestMedia.VideoId = video.Id;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Downloaded:" + video);
                        _convertVideoToWavTask.Publish(video.Id);
                        _processVideoTask.Publish(video.Id);
                    }
                    else
                    {
                        var existingVideos = await _context.Videos.Where(v => v.Video1Id == file.First().Id).ToListAsync();
                        // If file exists but video doesn't.
                        if (!existingVideos.Any())
                        {
                            // Delete existing file Record
                            await file.First().DeleteFileRecordAsync(_context);

                            // Create new video Record
                            await _context.Videos.AddAsync(video);
                            await _context.SaveChangesAsync();
                            latestMedia.VideoId = video.Id;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Downloaded:" + video);
                            _convertVideoToWavTask.Publish(video.Id);
                            _processVideoTask.Publish(video.Id);
                        }
                        // If video and file both exist.
                        else
                        {

                            var existingVideo = await _context.Videos.Where(v => v.Video1Id == file.First().Id).FirstAsync();
                            latestMedia.VideoId = existingVideo.Id;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Existing Video:" + existingVideo);

                            // Deleting downloaded video as it's duplicate.
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
                video = new Video
                {
                    Video1 = FileRecord.GetNewFileRecord(mediaResponse.FilePath, mediaResponse.Ext)
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
                video.Video1 = FileRecord.GetNewFileRecord(mediaResponse.FilePath, mediaResponse.Ext);
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
                    video.Video2 = FileRecord.GetNewFileRecord(mediaResponse2.FilePath, mediaResponse.Ext);
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
                _logger.LogError("DownloadEchoVideo failed. mediaId {0}, removing Media record", media.Id);
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

            if (FileRecord.IsValidFile(mediaResponse.FilePath))
            {
                Video video = new Video
                {
                    Video1 = FileRecord.GetNewFileRecord(mediaResponse.FilePath, mediaResponse.Ext)
                };
                return video;
            }
            else
            {
                // Deleting media is fine if download failed as we can get it back from the youtube playlist.
                _logger.LogError("DownloadYoutubeVideo failed. mediaId {0}, removing Media record", media.Id);
                using (var context = CTDbContext.CreateDbContext())
                {
                    context.Medias.Remove(media);
                    context.SaveChanges();
                }

                return null;
            }
        }

        public Video DownloadLocalPlaylist(Media media)
        {
            try
            {
                Video video = new Video();
                if (media.JsonMetadata.ContainsKey("video1Path"))
                {
                    var video1Path = media.JsonMetadata["video1Path"].ToString();
                    video.Video1 = FileRecord.GetNewFileRecord(video1Path, Path.GetExtension(video1Path));
                }
                if (media.JsonMetadata.ContainsKey("video2Path"))
                {
                    var video2Path = media.JsonMetadata["video2Path"].ToString();
                    video.Video2 = FileRecord.GetNewFileRecord(video2Path, Path.GetExtension(video2Path));
                }

                return video;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DownloadLocalPlaylist failed. mediaId {0}", media.Id);
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
                    Video video = new Video
                    {
                        Video1 = FileRecord.GetNewFileRecord(newPath, Path.GetExtension(newPath))
                    };
                    return video;
                }
                else
                {
                    // Deleting media is fine if download failed as we can get it back from the youtube playlist.
                    _logger.LogError("DownloadBoxVideo failed. mediaId {0}, removing Media record", media.Id);
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
                _logger.LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
        }
    }
}
