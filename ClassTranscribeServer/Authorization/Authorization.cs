using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ClassTranscribeServer.Authorization
{
    public class ViewOfferingRequirement : IAuthorizationRequirement { }

    public class EditOfferingRequirement : IAuthorizationRequirement { }

    public class ViewOfferingAuthorizationHandler :
    AuthorizationHandler<ViewOfferingRequirement, Offering>
    {
        CTDbContext _ctDbContext;
        RoleManager<IdentityRole> _roleManager;
        UserManager<ApplicationUser> _userManager;

        public ViewOfferingAuthorizationHandler(CTDbContext ctDbContext, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _ctDbContext = ctDbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       ViewOfferingRequirement requirement,
                                                       Offering offering)
        {
            ApplicationUser user = null;
            if (context.User != null)
            {
                var currentUserID = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                user = _ctDbContext.Users.Where(u => u.Id == currentUserID).First();
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
            return Task.CompletedTask;
        }
    }
}
