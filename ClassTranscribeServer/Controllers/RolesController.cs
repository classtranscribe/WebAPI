using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public RolesController(CTDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // POST: api/Roles
        [HttpPost]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task PostNewInstructor(string mailId)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(mailId);
            await _userManager.AddToRoleAsync(user, Globals.ROLE_INSTRUCTOR);
        }

        // POST: api/Roles
        [HttpDelete]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task RemoveInstructor(string mailId)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(mailId);
            await _userManager.RemoveFromRoleAsync(user, Globals.ROLE_INSTRUCTOR);
        }

        // POST: api/Roles
        [HttpGet]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<List<ApplicationUser>> GetInstructors(string universityId)
        {
            var instructorRoleId = await _roleManager.GetRoleIdAsync(new IdentityRole { Name = Globals.ROLE_INSTRUCTOR });
            var userRoles = _context.UserRoles.Where(ur => ur.RoleId == instructorRoleId);
            var userIds = _context.Users.Where(u => u.UniversityId == universityId).Join(userRoles, u => u.Id, ur => ur.UserId, (u, ur) => new ApplicationUser
            {
                Id = u.Id
            }).ToList();
            return userIds; 
        }
    }
}
