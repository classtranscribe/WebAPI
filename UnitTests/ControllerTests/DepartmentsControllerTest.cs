using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class DepartmentsControllerTest : BaseControllerTest
    {
        DepartmentsController _controller;

        public DepartmentsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new DepartmentsController(_context, null);
        }

        [Fact]
        public async Task Basic_Post_Put_Get_Department()
        {
            var department = new Department
            {
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = "0001"
            };

            var postResult = await _controller.PostDepartment(department);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var getResult = await _controller.GetDepartment(department.Id);
            Assert.Equal(department, getResult.Value);

            department.Acronym = "CompSci";

            var putResult = await _controller.PutDepartment(department.Id, department);
            Assert.IsType<NoContentResult>(putResult);

            getResult = await _controller.GetDepartment(department.Id);
            Assert.Equal(department, getResult.Value);
        }

        [Fact]
        public async Task Basic_Post_And_Delete_Department()
        {
            var department = new Department
            {
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = "0001"
            };

            var postResult = await _controller.PostDepartment(department);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var deleteResult = await _controller.DeleteDepartment(department.Id);
            Assert.Equal(department, deleteResult.Value);

            var getResult = await _controller.GetDepartment(department.Id);
            Assert.Equal(Status.Deleted, getResult.Value.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_Departments_By_University()
        {
            var departments = new List<Department> {
                new Department
                {
                    Name = "Computer Science",
                    Acronym = "CS",
                    UniversityId = "0001"
                },
                new Department
                {
                    Name = "Computer Science",
                    Acronym = "CS",
                    UniversityId = "2222"
                },
                new Department
                {
                    Name = "Philosophy",
                    Acronym = "PHIL",
                    UniversityId = "0001"
                }
            };

            foreach (var department in departments)
            {
                var postResult = await _controller.PostDepartment(department);
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }

            var getResult = await _controller.GetDepartments(departments[0].UniversityId);
            List<Department> departmentsByUniversity = getResult.Value.ToList();

            Assert.Equal(2, departmentsByUniversity.Count);
            Assert.Equal(departments[0], departmentsByUniversity[0]);
            Assert.Equal(departments[2], departmentsByUniversity[1]);
        }

        [Fact]
        public async Task Get_All_Departments()
        {
            var departments = new List<Department> {
                new Department
                {
                    Name = "Computer Science",
                    Acronym = "CS",
                    UniversityId = "0001"
                },
                new Department
                {
                    Name = "Computer Science",
                    Acronym = "CS",
                    UniversityId = "2222"
                },
                new Department
                {
                    Name = "Philosophy",
                    Acronym = "PHIL",
                    UniversityId = "0001"
                }
            };

            foreach (var department in departments)
            {
                var postResult = await _controller.PostDepartment(department);
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }

            var getResult = await _controller.GetDepartments();
            List<Department> allDepartments = getResult.Value.ToList();
            Assert.Equal(departments, allDepartments);

            var deleteResult = await _controller.DeleteDepartment(departments[1].Id);
            Assert.Equal(departments[1], deleteResult.Value);

            getResult = await _controller.GetDepartments();
            allDepartments = getResult.Value.ToList();

            Assert.Equal(2, allDepartments.Count);
            Assert.Equal(departments[0], allDepartments[0]);
            Assert.Equal(departments[2], allDepartments[1]);
        }

        [Fact]
        public async Task Post_Existing_Department()
        {
            var department = new Department
            {
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = "0001"
            };

            var postResult = await _controller.PostDepartment(department);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            postResult = await _controller.PostDepartment(department);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdExistingDepartment = createdResult.Value as Department;
            Assert.Equal(department.Id, createdExistingDepartment.Id);

            var getResult = await _controller.GetDepartments();
            List<Department> departmentsByDepartment = getResult.Value.ToList();

            Assert.Single(departmentsByDepartment);
            Assert.Equal(department, departmentsByDepartment[0]);
        }

        [Fact]
        public async Task Put_Department_Not_Found()
        {
            var department = new Department
            {
                Id = "not_existing",
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = "0001"
            };

            var putResult = await _controller.PutDepartment(department.Id, department);
            Assert.IsType<NotFoundResult>(putResult);
        }

        [Fact]
        public async Task Put_Department_Bad_Request()
        {
            var department = new Department
            {
                Id = "not_existing",
                Name = "Computer Science",
                Acronym = "CS",
                UniversityId = "0001"
            };

            var putResult = await _controller.PutDepartment("wrong_id", department);
            Assert.IsType<BadRequestResult>(putResult);

            putResult = await _controller.PutDepartment(null, null);
            Assert.IsType<BadRequestResult>(putResult);
        }

        [Fact]
        public async Task Delete_Department_Not_Found()
        {
            var deleteResult = await _controller.DeleteDepartment("not_existing");
            Assert.IsType<NotFoundResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteDepartment(null);
            Assert.IsType<NotFoundResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_Department_Not_Found()
        {
            var getResult = await _controller.GetDepartment("not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetDepartment(null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_Departments_By_University_Empty()
        {
            var getResult = await _controller.GetDepartments("not_existing");
            Assert.Empty(getResult.Value);

            getResult = await _controller.GetDepartments(null);
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Get_All_Departments_Empty()
        {
            var getResult = await _controller.GetDepartments();
            Assert.Empty(getResult.Value);
        }

        [Fact]
        public async Task Post_Null_Department()
        {
            var postResult = await _controller.PostDepartment(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }
    }
}
