using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System.Security.Claims;
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ClassTranscribeServer.Utils;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OfferingsController : ControllerBase
    {
        private readonly CTDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;
        private readonly UserManager<ApplicationUser> _userManager;

        public OfferingsController(CTDbContext context, IAuthorizationService authorizationService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _authorizationService = authorizationService;
            _userManager = userManager;
            _userUtils = new UserUtils(userManager, context);
        }

        // GET: api/Offerings/ByStudent
        // TODO: Implement Authorization
        /// <summary>
        /// Gets all Offerings for a student by userId
        /// </summary>
        [HttpGet("ByStudent")]
        public async Task<ActionResult<IEnumerable<OfferingListDTO>>> GetOfferingsByStudent()
        {
            // Get the user
            ApplicationUser user = null;
            if (User.Identity.IsAuthenticated && this.User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                var userId = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = await _context.Users.FindAsync(userId);
            }

            // Store the results
            List<Offering> offerings = new List<Offering>();

            // Get all the public offerings
            var public_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.Public).ToListAsync();
            offerings.AddRange(public_offerings);

            // Get all offering that need authentication
            if (user != null)
            {
                var authen_offerings = await _context.Offerings.Where(offer => offer.AccessType == AccessTypes.AuthenticatedOnly).ToListAsync();
                offerings.AddRange(authen_offerings);

                // Get all their university's offerings
                var university_offerings = await _context.Courses.Where(c => c.Department.University == user.University)
                                                            .SelectMany(c => c.CourseOfferings)
                                                            .Select(co => co.Offering)
                                                            .Where(o => o.AccessType == AccessTypes.UniversityOnly).ToListAsync();
                offerings.AddRange(university_offerings);

                // Get all offering that this user is a member
                var member_offerings = await _context.UserOfferings.Where(uo => uo.ApplicationUserId == user.Id && uo.Offering.AccessType == AccessTypes.StudentsOnly)
                    .Select(uo => uo.Offering).ToListAsync();
                offerings.AddRange(member_offerings);
            }

            var filteredOfferings = offerings.FindAll(o => o.Playlists.SelectMany(m => m.Medias).Count() > 0).OrderBy(o => o.Term.StartDate).ToList();

            var offeringListDTO = filteredOfferings.Select(o => new OfferingListDTO
            {
                Offering = o,
                Courses = o.CourseOfferings.Select(co => new CourseDTO { 
                    CourseNumber = co.Course.CourseNumber,
                    DepartmentId = co.Course.DepartmentId,
                    DepartmentAcronym = co.Course.Department.Acronym}).ToList(),
                //Courses = await _context.CourseOfferings.Where(co => co.OfferingId == o.Id).Select(co => co.Course).ToListAsync(),
                Term = o.Term
            }).ToList();

            // return the combined result
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
                Courses = await _context.CourseOfferings.Where(co => co.OfferingId == offering.Id).Select(co => co.Course).ToListAsync(),
                InstructorIds = await _context.UserOfferings
                .Where(uo => uo.OfferingId == offering.Id && uo.IdentityRole.Name == Globals.ROLE_INSTRUCTOR)
                .Select(uo => new ApplicationUser
                {
                    Id = uo.ApplicationUser.Id,
                    Email = uo.ApplicationUser.Email
                }).ToListAsync()
            };

            return offeringDTO;
        }

        // PUT: api/Offerings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffering(string id, Offering offering)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, id, Globals.POLICY_UPDATE_OFFERING);
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
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_INSTRUCTOR + "," + Globals.ROLE_TEACHING_ASSISTANT)]
        public async Task<ActionResult<Offering>> PostNewOffering(NewOfferingDTO newOfferingDTO)
        {
            _context.Offerings.Add(newOfferingDTO.Offering);
            await _context.SaveChangesAsync();
            _context.CourseOfferings.Add(new CourseOffering
            {
                CourseId = newOfferingDTO.CourseId,
                OfferingId = newOfferingDTO.Offering.Id
            });
            if (User.Identity.IsAuthenticated && this.User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                var userId = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                await _context.UserOfferings.AddAsync(new UserOffering
                {
                    ApplicationUserId = userId,
                    IdentityRole = _context.Roles.Where(r => r.Name == Globals.ROLE_INSTRUCTOR).FirstOrDefault(),
                    OfferingId = newOfferingDTO.Offering.Id
                });
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction("GetOffering", new { id = newOfferingDTO.Offering.Id }, newOfferingDTO.Offering);
        }

        [HttpPost("AddUsers/{offeringId}/{roleName}")]
        public async Task<ActionResult<IEnumerable<UserOffering>>> AddUsersToOffering(string offeringId, string roleName, List<string> mailIds)
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
            List<UserOffering> userOfferings = new List<UserOffering>();
            IdentityRole identityRole = _context.Roles.Where(r => r.Name == roleName).FirstOrDefault();
            foreach (string mailId in mailIds)
            {
                var user = await _userManager.FindByEmailAsync(mailId);
                if (user == null)
                {
                    user = await _userUtils.CreateNonExistentUser(mailId);
                }
                userOfferings.Add(new UserOffering
                {
                    ApplicationUserId = user.Id,
                    IdentityRole = identityRole,
                    OfferingId = offeringId
                });
            }

            foreach (var uo in userOfferings)
            {
                if (!(await _context.UserOfferings.Where(u => u.ApplicationUserId == uo.ApplicationUserId
                 && u.IdentityRoleId == uo.IdentityRole.Id
                 && u.OfferingId == uo.OfferingId).AnyAsync()))
                {
                    await _context.UserOfferings.AddAsync(uo);
                }
            }
            await _context.SaveChangesAsync();
            return userOfferings;
        }

        // DELETE: api/Offerings/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Offering>> DeleteOffering(string id)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, id, Globals.POLICY_UPDATE_OFFERING);
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

        }

        public class OfferingDTO
        {
            public Offering Offering { get; set; }
            public List<Course> Courses { get; set; }
            public List<ApplicationUser> InstructorIds { get; set; }
            public Term Term { get; set; }
        }

        public class OfferingListDTO 
        {
            public Offering Offering { get; set; }
            public List<CourseDTO> Courses { get; set; }
            public Term Term { get; set; }
        }

        public class CourseDTO
        {
            public string CourseName { get; set; }
            public string CourseNumber { get; set; }
            public string Description { get; set; }
            public string DepartmentId { get; set; }
            public string DepartmentAcronym { get; set; }
        }

    }

}
