using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class DownloadMediaTask : RabbitMQTask<Media>
    {
        private RpcClient _rpcClient;
        private ConvertVideoToWavTask _convertVideoToWavTask;

        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.DownloadMedia, "_1");
        }
        public DownloadMediaTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient, ConvertVideoToWavTask convertVideoToWavTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _convertVideoToWavTask = convertVideoToWavTask;
        }

        protected override async Task OnConsume(Media media)
        {

            Console.WriteLine("Consuming" + media);
            Video video = new Video();
            switch (media.SourceType)
            {
                case SourceType.Echo360: video = await DownloadEchoVideo(media); break;
                case SourceType.Youtube: video = await DownloadYoutubeVideo(media); break;
                case SourceType.Local: video = await DownloadLocalPlaylist(media); break;
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                var latestMedia = await _context.Medias.FindAsync(media.Id);
                // Don't add video if there are already videos for the given media.
                if (latestMedia.Video == null)
                {
                    // Check if Video already exists, if yes link it with this media item.
                    var file = _context.FileRecords.Where(f => f.Hash == video.Video1.Hash).ToList();
                    if (file.Count() == 0)
                    {
                        // Create new video Record
                        await _context.Videos.AddAsync(video);
                        await _context.SaveChangesAsync();
                        latestMedia.VideoId = video.Id;
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Downloaded:" + video);
                        _convertVideoToWavTask.Publish(video);
                    }
                    else
                    {
                        var existingVideos = await _context.Videos.Where(v => v.Video1Id == file.First().Id).ToListAsync();
                        // If file exists but video doesn't.
                        if(existingVideos.Count() == 0)
                        {
                            // Delete existing file Record
                            await file.First().DeleteFileRecordAsync(_context);

                            // Create new video Record
                            await _context.Videos.AddAsync(video);
                            await _context.SaveChangesAsync();
                            latestMedia.VideoId = video.Id;
                            await _context.SaveChangesAsync();
                            Console.WriteLine("Downloaded:" + video);
                            _convertVideoToWavTask.Publish(video);
                        }
                        // If video and file both exist.
                        else
                        {
                            
                            var existingVideo = await _context.Videos.Where(v => v.Video1Id == file.First().Id).FirstAsync();
                            latestMedia.VideoId = existingVideo.Id;
                            await _context.SaveChangesAsync();
                            Console.WriteLine("Existing Video:" + existingVideo);

                            // Deleting downloaded video as it's duplicate.
                            await video.DeleteVideoAsync(_context);
                        }
                    }
                }
            }
        }

        public async Task<Video> DownloadEchoVideo(Media media)
        {
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString(),
                AdditionalInfo = media.JsonMetadata["download_header"].ToString()
            });

            var mediaResponse2 = await _rpcClient.NodeServerClient.DownloadEchoVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["altVideoUrl"].ToString(),
                AdditionalInfo = media.JsonMetadata["download_header"].ToString()
            });
            Video video = null;
            if (mediaResponse.FilePath.Length > 0 && mediaResponse2.FilePath.Length > 0)
            {
                video = new Video
                {
                    Video1 = new FileRecord(mediaResponse.FilePath),
                    Video2 = new FileRecord(mediaResponse2.FilePath)
                };
            }
            else
            {
                throw new Exception("DownloadEchoVideo Failed + " + media.Id);
            }
            return video;
        }

        public async Task<Video> DownloadYoutubeVideo(Media media)
        {
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });

            Video video = null;
            if (mediaResponse.FilePath.Length > 0)
            {
                video = new Video
                {
                    Video1 = new FileRecord(mediaResponse.FilePath)
                };
            }
            else
            {
                throw new Exception("DownloadYoutubeVideo Failed + " + media.Id);
            }

            return video;
        }

        public async Task<Video> DownloadLocalPlaylist(Media media)
        {
            Video video = new Video();
            if (media.JsonMetadata.ContainsKey("video1Path"))
            {
                var video1Path = media.JsonMetadata["video1Path"].ToString();
                var newPath = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, Guid.NewGuid().ToString() + ".mp4");
                File.Copy(video1Path, newPath);
                video.Video1 = new FileRecord(newPath);
                
            }
            if (media.JsonMetadata.ContainsKey("video2Path"))
            {
                var video2Path = media.JsonMetadata["video2Path"].ToString();
                var newPath = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, Guid.NewGuid().ToString() + ".mp4");
                File.Copy(video2Path, newPath);
                video.Video1 = new FileRecord(newPath);
            }
            return video;
        }
    }
}
