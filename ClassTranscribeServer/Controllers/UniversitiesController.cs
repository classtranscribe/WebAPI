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
    public class UniversitiesController : BaseController
    {
        public UniversitiesController(CTDbContext context, ILogger<UniversitiesController> logger) : base(context, logger) { }

        // GET: api/Universities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<University>>> GetUniversities()
        {
            // Filter out UNK (Unkown university) from list of public, viewable universities
            return await _context.Universities.Where(u => u.Id != Seeder.UNK_UNIVERSITY_ID).OrderBy(u => u.Name).ToListAsync();
        }

        // GET: api/Universities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<University>> GetUniversity(string id)
        {
            var university = await _context.Universities.FindAsync(id);

            if (university == null)
            {
                return NotFound();
            }

            return university;
        }

        // PUT: api/Universities/5
        [HttpPut("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutUniversity(string id, University university)
        {
            if (university == null || id == null || id != university.Id)
            {
                return BadRequest();
            }

            _context.Entry(university).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UniversityExists(id))
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

        // POST: api/Universities
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<University>> PostUniversity(University university)
        {
            if (university == null)
            {
                return BadRequest();
            }

            _context.Universities.Add(university);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUniversity", new { id = university.Id }, university);
        }

        // DELETE: api/Universities/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<University>> DeleteUniversity(string id)
        {
            var university = await _context.Universities.FindAsync(id);
            if (university == null)
            {
                return NotFound();
            }

            _context.Universities.Remove(university);
            await _context.SaveChangesAsync();

            return university;
        }

        private bool UniversityExists(string id)
        {
            return _context.Universities.Any(e => e.Id == id);
        }
    }
}
