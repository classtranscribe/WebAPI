using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TermsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public TermsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Terms
        /// <summary>
        /// Gets all Terms.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/Courses    
        ///
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Term>>> GetTerms()
        {
            return await _context.Terms.ToListAsync();
        }

        /// <summary>
        /// Gets all Terms for universityId
        /// </summary>
        [HttpGet("ByUniversity/{universityId}")]
        public async Task<ActionResult<IEnumerable<Term>>> GetTerms(string universityId)
        {
            return await _context.Terms.Where(t => t.UniversityId == universityId).ToListAsync();
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
        public async Task<IActionResult> PutTerm(string id, Term term)
        {
            if (id != term.Id)
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
        public async Task<ActionResult<Term>> PostTerm(Term term)
        {
            _context.Terms.Add(term);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTerm", new { id = term.Id }, term);
        }

        // DELETE: api/Terms/5
        [HttpDelete("{id}")]
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
