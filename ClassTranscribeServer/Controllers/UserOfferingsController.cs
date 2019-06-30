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

        // GET: api/Courses/
        /// <summary>
        /// Gets all UserOfferings for a userId
        /// </summary>
        [HttpGet("ByUserId/{userId}")]
        public async Task<ActionResult<IEnumerable<UserOffering>>> GetUserOfferingsByUserId(string userId)
        {
            return await _context.UserOfferings.Where(uo => uo.ApplicationUserId == userId).ToListAsync();
        }

        // GET: api/Courses/
        /// <summary>
        /// Gets all Offerings per Course per Instructor
        /// </summary>
        [HttpGet("ByOfferingId/{offeringId}")]
        public async Task<ActionResult<IEnumerable<UserOffering>>> GetUserOfferingsByOfferingId(string offeringId)
        {
            return await _context.UserOfferings.Where(uo => uo.OfferingId == offeringId).ToListAsync();
        }

        // POST: api/UserOfferings
        [HttpPost]
        public async Task<ActionResult<UserOffering>> PostUserOffering(UserOfferingDTO userOfferingDTO)
        {
            UserOffering userOffering = new UserOffering
            {
                ApplicationUserId = userOfferingDTO.UserId,
                OfferingId = userOfferingDTO.OfferingId,
                IdentityRole = await _context.Roles.Where(r => r.Name == userOfferingDTO.RoleName).FirstAsync()
            };

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
        [HttpDelete("{offeringId}/{userId}")]
        public async Task<ActionResult<UserOffering>> DeleteUserOffering(string offeringId, string userId)
        {
            var userOffering = await _context.UserOfferings.Where(uo => uo.OfferingId == offeringId && uo.ApplicationUserId == userId).FirstAsync();
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

        public class UserOfferingDTO
        {
            public string OfferingId { get; set; }
            public string UserId { get; set; }
            public string RoleName { get; set; }
        }
    }
}
