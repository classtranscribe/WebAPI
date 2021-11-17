using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class RolesControllerTest : BaseControllerTest
    {
        private readonly RolesController _controller;

        public RolesControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new RolesController(
                (MockRoleManager)fixture._serviceProvider.GetService(typeof(MockRoleManager)),
                (MockUserManager)fixture._serviceProvider.GetService(typeof(MockUserManager)),
                _context,
                null,
                _userUtils
            );
        }

        [Fact]
        public void Get_Instructors()
        {
            var instructorId = "instructor";
            IdentityRole instructorRole = new IdentityRole
            {
                Id = instructorId,
                Name = Globals.ROLE_INSTRUCTOR,
                NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper()
            };
            _context.Roles.Add(instructorRole);
            _context.SaveChanges();

            var instructors = _controller.GetInstructors("non-existent-university");
            Assert.Empty(instructors);

            _context.Universities.AddRange(new University { Id = "UIUC" }, new University { Id = "UCLA" });
            _context.Users.AddRange(
                new ApplicationUser { Id = "001", UniversityId = "UIUC" },
                new ApplicationUser { Id = "002", UniversityId = "UIUC" },
                new ApplicationUser { Id = "003", UniversityId = "UIUC" },
                new ApplicationUser { Id = "004", UniversityId = "UCLA" }
            );
            _context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    RoleId = instructorId,
                    UserId = "001"
                },
                new IdentityUserRole<string>
                {
                    RoleId = instructorId,
                    UserId = "002"
                },
                new IdentityUserRole<string>
                {
                    RoleId = instructorId,
                    UserId = "004"
                }
            );
            _context.SaveChanges();

            instructors = _controller.GetInstructors("UIUC");
            Assert.Equal(2, instructors.Count);

            var instructorIds = instructors.Select(inst => inst.Id);
            Assert.Contains("001", instructorIds);
            Assert.Contains("002", instructorIds);
        }

        [Fact]
        public async Task Add_User_To_Role()
        {
            var result = await _controller.AddUserToRole("example", "role");
            Assert.IsType<OkResult>(result);

            await Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.AddUserToRole(null, null));

            await Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.AddUserToRole(null, "role"));
        }

        [Fact]
        public async Task Remove_Instructor()
        {
            var result = await _controller.RemoveInstructor("example");
            Assert.IsType<OkResult>(result);

            await Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.RemoveInstructor(null));
        }
    }
}