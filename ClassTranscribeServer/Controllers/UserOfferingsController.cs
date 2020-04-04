using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class UserOfferingsController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserOfferingsController(IAuthorizationService authorizationService, CTDbContext context, UserManager<ApplicationUser> userManager, ILogger<UserOfferingsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userManager = userManager;
            _userUtils = new UserUtils(userManager, context);
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
        [Authorize]
        public async Task<ActionResult<UserOffering>> PostUserOffering(UserOfferingDTO userOfferingDTO)
        {
            var offering = await _context.Offerings.FindAsync(userOfferingDTO.OfferingId);
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
        [Authorize]
        public async Task<ActionResult<UserOffering>> DeleteUserOffering(string offeringId, string userId)
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
                else
                {
                    return new ChallengeResult();
                }
            }
            var userOffering = await _context.UserOfferings.Where(uo => uo.OfferingId == offeringId && uo.ApplicationUserId == userId).FirstAsync();
            if (userOffering == null)
            {
                return NotFound();
            }

            _context.UserOfferings.Remove(userOffering);
            await _context.SaveChangesAsync();

            return userOffering;
        }



        [HttpPost("AddUsers/{offeringId}/{roleName}")]
        public async Task<ActionResult<IEnumerable<UserOffering>>> AddUsersToOffering(string offeringId, string roleName, List<string> mailIds)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null || mailIds == null || !mailIds.Any())
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

        [HttpGet("GetUsersOfOffering/{offeringId}/{roleName}")]
        public async Task<ActionResult<IEnumerable<string>>> GetUsersOfOffering(string offeringId, string roleName)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (roleName == null || offering == null)
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

            IdentityRole identityRole = _context.Roles.Where(r => r.Name == roleName).FirstOrDefault();
            return await _context.UserOfferings
                .Where(uo => uo.OfferingId == offeringId && uo.IdentityRoleId == identityRole.Id)
                .Select(uo => uo.ApplicationUser.Email).ToListAsync();
        }

        [HttpDelete("DeleteUserFromOffering/{offeringId}/{roleName}")]
        public async Task<ActionResult> DeleteUserFromOffering(string offeringId, string roleName, List<string> mailIds)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (roleName == null || offering == null || mailIds == null || !mailIds.Any())
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

            IdentityRole identityRole = _context.Roles.Where(r => r.Name == roleName).FirstOrDefault();
            var userIds = await _context.Users.Where(u => mailIds.Contains(u.Email)).Select(u => u.Id).ToListAsync();

            var uo = await _context.UserOfferings
                .Where(uo => uo.OfferingId == offeringId && uo.IdentityRoleId == identityRole.Id && userIds.Contains(uo.ApplicationUserId))
                .ToListAsync();
            _context.UserOfferings.RemoveRange(uo);
            await _context.SaveChangesAsync();
            return Ok();
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
