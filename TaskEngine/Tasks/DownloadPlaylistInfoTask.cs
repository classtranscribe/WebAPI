using System;
using System.Collections.Generic;
using Quartz;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase.Models;
using TaskEngine.Grpc;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace TaskEngine.Tasks
{
    class DownloadPlaylistInfoTask : RabbitMQTask<Playlist>, IJob
    {
        private RpcClient _rpcClient;
        private DownloadMediaTask _downloadMediaTask;
        public DownloadPlaylistInfoTask() { }

        private void Init(RabbitMQ rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.FetchPlaylistData, "_1");
        }
        public DownloadPlaylistInfoTask(RabbitMQ rabbitMQ, RpcClient rpcClient, DownloadMediaTask downloadMediaTask)
        {
            Init(rabbitMQ);
            _rpcClient = rpcClient;
            _downloadMediaTask = downloadMediaTask;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {

                Init((RabbitMQ)context.MergedJobDataMap["rabbitMQ"]);
                var period = DateTime.Now.AddMonths(-12);
                var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.Playlists).ToListAsync();
                // TEMPORARY CHANGE
                playlists.ForEach(p => Publish(p));
            }
        }

        protected override async Task OnConsume(Playlist p)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                List<Media> medias = new List<Media>();
                switch (p.SourceType)
                {
                    case SourceType.Echo360: medias = await GetEchoPlaylist(p, _context); break;
                    case SourceType.Youtube: medias = await GetYoutubePlaylist(p, _context); break;
                    case SourceType.Local: medias = await GetLocalPlaylist(p, _context); break;
                    // jason
                    case SourceType.Box: medias = await GetBoxPlaylist(p, _context); break;
                }
                medias.ForEach(m => _downloadMediaTask.Publish(m));
            }
        }

        public async Task<List<Media>> GetEchoPlaylist(Playlist playlist, CTDbContext _context)
        {
            var jsonString = await _rpcClient.NodeServerClient.GetEchoPlaylistRPCAsync(new CTGrpc.PlaylistRequest
            {
                Url = playlist.PlaylistIdentifier,
                Id = playlist.Id,
                Stream = 0
            });
            JArray jArray = JArray.Parse(jsonString.Json);
            List<Media> newMedia = new List<Media>();
            foreach (JObject jObject in jArray)
            {
                if (!await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["mediaId"].ToString() && m.SourceType == playlist.SourceType).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["mediaId"].ToString(),
                        CreatedAt = Convert.ToDateTime(jObject["createdAt"])
                    });
                }
            }
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }

        public async Task<List<Media>> GetYoutubePlaylist(Playlist playlist, CTDbContext _context)
        {
            var jsonString = await _rpcClient.NodeServerClient.GetYoutubePlaylistRPCAsync(new CTGrpc.PlaylistRequest
            {
                Url = playlist.PlaylistIdentifier,
                Id = playlist.Id,
            });
            JArray jArray = JArray.Parse(jsonString.Json);
            List<Media> newMedia = new List<Media>();
            foreach (JObject jObject in jArray)
            {
                if (!await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["videoId"].ToString() && m.SourceType == playlist.SourceType).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["videoId"].ToString()
                    });
                }
            }
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }

        public async Task<List<Media>> GetLocalPlaylist(Playlist playlist, CTDbContext _context)
        {
            return _context.Medias.Where(m => m.Videos.Count == 0 && m.PlaylistId == playlist.Id).ToList();
        }

        // jason
        public async Task<List<Media>> GetBoxPlaylist(Playlist playlist, CTDbContext _context)
        {
            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "refresh.json");
            JObject refresh_json = JObject.Parse(File.ReadAllText(@path));
            String access_token = (String)refresh_json.SelectToken("access_token");

            while (true)
            {
                // send request using RestSharp
                var client = new RestClient("https://uofi.app.box.com/2.0/folders/88965021711");
                var request = new RestRequest(Method.GET);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Connection", "keep-alive");
                request.AddHeader("Cookie", "box_visitor_id=5da4f447d00911.72030283");
                request.AddHeader("Accept-Encoding", "gzip, deflate");
                request.AddHeader("Host", "uofi.app.box.com");
                request.AddHeader("Postman-Token", "8e63d4fe-f3da-48ef-b756-a4baecaa4266,af322b88-474b-4778-acaf-363622702dc5");
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
                    // pick entries to get array of file
                    JObject Content = JObject.Parse(response.Content);
                    JArray jArray = (JArray)Content.SelectToken("item_collection").SelectToken("entries");
                    List<Media> newMedia = new List<Media>();

                    foreach (JObject jObject in jArray.Children())
                    {
                        newMedia.Add(new Media
                        {
                            JsonMetadata = jObject,
                            SourceType = playlist.SourceType,
                            PlaylistId = playlist.Id,
                            UniqueMediaIdentifier = jObject["sha1"].ToString(),
                            // Id used to download specific file
                            Id = jObject["id"].ToString()
                        });
                    }
                    await _context.Medias.AddRangeAsync(newMedia);
                    await _context.SaveChangesAsync();
                    return newMedia;
                }
            }
        }
    }
}
