using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class DownloadMediaTask : RabbitMQTask<JobObject<Media>>
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

        protected override async Task OnConsume(JobObject<Media> j)
        {
            Media media = j.Data;
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
            if (video == null || new FileInfo(video.Video1.Path).Length < 1000 || (video.Video2 != null && new FileInfo(video.Video2.Path).Length < 1000))
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
                        _convertVideoToWavTask.Publish(new JobObject<Video>
                        {
                            Data = video
                        });
                        _processVideoTask.Publish(new JobObject<Video>
                        {
                            Data = video
                        });
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
                            _convertVideoToWavTask.Publish(new JobObject<Video>
                            {
                                Data = video
                            });
                            _processVideoTask.Publish(new JobObject<Video>
                            {
                                Data = video
                            });
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
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadKalturaVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["downloadUrl"].ToString()
            });

            Video video;
            if (mediaResponse.FilePath.Length > 0)
            {
                video = new Video
                {
                    Video1 = new FileRecord(mediaResponse.FilePath)
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
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString(),
                AdditionalInfo = media.JsonMetadata["download_header"].ToString()
            });
            video1Success = mediaResponse.FilePath.Length > 0 &&
                File.Exists(mediaResponse.FilePath) &&
                new FileInfo(mediaResponse.FilePath).Length > 1000;
            if (video1Success)
            {
                video.Video1 = new FileRecord(mediaResponse.FilePath);
            }


            if (!string.IsNullOrEmpty(media.JsonMetadata["altVideoUrl"].ToString()))
            {

                var mediaResponse2 = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
                {
                    Id = media.Id,
                    VideoUrl = media.JsonMetadata["altVideoUrl"].ToString(),
                    AdditionalInfo = media.JsonMetadata["download_header"].ToString()
                });
                video2Success = mediaResponse2.FilePath.Length > 0 &&
                File.Exists(mediaResponse2.FilePath)
                 && new FileInfo(mediaResponse2.FilePath).Length > 1000;
                if (video2Success)
                {
                    video.Video2 = new FileRecord(mediaResponse2.FilePath);
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
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });

            if (mediaResponse.FilePath.Length > 0 &&
                File.Exists(mediaResponse.FilePath) && new FileInfo(mediaResponse.FilePath).Length > 1000)
            {
                Video video = new Video
                {
                    Video1 = new FileRecord(mediaResponse.FilePath)
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
                    var newPath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, Guid.NewGuid().ToString() + ".mp4");
                    File.Copy(video1Path, newPath);
                    video.Video1 = new FileRecord(newPath);

                }
                if (media.JsonMetadata.ContainsKey("video2Path"))
                {
                    var video2Path = media.JsonMetadata["video2Path"].ToString();
                    var newPath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, Guid.NewGuid().ToString() + ".mp4");
                    File.Copy(video2Path, newPath);
                    video.Video1 = new FileRecord(newPath);
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
                if (File.Exists(newPath) && new FileInfo(newPath).Length > 1000)
                {
                    Video video = new Video
                    {
                        Video1 = new FileRecord(newPath)
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
