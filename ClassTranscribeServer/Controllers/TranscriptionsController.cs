using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranscriptionsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public TranscriptionsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Transcriptions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transcription>>> GetTranscriptions()
        {
            return await _context.Transcriptions.ToListAsync();
        }

        // GET: api/Transcriptions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transcription>> GetTranscription(string id)
        {
            var transcription = await _context.Transcriptions.FindAsync(id);

            if (transcription == null)
            {
                return NotFound();
            }

            return transcription;
        }

        // PUT: api/Transcriptions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTranscription(string id, Transcription transcription)
        {
            if (id != transcription.Id)
            {
                return BadRequest();
            }

            _context.Entry(transcription).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TranscriptionExists(id))
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

        // POST: api/Transcriptions
        [HttpPost]
        public async Task<ActionResult<Transcription>> PostTranscription(Transcription transcription)
        {
            _context.Transcriptions.Add(transcription);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTranscription", new { id = transcription.Id }, transcription);
        }

        // DELETE: api/Transcriptions/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Transcription>> DeleteTranscription(string id)
        {
            var transcription = await _context.Transcriptions.FindAsync(id);
            if (transcription == null)
            {
                return NotFound();
            }

            _context.Transcriptions.Remove(transcription);
            await _context.SaveChangesAsync();

            return transcription;
        }

        private bool TranscriptionExists(string id)
        {
            return _context.Transcriptions.Any(e => e.Id == id);
        }
    }
}
