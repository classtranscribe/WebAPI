using System;
using System.Collections.Generic;
using System.Text;
using Quartz;
using System.Collections.Specialized;
using Quartz.Impl;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase.Models;
using TaskEngine.Grpc;
using Newtonsoft.Json.Linq;

namespace TaskEngine.Tasks
{
    class DownloadPlaylistInfoTask : IRabbitMQTask<Playlist>, IJob
    {
        private RabbitMQ _rabbitMQ;
        private CTDbContext _context;
        private string queueName;
        private RpcClient _rpcClient;
        private DownloadMediaTask _downloadMediaTask;
        public DownloadPlaylistInfoTask() { }
        
        private void Init(RabbitMQ rabbitMQ, CTDbContext context)
        {
            _rabbitMQ = rabbitMQ;
            _context = context;
            queueName = RabbitMQ.QueueNameBuilder(TaskType.FetchPlaylistData, "_1");
        }
        public DownloadPlaylistInfoTask(RabbitMQ rabbitMQ, CTDbContext context, RpcClient rpcClient, DownloadMediaTask downloadMediaTask)
        {
            Init(rabbitMQ, context);
            _rpcClient = rpcClient;
            _downloadMediaTask = downloadMediaTask;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Init((RabbitMQ)context.MergedJobDataMap["rabbitMQ"], (CTDbContext)context.MergedJobDataMap["CTDbContext"]);
            queueName = RabbitMQ.QueueNameBuilder(TaskType.FetchPlaylistData, "_1");
            var period = DateTime.Now.AddMonths(-12);
            var playlists = await _context.Offerings.Where(o => o.Term.StartDate >= period).SelectMany(o => o.OfferingPlaylists).Select(op => op.Playlist).ToListAsync();
            playlists.ForEach(p => Publish(p));
        }

        public void Publish(Playlist playlist)
        {
            Console.WriteLine(playlist.Id);
            _rabbitMQ.PublishTask(queueName, playlist);
        }
        public async void Consume()
        {
            _rabbitMQ.ConsumeTask<Playlist>(queueName, async (p) => {
                List<Media> medias = new List<Media>();
                switch (p.SourceType)
                {
                    case SourceType.Echo360: medias = await GetEchoPlaylist(p); break;
                    case SourceType.Youtube: medias = await GetYoutubePlaylist(p); break;
                    case SourceType.Local: medias = await GetLocalPlaylist(p); break;
                }
                // medias.ForEach(m => _downloadMediaTask.Publish(m));
            });
        }

        public async Task<List<Media>> GetEchoPlaylist(Playlist playlist)
        {
            var jsonString = await _rpcClient.NodeServerClient.GetEchoPlaylistAsync(new CTGrpc.PlaylistRequest
            {
                Url = playlist.PlaylistIdentifier,
                Id = playlist.Id,
                Stream = 0
            });
            JArray jArray = JArray.Parse(jsonString.Json);
            List<Media> newMedia = new List<Media>();
            foreach(JObject jObject in jArray)
            {
                if(await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["mediaId"].ToString() && m.SourceType == playlist.SourceType).CountAsync() == 0)
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

        public async Task<List<Media>> GetYoutubePlaylist(Playlist playlist)
        {
            var jsonString = await _rpcClient.NodeServerClient.GetYoutubePlaylistAsync(new CTGrpc.PlaylistRequest
            {
                Url = playlist.PlaylistIdentifier,
                Id = playlist.Id,
            });
            JArray jArray = JArray.Parse(jsonString.Json);
            List<Media> newMedia = new List<Media>();
            foreach (JObject jObject in jArray)
            {
                if (await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["videoId"].ToString() && m.SourceType == playlist.SourceType).CountAsync() == 0)
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

        public async Task<List<Media>> GetLocalPlaylist(Playlist playlist)
        {
            return null;
        }

    }
}
