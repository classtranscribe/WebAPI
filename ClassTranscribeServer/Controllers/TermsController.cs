using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TermsController : BaseController
    {
        public TermsController(CTDbContext context, ILogger<TermsController> logger) : base(context, logger) { }

        /// <summary>
        /// Gets all Terms for universityId
        /// </summary>
        [HttpGet("ByUniversity/{universityId}")]
        public async Task<ActionResult<IEnumerable<Term>>> GetTerms(string universityId)
        {
            return await _context.Terms.Where(t => t.UniversityId == universityId).OrderBy(t => t.StartDate).ToListAsync();
        }

        // GET: api/Terms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Term>> GetTerm(string id)
        {
            var term = await _context.Terms.FindAsync(id);

            if (term == null)
            {
                return NotFound();
            }

            return term;
        }

        // PUT: api/Terms/5
        [HttpPut("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutTerm(string id, Term term)
        {
            if (term == null || id == null || id != term.Id)
            {
                return BadRequest();
            }

            _context.Entry(term).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TermExists(id))
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

        // POST: api/Terms
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Term>> PostTerm(Term term)
        {
            if (term == null)
            {
                return BadRequest();
            }

            _context.Terms.Add(term);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTerm", new { id = term.Id }, term);
        }

        // DELETE: api/Terms/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Term>> DeleteTerm(string id)
        {
            var term = await _context.Terms.FindAsync(id);
            if (term == null)
            {
                return NotFound();
            }

            _context.Terms.Remove(term);
            await _context.SaveChangesAsync();

            return term;
        }

        private bool TermExists(string id)
        {
            return _context.Terms.Any(e => e.Id == id);
        }
    }
}
