using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly WakeDownloader _wakeDownloader;
        private readonly UserUtils _userUtils;

        public PlaylistsController(IAuthorizationService authorizationService, 
            WakeDownloader wakeDownloader, 
            CTDbContext context, 
            UserUtils userUtils,
            ILogger<PlaylistsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
            _userUtils = userUtils;
        }

        // GET: api/Playlists
        /// <summary>
        /// Gets the Playlist for videoId
        /// </summary>
        [HttpGet("ByVideo/{videoId}")]
        public async Task<ActionResult<PlaylistDTO>> GetPlaylistsByVideoId(string videoId)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                return BadRequest();
            }

            var medias = await _context.Medias
                .Where(p => p.VideoId == videoId)
                .OrderBy(p => p.CreatedAt).ToListAsync();
            
            if (medias == null)
            {
                return BadRequest();
            }

            var temp = await _context.Playlists
                .Where(p => p.Id == medias.FirstOrDefault().PlaylistId)
                .OrderBy(p => p.Index)
                .ThenBy(p => p.CreatedAt).ToListAsync();
            var playlist = temp.Select(p => new PlaylistDTO
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                SourceType = p.SourceType,
                OfferingId = p.OfferingId,
                Name = p.Name,
                Index = p.Index,
                PlaylistIdentifier = p.PlaylistIdentifier,
                PublishStatus = p.PublishStatus,
                ListCheckedAt = p.ListCheckedAt,
                ListUpdatedAt = p.ListUpdatedAt,
                Options = p.GetOptionsAsJson()
            }).ToList().FirstOrDefault();
            return playlist;
        }

        // GET: api/Playlists
        /// <summary>
        /// Gets all Playlists for offeringId
        /// </summary>
        [HttpGet("ByOffering/{offeringId}")]
        public async Task<ActionResult<IEnumerable<PlaylistDTO>>> GetPlaylists(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized(new { Reason = "Insufficient Permission", offering.AccessType });
            }
            var temp = await _context.Playlists
                .Where(p => p.OfferingId == offeringId)
                .OrderBy(p => p.Index)
                .ThenBy(p => p.CreatedAt).ToListAsync();
            var playlists = temp.Select(p => new PlaylistDTO
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                SourceType = p.SourceType,
                OfferingId = p.OfferingId,
                Name = p.Name,
                Index = p.Index,
                PlaylistIdentifier = p.PlaylistIdentifier,
                PublishStatus = p.PublishStatus,
                ListCheckedAt = p.ListCheckedAt,
                ListUpdatedAt = p.ListUpdatedAt,
                Options = p.GetOptionsAsJson()
            }).ToList();
            return playlists;
        }

        // GET: api/Playlists
        /// <summary>
        /// Gets all Playlists for offeringId
        /// </summary>
        [HttpGet("ByOffering2/{offeringId}")]
        public async Task<ActionResult<IEnumerable<PlaylistDTO>>> GetPlaylists2(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized(new { Reason = "Insufficient Permission", offering.AccessType });
            }

            var playLists = await _context.Playlists
                .Where(p => p.OfferingId == offeringId)
                .OrderBy(p => p.Index)
                .ThenBy(p => p.CreatedAt).ToListAsync();

            var hideRoomVideos = new Dictionary<string,bool>(); 
            foreach (var p in playLists) {
                var restrict = (bool?) p.GetOptionsAsJson()[ "restrictRoomStream"] ?? false;
                hideRoomVideos.Add(p.Id, restrict);
            };
            var playlistDTOs = playLists.Select(p => new PlaylistDTO
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                SourceType = p.SourceType,
                OfferingId = p.OfferingId,
                Name = p.Name,
                Index = p.Index,
                PlaylistIdentifier = p.PlaylistIdentifier,
                PublishStatus = p.PublishStatus,
                ListCheckedAt = p.ListCheckedAt,
                ListUpdatedAt = p.ListUpdatedAt,
                Options = p.GetOptionsAsJson(),
                Medias = p.Medias.Where(m => m.Video != null).Select(m => new MediaDTO
                {
                    Id = m.Id,
                    Index = m.Index,
                    Name = m.Name,
                    JsonMetadata = m.JsonMetadata,
                    CreatedAt = m.CreatedAt,
                    SceneDetectReady = m.Video.HasSceneObjectData(),
                    Ready = m.Video != null && "NoError" == m.Video.TranscriptionStatus ,
                    SourceType = m.SourceType,
                    Duration = m.Video?.Duration,
                    PublishStatus = m.PublishStatus,
                    Options = m.GetOptionsAsJson(),
                    Video = new VideoDTO
                    {
                        Id = m.Video.Id,
                        Video1Path = m.Video.ProcessedVideo1?.Path != null ? m.Video.ProcessedVideo1.Path : m.Video.Video1?.Path,
                        Video2Path = hideRoomVideos[p.Id] ?  null : ( m.Video.ProcessedVideo2?.Path != null ? m.Video.ProcessedVideo2.Path : m.Video.Video2?.Path),
                        ASLPath = m.Video.ProcessedASLVideo?.Path != null ? m.Video.ProcessedASLVideo.Path : m.Video.ASLVideo?.Path,
                        TaskLog = m.Video.TaskLog
                    },
                    Transcriptions = m.Video.Transcriptions.Select(t => new TranscriptionDTO
                    {
                        Id = t.Id,
                        Path = t.File?.Path,
                        SrtPath = t.SrtFile?.Path,
                        Language = t.Language,
                        Label = t.Label,
                        SourceLabel = t.SourceLabel,
                        TranscriptionType = (int) t.TranscriptionType

                    }).ToList()
                }).ToList()
            }).ToList();
            return playlistDTOs;
        }

        [HttpGet("SearchForMedia/{offeringId}/{query}")]
        public async Task<ActionResult<IEnumerable<MediaSearchDTO>>> SearchForMedia(string offeringId, string query)
        {
            var mediaSearches = await _context.Medias.Where(m => m.Playlist.OfferingId == offeringId &&
            EF.Functions.ToTsVector("english", m.Name).Matches(query))
                .Select(m => new MediaSearchDTO
                {
                    Name = m.Name,
                    MediaId = m.Id,
                    PlaylistName = m.Playlist.Name,
                    PlaylistId = m.PlaylistId,
                    PublishStatus = m.PublishStatus
                })
                .Take(50)
                .ToListAsync();

            return mediaSearches;
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistDTO>> GetPlaylist(string id)
        {
            var p = await _context.Playlists.FindAsync(id);
            var user = await _userUtils.GetUser(User);
            if (p == null)
            {
                return NotFound();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, p.Offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            // Single Database Query to get videos, transcriptions
            var mediaList = await _context.Medias.Include(m=>m.Video).ThenInclude(v=>v.Transcriptions).Where(m=> m.PlaylistId == id).OrderBy(m => m.Index).ThenBy(m => m.CreatedAt).ToListAsync();
            
            var mediaIds = mediaList.Select(m=>m.Id).ToArray();

            // user is null for unit tests
            var partialWatchHistories = user !=null ? await _context.WatchHistories.Where(w => w.ApplicationUserId == user.Id && mediaIds.Contains(w.MediaId)).ToListAsync() : null;
            // In memory transformation into DTO resut

            var hideRoomVideos = new Dictionary<string,bool>(); 
            var restrict = (bool?) p.GetOptionsAsJson()[ "restrictRoomStream"] ?? false;
            
            List<MediaDTO> mediasDTO = mediaList.Select(m => new MediaDTO
                {
                    Id = m.Id,
                    Index = m.Index,
                    Name = m.Name,
                    PlaylistId = m.PlaylistId,
                    CreatedAt = m.CreatedAt,
                    JsonMetadata = m.JsonMetadata,
                    SourceType = m.SourceType,
                    Duration = m.Video?.Duration,
                    PublishStatus = m.PublishStatus,
                    Options = m.GetOptionsAsJson(),
                    SceneDetectReady = m.Video != null && m.Video.HasSceneObjectData(),
                    Ready = m.Video != null && "NoError" == m.Video.TranscriptionStatus ,
                    Video = m.Video == null ? null : new VideoDTO
                   {
                        Id = m.Video.Id,
                        Video1Path = m.Video.ProcessedVideo1?.Path != null ? m.Video.ProcessedVideo1.Path : m.Video.Video1?.Path,
                        Video2Path = restrict ?  null : ( m.Video.ProcessedVideo2?.Path != null ? m.Video.ProcessedVideo2.Path : m.Video.Video2?.Path),
                        ASLPath = m.Video.ProcessedASLVideo?.Path != null ? m.Video.ProcessedASLVideo.Path : m.Video.ASLVideo?.Path,
                        TaskLog = m.Video.TaskLog
                    },
                    Transcriptions = m.Video?.Transcriptions.Select(t => new TranscriptionDTO
                    {
                        Id = t.Id,
                        Path = t.File?.Path,
                        SrtPath = t.SrtFile?.Path,
                        Language = t.Language
                    }).ToList(),
                    WatchHistory = user != null ? partialWatchHistories.Where(w => w.MediaId == m.Id).FirstOrDefault() :null
                }).ToList();

            return new PlaylistDTO
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                SourceType = p.SourceType,
                OfferingId = p.OfferingId,
                Name = p.Name,
                Medias = mediasDTO,
                JsonMetadata = p.JsonMetadata,
                PlaylistIdentifier = p.PlaylistIdentifier,
                PublishStatus = p.PublishStatus,
                ListUpdatedAt = p.ListUpdatedAt,
                ListCheckedAt = p.ListCheckedAt,
                Options = p.GetOptionsAsJson()
            };
        }
       
       // PUT: api/Playlists/Option
        [HttpPut("Option/{id}")]
        [Authorize]
        public async Task<IActionResult> PutPlaylistOptions(string id, JObject options)
        {
            if ( id == null || options == null )
            {
                return BadRequest();
            }
            var p = await _context.Playlists.FindAsync(id);
            var offering = await _context.Offerings.FindAsync(p.OfferingId);

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            
            p.SetOptionsAsJson(options);
           
            try
            {
                await _context.SaveChangesAsync();
                _wakeDownloader.UpdatePlaylist(p.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        
        // PUT: api/Playlists/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutPlaylist(string id, PlaylistUpdateDTO playlist)
        {
            if (playlist == null || playlist.Id == null || id != playlist.Id || playlist.OfferingId == null)
            {
                return BadRequest("Validation checks");
            }
            var offering = await _context.Offerings.FindAsync(playlist.OfferingId);
            if (offering == null)
            {
                return BadRequest("No such offering");
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var p = await _context.Playlists.FindAsync(playlist.Id);

            p.Name = playlist.Name;
            p.SetOptionsAsJson(playlist.Options);
            p.PublishStatus = playlist.PublishStatus;

            try
            {
                await _context.SaveChangesAsync();
                _wakeDownloader.UpdatePlaylist(playlist.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Playlists
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PlaylistDTO>> PostPlaylist(NewPlaylistDTO playlist)
        {
            
            if (playlist == null || playlist.OfferingId == null)
            {
                return BadRequest("Playlist missing or OfferingId missing");
            }

            var offering = await _context.Offerings.FindAsync(playlist.OfferingId);

            if (offering == null)
            {
                return BadRequest("No such offering");
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);

            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult("Not authenticated");
                }

                return new ChallengeResult("Not authorized for this offering");
            }
           
            if (playlist.PlaylistIdentifier != null && playlist.PlaylistIdentifier.Length > 0)
            {
                playlist.PlaylistIdentifier = playlist.PlaylistIdentifier.Trim();
            }
            var p = playlist.ToPlaylist();
            // If playlists are deleted the Count != Max Index, so use the max index (still not perfect, what if 2 playlists are created at the same time)
            if (offering.Playlists != null && offering.Playlists.Count > 0)
            {
                p.Index = 1 + offering.Playlists.Max(pl => pl.Index);
            }

            _context.Playlists.Add(p);
            await _context.SaveChangesAsync();
            _wakeDownloader.UpdatePlaylist(p.Id);

            return await GetPlaylist(p.Id);
        }

        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Playlist>> DeletePlaylist(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var playlist = await _context.Playlists.FindAsync(id);

            if (playlist == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.Offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return playlist;
        }

        // POST /api/Playlist/Reorder/{offeringId}
        [HttpPost("Reorder/{offeringId}")]
        public async Task<ActionResult> Reorder(string offeringId, List<string> playlistIds)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (playlistIds == null || !playlistIds.Any() || offering == null || offering.Playlists == null || offering.Playlists.Count != playlistIds.Count)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var playlists = new List<Playlist>();
            for (int i = 0; i < playlistIds.Count; i++)
            {
                var playlist = await _context.Playlists.FindAsync(playlistIds[i]);
                if (playlist == null || playlist.OfferingId != offeringId)
                {
                    return BadRequest("Invalid playlistIds");
                }
                playlist.Index = i;
                playlists.Add(playlist);
            }
            _context.Playlists.UpdateRange(playlists);
            await _context.SaveChangesAsync();
            return RedirectToAction("GetPlaylists", new { offeringId });
        }

        private bool PlaylistExists(string id)
        {
            return _context.Playlists.Any(e => e.Id == id);
        }
    }

    public class VideoDTO
    {
        public string Id { get; set; }
        public string Video1Path { get; set; }
        public string Video2Path { get; set; }
        public string ASLPath { get; set; }
        public String TaskLog { get; set; }
    }

    public class TranscriptionDTO
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string SrtPath { get; set; }
        
        public int TranscriptionType { get; set;  } // 0=Caption 1=Description

        public String Label { get; set; }

        public String SourceLabel { get; set; } // where did this transcription originate?
        public string Language { get; set; }
    }
    public class PlaylistUpdateDTO {
        public string Id { get; set; }
        public string OfferingId { get; set; }
        public string Name { get; set; }
        public JObject Options { get; set; }
        public PublishStatus PublishStatus { get; set; }
    }

    public class NewPlaylistDTO
    {
        public string OfferingId { get; set; }
        public string Name { get; set; }
        public SourceType SourceType { get; set; }
        public string PlaylistIdentifier { get; set; }
        public JObject JsonMetadata { get; set; }
        public JObject Options { get; set; }
        public PublishStatus PublishStatus { get; set; }

        public Playlist ToPlaylist()
        {
            return new Playlist
            {
                OfferingId = OfferingId,
                Name = Name,
                PublishStatus = PublishStatus,
                SourceType = SourceType,
                JsonMetadata = JsonMetadata,
                Options = Options.ToString(Newtonsoft.Json.Formatting.None)
            };
        }
    }
    public class PlaylistDTO
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public SourceType SourceType { get; set; }
        public string OfferingId { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public string PlaylistIdentifier { get; set; }
        public List<MediaDTO> Medias { get; set; }
        public JObject JsonMetadata { get; set; }
        public JObject Options { get; set; }
        public PublishStatus PublishStatus { get; set; }
#nullable enable
        public DateTime? ListUpdatedAt {get; set; }
        public DateTime? ListCheckedAt {get; set; }
#nullable disable
    }

    public class MediaDTO
    {
        public string Id { get; set; }
        public string PlaylistId { get; set; }
        public DateTime CreatedAt { get; set; }
        public JObject JsonMetadata { get; set; }
        public SourceType SourceType { get; set; }
        public bool Ready { get; set; }
        public PublishStatus PublishStatus { get; set; }

        public bool SceneDetectReady { get; set; }

        public VideoDTO Video { get; set; }
        public List<TranscriptionDTO> Transcriptions { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }

        public TimeSpan? Duration { get; set; }
        public WatchHistory WatchHistory { get; set; }
        public JObject Options { get; set; }
    }

    public class MediaSearchDTO
    {
        public string Name { get; set; }
        public string MediaId { get; set; }
        public string PlaylistName { get; set; }
        public string PlaylistId { get; set; }
        public PublishStatus PublishStatus { get; set; }
    }
}
