using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class DownloadMediaTask : RabbitMQTask<Media>
    {
        private RpcClient _rpcClient;
        private ConvertVideoToWavTask _convertVideoToWavTask;

        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.DownloadMedia, "_1");
        }
        public DownloadMediaTask(RabbitMQ rabbitMQ, RpcClient rpcClient, ConvertVideoToWavTask convertVideoToWavTask)
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
                // jason
                case SourceType.Box: video = await DownloadBoxVideo(media); break;
            }
            using (var _context = CTDbContext.CreateDbContext())
            {
                await _context.Videos.AddAsync(video);
                await _context.SaveChangesAsync();
                Console.WriteLine("Downloaded:" + video);
                // _convertVideoToWavTask.Publish(video);
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

            Video video = new Video
            {
                Video1 = new FileRecord(mediaResponse.FilePath),
                Video2 = new FileRecord(mediaResponse2.FilePath),
                MediaId = media.Id
            };
            return video;
        }

        public async Task<Video> DownloadYoutubeVideo(Media media)
        {
            var mediaResponse = await _rpcClient.NodeServerClient.DownloadYoutubeVideoRPCAsync(new CTGrpc.MediaRequest
            {
                Id = media.Id,
                VideoUrl = media.JsonMetadata["videoUrl"].ToString()
            });

            Video video = new Video
            {
                Video1 = new FileRecord(mediaResponse.FilePath),
                MediaId = media.Id
            };
            return video;
        }

        public async Task<Video> DownloadLocalPlaylist(Media media)
        {
            Video video = new Video
            {
                MediaId = media.Id
            };
            if (media.JsonMetadata.ContainsKey("video1Path"))
            {
                var video1Path = media.JsonMetadata["video1Path"].ToString();
                var newPath = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, System.Guid.NewGuid().ToString() + ".mp4");
                File.Copy(video1Path, newPath);
                video.Video1 = new FileRecord(newPath);
                
            }
            if (media.JsonMetadata.ContainsKey("video2Path"))
            {
                var video2Path = media.JsonMetadata["video2Path"].ToString();
                var newPath = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, System.Guid.NewGuid().ToString() + ".mp4");
                File.Copy(video2Path, newPath);
                video.Video1 = new FileRecord(newPath);
            }
            return video;
        }

        // jason
        public async Task<Video> DownloadBoxVideo(Media media)
        {
            // send request using RestSharp
            var client = new RestClient($"https://uofi.app.box.com/2.0/files/{media.Id}/content");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Referer", $"https://uofi.app.box.com/2.0/files/{media.Id}/content");
            request.AddHeader("Cookie", "box_visitor_id=5da4f447d00911.72030283");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Postman-Token", "a02a657a-e03b-41b3-a747-6190a61f3bea,b759367e-377f-46a6-b96f-f3a78a984ad3");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("User-Agent", "PostmanRuntime/7.18.0");
            // TODO: figure out a way to refresh token
            request.AddHeader("Authorization", "Bearer MWsSoDokG53x0GhgPzIwsqeurPX9uYbG");

            // download the file to the local
            var path = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, System.Guid.NewGuid().ToString() + ".mp4");
            client.DownloadData(request).SaveAs(path);
            Video video = new Video
            {
                Video1 = new FileRecord(path),
                MediaId = media.Id
            };
            return video;
        }
    }
}
