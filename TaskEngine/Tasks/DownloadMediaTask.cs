using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using RestSharp;
using RestSharp.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

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
                // jason
                case SourceType.Box: video = await DownloadBoxVideo(media); break;
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
                        await _context.Videos.AddAsync(video);
                        await _context.SaveChangesAsync();
                        latestMedia.VideoId = video.Id;
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Downloaded:" + video);
                        _convertVideoToWavTask.Publish(video);
                    }
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

        // jason
        public async Task<Video> DownloadBoxVideo(Media media)
        {
            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "refresh.json");
            JObject refresh_json = JObject.Parse(File.ReadAllText(@path));
            String access_token = (String)refresh_json.SelectToken("access_token");

            while (true)
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
                request.AddHeader("Authorization", $"Bearer {access_token}");
                IRestResponse response = client.Execute(request);
                HttpStatusCode statusCode = response.StatusCode;
                int numericStatusCode = (int)statusCode;
                if (numericStatusCode == 401)
                {
                    String refresh_token = (String)refresh_json.SelectToken("refresh_token");
                    // get access token using refresh token
                    var refresh_client = new RestClient("https://api.box.com/oauth2/token");
                    var refresh_request = new RestRequest(Method.POST);
                    refresh_request.AddHeader("cache-control", "no-cache");
                    refresh_request.AddHeader("Connection", "keep-alive");
                    refresh_request.AddHeader("Cookie", "box_visitor_id=5da4f447d00911.72030283; site_preference=desktop");
                    refresh_request.AddHeader("Content-Length", "193");
                    refresh_request.AddHeader("Accept-Encoding", "gzip, deflate");
                    refresh_request.AddHeader("Host", "api.box.com");
                    refresh_request.AddHeader("Postman-Token", "35cc41df-37bc-475b-a330-787ee4dd5647,f83f9780-407a-4d36-96a3-645699f1ae44");
                    refresh_request.AddHeader("Cache-Control", "no-cache");
                    refresh_request.AddHeader("Accept", "*/*");
                    refresh_request.AddHeader("User-Agent", "PostmanRuntime/7.18.0");
                    refresh_request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    refresh_request.AddParameter("undefined", $"grant_type=refresh_token&client_id=hyqhskag8e4mko8for8dxjdumu37lpyd&client_secret=Byjx7nDHwLgnH8KPF0BkdVXoQOJXpCtd&refresh_token={refresh_token}", ParameterType.RequestBody);
                    IRestResponse refresh_response = refresh_client.Execute(refresh_request);
                    JObject refresh_content = JObject.Parse(refresh_response.Content);

                    // save the refresh token
                    using (StreamWriter file = File.CreateText(@path))
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        refresh_content.WriteTo(writer);
                    }

                    access_token = (String)refresh_content.SelectToken("access_token");
                } else
                {
                    // download the file to the local
                    var file_path = System.IO.Path.Combine(Globals.appSettings.DATA_DIRECTORY, System.Guid.NewGuid().ToString() + ".mp4");
                    client.DownloadData(request).SaveAs(file_path);
                    Video video = new Video
                    {
                        Video1 = new FileRecord(file_path),
                        MediaId = media.Id
                    };
                    return video;
                }
            }
        }
    }
}
