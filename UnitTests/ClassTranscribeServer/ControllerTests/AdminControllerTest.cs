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
                new Course { FilePath = "aaa" },
                new Course { FilePath = "bbb" },
            };
            var courseOfferings = new List<CourseOffering> {
                new CourseOffering { CourseId = "001" },
                new CourseOffering { FilePath = "ccc" },
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
            var newCo = new CourseOffering { CourseId = "002" };
            _context.Courses.Add(newCourse);
            _context.CourseOfferings.AddRange(
                new CourseOffering { CourseId = null },
                new CourseOffering { CourseId = "non-existing" },
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
