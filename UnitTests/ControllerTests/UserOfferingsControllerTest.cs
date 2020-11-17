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
            AssertUserOffering(deleteResult.Value[0], userOfferingDTO);

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

            var roles = new List<string>() { Globals.ROLE_INSTRUCTOR };
            AddUserOfferingsOneToMany(offeringId, TestGlobals.TEST_USER_ID, roles);

            List<string> mailIds = new List<string>() { "mailId1@test.edu", "mailId2@test.edu" };
            var addResult = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_INSTRUCTOR, mailIds);
            Assert.Equal(2, addResult.Value.Count());

            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(3, getResult.Value.Count());
        }

        [Fact]
        public async Task Add_Multirole_User_To_Offerings()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var roles = new List<string>() { Globals.ROLE_INSTRUCTOR, Globals.ROLE_STUDENT };
            AddUserOfferingsOneToMany(offeringId, TestGlobals.TEST_USER_ID , roles);
            var userResult = _context.UserOfferings.Where(u => u.ApplicationUserId == TestGlobals.TEST_USER_ID);
            Assert.Equal(2, userResult.Count());
            Assert.Equal(roles, userResult.Select(u => u.IdentityRole.Name).ToList());

            List<string> mailIds = new List<string>() { "mailId1@test.edu" };
            var addResultInstructor = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_INSTRUCTOR, mailIds);
            var addResultStudent = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            var addResultTA = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_TEACHING_ASSISTANT, mailIds);
            Assert.Equal(1, addResultInstructor.Value.Count());
            Assert.Equal(1, addResultStudent.Value.Count());
            Assert.Equal(1, addResultTA.Value.Count());

            var userId = addResultInstructor.Value.First().ApplicationUserId;
            var userResult1 = _context.UserOfferings.Where(u => u.ApplicationUserId == userId);
            Assert.Equal(3, userResult1.Count());

            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(5, getResult.Value.Count());
        }

        [Fact]
        public async Task Add_Samerole_User_To_Offerings()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            List<string> mailIds = new List<string>() { "mailId1@test.edu" };
            var addResultStudent1 = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            Assert.Equal(1, addResultStudent1.Value.Count());
            var addResultStudent = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_STUDENT, mailIds);

            var getResult1 = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(1, getResult1.Value.Count());
        }

        [Fact]
        public async Task Switch_Student_To_Instructor()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);
            
            var email = "mailId1@test.edu";
            AddUser(email);

            List<string> mailIds = new List<string>() { email };
            var addResultStudent = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            Assert.Equal(1, addResultStudent.Value.Count());

            await _controller.DeleteUserFromOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Empty(getResult.Value);

            var addResultInstructor = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_INSTRUCTOR, mailIds);
            Assert.Equal(1, addResultInstructor.Value.Count());

            var getResult1 = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(1, getResult1.Value.Count());

            var userId = addResultInstructor.Value.First().ApplicationUserId;
            var userResult = _context.UserOfferings.Where(u => u.ApplicationUserId == userId);
            Assert.Equal(Globals.ROLE_INSTRUCTOR, userResult.First().IdentityRole.Name);
        }

        [Fact]
        public async Task Delete_Roles_From_Multirole_User()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);
            
            var email = "mailId1@test.edu";
            AddUser(email);

            List<string> mailIds = new List<string>() { email };
            var addResultStudent = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            var addResultInstructor = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_INSTRUCTOR, mailIds);
            var addResultTA = await _controller.AddUsersToOffering(offeringId, Globals.ROLE_TEACHING_ASSISTANT, mailIds);
            var getResult = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(3, getResult.Value.Count());

            await _controller.DeleteUserFromOffering(offeringId, Globals.ROLE_STUDENT, mailIds);
            var getResult1 = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Equal(2, getResult1.Value.Count());

            var userResult = _context.UserOfferings.Where(u => u.ApplicationUserId == email);
            Assert.Equal(new List<string>(){Globals.ROLE_INSTRUCTOR, Globals.ROLE_TEACHING_ASSISTANT}, userResult.Select(u => u.IdentityRole.Name).ToList());

            await _controller.DeleteUserOffering(offeringId, email);
            var getResult2 = await _controller.GetUserOfferingsByOfferingId(offeringId);
            Assert.Empty(getResult2.Value);
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
            var roles = new List<string>() { Globals.ROLE_INSTRUCTOR, Globals.ROLE_INSTRUCTOR };
            AddUsers(userIds);
            AddUserOfferingsOneToOne(offeringId, userIds, roles);

            var getResult = await _controller.GetUsersOfOffering(offeringId, Globals.ROLE_INSTRUCTOR);
            Assert.Equal(2, getResult.Value.Count());
            Assert.Contains(userIds[0], getResult.Value);
            Assert.Contains(userIds[1], getResult.Value);
        }

        [Fact]
        public async Task Get_Users_Of_Offering_Multirole()
        {
            var offeringId = "0001";
            SetupEntities(offeringId);

            var id1 = "example1";
            var userIds = new List<string>() { id1, "example2", "example3" };
            var roles = new List<string>() { Globals.ROLE_INSTRUCTOR, Globals.ROLE_INSTRUCTOR, Globals.ROLE_INSTRUCTOR };
            var multiroles = new List<string>() { Globals.ROLE_STUDENT };

            AddUsers(userIds);
            AddUserOfferingsOneToOne(offeringId, userIds, roles);
            AddUserOfferingsOneToMany(offeringId, id1, multiroles);
            
            var getResult = await _controller.GetUsersOfOffering(offeringId, Globals.ROLE_INSTRUCTOR);
            Assert.Equal(3, getResult.Value.Count());
            Assert.Contains(userIds[0], getResult.Value);
            Assert.Contains(userIds[1], getResult.Value);
            Assert.Contains(userIds[2], getResult.Value);

            var getResult1 = await _controller.GetUsersOfOffering(offeringId, Globals.ROLE_STUDENT);
            Assert.Equal(1, getResult1.Value.Count());
            Assert.Equal(id1, getResult.Value.First());

            var getResult2 = await _controller.GetUsersOfOffering(offeringId, Globals.ROLE_TEACHING_ASSISTANT);
            Assert.Empty(getResult2.Value);
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

            IdentityRole studentRole = new IdentityRole
            {
                Name = Globals.ROLE_STUDENT,
                NormalizedName = Globals.ROLE_STUDENT.ToUpper()
            };

            IdentityRole TARole = new IdentityRole
            {
                Name = Globals.ROLE_TEACHING_ASSISTANT,
                NormalizedName = Globals.ROLE_TEACHING_ASSISTANT.ToUpper()
            };

            _context.Roles.Add(instructorRole);
            _context.Roles.Add(studentRole);
            _context.Roles.Add(TARole);

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            _context.SaveChanges();
        }

        private void AddUser(string id) {
            _context.Users.Add(new ApplicationUser { Id = id, Email = id });
        }

        private void AddUsers(List<string> ids) {
            foreach (var userId in ids)
            {
                AddUser(userId);
            }
        }

        private async void AddUserOfferingsOneToOne(string offeringId, List<string> ids, List<string> roles) {
            Assert.Equal(ids.Count(), roles.Count());
            for (var i = 0; i < ids.Count(); i++)
            {
                var postResult = await _controller.PostUserOffering(new UserOfferingDTO
                {
                    OfferingId = offeringId,
                    UserId = ids[i],
                    RoleName = roles[i]
                });
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }
        }

        private async void AddUserOfferingsOneToMany(string offeringId, string id, List<string> roles) {
            for (var i = 0; i < roles.Count(); i++)
            {
                var postResult = await _controller.PostUserOffering(new UserOfferingDTO
                {
                    OfferingId = offeringId,
                    UserId = id,
                    RoleName = roles[i]
                });
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }
        }
    }
}
