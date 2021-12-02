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
            if (courseOffering == null)
            {
                return BadRequest();
            }

            // The CourseOffering must be linked to a Course in order for the FilePath field to be set correctly
            var linkedCourse = await _context.Courses.FindAsync(courseOffering.CourseId);
            if (linkedCourse == null)
            {
                return BadRequest("CourseOfferings must be linked to a Course");
            }

            var existingCourseOffering = await _context.CourseOfferings
                .Where(c => c.CourseId == courseOffering.CourseId && c.OfferingId == courseOffering.OfferingId)
                .FirstOrDefaultAsync();

            if (existingCourseOffering != null)
            {
                return Ok("Course Offering already exists!");
            }

            _context.CourseOfferings.Add(courseOffering);
            await _context.SaveChangesAsync();
            await FileRecord.SetFilePath(_context, courseOffering);

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
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
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

        public class CourseOfferingDTO
        {
            public Course Course { get; set; }
            public List<Offering> Offerings { get; set; }
        }
    }
}
