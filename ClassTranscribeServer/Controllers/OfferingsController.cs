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
    public class OfferingsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public OfferingsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Offerings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Offering>>> GetOfferings()
        {
            return await _context.Offerings.ToListAsync();
        }

        // GET: api/Offerings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Offering>> GetOffering(string id)
        {
            var offering = await _context.Offerings.FindAsync(id);

            if (offering == null)
            {
                return NotFound();
            }

            return offering;
        }

        // PUT: api/Offerings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffering(string id, Offering offering)
        {
            if (id != offering.Id)
            {
                return BadRequest();
            }

            _context.Entry(offering).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OfferingExists(id))
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

        // POST: api/Offerings
        [HttpPost]
        public async Task<ActionResult<Offering>> PostOffering(Offering offering)
        {
            _context.Offerings.Add(offering);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOffering", new { id = offering.Id }, offering);
        }

        // DELETE: api/Offerings/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Offering>> DeleteOffering(string id)
        {
            var offering = await _context.Offerings.FindAsync(id);
            if (offering == null)
            {
                return NotFound();
            }

            _context.Offerings.Remove(offering);
            await _context.SaveChangesAsync();

            return offering;
        }

        private bool OfferingExists(string id)
        {
            return _context.Offerings.Any(e => e.Id == id);
        }
    }
}
