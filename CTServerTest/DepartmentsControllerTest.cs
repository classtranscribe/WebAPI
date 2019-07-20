using System;
using Xunit;
using ClassTranscribeServer.Controllers;
using ClassTranscribeDatabase;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClassTranscribeUnitTesting
{
    public class DepartmentsControllerTest
    {
        CTDbContext _context;
        DepartmentsController _controller;

        public DepartmentsControllerTest()
        {
            _context = CTDbContext.CreateDbContext();
            _context.Database.EnsureCreated();
            _controller = new DepartmentsController(_context);
        }

        [Fact]
        public async Task Get_All_Departments()
        {
            //using (var dbContext = new CTDbContext())
            //{
            //    dbContext.Database.EnsureDeleted();
            //}
            var result = await _controller.GetDepartments();
            var value = Assert.IsType<List<Department>>(result.Value);
            Assert.Equal(3, value.Count);
        }

        [Fact]
        public async Task Get_Departments_By_University_Id()
        {
            var result = await _controller.GetDepartments("1001");
            var value = Assert.IsType<List<Department>>(result.Value);
            Assert.Equal(3, value.Count);
        }

        [Fact]
        public async Task Get_Departments_By_University_Id_Not_Found()
        {
            var result = await _controller.GetDepartments("1793");
            var value = Assert.IsType<List<Department>>(result.Value);
            Assert.Equal(0, value.Count);
        }

        [Fact]
        public async Task Get_By_Department_ID()
        {
            var result = await _controller.GetDepartment("2001");
            Assert.Equal("Computer Science", result.Value.Name);
        }

        [Fact]
        public async Task Get_By_Department_ID_NotFound()
        {
            // TODO: Failed this test. The return type is a Department instead of NotFound.
            var result = await _controller.GetDepartment("1793");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_Department()
        {
            Department test_department = new Department()
            {
                Id = "2004",
                Name = "Physics",
                Acronym = "PHYS",
                UniversityId = "1001"
            };

            var result = await _controller.PostDepartment(test_department);
            var return_department = Assert.IsType<Department>(result.Value);

            Assert.Equal(test_department.Id, return_department.Id);
            Assert.Equal(test_department.Name, return_department.Name);
            Assert.Equal(test_department.Acronym, return_department.Acronym);
            Assert.Equal(test_department.UniversityId, return_department.UniversityId);

        }

        [Fact]
        public async Task Put_Department()
        {
            Department test_department = new Department()
            {
                Id = "2004",
                Name = "Physics",
                Acronym = "PHYS!!!",
                UniversityId = "1001"
            };

            var result = await _controller.PutDepartment("2004", test_department);
            Assert.IsType<NoContentResult>(result);

            var modified_result = await _controller.GetDepartment("2004");
            Assert.Equal("PHYS!!!", modified_result.Value.Acronym);
        }

        [Fact]
        public async Task Put_Department_Bad_Request()
        {
            Department test_department = new Department()
            {
                Id = "2004",
                Name = "Physics",
                Acronym = "PHYS!!!",
                UniversityId = "1001"
            };

            var result = await _controller.PutDepartment("1793", test_department);
            Assert.IsType<BadRequestResult>(result);

        }

        [Fact]
        public async Task Put_Department_Not_Found()
        {
            Department test_department = new Department()
            {
                Id = "1793",
                Name = "Physics",
                Acronym = "PHYS!!!",
                UniversityId = "1001"
            };

            var result = await _controller.PutDepartment("1793", test_department);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Department()
        {
            Department test_department = new Department()
            {
                Id = "2004",
                Name = "Physics",
                Acronym = "PHYS!!!",
                UniversityId = "1001"
            };

            var result = await _controller.DeleteDepartment(test_department.Id);
            var return_department = Assert.IsType<Department>(result.Value);

            Assert.Equal(test_department.Id, return_department.Id);
            Assert.Equal(test_department.Name, return_department.Name);
            Assert.Equal(test_department.Acronym, return_department.Acronym);
            Assert.Equal(test_department.UniversityId, return_department.UniversityId);
        }

        [Fact]
        public async Task Delete_Department_Not_Found()
        {
            // TODO: Failed. Result should be NotFound instead of null?
            var result = await _controller.DeleteDepartment("1793");
            Assert.IsType<NotFoundResult>(result.Value);
        }

    }
}
