using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class CourseOfferingsControllerTest : BaseControllerTest
    {
        private readonly CourseOfferingsController _controller;

        public CourseOfferingsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new CourseOfferingsController(fixture._authorizationService, _context, null);
        }

        [Fact]
        public async Task Basic_Post_And_Delete_CourseOffering()
        {
            var offeringId = "1111";
            var userId = "13";
            var courseIds = new List<string> { "0001" };
            SetupEntities(offeringId, userId, courseIds);

            var courseOffering = new CourseOffering
            {
                CourseId = courseIds[0],
                OfferingId = offeringId
            };

            var postResult = await _controller.PostCourseOffering(courseOffering);
            Assert.IsType<OkResult>(postResult.Result);
            Assert.True(Common.IsValidFilePath(courseOffering));

            var deleteResult = await _controller.DeleteCourseOffering(courseOffering.CourseId, courseOffering.OfferingId);
            Assert.Single(deleteResult.Value);
            Assert.Equal(courseOffering, deleteResult.Value[0]);
            Assert.Equal(Status.Deleted, courseOffering.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_CourseOfferings_By_Instructor()
        {   
            var offeringId = "1111";
            var userId = "13";
            var courseIds = new List<string> { "0001", "0002" };
            SetupEntities(offeringId, userId, courseIds);

            var courseOfferings = new List<CourseOffering> {
                new CourseOffering
                {
                    CourseId = courseIds[0],
                    OfferingId = offeringId
                },
                new CourseOffering
                {
                    CourseId = courseIds[0],
                    OfferingId = "not_existing"
                },
                new CourseOffering
                {
                    CourseId = courseIds[1],
                    OfferingId = offeringId
                }
            };

            foreach (var courseOffering in courseOfferings)
            {
                var postResult = await _controller.PostCourseOffering(courseOffering);
                Assert.IsType<OkResult>(postResult.Result);
            }

            var getResult = await _controller.GetCourseOfferingsByInstructor(userId);
            List<CourseOfferingsController.CourseOfferingDTO> courseOfferingDTOs = getResult.Value.ToList();

            Assert.Equal(2, courseOfferingDTOs.Count);

            Assert.Equal(courseOfferings[0].CourseId, courseOfferingDTOs[0].Course.Id);
            Assert.Single(courseOfferingDTOs[0].Offerings);
            Assert.Equal(courseOfferings[0].OfferingId, courseOfferingDTOs[0].Offerings[0].Id);

            Assert.Equal(courseOfferings[2].CourseId, courseOfferingDTOs[1].Course.Id);
            Assert.Single(courseOfferingDTOs[1].Offerings);
            Assert.Equal(courseOfferings[2].OfferingId, courseOfferingDTOs[1].Offerings[0].Id);
        }

        [Fact]
        public async Task Post_Existing_CourseOffering()
        {
            var offeringId = "1111";
            var userId = "13";
            var courseId = "0001";
            SetupEntities(offeringId, userId, new List<string> { courseId });

            var courseOffering = new CourseOffering
            {
                CourseId = courseId,
                OfferingId = offeringId
            };

            var postResult = await _controller.PostCourseOffering(courseOffering);
            Assert.IsType<OkResult>(postResult.Result);

            postResult = await _controller.PostCourseOffering(courseOffering);
            Assert.IsType<OkObjectResult>(postResult.Result);

            var getResult = await _controller.GetCourseOfferingsByInstructor(userId);
            List<CourseOfferingsController.CourseOfferingDTO> courseOfferingDTOs = getResult.Value.ToList();

            Assert.Single(courseOfferingDTOs);
            Assert.Single(courseOfferingDTOs[0].Offerings);
            Assert.Equal(courseOffering.OfferingId, courseOfferingDTOs[0].Offerings[0].Id);
            Assert.Equal(courseOffering.CourseId, courseOfferingDTOs[0].Course.Id);
        }

        [Fact]
        public async Task Delete_CourseOffering_Not_Found()
        {
            var deleteResult = await _controller.DeleteCourseOffering("not_existing", "not_existing");
            Assert.IsType<BadRequestResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteCourseOffering(null, null);
            Assert.IsType<BadRequestResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_CourseOfferings_By_Instructor_Empty()
        {
            var getResult = await _controller.GetCourseOfferingsByInstructor("not_existing");
            Assert.Empty(getResult.Value);

            getResult = await _controller.GetCourseOfferingsByInstructor(null);
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Null_CourseOffering()
        {
            var postResult = await _controller.PostCourseOffering(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }

        private void SetupEntities(string offeringId, string userId, List<string> courseIds)
        {
            IdentityRole instructorRole = new IdentityRole
            {
                Name = Globals.ROLE_INSTRUCTOR,
                NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper()
            };
            _context.Roles.Add(instructorRole);

            _context.Offerings.Add(new Offering
            {
                Id = offeringId
            });

            _context.UserOfferings.Add(new UserOffering
            {
                OfferingId = offeringId,
                ApplicationUserId = userId,
                IdentityRoleId = instructorRole.Id
            });

            _context.Courses.AddRange(courseIds.Select(courseId => new Course
            {
                Id = courseId,
                CourseNumber = courseId,
                DepartmentId = "0000",
                FilePath = "0101-path"
            }));
        }
    }
}
