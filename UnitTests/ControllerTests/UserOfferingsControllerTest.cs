using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static ClassTranscribeServer.Controllers.UserOfferingsController;

namespace UnitTests.ControllerTests
{
    public class UserOfferingsControllerTest : BaseControllerTest
    {
        private readonly UserOfferingsController _controller;

        public UserOfferingsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new UserOfferingsController(
                fixture._authorizationService,
                _context,
                (MockUserManager) fixture._serviceProvider.GetService(typeof(MockUserManager)),
                _userUtils,
                null
            );
        }

        [Fact]
        public async Task Basic_Post_And_Delete_UserOffering()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var userOfferingDTO = new UserOfferingDTO
            {
                OfferingId = offeringId,
                UserId = TestGlobals.TEST_USER_ID,
                RoleName = Globals.ROLE_INSTRUCTOR
            };

            var postResult = await _controller.PostUserOffering(userOfferingDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Single(getResult.Value);
            Assert.Equal(Status.Active, getResult.Value.ElementAt(0).IsDeletedStatus);

            var deleteResult = await _controller.DeleteUserOffering(offeringId, TestGlobals.TEST_USER_ID);
            AssertUserOffering(deleteResult.Value, userOfferingDTO);

            getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Invalid_and_Valid_UserOfferings()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var userOfferingDTO = new UserOfferingDTO
            {
                OfferingId = "non-existing",
                UserId = TestGlobals.TEST_USER_ID,
                RoleName = Globals.ROLE_INSTRUCTOR
            };

            var postResult = await _controller.PostUserOffering(userOfferingDTO);
            Assert.IsType<BadRequestResult>(postResult.Result);

            userOfferingDTO.OfferingId = offeringId;

            postResult = await _controller.PostUserOffering(userOfferingDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);
        }

        [Fact]
        public async Task Add_Users_To_Offerings()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var userOfferingDTO = new UserOfferingDTO
            {
                OfferingId = offeringId,
                UserId = TestGlobals.TEST_USER_ID,
                RoleName = Globals.ROLE_INSTRUCTOR
            };

            var postResult = await _controller.PostUserOffering(userOfferingDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            List<string> mailIds = new List<string>() { "mailId1@test.edu", "mailId2@test.edu" };
            var addResult = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_INSTRUCTOR, mailIds);
            Assert.Equal(2, addResult.Value.Count());

            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(3, getResult.Value.Count());
        }

        [Fact]
        public async Task Add_Users_To_Offerings_Invalid()
        {
            var addResult = await _controller.AddUsersToOffering("none", "none", new List<string>());
            Assert.IsType<BadRequestResult>(addResult.Result);

            addResult = await _controller.AddUsersToOffering(null, null, null);
            Assert.IsType<BadRequestResult>(addResult.Result);
        }

        [Fact]
        public async Task Get_Users_Of_Offering()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var userIds = new List<string>() { "example1", "example2" };

            foreach (var userId in userIds)
            {
                _context.Users.Add(new ApplicationUser { Id = userId, Email = userId });

                var postResult = await _controller.PostUserOffering(new UserOfferingDTO
                {
                    OfferingId = offeringId,
                    UserId = userId,
                    RoleName = Globals.ROLE_INSTRUCTOR
                });

                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }

            var getResult = await _controller.GetUsersOfOffering(offeringId, Globals.ROLE_INSTRUCTOR);
            Assert.Equal(2, getResult.Value.Count());
            Assert.Contains(userIds[0], getResult.Value);
            Assert.Contains(userIds[1], getResult.Value);
        }

        [Fact]
        public async Task Get_Users_Of_Offering_BadRequest()
        {
            var getResult = await _controller.GetUsersOfOffering("none", "none");
            Assert.IsType<BadRequestResult>(getResult.Result);

            getResult = await _controller.GetUsersOfOffering(null, null);
            Assert.IsType<BadRequestResult>(getResult.Result);
        }

        [Fact]
        public async Task Delete_UserOffering_Bad_Request()
        {
            var deleteResult = await _controller.DeleteUserOffering("not_existing", "not_existing");
            Assert.IsType<BadRequestResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteUserOffering(null, null);
            Assert.IsType<BadRequestResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_UserOffering_Empty()
        {
            var getResult = await _controller.GetUserOfferingsByOfferingId("not_existing");
            Assert.Empty(getResult.Value);

            getResult = await _controller.GetUserOfferingsByOfferingId(null);
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Null_UserOffering()
        {
            var postResult = await _controller.PostUserOffering(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }

        [Fact]
        public async Task Delete_Users_From_Offerings_Invalid()
        {
            var deleteResult = await _controller.DeleteUserFromOffering("none", "none", new List<string>());
            Assert.IsType<BadRequestResult>(deleteResult);

            deleteResult = await _controller.DeleteUserFromOffering(null, null, null);
            Assert.IsType<BadRequestResult>(deleteResult);
        }

        private void AssertUserOffering(UserOffering userOffering, UserOfferingDTO userOfferingDTO)
        {
            Assert.Equal(userOfferingDTO.OfferingId, userOffering.OfferingId);
            Assert.Equal(userOfferingDTO.UserId, userOffering.ApplicationUserId);
            Assert.Equal(userOfferingDTO.RoleName, userOffering.IdentityRole.Name);
        }

        private void SetupEntities(string offeringId)
        {
            _context.Offerings.Add(new Offering
            {
                Id = offeringId,
                SectionName = "example_section",
                TermId = "example_term"
            });

            IdentityRole instructorRole = new IdentityRole
            {
                Name = Globals.ROLE_INSTRUCTOR,
                NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper()
            };
            _context.Roles.Add(instructorRole);

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            _context.SaveChanges();
        }
    }
}
