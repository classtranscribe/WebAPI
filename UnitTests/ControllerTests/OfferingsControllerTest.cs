using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static ClassTranscribeServer.Controllers.OfferingsController;

namespace UnitTests.ControllerTests
{
    public class OfferingsControllerTest : BaseControllerTest
    {
        private readonly OfferingsController _controller;

        public OfferingsControllerTest(GlobalFixture fixture) : base(fixture)
        {   
            _controller = new OfferingsController(
                fixture._authorizationService,
                _context,
                _userUtils,
                null
            )
            {
                ControllerContext = fixture._controllerContext
            };
        }

        [Fact]
        public async Task Basic_Post_Put_Offering()
        {
            var departmentId = "0001";
            var courseId = "001";
            var termId = "000";
            SetupEntities(departmentId, courseId, termId);

            var offering = new Offering
            {
                SectionName = "A",
                TermId = termId
            };

            var offeringDTO = new NewOfferingDTO
            {
                Offering = offering,
                CourseId = courseId
            };

            var postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            offering.SectionName= "B";

            var putResult = await _controller.PutOffering(offering.Id, offering);
            Assert.IsType<NoContentResult>(putResult);

            var getResult = await _controller.GetOffering(offering.Id);
            AssertOfferingDTO(getResult.Value, offering, courseId, departmentId);
        }

        [Fact]
        public async Task Put_Json_Metadata()
        {
            var departmentId = "0001";
            var courseId = "001";
            var termId = "000";
            SetupEntities(departmentId, courseId, termId);

            var offering = new Offering
            {
                SectionName = "A",
                TermId = termId
            };

            var offeringDTO = new NewOfferingDTO
            {
                Offering = offering,
                CourseId = courseId
            };

            var postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var metadata = new JObject
            {
                { "example", "Hello world!" }
            };

            var putResult = await _controller.PutOffering(offering.Id, metadata);
            Assert.IsType<NoContentResult>(putResult);

            var getResult = await _controller.GetOffering(offering.Id);
            Assert.Equal(metadata, getResult.Value.Offering.JsonMetadata);
            AssertOfferingDTO(getResult.Value, offering, courseId, departmentId);
        }

        [Fact]
        public async Task Basic_Post_And_Delete_Offering()
        {
            var departmentId = "0001";
            var courseId = "001";
            var termId = "000";
            SetupEntities(departmentId, courseId, termId);

            var offering = new Offering
            {
                SectionName = "A",
                TermId = termId
            };

            var offeringDTO = new NewOfferingDTO
            {
                Offering = offering,
                CourseId = courseId
            };

            var postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var deleteResult = await _controller.DeleteOffering(offering.Id);
            Assert.Equal(offering, deleteResult.Value);

            var getResult = await _controller.GetOffering(offering.Id);
            Assert.Equal(Status.Deleted, getResult.Value.Offering.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_Offerings_By_Student()
        {
            var departmentId = "0001";
            var courseId = "001";
            var termId = "000";
            SetupEntities(departmentId, courseId, termId);

            var offerings = new List<Offering> {
                new Offering
                {
                    SectionName = "A",
                    TermId = termId
                },
                new Offering
                {
                    SectionName = "B",
                    TermId = termId
                },
                new Offering
                {
                    SectionName = "C",
                    TermId = termId
                }
            };

            foreach (var offering in offerings)
            {
                var offeringDTO = new NewOfferingDTO
                {
                    Offering = offering,
                    CourseId = courseId
                };

                var postResult = await _controller.PostNewOffering(offeringDTO);
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }

            var playlist = new Playlist { OfferingId = offerings[1].Id };
            _context.Playlists.Add(playlist);
            _context.Medias.Add(new Media { PlaylistId = playlist.Id });

            var getResult = await _controller.GetOfferingsByStudent();
            List<OfferingDTO> offeringsByStudent = getResult.Value.ToList();

            Assert.Single(offeringsByStudent);
            AssertOfferingDTO(offeringsByStudent[0], offerings[1], courseId, departmentId);
        }

        [Fact]
        public async Task Post_Invalid_and_Valid_Offerings()
        {
            var departmentId = "0001";
            var courseId = "001";
            var termId = "000";
            SetupEntities(departmentId, courseId, termId);

            var offering = new Offering
            {
                SectionName = "A",
                TermId = termId
            };

            var offeringDTO = new NewOfferingDTO
            {
                Offering = offering
            };

            var postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            offeringDTO.DepartmentId = "invalid-ID";

            postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            offeringDTO.NewCourseNumber = "002";

            postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            offeringDTO.DepartmentId = departmentId;

            postResult = await _controller.PostNewOffering(offeringDTO);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);
        }

        [Fact]
        public async Task Put_Offering_Not_Found()
        {
            var offering = new Offering
            {
                Id = "not_existing"
            };

            var putResult = await _controller.PutOffering(offering.Id, offering);
            Assert.IsType<NotFoundResult>(putResult);

            putResult = await _controller.PutOffering(offering.Id, new JObject());
            Assert.IsType<NotFoundResult>(putResult);
        }

        [Fact]
        public async Task Put_Offering_Bad_Request()
        {
            var offering = new Offering
            {
                Id = "not_existing"
            };

            var putResult = await _controller.PutOffering("not_matching_id", offering);
            Assert.IsType<BadRequestObjectResult>(putResult);

            putResult = await _controller.PutOffering(null, (Offering) null);
            Assert.IsType<BadRequestObjectResult>(putResult);

            putResult = await _controller.PutOffering(null, (JObject) null);
            Assert.IsType<BadRequestObjectResult>(putResult);
        }

        [Fact]
        public async Task Delete_Offering_Bad_Request()
        {
            var deleteResult = await _controller.DeleteOffering("not_existing");
            Assert.IsType<BadRequestResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteOffering(null);
            Assert.IsType<BadRequestResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_Offering_Not_Found()
        {
            var getResult = await _controller.GetOffering("not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetOffering(null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_Offerings_By_Student_Empty()
        {
            var getResult = await _controller.GetOfferingsByStudent();
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Null_Offering()
        {
            var postResult = await _controller.PostNewOffering(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }

        private void AssertOfferingDTO(OfferingDTO offeringDTO, Offering offering, string courseId, string departmentId)
        {
            Assert.Equal(offering, offeringDTO.Offering);
            Assert.Single(offeringDTO.Courses);
            Assert.Equal(courseId, offeringDTO.Courses[0].CourseId);
            Assert.Equal(departmentId, offeringDTO.Courses[0].DepartmentId);
            Assert.Single(offeringDTO.InstructorIds);
            Assert.Equal(TestGlobals.TEST_USER_ID, offeringDTO.InstructorIds[0].Id);
            Assert.Equal(offering.TermId, offeringDTO.Term.Id);
        }

        private void SetupEntities(string departmentId, string courseId, string termId)
        {
            _context.Departments.Add(new Department { Id = departmentId });
            _context.Terms.Add(new Term { Id = termId });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            _context.Courses.Add(new Course
            {
                Id = courseId,
                CourseNumber = courseId,
                DepartmentId = departmentId
            });

            _context.Roles.Add(new IdentityRole
            {
                Name = Globals.ROLE_INSTRUCTOR,
                NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper()
            });
        }
    }
}
