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
    public class GlossaryController : BaseController
    {
        public GlossaryController(CTDbContext context, ILogger<GlossaryController> logger) : base(context, logger) { }

        // GET: api/Glossaries/3
        [HttpGet("{id}")]
        public async Task<ActionResult<Glossary>> GetGlossary(string id)
        {

            var glossary = await _context.Glossaries.FindAsync(id);

            if (glossary == null)
            {
                return NotFound();
            }

            return glossary;
        }

        // POST: api/Glossaries
        [HttpPost]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Glossary>> PostGlossary(Glossary glossary)
        {
            if (glossary == null)
            {
                return BadRequest();
            }

            _context.Glossaries.Add(glossary);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGlossary", new { id = glossary.Id }, glossary);
        }

        // DELETE: api/Glossaries/3
        [HttpDelete("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Glossary>> DeleteGlossary(string id)
        {
            var glossary = await _context.Glossaries.FindAsync(id);
            if (glossary == null)
            {
                return NotFound();
            }

            _context.Glossaries.Remove(glossary);
            await _context.SaveChangesAsync();

            return glossary;
        }

        // PUT: api/Glossaries/3
        [HttpPut("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutGlossary(string id, Glossary glossary)
        {
            if (glossary == null || id == null || id != glossary.Id)
            {
                return BadRequest();
            }

            _context.Entry(glossary).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GlossaryExists(id))
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


        // Upvote: api/Glossaries/Upvote/3
        [HttpPut("Upvote/{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> UpvoteGlossary(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var glossary = await _context.Glossaries.FindAsync(id);

            if (glossary == null)
            {
                return NotFound();
            }

            glossary.Likes++;

            _context.Entry(glossary).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GlossaryExists(id))
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

        /// <summary>
        /// Gets all glossaries for a term from an CourseOffering
        /// </summary>
        [HttpGet("ByTermCourseOffering/{term}")]
        public async Task<ActionResult<IEnumerable<Glossary>>> GetAllGlossaryByTermCourseOffering(string term, string courseId, string offeringId) 
        {

            var glossaries = await _context.Glossaries.Where(c => c.CourseId == courseId && c.OfferingId == offeringId && c.Term == term).OrderBy(c => c.Id).ToListAsync();
        
            if (glossaries == null)
            {
                return NotFound();
            }

            return glossaries;
        }

        // GET: api/Glossary/GetGlossaryByTerm
        [HttpGet("GetGlossaryByTerm")]
        public async Task<ActionResult<IEnumerable<Glossary>>> GetAllGlossaryByTerm(string term) 
        {

            var glossaries = await _context.Glossaries.Where(c => c.Term == term).OrderBy(c => c.Id).ToListAsync();
        
            if (glossaries == null)
            {
                return NotFound();
            }

            return glossaries;
        }

        /// <summary>
        /// Gets all glossaries from an CourseOffering
        /// </summary>
        [HttpGet("ByCourseOffering")]
        public async Task<ActionResult<IEnumerable<Glossary>>> GetAllGlossaryByCourseOffering(string courseId, string offeringId) 
        {

            var glossaries = await _context.Glossaries.Where(c => c.CourseId == courseId && c.OfferingId == offeringId).OrderBy(c => c.Id).ToListAsync();
        
            if (glossaries == null)
            {
                return NotFound();
            }

            return glossaries;
        }

        private bool GlossaryExists(string id)
        {
            return _context.Glossaries.Any(e => e.Id == id);
        }
    }
}