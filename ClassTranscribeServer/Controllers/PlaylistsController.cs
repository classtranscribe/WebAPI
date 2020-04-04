using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public PlaylistsController(IAuthorizationService authorizationService, WakeDownloader wakeDownloader, CTDbContext context, ILogger<PlaylistsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
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
                return Unauthorized(new { Reason = "Insufficient Permission", AccessType = offering.AccessType });
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
                Index = p.Index
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
                return Unauthorized(new { Reason = "Insufficient Permission", AccessType = offering.AccessType });
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
                Medias = p.Medias.Where(m => m.Video != null).Select(m => new MediaDTO
                {
                    Id = m.Id,
                    Index = m.Index,
                    Name = m.Name,
                    JsonMetadata = m.JsonMetadata,
                    CreatedAt = m.CreatedAt,
                    Ready = m.Video.Transcriptions.Any(),
                    SourceType = m.SourceType,
                    Video = new VideoDTO
                    {
                        Id = m.Video.Id,
                        Video1Path = m.Video.Video1 != null ? m.Video.Video1.Path : null,
                        Video2Path = m.Video.Video2 != null ? m.Video.Video2.Path : null,
                    },
                    Transcriptions = m.Video.Transcriptions.Select(t => new TranscriptionDTO
                    {
                        Id = t.Id,
                        Path = t.File.Path,
                        Language = t.Language
                    }).ToList()
                }).ToList()
            }).ToList();
            return playlists;
        }

        [HttpGet("SearchForMedia/{offeringId}/{query}")]
        public async Task<ActionResult<IEnumerable<MediaSearchDTO>>> SearchForMedia(string offeringId, string query)
        {
            var mediaSearches = await _context.Medias.Where(m => m.Playlist.OfferingId == offeringId &&
            EF.Functions.ToTsVector("english", m.Name).Matches(query))
                .Select(m => new MediaSearchDTO { Name = m.Name, MediaId = m.Id, PlaylistName = m.Playlist.Name, PlaylistId = m.PlaylistId })
                .Take(50)
                .ToListAsync();

            return mediaSearches;
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistDTO>> GetPlaylist(string id)
        {
            var p = await _context.Playlists.FindAsync(id);

            if (p == null)
            {
                return NotFound();
            }
            List<MediaDTO> medias = p.Medias
                .OrderBy(m => m.Index)
                .ThenBy(m => m.CreatedAt).Select(m => new MediaDTO
                {
                    Id = m.Id,
                    Index = m.Index,
                    Name = m.Name,
                    PlaylistId = m.PlaylistId,
                    CreatedAt = m.CreatedAt,
                    JsonMetadata = m.JsonMetadata,
                    SourceType = m.SourceType,
                    Ready = m.Video == null ? false : m.Video.Transcriptions.Any(),
                    Video = m.Video == null ? null : new VideoDTO
                    {
                        Id = m.Video.Id,
                        Video1Path = m.Video.Video1?.Path,
                        Video2Path = m.Video.Video2?.Path
                    },
                    Transcriptions = m.Video == null ? null : m.Video.Transcriptions.Select(t => new TranscriptionDTO
                    {
                        Id = t.Id,
                        Path = t.File != null ? t.File.Path : null,
                        Language = t.Language
                    }).ToList(),
                }).ToList();

            return new PlaylistDTO
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                SourceType = p.SourceType,
                OfferingId = p.OfferingId,
                Name = p.Name,
                Medias = medias
            };
        }

        // PUT: api/Playlists/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutPlaylist(string id, Playlist playlist)
        {
            if (playlist == null || playlist.Id == null || id != playlist.Id)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.Offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }
                else
                {
                    return new ChallengeResult();
                }
            }

            _context.Entry(playlist).State = EntityState.Modified;

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
        public async Task<ActionResult<Playlist>> PostPlaylist(Playlist playlist)
        {
            if (playlist == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.Offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }
                else
                {
                    return new ChallengeResult();
                }
            }
            if (playlist.PlaylistIdentifier != null && playlist.PlaylistIdentifier.Length > 0)
            {
                playlist.PlaylistIdentifier = playlist.PlaylistIdentifier.Trim();
            }
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
            _wakeDownloader.UpdatePlaylist(playlist.Id);

            return CreatedAtAction("GetPlaylist", new { id = playlist.Id }, playlist);
        }

        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Playlist>> DeletePlaylist(string id)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.Offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }
                else
                {
                    return new ChallengeResult();
                }
            }
            if (playlist == null)
            {
                return NotFound();
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
            if (playlistIds == null || !playlistIds.Any() || offering == null || offering.Playlists.Count != playlistIds.Count)
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
                else
                {
                    return new ChallengeResult();
                }
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
            return RedirectToAction("GetPlaylists", new { offeringId = offeringId });
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
    }

    public class TranscriptionDTO
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Language { get; set; }
    }

    public class PlaylistDTO
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public SourceType SourceType { get; set; }
        public string OfferingId { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public List<MediaDTO> Medias { get; set; }
    }

    public class MediaDTO
    {
        public string Id { get; set; }
        public string PlaylistId { get; set; }
        public DateTime CreatedAt { get; set; }
        public JObject JsonMetadata { get; set; }
        public SourceType SourceType { get; set; }
        public bool Ready { get; set; }
        public VideoDTO Video { get; set; }
        public List<TranscriptionDTO> Transcriptions { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
    }

    public class MediaSearchDTO
    {
        public string Name { get; set; }
        public string MediaId { get; set; }
        public string PlaylistName { get; set; }
        public string PlaylistId { get; set; }
    }
}
