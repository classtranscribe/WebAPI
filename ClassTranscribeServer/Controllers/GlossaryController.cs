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
        [Authorize(Roles = Globals.ROLE_ADMIN)]
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
        [Authorize(Roles = Globals.ROLE_ADMIN)]
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
        [Authorize(Roles = Globals.ROLE_ADMIN)]
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

            return NoContent();
        }

        private bool GlossaryExists(string id)
        {
            return _context.Glossaries.Any(e => e.Id == id);
        }
    }
}