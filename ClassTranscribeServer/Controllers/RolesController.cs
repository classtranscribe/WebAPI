using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly CTDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUtils _userutils;

        public RolesController(CTDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _userutils = new UserUtils(userManager, context);
        }

        // POST: api/Roles
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult> PostNewInstructor(string mailId)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(mailId);
            if(user == null)
            {
                user = await _userutils.CreateNonExistentUser(mailId);
            }
            if (!(await _userManager.IsInRoleAsync(user, Globals.ROLE_INSTRUCTOR)))
            {
                await _userManager.AddToRoleAsync(user, Globals.ROLE_INSTRUCTOR);
            }
            return Ok();
        }

        // POST: api/Roles
        [HttpDelete]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult> RemoveInstructor(string mailId)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(mailId);
            await _userManager.RemoveFromRoleAsync(user, Globals.ROLE_INSTRUCTOR);
            return Ok();
        }

        // POST: api/Roles
        [HttpGet]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<List<ApplicationUser>> GetInstructors(string universityId)
        {
            var instructorRoleId = _context.Roles.Where(r => r.Name == Globals.ROLE_INSTRUCTOR).First().Id;

            var userIds = (from user in _context.Users
                           join ur in _context.UserRoles on user.Id equals ur.UserId
                           where user.UniversityId == universityId && ur.RoleId == instructorRoleId
                            select new ApplicationUser { Id = user.Id, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName }).ToList();
            return userIds; 
        }
    }
}
