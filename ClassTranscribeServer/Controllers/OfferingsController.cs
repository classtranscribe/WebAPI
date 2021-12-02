using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfferingsController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;

        public OfferingsController(IAuthorizationService authorizationService,
            CTDbContext context, UserUtils userUtils, ILogger<OfferingsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userUtils = userUtils;
        }

        // GET: api/Offerings/ByStudent
        // TODO: Implement Authorization
        /// <summary>
        /// Gets all Offerings for a student by userId
        /// </summary>
        [HttpGet("ByStudent")]
        public async Task<ActionResult<IEnumerable<OfferingDTO>>> GetOfferingsByStudent()
        {
            // Store the results
            List<Offering> offerings = await _context.Offerings.ToListAsync();

            // Only include offerings that have existing media items and are published
            return offerings
                .FindAll(o => o.Playlists != null && o.Playlists.SelectMany(m => m.Medias).Any() && o.PublishStatus == PublishStatus.Published)
                .OrderBy(o => o.Term.StartDate)
                .Select(o => GetOfferingDTO(o))
                .ToList();
        }

        // GET: api/Offerings/ByInstructor/{userId}
        /// <summary>
        /// Gets all offerings for an instructor
        /// </summary>
        [HttpGet("ByInstructor/{userId}")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult<IEnumerable<OfferingDTO>>> GetOfferingsByInstructor(string userId)
        {
            var user = await _userUtils.GetUser(User);

            // This endpoint should be accessible only for the instructor who send the request (and admins)
            if (user == null || (user.Id != userId && !User.IsInRole(Globals.ROLE_ADMIN)))
            {
                return Unauthorized();
            }

            // Store the results
            List<Offering> offerings = await _context.Offerings.ToListAsync();

            return offerings
                .FindAll(o => o.OfferingUsers.Where(uo => uo.ApplicationUserId == userId && uo.IdentityRole.Name == Globals.ROLE_INSTRUCTOR).Any())
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => GetOfferingDTO(o))
                .ToList();
        }

        // GET: api/Offerings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OfferingDTO>> GetOffering(string id)
        {

            var offering = await _context.Offerings.FindAsync(id);

            if (offering == null)
            {
                return NotFound();
            }

            return GetOfferingDTO(offering);
        }

        // PUT: api/Offerings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffering(string id, Offering offering)
        {
            if (offering == null || offering.Id == null || id != offering.Id)
            {
                return BadRequest("Invalid Offering.Id");
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

        // PUT: api/Offerings/5
        [HttpPut("JsonMetadata/{id}")]
        public async Task<IActionResult> PutOffering(string id, JObject jsonMetadata)
        {
            var offering = await _context.Offerings.FindAsync(id);
            if (offering == null)
            {
                return BadRequest("Invalid Offering.Id");
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
            offering.JsonMetadata = jsonMetadata ?? new JObject();
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
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_INSTRUCTOR + "," + Globals.ROLE_TEACHING_ASSISTANT)]
        public async Task<ActionResult<Offering>> PostNewOffering(NewOfferingDTO newOfferingDTO)
        {
            if (newOfferingDTO == null)
            {
                return BadRequest();
            }

            if (newOfferingDTO.CourseId == null)
            {
                if (newOfferingDTO.DepartmentId == null || newOfferingDTO.NewCourseNumber == null)
                {
                    return BadRequest("Must specify departmentId and newCourseNumber");
                }

                var isValidDept = (await _context.Departments.FindAsync(newOfferingDTO.DepartmentId)) != null;

                if (!isValidDept)
                {
                    return BadRequest("Invalid department ID");
                }

                var course = await _context.Courses.Where(c => c.CourseNumber == newOfferingDTO.NewCourseNumber && c.DepartmentId == newOfferingDTO.DepartmentId).FirstOrDefaultAsync();

                if (course == null)
                {
                    course = new Course
                    {
                        DepartmentId = newOfferingDTO.DepartmentId,
                        CourseNumber = newOfferingDTO.NewCourseNumber
                    };

                    await _context.Courses.AddAsync(course);
                    await _context.SaveChangesAsync();
                    await FileRecord.SetFilePath(_context, course);
                }

                newOfferingDTO.CourseId = course.Id;
            }

            _context.Offerings.Add(newOfferingDTO.Offering);
            await _context.SaveChangesAsync();

            var courseOffering = new CourseOffering
            {
                CourseId = newOfferingDTO.CourseId,
                OfferingId = newOfferingDTO.Offering.Id
            };

            _context.CourseOfferings.Add(courseOffering);
            await _context.SaveChangesAsync();
            await FileRecord.SetFilePath(_context, courseOffering);

            var user = await _userUtils.GetUser(User);

            if (user != null)
            {
                await _context.UserOfferings.AddAsync(new UserOffering
                {
                    ApplicationUserId = user.Id,
                    IdentityRole = _context.Roles.Where(r => r.Name == Globals.ROLE_INSTRUCTOR).FirstOrDefault(),
                    OfferingId = newOfferingDTO.Offering.Id
                });

                await _context.SaveChangesAsync();
            }

            return CreatedAtAction("GetOffering", new { id = newOfferingDTO.Offering.Id }, newOfferingDTO.Offering);
        }

        // DELETE: api/Offerings/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Offering>> DeleteOffering(string id)
        {
            var offering = await _context.Offerings.FindAsync(id);
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

            _context.Offerings.Remove(offering);
            await _context.SaveChangesAsync();

            return offering;
        }

        private bool OfferingExists(string id)
        {
            return _context.Offerings.Any(e => e.Id == id);
        }

        private static OfferingDTO GetOfferingDTO(Offering offering)
        {
            return new OfferingDTO
            {
                Offering = offering,
                Courses = offering.CourseOfferings
                .Select(co => new CourseDTO
                {
                    CourseId = co.Course.Id,
                    CourseNumber = co.Course.CourseNumber,
                    DepartmentId = co.Course.DepartmentId,
                    DepartmentAcronym = co.Course.Department.Acronym
                }).ToList(),
                Term = offering.Term,
                InstructorIds = offering.OfferingUsers
                .Where(uo => uo.IdentityRole.Name == Globals.ROLE_INSTRUCTOR)
                .Select(uo => new ApplicationUser
                {
                    Id = uo.ApplicationUser.Id,
                    Email = uo.ApplicationUser.Email,
                    FirstName = uo.ApplicationUser.FirstName,
                    LastName = uo.ApplicationUser.LastName
                }).ToList()
            };
        }

        public class NewOfferingDTO
        {
            public Offering Offering { get; set; }
            public string CourseId { get; set; }
            public string DepartmentId { get; set; }
            public string NewCourseNumber { get; set; }
        }

        public class OfferingDTO
        {
            public Offering Offering { get; set; }
            public List<CourseDTO> Courses { get; set; }
            public List<ApplicationUser> InstructorIds { get; set; }
            public Term Term { get; set; }
        }

        public class CourseDTO
        {
            public string CourseId { get; set; }
            public string CourseName { get; set; }
            public string CourseNumber { get; set; }
            public string Description { get; set; }
            public string DepartmentId { get; set; }
            public string DepartmentAcronym { get; set; }
        }

    }

}
