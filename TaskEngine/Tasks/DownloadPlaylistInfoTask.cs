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
                        UniqueMediaIdentifier = jObject["mediaId"].ToString()
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

    }
}
