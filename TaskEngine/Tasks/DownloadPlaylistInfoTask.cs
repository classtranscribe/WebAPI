using Box.V2.Models;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskEngine.Grpc;
using static ClassTranscribeDatabase.CommonUtils;

namespace TaskEngine.Tasks
{
    class DownloadPlaylistInfoTask : RabbitMQTask<Playlist>
    {
        private readonly RpcClient _rpcClient;
        private readonly DownloadMediaTask _downloadMediaTask;
        private readonly BoxAPI _box;
        private readonly SlackLogger _slack;

        public DownloadPlaylistInfoTask(RabbitMQConnection rabbitMQ, RpcClient rpcClient,
            DownloadMediaTask downloadMediaTask, BoxAPI box,
            ILogger<DownloadPlaylistInfoTask> logger, SlackLogger slack)
            : base(rabbitMQ, TaskType.DownloadPlaylistInfo, logger)
        {
            _rpcClient = rpcClient;
            _downloadMediaTask = downloadMediaTask;
            _box = box;
            _slack = slack;
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
                    case SourceType.Kaltura: medias = await GetKalturaPlaylist(p, _context); break;
                    case SourceType.Box: medias = await GetBoxPlaylist(p, _context); break;
                }
                medias.ForEach(m => _downloadMediaTask.Publish(m));
            }
        }

        public async Task<List<Media>> GetKalturaPlaylist(Playlist playlist, CTDbContext _context)
        {
            var jsonString = await _rpcClient.PythonServerClient.GetKalturaChannelEntriesRPCAsync(new CTGrpc.PlaylistRequest
            {
                Url = playlist.PlaylistIdentifier,
                Id = playlist.Id
            });
            JArray jArray = JArray.Parse(jsonString.Json);
            List<Media> newMedia = new List<Media>();
            foreach (JObject jObject in jArray)
            {
                if (jObject["id"].ToString().Length > 0 && !await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["id"].ToString() && m.SourceType == playlist.SourceType).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["id"].ToString(),
                        CreatedAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddSeconds(jObject["createdAt"].ToObject<int>())
                    });
                }
            }
            newMedia.ForEach(m => m.Name = GetMediaName(m));
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
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
                if (jObject["mediaId"].ToString().Length > 0 && !await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["mediaId"].ToString() && m.SourceType == playlist.SourceType).AnyAsync())
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
            newMedia.ForEach(m => m.Name = GetMediaName(m));
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
            newMedia.ForEach(m => m.Name = GetMediaName(m));
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }

        public async Task<List<Media>> GetLocalPlaylist(Playlist playlist, CTDbContext _context)
        {
            var medias = await _context.Medias.Where(m => m.Video == null && m.PlaylistId == playlist.Id).ToListAsync();
            medias.ForEach(m => m.Name = GetMediaName(m));
            await _context.SaveChangesAsync();
            return medias;
        }

        private async Task<List<Media>> GetBoxPlaylist(Playlist playlist, CTDbContext _context)
        {
            try
            {
                var client = await _box.GetBoxClientAsync();
                /// Try to refresh the access token
                var folderInfo = await client.FoldersManager.GetInformationAsync(playlist.PlaylistIdentifier);
                playlist.JsonMetadata = JObject.FromObject(folderInfo);

                var items = (await client.FoldersManager.GetFolderItemsAsync(playlist.PlaylistIdentifier, 500)).Entries.OfType<BoxFile>();
                // Process only files with an mp4 extension.
                items = items.Where(i => i.Name.Substring(i.Name.LastIndexOf(".") + 1) == "mp4").ToList();
                List<Media> newMedia = new List<Media>();

                foreach (var item in items)
                {
                    var file = await client.FilesManager.GetInformationAsync(item.Id);
                    if (file.Id.Length > 0 && !await _context.Medias.Where(m => m.UniqueMediaIdentifier == file.Id && m.SourceType == playlist.SourceType).AnyAsync())
                    {
                        newMedia.Add(new Media
                        {
                            SourceType = playlist.SourceType,
                            PlaylistId = playlist.Id,
                            UniqueMediaIdentifier = file.Id,
                            JsonMetadata = JObject.FromObject(file),
                            CreatedAt = file.CreatedAt ?? DateTime.Now
                        });
                    }

                }
                newMedia.ForEach(m => m.Name = GetMediaName(m));
                await _context.Medias.AddRangeAsync(newMedia);
                await _context.SaveChangesAsync();
                return newMedia;
            }
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                _logger.LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw e;
            }
        }

        public static string GetMediaName(Media media)
        {
            string name;
            switch (media.SourceType)
            {
                case SourceType.Echo360:
                    string lessonName;
                    if (media.JsonMetadata.ContainsKey("lessonName"))
                    {
                        lessonName = media.JsonMetadata["lessonName"].ToString();
                    }
                    else
                    {
                        lessonName = "Untitled";
                    }

                    string title;
                    if (media.JsonMetadata.ContainsKey("title"))
                    {
                        title = media.JsonMetadata["title"].ToString();
                    }
                    else
                    {
                        title = null;
                    }

                    DateTime createdAt;
                    if (media.JsonMetadata.ContainsKey("createdAt"))
                    {
                        createdAt = Convert.ToDateTime(media.JsonMetadata["createdAt"].ToString());
                    }
                    else
                    {
                        createdAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    }

                    if (title != null)
                    {
                        name = title;
                    }
                    else
                    {
                        name = $"{lessonName} {createdAt.ToString("MMMM dd, yyyy")}";
                    }
                    break;
                case SourceType.Youtube:
                    if (media.JsonMetadata.ContainsKey("title") && media.JsonMetadata["title"].ToString().Length > 0)
                    {
                        name = media.JsonMetadata["title"].ToString();
                    }
                    else
                    {
                        name = "Untitled";
                    }
                    break;
                case SourceType.Local:
                    string fileName;
                    if (media.JsonMetadata.ContainsKey("filename"))
                    {
                        fileName = media.JsonMetadata["filename"].ToString();
                    }
                    else
                    {

                        JObject tempObj = JObject.Parse(media.JsonMetadata["video1"].ToString());
                        if (tempObj.ContainsKey("FileName"))
                        {
                            fileName = tempObj["FileName"].ToString();
                        }
                        else
                        {
                            fileName = "Untitled";
                        }

                    }
                    name = fileName.Replace(".mp4", "");
                    break;
                case SourceType.Kaltura:
                    string temp;
                    if (media.JsonMetadata.ContainsKey("name"))
                    {
                        temp = media.JsonMetadata["name"].ToString();
                    }
                    else
                    {
                        temp = "Untitled";
                    }

                    if (media.JsonMetadata.ContainsKey("createdAt"))
                    {

                        createdAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddSeconds(media.JsonMetadata["createdAt"].ToObject<int>());
                    }
                    else
                    {
                        createdAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    }
                    name = $"{temp} {createdAt.ToString("MMMM dd, yyyy")}";
                    break;
                case SourceType.Box:
                    if (media.JsonMetadata.ContainsKey("name"))
                    {
                        name = media.JsonMetadata["name"].ToString();
                    }
                    else
                    {
                        name = "Untitled";
                    }
                    break;
                default:
                    name = "Untitled";
                    break;
            }
            return name;
        }
    }
}
