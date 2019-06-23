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
    public class UserOfferingsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public UserOfferingsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/UserOfferings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserOffering>>> GetUserOfferings()
        {
            return await _context.UserOfferings.ToListAsync();
        }

        // GET: api/UserOfferings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserOffering>> GetUserOffering(string id)
        {
            var userOffering = await _context.UserOfferings.FindAsync(id);

            if (userOffering == null)
            {
                return NotFound();
            }

            return userOffering;
        }

        // PUT: api/UserOfferings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserOffering(string id, UserOffering userOffering)
        {
            if (id != userOffering.ApplicationUserId)
            {
                return BadRequest();
            }

            _context.Entry(userOffering).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserOfferingExists(id))
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

        // POST: api/UserOfferings
        [HttpPost]
        public async Task<ActionResult<UserOffering>> PostUserOffering(UserOffering userOffering)
        {
            _context.UserOfferings.Add(userOffering);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserOfferingExists(userOffering.ApplicationUserId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUserOffering", new { id = userOffering.ApplicationUserId }, userOffering);
        }

        // DELETE: api/UserOfferings/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<UserOffering>> DeleteUserOffering(string id)
        {
            var userOffering = await _context.UserOfferings.FindAsync(id);
            if (userOffering == null)
            {
                return NotFound();
            }

            _context.UserOfferings.Remove(userOffering);
            await _context.SaveChangesAsync();

            return userOffering;
        }

        private bool UserOfferingExists(string id)
        {
            return _context.UserOfferings.Any(e => e.ApplicationUserId == id);
        }
    }
}
