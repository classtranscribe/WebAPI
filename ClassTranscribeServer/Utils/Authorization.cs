using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Authorization
{
    public class ReadOfferingRequirement : IAuthorizationRequirement { }
    public class UpdateOfferingRequirement : IAuthorizationRequirement { }
    public class UpdateOfferingAuthorizationHandler :
    AuthorizationHandler<UpdateOfferingRequirement, Offering>
    {
        CTDbContext _ctDbContext;
        RoleManager<IdentityRole> _roleManager;
        UserManager<ApplicationUser> _userManager;

        public UpdateOfferingAuthorizationHandler(CTDbContext ctDbContext, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _ctDbContext = ctDbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       UpdateOfferingRequirement requirement,
                                                       Offering offering)
        {
            ApplicationUser user = null;
            if (context.User == null || context.User.FindFirst(ClaimTypes.NameIdentifier) == null)
            {
                return;
            }
            else
            {
                var currentUserID = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = _ctDbContext.Users.Where(u => u.Id == currentUserID).First();
            }
            var InstructorRole = await _roleManager.FindByNameAsync(Globals.ROLE_INSTRUCTOR);
            if (context.User.IsInRole(Globals.ROLE_ADMIN))
            {
                context.Succeed(requirement);
            }
            if (_ctDbContext.UserOfferings.Where((uo) => uo.ApplicationUserId == user.Id && uo.OfferingId == offering.Id && uo.IdentityRoleId == InstructorRole.Id).Any())
            {
                context.Succeed(requirement);
            }
            _ctDbContext.Entry(user).State = EntityState.Detached;
        }

    }

    public class ReadOfferingAuthorizationHandler :
    AuthorizationHandler<ReadOfferingRequirement, Offering>
    {
        CTDbContext _ctDbContext;
        RoleManager<IdentityRole> _roleManager;
        UserManager<ApplicationUser> _userManager;

        public ReadOfferingAuthorizationHandler(CTDbContext ctDbContext, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _ctDbContext = ctDbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       ReadOfferingRequirement requirement,
                                                       Offering offering)
        {
            ApplicationUser user = null;
            if (context.User != null && context.User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                var currentUserID = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = await _ctDbContext.Users.Where(u => u.Id == currentUserID).FirstAsync();
            }

            if (offering.AccessType == AccessTypes.Public)
            {
                context.Succeed(requirement);
            }
            else if (offering.AccessType == AccessTypes.AuthenticatedOnly && user != null)
            {
                context.Succeed(requirement);
            }
            else if (offering.AccessType == AccessTypes.UniversityOnly)
            {
                var universityId = await _ctDbContext.CourseOfferings.Where(co => co.OfferingId == offering.Id)
                    .Select(c => c.Course.Department.UniversityId).FirstAsync();
                if (user.UniversityId == universityId)
                {
                    context.Succeed(requirement);
                }
            }
            else if (offering.AccessType == AccessTypes.StudentsOnly && offering.OfferingUsers.Select(ou => ou.ApplicationUser).Contains(user))
            {
                context.Succeed(requirement);
            }
            if (context.User.IsInRole(Globals.ROLE_ADMIN))
            {
                context.Succeed(requirement);
            }
            if (user != null)
            {
                _ctDbContext.Entry(user).State = EntityState.Detached;
            }
        }
    }
}
