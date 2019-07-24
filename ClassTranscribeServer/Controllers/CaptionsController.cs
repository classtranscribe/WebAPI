using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public CaptionsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Captions/5
        [HttpGet("ByTranscription/{TranscriptionId}")]
        public async Task<ActionResult<IEnumerable<Caption>>> GetCaptions(string TranscriptionId)
        {
            return await _context.Captions.Where(c => c.TranscriptionId == TranscriptionId).OrderBy(c => c.Index).ToListAsync();
        }

        // GET: api/Captions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Caption>> GetCaption(string id)
        {
            var caption = await _context.Captions.FindAsync(id);

            if (caption == null)
            {
                return NotFound();
            }

            return caption;
        }

        // PUT: api/Captions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCaption(string id, Caption caption)
        {
            if (id != caption.Id)
            {
                return BadRequest();
            }

            _context.Entry(caption).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CaptionExists(id))
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

        // POST: api/Captions
        [HttpPost]
        public async Task<ActionResult<Caption>> PostCaption(Caption caption)
        {
            _context.Captions.Add(caption);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCaption", new { id = caption.Id }, caption);
        }

        // DELETE: api/Captions/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Caption>> DeleteCaption(string id)
        {
            var caption = await _context.Captions.FindAsync(id);
            if (caption == null)
            {
                return NotFound();
            }

            _context.Captions.Remove(caption);
            await _context.SaveChangesAsync();

            return caption;
        }

        private bool CaptionExists(string id)
        {
            return _context.Captions.Any(e => e.Id == id);
        }
    }
}
