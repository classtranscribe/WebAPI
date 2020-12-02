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
        public async Task Get_Offerings_By_Instructor()
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

            // This offering shouldn't be returned because it was made by a different user
            var ignoredOfferingId = "shouldIgnore";
            _context.Offerings.Add(new Offering
            {
                Id = ignoredOfferingId,
                SectionName = "D",
                TermId = termId
            });
            _context.CourseOfferings.Add(new CourseOffering
            {
                CourseId = courseId,
                OfferingId = ignoredOfferingId
            });
            _context.UserOfferings.Add(new UserOffering
            {
                ApplicationUserId = "otherUser",
                IdentityRole = _context.Roles.Where(r => r.Name == Globals.ROLE_INSTRUCTOR).FirstOrDefault(),
                OfferingId = ignoredOfferingId
            });
            _context.SaveChanges();

            var getResult = await _controller.GetOfferingsByInstructor(TestGlobals.TEST_USER_ID);
            List<OfferingDTO> offeringsByInstructor = getResult.Value.ToList();

            Assert.Equal(3, offeringsByInstructor.Count());

            for (int i = 0; i < offerings.Count(); i++)
            {
                // The returned offerings should be in opposite order because they are
                // returned in descending order based on when they were created
                var reverseIdx = offerings.Count() - i - 1;
                AssertOfferingDTO(offeringsByInstructor[reverseIdx], offerings[i], courseId, departmentId);
            }
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
        public async Task Get_Offerings_By_Instructor_Access_Control()
        {
            var otherUserId = "otherUser";
            _context.Users.AddRange(new List<ApplicationUser>()
            {
                new ApplicationUser { Id = TestGlobals.TEST_USER_ID },
                new ApplicationUser { Id = otherUserId },
            });
            _context.SaveChanges();

            var getResult = await _controller.GetOfferingsByInstructor("none");
            Assert.IsType<UnauthorizedResult>(getResult.Result);

            getResult = await _controller.GetOfferingsByInstructor(null);
            Assert.IsType<UnauthorizedResult>(getResult.Result);

            // Tests are setup to be "logged in" as the TEST_USER, so this should pass
            getResult = await _controller.GetOfferingsByInstructor(TestGlobals.TEST_USER_ID);
            Assert.Empty(getResult.Value);

            // Unauthorized because instructors should only be able to access their own data
            getResult = await _controller.GetOfferingsByInstructor(otherUserId);
            Assert.IsType<UnauthorizedResult>(getResult.Result);

            MakeTestUserAdmin();

            // Now authorized because admins can access all data
            getResult = await _controller.GetOfferingsByInstructor(otherUserId);
            Assert.IsType<UnauthorizedResult>(getResult.Result);
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
            Assert.Equal(departmentId, offeringDTO.Courses[0].DepartmentAcronym);
            Assert.Single(offeringDTO.InstructorIds);
            Assert.Equal(TestGlobals.TEST_USER_ID, offeringDTO.InstructorIds[0].Id);
            Assert.Equal(offering.TermId, offeringDTO.Term.Id);
        }

        private void SetupEntities(string departmentId, string courseId, string termId)
        {
            _context.Departments.Add(new Department {
                Id = departmentId,
                Acronym = departmentId,
            });

            _context.Terms.Add(new Term { Id = termId });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            _context.Courses.Add(new Course
            {
                Id = courseId,
                CourseNumber = courseId,
                DepartmentId = departmentId,
            });

            _context.Roles.Add(new IdentityRole
            {
                Name = Globals.ROLE_INSTRUCTOR,
                NormalizedName = Globals.ROLE_INSTRUCTOR.ToUpper()
            });
        }

        private void MakeTestUserAdmin()
        {
            string adminId = "admin";

            _context.Roles.Add(new IdentityRole
            {
                Id = adminId,
                Name = Globals.ROLE_ADMIN,
                NormalizedName = Globals.ROLE_ADMIN.ToUpper()
            });

            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = adminId,
                UserId = TestGlobals.TEST_USER_ID
            });

            _context.SaveChanges();
        }
    }
}
