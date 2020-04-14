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
    public class EPubChaptersController : ControllerBase
    {
        private readonly CTDbContext _context;

        public EPubChaptersController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/EPubChapters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EPubChapter>>> GetEPubChapters()
        {
            return await _context.EPubChapters.ToListAsync();
        }

        // GET: api/EPubChapters/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EPubChapter>> GetEPubChapter(string id)
        {
            var ePubChapter = await _context.EPubChapters.FindAsync(id);

            if (ePubChapter == null)
            {
                return NotFound();
            }

            return ePubChapter;
        }

        // PUT: api/EPubChapters/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEPubChapter(string id, EPubChapter ePubChapter)
        {
            if (id != ePubChapter.Id)
            {
                return BadRequest();
            }

            _context.Entry(ePubChapter).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EPubChapterExists(id))
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

        // POST: api/EPubChapters
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<EPubChapter>> PostEPubChapter(EPubChapter ePubChapter)
        {
            _context.EPubChapters.Add(ePubChapter);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEPubChapter", new { id = ePubChapter.Id }, ePubChapter);
        }

        // DELETE: api/EPubChapters/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<EPubChapter>> DeleteEPubChapter(string id)
        {
            var ePubChapter = await _context.EPubChapters.FindAsync(id);
            if (ePubChapter == null)
            {
                return NotFound();
            }

            _context.EPubChapters.Remove(ePubChapter);
            await _context.SaveChangesAsync();

            return ePubChapter;
        }

        private bool EPubChapterExists(string id)
        {
            return _context.EPubChapters.Any(e => e.Id == id);
        }
    }
}
