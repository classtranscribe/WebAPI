using ClassTranscribeDatabase.Models;
using ClassTranscribeServer;
using ClassTranscribeServer.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class AdminControllerTest : BaseControllerTest
    {
        private readonly AdminController _controller;

        public AdminControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new AdminController(
                fixture._authorizationService,
                (WakeDownloader)fixture._serviceProvider.GetService(typeof(WakeDownloader)),
                _context,
                null
            );
        }

        [Fact]
        public async Task Generate_File_Paths()
        {
            var result = await _controller.GenerateFilePaths();
            Assert.Equal(0, result.Value);

            var courses = new List<Course> {
                new Course { Id = "001" },
                new Course { Id = "002" },
                new Course { Id = "003", FilePath = "aaa" },

                new Course { Id = "004", FilePath = "bbb" },
            };
            var courseOfferings = new List<CourseOffering> {
                new CourseOffering { Id = "co001", CourseId = "001", OfferingId= "Oid3" },
                new CourseOffering { Id = "co002", CourseId = "002", FilePath = "ccc", OfferingId = "Oid4" },
            };
            _context.Courses.AddRange(courses);
            _context.CourseOfferings.AddRange(courseOfferings);
            _context.SaveChanges();

            result = await _controller.GenerateFilePaths();
            Assert.Equal(3, result.Value);

            Assert.True(Common.IsValidFilePath(courses[0]));
            Assert.True(Common.IsValidFilePath(courses[1]));
            Assert.True(Common.IsValidFilePath(courseOfferings[0]));

            result = await _controller.GenerateFilePaths();
            Assert.Equal(0, result.Value);

            // only course offerings with valid CourseId fields should be processed
            var newCourse = new Course { };
            var newCo = new CourseOffering { CourseId = "002" , OfferingId = "cc0"};
            var newOff0 = new Offering { Id = "cc0" };
            var newOff1 = new Offering { Id = "cc1" };
            var newOff2 = new Offering { Id = "cc2" };
            _context.Offerings.AddRange(newOff0, newOff1, newOff2);
            _context.Courses.Add(newCourse);
            _context.CourseOfferings.AddRange(
                new CourseOffering { CourseId = "blah", OfferingId = "cc1" },
                new CourseOffering { CourseId = "non-existing" , OfferingId = "cc2" },
                newCo
            );

            _context.SaveChanges();

            result = await _controller.GenerateFilePaths();
            Assert.Equal(2, result.Value);

            
            Assert.True(Common.IsValidFilePath(newCourse));
            Assert.True(Common.IsValidFilePath(newCo));
        }
    }
}
