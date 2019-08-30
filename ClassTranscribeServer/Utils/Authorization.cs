using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassTranscribeServer.Authorization
{
    public class ReadOfferingRequirement : IAuthorizationRequirement { }
    public class UpdateOfferingRequirement : IAuthorizationRequirement { }
    public class CreateOfferingRequirement : IAuthorizationRequirement { }

    public class UpdateOfferingAuthorizationHandler :
    AuthorizationHandler<UpdateOfferingRequirement, string>
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
                                                       string offeringId)
        {
            var offering = await _ctDbContext.Offerings.FindAsync(offeringId);
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
            var InstructorRoleId = await _roleManager.GetRoleIdAsync(new IdentityRole { Name = Globals.ROLE_INSTRUCTOR });
            if (context.User.IsInRole(Globals.ROLE_ADMIN))
            {
                context.Succeed(requirement);
            }
            if (_ctDbContext.UserOfferings.Where( (uo) => uo.ApplicationUserId == user.Id && uo.OfferingId == offering.Id && uo.IdentityRoleId == InstructorRoleId).Any())
            {
                context.Succeed(requirement);
            }
        }

    }

    public class ReadOfferingAuthorizationHandler :
    AuthorizationHandler<ReadOfferingRequirement, string>
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
                                                       string offeringId)
        {
            var offering = await _ctDbContext.Offerings.FindAsync(offeringId);
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
            else if (offering.AccessType == AccessTypes.UniversityOnly && offering.CourseOfferings.Select(c => c.Course.Department.University).Contains(user.University))
            {
                context.Succeed(requirement);
            }
            else if (offering.AccessType == AccessTypes.StudentsOnly && offering.OfferingUsers.Select(ou => ou.ApplicationUser).Contains(user))
            {
                context.Succeed(requirement);
            }
            else if (context.User.IsInRole(Globals.ROLE_ADMIN))
            {
                context.Succeed(requirement);
            }
        }
    }    
}
