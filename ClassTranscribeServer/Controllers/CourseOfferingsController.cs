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
    public class CourseOfferingsController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;

        public CourseOfferingsController(IAuthorizationService authorizationService, CTDbContext context, ILogger<CourseOfferingsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
        }

        // GET: api/Courses/
        /// <summary>
        /// Gets all Offerings per Course per Instructor
        /// </summary>
        [HttpGet("ByInstructor/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseOfferingDTO>>> GetCourseOfferingsByInstructor(string userId)
        {
            var courseOfferings = await _context.UserOfferings
                .Where(uo => uo.ApplicationUserId == userId && uo.IdentityRole.Name == Globals.ROLE_INSTRUCTOR)
                .Select(u => u.Offering).SelectMany(u => u.CourseOfferings).ToListAsync();

            return courseOfferings.GroupBy(co => co.Course, co => co.Offering).Select(g => new CourseOfferingDTO
            {
                Course = g.Key,
                Offerings = g.ToList()
            }).ToList();
        }

        // POST: api/CourseOfferings
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_INSTRUCTOR + "," + Globals.ROLE_TEACHING_ASSISTANT)]
        public async Task<ActionResult<CourseOffering>> PostCourseOffering(CourseOffering courseOffering)
        {
            _context.CourseOfferings.Add(courseOffering);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (CourseOfferingExists(courseOffering.CourseId))
                {
                    return Ok("Course Offering already exists!");
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        // DELETE: api/CourseOfferings/{courseId}/{offeringId}
        /// <summary>
        /// Deletes CourseOffering
        /// </summary>
        [HttpDelete("{courseId}/{offeringId}")]
        [Authorize]
        public async Task<ActionResult<List<CourseOffering>>> DeleteCourseOffering(string courseId, string offeringId)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offeringId, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }
                else
                {
                    return new ChallengeResult();
                }
            }
            var courseOfferings = await _context.CourseOfferings.Where(co => co.OfferingId == offeringId && co.CourseId == courseId).ToListAsync();
            if (courseOfferings == null)
            {
                return NotFound();
            }

            _context.CourseOfferings.RemoveRange(courseOfferings);
            await _context.SaveChangesAsync();

            return courseOfferings;
        }

        private bool CourseOfferingExists(string id)
        {
            return _context.CourseOfferings.Any(e => e.CourseId == id);
        }
        public class CourseOfferingDTO
        {
            public Course Course { get; set; }
            public List<Offering> Offerings { get; set; }
        }
    }
}
