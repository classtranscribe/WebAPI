using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class CoursesControllerTest : BaseControllerTest
    {
        CoursesController _controller;

        public CoursesControllerTest() : base()
        {
            _controller = new CoursesController(_context, null);
        }

        [Fact]
        public async Task Basic_Post_Put_Get_Course()
        {
            var course = new Course
            {
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            var postResult = await _controller.PostCourse(course);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            var getResult = await _controller.GetCourse(course.Id);

            Assert.NotNull(getResult.Value);
            Assert.Equal(course, getResult.Value);

            course.CourseNumber = "new title";

            var putResult = await _controller.PutCourse(course.Id, course);
            var noContentResult = putResult as NoContentResult;

            Assert.NotNull(noContentResult);

            getResult = await _controller.GetCourse(course.Id);

            Assert.NotNull(getResult.Value);
            Assert.Equal(course, getResult.Value);
        }

        [Fact]
        public async Task Basic_Post_And_Delete_Course()
        {
            var course = new Course
            {
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            var postResult = await _controller.PostCourse(course);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            var deleteResult = await _controller.DeleteCourse(course.Id);

            Assert.NotNull(deleteResult.Value);
            Assert.Equal(course, deleteResult.Value);

            var getResult = await _controller.GetCourse(course.Id);

            Assert.Equal(Status.Deleted, getResult.Value.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_Courses_By_Department()
        {
            var courses = new List<Course> {
                new Course
                {
                    CourseNumber = "101",
                    DepartmentId = "0001"
                },
                new Course
                {
                    CourseNumber = "102",
                    DepartmentId = "2222"
                },
                new Course
                {
                    CourseNumber = "103",
                    DepartmentId = "0001"
                }
            };

            foreach (var course in courses)
            {
                var postResult = await _controller.PostCourse(course);
                var createdResult = postResult.Result as CreatedAtActionResult;

                Assert.NotNull(createdResult);
            }

            var getResult = await _controller.GetCourses(courses[0].DepartmentId);
            List<Course> coursesByDepartment = getResult.Value.ToList();

            Assert.Equal(2, coursesByDepartment.Count);
            Assert.Equal(courses[0], coursesByDepartment[0]);
            Assert.Equal(courses[2], coursesByDepartment[1]);
        }

        [Fact]
        public async Task Post_Existing_Course()
        {
            var course = new Course
            {
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            var postResult = await _controller.PostCourse(course);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            var existingCourse = new Course
            {
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            postResult = await _controller.PostCourse(existingCourse);
            createdResult = postResult.Result as CreatedAtActionResult;
            var createdExistingCourse = createdResult.Value as Course;

            Assert.NotNull(createdResult);
            Assert.Equal(course.Id, createdExistingCourse.Id);

            var getResult = await _controller.GetCourses(course.DepartmentId);
            List<Course> coursesByDepartment = getResult.Value.ToList();

            Assert.Single(coursesByDepartment);
        }
    }
}
