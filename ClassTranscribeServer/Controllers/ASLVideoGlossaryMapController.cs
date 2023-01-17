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
    public class ASLVideoGlossaryMapController : BaseController
    {
        public ASLVideoGlossaryMapController(CTDbContext context, ILogger<ASLVideoGlossaryMapController> logger) : base(context, logger) { }

        // GET: api/ASLVideoGlossaryMap/3
        [HttpGet("{id}")]
        public async Task<ActionResult<ASLVideoGlossaryMap>> GetASLVideoGlossaryMap(string id)
        {

            var aSLVideoGlossaryMap = await _context.ASLVideoGlossaryMaps.FindAsync(id);

            if (aSLVideoGlossaryMap == null)
            {
                return NotFound();
            }

            return aSLVideoGlossaryMap;
        }

        // POST: api/ASLVideoGlossaryMap
        [HttpPost]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<ASLVideoGlossaryMap>> PostASLVideoGlossaryMap(ASLVideoGlossaryMap aSLVideoGlossaryMap)
        {
            if (aSLVideoGlossaryMap == null)
            {
                return BadRequest();
            }

            _context.ASLVideoGlossaryMaps.Add(aSLVideoGlossaryMap);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetASLVideoGlossaryMap", new { id = aSLVideoGlossaryMap.Id }, aSLVideoGlossaryMap);
        }

        // DELETE: api/ASLVideoGlossaryMap/3
        [HttpDelete("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult<ASLVideoGlossaryMap>> DeleteASLVideoGlossaryMap(string id)
        {
            var aSLVideoGlossaryMap = await _context.ASLVideoGlossaryMaps.FindAsync(id);
            if (aSLVideoGlossaryMap == null)
            {
                return NotFound();
            }

            _context.ASLVideoGlossaryMaps.Remove(aSLVideoGlossaryMap);
            await _context.SaveChangesAsync();

            return aSLVideoGlossaryMap;
        }

        // PUT: api/ASLVideoGlossaryMap/3
        [HttpPut("{id}")]
        // [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> PutASLVideoGlossaryMap(string id, ASLVideoGlossaryMap aSLVideoGlossaryMap)
        {
            if (aSLVideoGlossaryMap == null || id == null || id != aSLVideoGlossaryMap.Id)
            {
                return BadRequest();
            }

            _context.Entry(aSLVideoGlossaryMap).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ASLVideoGlossaryMapExists(id))
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

        private bool ASLVideoGlossaryMapExists(string id)
        {
            return _context.ASLVideoGlossaryMaps.Any(e => e.Id == id);
        }
    }
}