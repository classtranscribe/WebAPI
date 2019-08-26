using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<ActionResult<IEnumerable<Playlist>>> GetPlaylists(string offeringId)
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
            return (await _context.Offerings.FindAsync(offeringId)).Playlists;
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistDTO>> GetPlaylist(string id)
        {
            var playlist = await _context.Playlists.FindAsync(id);

            if (playlist == null)
            {
                return NotFound();
            }
            List<MediaDTO> medias = playlist.Medias.OrderBy(m => m.CreatedAt).Select(m => new MediaDTO
            {
                Media = m,
                Videos = m.Videos,
                Transcriptions = m.Transcriptions
            }).ToList();

            return new PlaylistDTO {
                Playlist = playlist,
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
            public Playlist Playlist { get; set; }
            public List<MediaDTO> Medias { get; set; }
        }

        public class MediaDTO
        {
            public Media Media { get; set; }
            public List<Video> Videos { get; set; }
            public List<Transcription> Transcriptions { get; set; }

        }
    }
}
