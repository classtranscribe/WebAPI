using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ASLVideoController : BaseController
    {
        public ASLVideoController(CTDbContext context, ILogger<ASLVideoController> logger) : base(context, logger) { }

        // GET: api/ASLVideos/3
        [HttpGet("{id}")]
        public async Task<ActionResult<ASLVideo>> GetASLVideo(string id)
        {

            var aSLVideo = await _context.ASLVideos.FindAsync(id);

            if (aSLVideo == null)
            {
                return NotFound();
            }

            return aSLVideo;
        }

        // POST: api/ASLVideos
        [HttpPost]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<ASLVideo>> PostASLVideo(ASLVideo aSLVideo)
        {
            if (aSLVideo == null)
            {
                return BadRequest();
            }

            _context.ASLVideos.Add(aSLVideo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetASLVideo", new { id = aSLVideo.Id }, aSLVideo);
        }

        // DELETE: api/ASLVideos/3
        [HttpDelete("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<ASLVideo>> DeleteASLVideo(string id)
        {
            var aSLVideo = await _context.ASLVideos.FindAsync(id);
            if (aSLVideo == null)
            {
                return NotFound();
            }

            _context.ASLVideos.Remove(aSLVideo);
            await _context.SaveChangesAsync();

            return aSLVideo;
        }

        // PUT: api/ASLVideos/3
        [HttpPut("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutASLVideo(string id, ASLVideo aSLVideo)
        {
            if (aSLVideo == null || id == null || id != aSLVideo.Id)
            {
                return BadRequest();
            }

            _context.Entry(aSLVideo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ASLVideoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        private bool ASLVideoExists(string id)
        {
            return _context.ASLVideos.Any(e => e.Id == id);
        }
    }
}