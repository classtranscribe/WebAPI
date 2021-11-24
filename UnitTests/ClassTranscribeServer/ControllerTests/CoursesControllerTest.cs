using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class CoursesControllerTest : BaseControllerTest
    {
        private readonly CoursesController _controller;

        public CoursesControllerTest(GlobalFixture fixture) : base(fixture)
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var getResult = await _controller.GetCourse(course.Id);
            Assert.Equal(course, getResult.Value);

            course.CourseNumber = "new title";

            var putResult = await _controller.PutCourse(course.Id, course);
            Assert.IsType<NoContentResult>(putResult);

            getResult = await _controller.GetCourse(course.Id);
            Assert.Equal(course, getResult.Value);
            Assert.True(Common.IsValidFilePath(course));
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var deleteResult = await _controller.DeleteCourse(course.Id);
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
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var existingCourse = new Course
            {
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            postResult = await _controller.PostCourse(existingCourse);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdExistingCourse = createdResult.Value as Course;
            Assert.Equal(course.Id, createdExistingCourse.Id);

            var getResult = await _controller.GetCourses(course.DepartmentId);
            List<Course> coursesByDepartment = getResult.Value.ToList();

            Assert.Single(coursesByDepartment);
            Assert.Equal(course, coursesByDepartment[0]);
        }

        [Fact]
        public async Task Put_Course_Not_Found()
        {
            var course = new Course
            {
                Id = "not_existing",
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            var putResult = await _controller.PutCourse(course.Id, course);
            Assert.IsType<NotFoundResult>(putResult);
        }

        [Fact]
        public async Task Put_Course_Bad_Request()
        {
            var course = new Course
            {
                Id = "not_existing",
                CourseNumber = "241",
                DepartmentId = "0000"
            };

            var putResult = await _controller.PutCourse("wrong_id", course);
            Assert.IsType<BadRequestResult>(putResult);

            putResult = await _controller.PutCourse(null, null);
            Assert.IsType<BadRequestResult>(putResult);
        }

        [Fact]
        public async Task Delete_Course_Not_Found()
        {
            var deleteResult = await _controller.DeleteCourse("not_existing");
            Assert.IsType<NotFoundResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteCourse(null);
            Assert.IsType<NotFoundResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_Course_Not_Found()
        {
            var getResult = await _controller.GetCourse("not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetCourse(null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_Courses_By_Department_Empty()
        {
            var getResult = await _controller.GetCourses("not_existing");
            Assert.Empty(getResult.Value);

            getResult = await _controller.GetCourses(null);
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Null_Course()
        {
            var postResult = await _controller.PostCourse(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }
    }
}
