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
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly CTDbContext _context;

        public MediaController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Media/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Media>> GetMedia(string id)
        {
            var media = await _context.Medias.FindAsync(id);

            if (media == null)
            {
                return NotFound();
            }

            return media;
        }

        // PUT: api/Media/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutMedia(string id, Media media)
        {
            if (id != media.Id)
            {
                return BadRequest();
            }

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
                if(Path.GetExtension(video1.FileName) != ".mp4")
                {
                    return BadRequest("File Format not permitted");
                }
                var filePath = Path.GetTempFileName();
                using ( var stream = new FileStream(filePath, FileMode.Create))
                {
                    await video1.CopyToAsync(stream);

                    media.JsonMetadata.Add("video1", JsonConvert.SerializeObject(video1));
                    media.JsonMetadata.Add("video1Path", filePath);
                }
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

                    media.JsonMetadata.Add("video2", JsonConvert.SerializeObject(video2));
                    media.JsonMetadata.Add("video2Path", filePath);
                }
            }

            _context.Medias.Add(media);
            await _context.SaveChangesAsync();

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

        private bool MediaExists(string id)
        {
            return _context.Medias.Any(e => e.Id == id);
        }
    }
}
