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
using System.Security.Claims;
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


            // Filter out offerings where there is no media items available.
            var filteredOfferings = offerings.FindAll(o => o.Playlists.SelectMany(m => m.Medias).Any()).OrderBy(o => o.Term.StartDate).ToList();

            var offeringListDTO = filteredOfferings.Select(o => new OfferingDTO
            {
                Offering = o,
                Courses = o.CourseOfferings.Select(co => new CourseDTO
                {
                    CourseId = co.Course.Id,
                    CourseNumber = co.Course.CourseNumber,
                    DepartmentId = co.Course.DepartmentId,
                    DepartmentAcronym = co.Course.Department.Acronym
                }).ToList(),
                Term = o.Term,
                InstructorIds = o.OfferingUsers
                .Where(uo => uo.IdentityRole.Name == Globals.ROLE_INSTRUCTOR)
                .Select(uo => new ApplicationUser
                {
                    Id = uo.ApplicationUser.Id,
                    Email = uo.ApplicationUser.Email,
                    FirstName = uo.ApplicationUser.FirstName,
                    LastName = uo.ApplicationUser.LastName
                }).ToList()
            }).ToList();

            return offeringListDTO;
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

            OfferingDTO offeringDTO = new OfferingDTO
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

            return offeringDTO;
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
                else
                {
                    return new ChallengeResult();
                }
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
                else
                {
                    return new ChallengeResult();
                }
            }
            offering.JsonMetadata = jsonMetadata;
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

                var isValidDept = await _context.Departments.AnyAsync(d => d.Id == newOfferingDTO.DepartmentId);

                if (!isValidDept)
                {
                    return BadRequest("Invalid department ID");
                }

                if (await _context.Courses.AnyAsync(c => c.CourseNumber == newOfferingDTO.NewCourseNumber && c.DepartmentId == newOfferingDTO.DepartmentId))
                {
                    var existingCourse = await _context.Courses.Where(c => c.CourseNumber == newOfferingDTO.NewCourseNumber && c.DepartmentId == newOfferingDTO.DepartmentId).FirstAsync();
                    newOfferingDTO.CourseId = existingCourse.Id;
                }
                else
                {
                    var newCourse = new Course
                    {
                        DepartmentId = newOfferingDTO.DepartmentId,
                        CourseNumber = newOfferingDTO.NewCourseNumber
                    };

                    await _context.Courses.AddAsync(newCourse);
                    await _context.SaveChangesAsync();

                    newOfferingDTO.CourseId = newCourse.Id;
                }
            }

            _context.Offerings.Add(newOfferingDTO.Offering);
            await _context.SaveChangesAsync();

            _context.CourseOfferings.Add(new CourseOffering
            {
                CourseId = newOfferingDTO.CourseId,
                OfferingId = newOfferingDTO.Offering.Id
            });

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
                else
                {
                    return new ChallengeResult();
                }
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
