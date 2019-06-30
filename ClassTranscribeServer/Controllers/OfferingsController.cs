using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System.Security.Claims;
using System;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Offering>>> GetOfferingsByStudent(string userId)
        {
            // Get the user
            ApplicationUser user = null;
            if (_context.Users != null)
            {
                var currentUserID = _context.Users.Find(ClaimTypes.NameIdentifier).Id;
                user = _context.Users.Where(u => u.Id == currentUserID).First();
            }

            // Store the results
            List<Offering> offerings = new List<Offering>();

            // Get all the public offerings
            var public_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.Public).ToListAsync();
            offerings.Concat(public_offerings);

            // Get all offering that need authentication
            if (user != null)
            {
                var authen_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.AuthenticatedOnly).ToListAsync();
                offerings.Concat(authen_offerings);
            }

            // Get all their university's offerings
            var university_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.UniversityOnly && offer.CourseOfferings.Select(c => c.Course.Department.University).Contains(user.University)).ToListAsync();
            offerings.Concat(university_offerings);

            // Get all offering that this user is a member
            var member_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.StudentsOnly && offer.OfferingUsers.Select(ou => ou.ApplicationUser).Contains(user)).ToListAsync();
            offerings.Concat(member_offerings);

            // return the combined result
            return offerings;
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

        /// <summary>
        /// Post new Offering for a course for an instructor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Offering>> PostNewOffering(NewOfferingDTO newOfferingDTO)
        {
            _context.Offerings.Add(newOfferingDTO.Offering);
            await _context.SaveChangesAsync();
            _context.CourseOfferings.Add(new CourseOffering
            {
                CourseId = newOfferingDTO.CourseId,
                OfferingId = newOfferingDTO.Offering.Id
            });
            _context.UserOfferings.Add(new UserOffering
            {
                ApplicationUserId = newOfferingDTO.InstructorId,
                IdentityRole = _context.Roles.Where(r => r.Name == "Instructor").FirstOrDefault(),
                OfferingId = newOfferingDTO.Offering.Id
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOffering", new { id = newOfferingDTO.Offering.Id }, newOfferingDTO.Offering);
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

        public class NewOfferingDTO
        {
            public Offering Offering { get; set; }
            public string CourseId { get; set; }
            public string InstructorId { get; set; }

        }
    }

}
