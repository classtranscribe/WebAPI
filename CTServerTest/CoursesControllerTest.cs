using System;
using Xunit;
using ClassTranscribeServer.Controllers;
using ClassTranscribeDatabase;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using ClassTranscribeServer.Seed;

namespace ClassTranscribeUnitTesting
{


    public class CoursesControllerTest
    {

        CTDbContext _context;
        CoursesController _controller;

        public CoursesControllerTest()
        {
            _context = CTDbContext.CreateDbContext();
            _context.Database.EnsureCreated();
            _controller = new CoursesController(_context);
        }

        [Fact]
        public async Task Get_All_Courses()
        {
            //using (var dbContext = new CTDbContext())
            //{
            //    dbContext.Database.EnsureDeleted();
            //}
            var result = await _controller.GetCourses();
            var value = Assert.IsType<List<Course>>(result.Value);
            Assert.Equal(6, value.Count);
        }

        [Fact]
        public async Task Get_Courses_By_Department_Id()
        {
            var result = await _controller.GetCourses("2001");
            var value = Assert.IsType<List<Course>>(result.Value);
            Assert.Equal(2, value.Count);
        }

        [Fact]
        public async Task Get_Courses_By_Department_Id_Not_Found()
        {
            var result = await _controller.GetCourses("1793");
            var value = Assert.IsType<List<Course>>(result.Value);
            Assert.Equal(0, value.Count);
        }

        // TODO
        [Fact]
        public async Task Get_Courses_By_Instructor_Id()
        {
            
        }


        [Fact]
        public async Task Get_By_Course_ID()
        {
            var result  = await _controller.GetCourse("3001");
            Assert.Equal("425", result.Value.CourseNumber);
        }

        [Fact]
        public async Task Get_By_Course_ID_NotFound()
        {
            // TODO: Failed this test. There might be something wrong with the controller.
            var result = await _controller.GetCourse("1793");
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Put_Course()
        {
            Course test_course = new Course()
            {
                Id = "3001",
                CourseNumber = "425",
                CourseName = "Distributed Systems",
                DepartmentId = "2001",
                Description = "Protocols, specification techniques, global states and their determination, reliable broadcast, transactions and commitment, security, and real-time systems."

            };

            var result = await _controller.PutCourse("3001", test_course);
            Assert.IsType<NoContentResult>(result);

            var modified_result = await _controller.GetCourse("3001");
            Assert.Equal("Protocols, specification techniques, global states and their determination, reliable broadcast, transactions and commitment, security, and real-time systems.", modified_result.Value.Description);
        }

        [Fact]
        public async Task Put_Course_Bad_Request()
        {
            Course test_course = new Course()
            {
                Id = "3001",
                CourseNumber = "425",
                CourseName = "Distributed Systems",
                DepartmentId = "2001",
                Description = "Protocols, specification techniques, global states and their determination, reliable broadcast, transactions and commitment, security, and real-time systems."

            };

            var result = await _controller.PutCourse("3002", test_course);
            Assert.IsType<BadRequestResult>(result);

        }

        [Fact]
        public async Task Put_Course_Not_Found()
        {
            Course test_course = new Course()
            {
                Id = "1793",
                CourseNumber = "425",
                CourseName = "Distributed Systems",
                DepartmentId = "2001",
                Description = "Protocols, specification techniques, global states and their determination, reliable broadcast, transactions and commitment, security, and real-time systems."

            };

            var result = await _controller.PutCourse("1793", test_course);
            Assert.IsType<NotFoundResult>(result);

        }

        [Fact]
        public async Task Post_Course()
        {
            Course test_course = new Course()
            {
                Id = "3007",
                CourseNumber = "241",
                CourseName = "System Programming",
                DepartmentId = "2001",
                Description = "Basics of system programming, including POSIX processes, process control, inter-process communication, synchronization, signals, simple memory management, file I/O and directories, shell programming, socket network programming, RPC programming in distributed systems, basic security mechanisms, and standard tools for systems programming such as debugging tools."

            };

            var result = await _controller.PostCourse(test_course);
            var return_course = Assert.IsType<Course>(result.Value);

            Assert.Equal(test_course.Id, return_course.Id);
            Assert.Equal(test_course.CourseNumber, return_course.CourseNumber);
            Assert.Equal(test_course.CourseName, return_course.CourseName);
            Assert.Equal(test_course.DepartmentId, return_course.DepartmentId);
            Assert.Equal(test_course.Description, return_course.Description);
        }

        [Fact]
        public async Task Delete_Course()
        {
            Course test_course = new Course()
            {
                Id = "3007",
                CourseNumber = "241",
                CourseName = "System Programming",
                DepartmentId = "2001",
                Description = "Basics of system programming, including POSIX processes, process control, inter-process communication, synchronization, signals, simple memory management, file I/O and directories, shell programming, socket network programming, RPC programming in distributed systems, basic security mechanisms, and standard tools for systems programming such as debugging tools."

            };

            var result = await _controller.DeleteCourse(test_course.Id);
            var return_course = Assert.IsType<Course>(result.Value);

            Assert.Equal(test_course.Id, return_course.Id);
            Assert.Equal(test_course.CourseNumber, return_course.CourseNumber);
            Assert.Equal(test_course.CourseName, return_course.CourseName);
            Assert.Equal(test_course.DepartmentId, return_course.DepartmentId);
            Assert.Equal(test_course.Description, return_course.Description);
        }

        [Fact]
        public async Task Delete_Course_Not_Found()
        {
            // TODO: Failed. Result should be NotFound instead of null?
            var result = await _controller.DeleteCourse("3009");
            Assert.IsType<NotFoundResult>(result.Value);
        }

    }
}