using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests
{
    public class MockUserManager : UserManager<ApplicationUser>
    {
        public MockUserManager()
            : base(new Mock<IUserStore<ApplicationUser>>().Object,
                   new Mock<IOptions<IdentityOptions>>().Object,
                   new Mock<IPasswordHasher<ApplicationUser>>().Object,
                   new IUserValidator<ApplicationUser>[0],
                   new IPasswordValidator<ApplicationUser>[0],
                   new Mock<ILookupNormalizer>().Object,
                   new Mock<IdentityErrorDescriber>().Object,
                   new Mock<IServiceProvider>().Object,
                   new Mock<ILogger<UserManager<ApplicationUser>>>().Object)
        { }

        public override Task<ApplicationUser> FindByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return Task.FromResult(new ApplicationUser { Id = email, Email = email });
        }

        public override Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            return Task.FromResult((IList<string>) new List<string>());
        }

        public override Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role)
        {
            return Task.FromResult(new IdentityResult());
        }

        public override Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            return Task.FromResult(true);
        }
    }

    public class MockRoleManager : RoleManager<IdentityRole>
    {
        public MockRoleManager()
            : base(new Mock<IRoleStore<IdentityRole>>().Object,
                   new IRoleValidator<IdentityRole>[0],
                   new Mock<ILookupNormalizer>().Object,
                   new Mock<IdentityErrorDescriber>().Object,
                   new Mock<ILogger<RoleManager<IdentityRole>>>().Object)
        { }

        public override Task<bool> RoleExistsAsync(string roleName)
        {
            return Task.FromResult(true);
        }
    }

    public class MockSignInManager : SignInManager<ApplicationUser>
    {
        public MockSignInManager()
            : base(new MockUserManager(),
                   new Mock<IHttpContextAccessor>().Object,
                   new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                   new Mock<IOptions<IdentityOptions>>().Object,
                   new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                   new Mock<IAuthenticationSchemeProvider>().Object,
                   new Mock<IUserConfirmation<ApplicationUser>>().Object)
        { }

        public override Task SignInAsync(ApplicationUser user, bool isPersistent, string authenticationMethod = null)
        {
            return Task.CompletedTask;
        }
    }
}