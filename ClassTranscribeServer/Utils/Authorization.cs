using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Authorization
{
    public class ReadOfferingRequirement : IAuthorizationRequirement { }
    public class UpdateOfferingRequirement : IAuthorizationRequirement { }
    public class UpdateOfferingAuthorizationHandler :
    AuthorizationHandler<UpdateOfferingRequirement, Offering>
    {
        private readonly CTDbContext _ctDbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserUtils _userUtils;

        public UpdateOfferingAuthorizationHandler(CTDbContext ctDbContext, RoleManager<IdentityRole> roleManager, UserUtils userUtils)
        {
            _ctDbContext = ctDbContext;
            _roleManager = roleManager;
            _userUtils = userUtils;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       UpdateOfferingRequirement requirement,
                                                       Offering offering)
        {
            if (context == null)
            {
                return;
            }

            var user = await _userUtils.GetUser(context.User);
            var InstructorRole = await _roleManager.FindByNameAsync(Globals.ROLE_INSTRUCTOR);
            if (context.User.IsInRole(Globals.ROLE_ADMIN))
            {
                context.Succeed(requirement);
            }
            if (_ctDbContext.UserOfferings.Where((uo) => uo.ApplicationUserId == user.Id && uo.OfferingId == offering.Id && uo.IdentityRoleId == InstructorRole.Id).Any())
            {
                context.Succeed(requirement);
            }
            if (user != null)
            {
                _ctDbContext.Entry(user).State = EntityState.Detached;
            }
        }
    }

    public class ReadOfferingAuthorizationHandler :
    AuthorizationHandler<ReadOfferingRequirement, Offering>
    {
        private readonly CTDbContext _ctDbContext;
        private readonly UserUtils _userUtils;

        public ReadOfferingAuthorizationHandler(CTDbContext ctDbContext, UserUtils userUtils)
        {
            _ctDbContext = ctDbContext;
            _userUtils = userUtils;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       ReadOfferingRequirement requirement,
                                                       Offering offering)
        {
            if (context == null)
            {
                return;
            }

            var user = await _userUtils.GetUser(context.User);
            
            if (offering != null && offering.AccessType == AccessTypes.Public)
            {
                context.Succeed(requirement);
            }
            else if (offering != null && offering.AccessType == AccessTypes.AuthenticatedOnly && user != null)
            {
                context.Succeed(requirement);
            }
            else if (offering != null && offering.AccessType == AccessTypes.UniversityOnly && user != null)
            {
                var universityId = await _ctDbContext.CourseOfferings
                    .Include(co=>co.Course).ThenInclude(c=>c.Department)
                    .Where(co => co.OfferingId == offering.Id)
                    .Select(c => c.Course.Department.UniversityId).FirstAsync();
                if (user.UniversityId == universityId)
                {
                    context.Succeed(requirement);
                }
            }
            else if (offering != null && offering.AccessType == AccessTypes.StudentsOnly && user != null && offering.OfferingUsers.Select(ou => ou.ApplicationUser).Contains(user))
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
