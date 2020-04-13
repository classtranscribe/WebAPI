using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;

        public MediaController(IAuthorizationService authorizationService, 
            WakeDownloader wakeDownloader, 
            CTDbContext context,
            UserUtils userUtils,
            ILogger<MediaController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
            _userUtils = userUtils;
        }

        // GET: api/Media/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MediaDTO>> GetMedia(string id)
        {
            var media = await _context.Medias.FindAsync(id);

            if (media == null)
            {
                return NotFound();
            }

            var v = await _context.Videos.FindAsync(media.VideoId);
            var user = _userUtils.GetUser(this.User);
            var mediaDTO = new MediaDTO
            {
                Id = media.Id,
                Name = media.Name,
                PlaylistId = media.PlaylistId,
                CreatedAt = media.CreatedAt,
                JsonMetadata = media.JsonMetadata,
                SourceType = media.SourceType,
                Transcriptions = media.Video.Transcriptions
                .Select(t => new TranscriptionDTO
                {
                    Id = t.Id,
                    Path = t.File != null ? t.File.Path : null,
                    Language = t.Language
                }).ToList(),
                Video = new VideoDTO
                {
                    Id = media.Video.Id,
                    Video1Path = media.Video.Video1?.Path,
                    Video2Path = media.Video.Video2?.Path
                },
                WatchHistory = user != null ? media.WatchHistories.Where(w => w.ApplicationUserId == user.Id).FirstOrDefault() : null
            };

            return mediaDTO;
        }

        [HttpPut("PutMediaName")]
        public async Task<IActionResult> PutMediaName(string mediaId, string name)
        {
            if (!MediaExists(mediaId))
            {
                return NotFound();
            }

            Media media = await _context.Medias.FindAsync(mediaId);
            media.Name = name;
            _context.Entry(media).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(mediaId))
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

        // PUT: api/Media/5
        [HttpPut("PutJsonMetaData/{id}")]
        [Authorize]
        public async Task<IActionResult> PutJsonMetaData(JObject jsonMetaData, string id)
        {
            if (!MediaExists(id))
            {
                return NotFound();
            }

            Media media = await _context.Medias.FindAsync(id);
            media.JsonMetadata = jsonMetaData;
            _context.Entry(media).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(id))
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

        // POST: api/Media
        [DisableRequestSizeLimit]
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Media>> PostMedia(IFormFile video1, IFormFile video2, [FromForm] string playlistId)
        {
            if (video1 == null)
            {
                return BadRequest("video1 is compulsory");
            }
            Media media = new Media
            {
                PlaylistId = playlistId,
                SourceType = SourceType.Local,
                JsonMetadata = new JObject()
            };
            // full path to file in temp location
            if (video1.Length > 0)
            {
                if (Path.GetExtension(video1.FileName) != ".mp4")
                {
                    return BadRequest("File Format not permitted");
                }
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await video1.CopyToAsync(stream);
                }
                media.UniqueMediaIdentifier = FileRecord.ComputeSha256HashForFile(filePath);
                media.JsonMetadata.Add("video1", JsonConvert.SerializeObject(video1));
                media.JsonMetadata.Add("video1Path", filePath);
            }
            // Copy second File
            if (video2 != null && video2.Length > 0)
            {
                if (Path.GetExtension(video2.FileName) != ".mp4")
                {
                    return BadRequest("File Format not permitted");
                }
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await video2.CopyToAsync(stream);
                }
                media.JsonMetadata.Add("video2", JsonConvert.SerializeObject(video2));
                media.JsonMetadata.Add("video2Path", filePath);
            }

            _context.Medias.Add(media);
            await _context.SaveChangesAsync();
            _wakeDownloader.UpdatePlaylist(playlistId);
            return CreatedAtAction("GetMedia", new { id = media.Id }, media);
        }

        // DELETE: api/Media/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Media>> DeleteMedia(string id)
        {
            var media = await _context.Medias.FindAsync(id);
            if (media == null)
            {
                return NotFound();
            }
            _context.Medias.Remove(media);
            await _context.SaveChangesAsync();

            return media;
        }

        // POST /api/Media/Reorder/{playlistId}
        [HttpPost("Reorder/{playlistId}")]
        public async Task<ActionResult> Reorder(string playlistId, List<string> mediaIds)
        {
            var playlist = await _context.Playlists.FindAsync(playlistId);
            if (mediaIds == null || !mediaIds.Any() || playlist == null || playlist.Medias.Count != mediaIds.Count || playlist.OfferingId == null)
            {
                return BadRequest();
            }
            var offering = await _context.Offerings.FindAsync(playlist.OfferingId);
            if (offering == null)
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
            var medias = new List<Media>();
            for (int i = 0; i < mediaIds.Count; i++)
            {
                var media = await _context.Medias.FindAsync(mediaIds[i]);
                if (media == null || media.PlaylistId != playlistId)
                {
                    return BadRequest("Invalid mediaIds");
                }
                media.Index = i;
                medias.Add(media);
            }
            _context.Medias.UpdateRange(medias);
            await _context.SaveChangesAsync();
            return RedirectToAction("GetPlaylist", "Playlists", new { id = playlistId });
        }

        private bool MediaExists(string id)
        {
            return _context.Medias.Any(e => e.Id == id);
        }
    }
}
