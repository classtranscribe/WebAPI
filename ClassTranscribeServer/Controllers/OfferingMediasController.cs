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
    public class OfferingMediasController : ControllerBase
    {
        private readonly CTDbContext _context;

        public OfferingMediasController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/OfferingMedias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OfferingMedia>>> GetOfferingMedias()
        {
            return await _context.OfferingMedias.ToListAsync();
        }

        // GET: api/OfferingMedias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OfferingMedia>> GetOfferingMedia(string id)
        {
            var offeringMedia = await _context.OfferingMedias.FindAsync(id);

            if (offeringMedia == null)
            {
                return NotFound();
            }

            return offeringMedia;
        }

        // PUT: api/OfferingMedias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOfferingMedia(string id, OfferingMedia offeringMedia)
        {
            if (id != offeringMedia.OfferingId)
            {
                return BadRequest();
            }

            _context.Entry(offeringMedia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OfferingMediaExists(id))
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

        // POST: api/OfferingMedias
        [HttpPost]
        public async Task<ActionResult<OfferingMedia>> PostOfferingMedia(OfferingMedia offeringMedia)
        {
            _context.OfferingMedias.Add(offeringMedia);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (OfferingMediaExists(offeringMedia.OfferingId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetOfferingMedia", new { id = offeringMedia.OfferingId }, offeringMedia);
        }

        // DELETE: api/OfferingMedias/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<OfferingMedia>> DeleteOfferingMedia(string id)
        {
            var offeringMedia = await _context.OfferingMedias.FindAsync(id);
            if (offeringMedia == null)
            {
                return NotFound();
            }

            _context.OfferingMedias.Remove(offeringMedia);
            await _context.SaveChangesAsync();

            return offeringMedia;
        }

        private bool OfferingMediaExists(string id)
        {
            return _context.OfferingMedias.Any(e => e.OfferingId == id);
        }
    }
}
