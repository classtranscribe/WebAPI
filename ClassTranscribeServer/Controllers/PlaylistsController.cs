using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly CTDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public PlaylistsController(CTDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        // GET: api/Playlists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Playlist>>> GetPlaylists()
        {
            return await _context.Playlists.ToListAsync();
        }

        // GET: api/Playlists
        /// <summary>
        /// Gets all Playlists for offeringId
        /// </summary>
        [HttpGet("ByOffering/{offeringId}")]
        public async Task<ActionResult<IEnumerable<PlaylistDTO>>> GetPlaylists(string offeringId)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offeringId, Globals.POLICY_READ_OFFERING);
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
            var playlists = await _context.Playlists.Where(p => p.OfferingId == offeringId)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PlaylistDTO{ 
                    Id = p.Id,
                    CreatedAt = p.CreatedAt,
                    SourceType = p.SourceType,
                    OfferingId = p.OfferingId,
                    Name = p.Name,
                    Medias = p.Medias.Select(m => new MediaDTO{
                        Id = m.Id,
                        JsonMetadata = m.JsonMetadata,
                        CreatedAt = m.CreatedAt,
                        Ready = m.Transcriptions.Any()
                    }).ToList()
            }).ToListAsync();
            // Sorting by descending.
            playlists.ForEach(p => p.Medias.Sort((x,y) => -1 * x.CreatedAt.CompareTo(y.CreatedAt)));
            return playlists;
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
            List<MediaDTO> medias = p.Medias.OrderBy(m => m.CreatedAt).Select(m => new MediaDTO
            {
                Id = m.Id,
                CreatedAt = m.CreatedAt,
                JsonMetadata = m.JsonMetadata,
                SourceType = m.SourceType,
                Ready = m.Transcriptions.Any(),
                Videos = GetVideoDTOs(m.Videos),
                Transcriptions = GetTranscriptionDTOs(m.Transcriptions)
            }).ToList();

            return new PlaylistDTO {
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
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.OfferingId, Globals.POLICY_UPDATE_OFFERING);
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
            if (id != playlist.Id)
            {
                return BadRequest();
            }

            _context.Entry(playlist).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                WakeDownloader.Wake();
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
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.OfferingId, Globals.POLICY_UPDATE_OFFERING);
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
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
            WakeDownloader.Wake();

            return CreatedAtAction("GetPlaylist", new { id = playlist.Id }, playlist);
        }

        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Playlist>> DeletePlaylist(string id)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, playlist.OfferingId, Globals.POLICY_UPDATE_OFFERING);
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

        private bool PlaylistExists(string id)
        {
            return _context.Playlists.Any(e => e.Id == id);
        }

        public class PlaylistDTO
        {
            public string Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public SourceType SourceType { get; set; }
            public string OfferingId { get; set; }
            public string Name { get; set; }
            public List<MediaDTO> Medias { get; set; }
        }

        public class MediaDTO
        {
            public string Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public JObject JsonMetadata { get; set; }
            public SourceType SourceType { get; set; }
            public bool Ready { get; set; }
            public List<VideoDTO> Videos { get; set; }
            public List<TranscriptionDTO> Transcriptions { get; set; }
        }

        [NonAction]
        public List<VideoDTO> GetVideoDTOs(List<Video> vs)
        {
            return vs.Select(v => new VideoDTO
            {
                Id = v.Id,
                Video1Path = v.Video1.Path,
                Video2Path = v.Video2.Path
            }).ToList();
        }

        [NonAction]
        public List<TranscriptionDTO> GetTranscriptionDTOs(List<Transcription> ts)
        {
            return ts.Select(t => new TranscriptionDTO
            {
                Id = t.Id,
                Path = t.File.Path,
                Language = t.Language
            }).ToList();
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
    }
}
