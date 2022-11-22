using Box.V2.Models;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    /// <summary>
    /// This task fetches all the info about a media under a given playlist
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DownloadPlaylistInfoTask : RabbitMQTask<string>
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

        protected override async Task OnConsume(string playlistId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, playlistId); // may throw AlreadyInProgress exception
            using (var _context = CTDbContext.CreateDbContext())
            {
                var playlist = await _context.Playlists.FindAsync(playlistId);
                int index = 0;
                try {
                    index = 1 + await _context.Medias.Where(m=> m.PlaylistId == playlist.Id).Select(m => m.Index).MaxAsync();
                } catch(Exception) {
                    // ignored (e.g. no media). Tried DefaultIfEmpty but that threw an Entity Framework runtime error; hence this slightly clunky exception implementation
                }
                GetLogger().LogInformation($"Playlist {playlistId}: Starting index = {index}");
                List<Media> medias = new List<Media>();
                switch (playlist.SourceType)
                {
                    case SourceType.Echo360: medias = await GetEchoPlaylist(playlist, index,  _context); break;
                    case SourceType.Youtube: medias = await GetYoutubePlaylist(playlist, index, _context); break;
                    case SourceType.Local: medias = await GetLocalPlaylist(playlist, _context); break;
                    case SourceType.Kaltura: medias = await GetKalturaPlaylist(playlist, index, _context); break;
                    case SourceType.Box: medias = await GetBoxPlaylist(playlist, index, _context); break;
                }
                
                GetLogger().LogInformation($"Playlist {playlistId}: {medias.Count} media listed.");
                if( medias.Count > 0) {
                    GetLogger().LogInformation($"Playlist {playlistId}: Publishing { medias.Count } download tasks");
                    medias.ForEach(m => _downloadMediaTask.Publish(m.Id));
                } else {
                    GetLogger().LogInformation($"Playlist {playlistId}: No new media to download");
                }

                 // reload a fresh playlist since it's been a while ...
                playlist = await _context.Playlists.FindAsync(playlistId);
                
                playlist.ListCheckedAt = DateTime.Now;
                // By updating a null value, means we can differentiate between an empty playlist and a new playlist
                if(medias.Count > 0 || playlist.ListUpdatedAt == null) {
                    playlist.ListUpdatedAt = playlist.ListCheckedAt;
                }
                await _context.SaveChangesAsync();
                
            }
        }

        public async Task<List<Media>> GetKalturaPlaylist(Playlist playlist, int index,  CTDbContext _context)
        {
            List<Media> newMedia = new List<Media>();
            CTGrpc.JsonString jsonString = null;
            try
            {
                jsonString = await _rpcClient.PythonServerClient.GetKalturaChannelEntriesRPCAsync(new CTGrpc.PlaylistRequest
                {
                    Url = playlist.PlaylistIdentifier
                });
            }
            catch (RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.InvalidArgument)
                {
                    if (e.Status.Detail == "INVALID_PLAYLIST_IDENTIFIER")
                    {
                        // Notification to Instructor.
                    }
                    GetLogger().LogError($"playlist=({playlist.Id}):{e.Message}");
                }
                return newMedia;
            }
            JArray jArray = JArray.Parse(jsonString.Json);

            var skipped = 0;

            foreach (JObject jObject in jArray)
            {
                // Check if there is a valid Id, and for the same playlist the same media does not exist.
                if (jObject["id"].ToString().Length > 0 &&
                    !await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["id"].ToString() &&
                //    m.SourceType == playlist.SourceType &&
                    m.PlaylistId == playlist.Id).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["id"].ToString(),
                        CreatedAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddSeconds(jObject["createdAt"].ToObject<int>()),
                        Index = index
                    });
                    index ++;
                } else
                {
                    skipped ++;
                }
            }
            newMedia.ForEach(m => m.Name = GetMediaName(m));
            GetLogger().LogInformation($"Kaltura playlist=({playlist.Id}): {skipped} skipped existing. Adding {newMedia.Count} new media items");

            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }

        public async Task<List<Media>> GetEchoPlaylist(Playlist playlist, int index, CTDbContext _context)
        {
            List<Media> newMedia = new List<Media>();
            CTGrpc.JsonString jsonString = null;
            try
            {
                jsonString = await _rpcClient.PythonServerClient.GetEchoPlaylistRPCAsync(new CTGrpc.PlaylistRequest
                {
                    Url = playlist.PlaylistIdentifier,
                    Stream = 0
                });
            }
            catch (RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.InvalidArgument)
                {
                    if (e.Status.Detail == "INVALID_PLAYLIST_IDENTIFIER")
                    {
                        // Notification to Instructor.
                    }
                    GetLogger().LogError(e.Message);
                }
                return newMedia;
            }
            JObject res = JObject.Parse(jsonString.Json);

            // Add DownloadHeader to playlist, required for downloading media.
            if (playlist.JsonMetadata.ContainsKey("downloadHeader"))
            {
                playlist.JsonMetadata["downloadHeader"] = res["downloadHeader"].ToString();
            }
            else
            {
                playlist.JsonMetadata.Add("downloadHeader", res["downloadHeader"].ToString());
            }

            JArray jArray = res["medias"] as JArray;

            
            foreach (JObject jObject in jArray)
            {
                // Check if there is a valid Id, and for the same playlist the same media does not exist.
                if (jObject["mediaId"].ToString().Length > 0 &&
                    !await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["mediaId"].ToString() &&
                    //m.SourceType == playlist.SourceType &&
                    m.PlaylistId == playlist.Id).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["mediaId"].ToString(),
                        CreatedAt = Convert.ToDateTime(jObject["createdAt"], CultureInfo.InvariantCulture),
                        Index = index
                    });
                    index ++;
                }
            }
            newMedia.ForEach(m => m.Name = GetMediaName(m));
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }

        public async Task<List<Media>> GetYoutubePlaylist(Playlist playlist, int index, CTDbContext _context)
        {
            List<Media> newMedia = new List<Media>();
            CTGrpc.JsonString jsonString = null;
            CTGrpc.JsonString metadata = new CTGrpc.JsonString
            {
                Json = playlist.JsonMetadata.HasValues ? playlist.JsonMetadata.ToString() : ""
            };
            try
            {
                jsonString = await _rpcClient.PythonServerClient.GetYoutubePlaylistRPCAsync(new CTGrpc.PlaylistRequest
                {
                    Url = playlist.PlaylistIdentifier,
                    Metadata = metadata
                });
            }
            catch (RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.InvalidArgument)
                {
                    if (e.Status.Detail == "INVALID_PLAYLIST_IDENTIFIER")
                    {
                        // Notification to Instructor.
                    }
                    GetLogger().LogError(e.Message);
                }
                return newMedia;
            }
            JArray jArray = JArray.Parse(jsonString.Json);
           
            
            int skipped = 0;
            GetLogger().LogInformation($"{playlist.Id}:Starting index {index}");
            foreach (JObject jObject in jArray)
            {
                // Check if there is a valid videoId, and for the same playlist the same media does not exist.
                if (jObject["videoId"].ToString().Length > 0 &&
                    !await _context.Medias.Where(m => m.UniqueMediaIdentifier == jObject["videoId"].ToString() &&
                //m.SourceType == playlist.SourceType &&
                m.PlaylistId == playlist.Id).AnyAsync())
                {
                    newMedia.Add(new Media
                    {
                        JsonMetadata = jObject,
                        SourceType = playlist.SourceType,
                        PlaylistId = playlist.Id,
                        UniqueMediaIdentifier = jObject["videoId"].ToString(),
                        CreatedAt = Convert.ToDateTime(jObject["createdAt"], CultureInfo.InvariantCulture),
                        Index = index
                    });
                    index ++;
                } else {
                    skipped ++;
                }
            }
            GetLogger().LogInformation($"Youtube playlist=({playlist.Id}): {skipped} skipped existing. Adding {newMedia.Count} new media items");

            newMedia.ForEach(m => m.Name = GetMediaName(m));
            await _context.Medias.AddRangeAsync(newMedia);
            await _context.SaveChangesAsync();
            return newMedia;
        }
      
        /// For recently directly/manually uploaded video files, 
        /// only the media entry for each uploaded video is created
        /// But we process them here AND in the DownloadMedia 
        /// (which ultimately sets the name)
        /// i.e. to better understand this code first also read the DownloadMedia code
        /// When run as part of the periodic update all playlists, this
        /// code should be a NOOP
        public async Task<List<Media>> GetLocalPlaylist(Playlist playlist, CTDbContext _context)
        {
            var medias = await _context.Medias.Where(m => m.Video == null && m.PlaylistId == playlist.Id).ToListAsync();
            medias.ForEach(m => m.Name = GetMediaName(m));
            
            await _context.SaveChangesAsync();
            return medias;
        }

        private async Task<List<Media>> GetBoxPlaylist(Playlist playlist, int index, CTDbContext _context)
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
                    // Check if there is a valid file.Id, and for the same playlist the same media does not exist.
                    if (file.Id.Length > 0 &&
                        !await _context.Medias.Where(m => m.UniqueMediaIdentifier == file.Id &&
                        m.SourceType == playlist.SourceType &&
                        m.PlaylistId == playlist.Id).AnyAsync())
                    {
                        newMedia.Add(new Media
                        {
                            SourceType = playlist.SourceType,
                            PlaylistId = playlist.Id,
                            UniqueMediaIdentifier = file.Id,
                            JsonMetadata = JObject.FromObject(file),
                            CreatedAt = file.CreatedAt ?? DateTime.Now,
                            Index = index
                        });
                        index ++;
                    }

                }
                newMedia.ForEach(m => m.Name = GetMediaName(m));
                await _context.Medias.AddRangeAsync(newMedia);
                await _context.SaveChangesAsync();
                return newMedia;
            }
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                GetLogger().LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
        }
    }
}
