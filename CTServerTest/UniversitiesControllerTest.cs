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
    public class UniversitiesControllerTest
    {
        CTDbContext _context;
        UniversitiesController _controller;

        public UniversitiesControllerTest()
        {
            _context = CTDbContext.CreateDbContext();
            _context.Database.EnsureCreated();
            _controller = new UniversitiesController(_context);
        }

        [Fact]
        public async Task Get_All_Universities()
        {
            //using (var dbContext = new CTDbContext())
            //{
            //    dbContext.Database.EnsureDeleted();
            //}
            var result = await _controller.GetUniversities();
            var value = Assert.IsType<List<University>>(result.Value);
            Assert.Equal(1, value.Count);
        }

        [Fact]
        public async Task Get_By_University_ID()
        {
            var result = await _controller.GetUniversity("1001");
            Assert.Equal("UIUC", result.Value.Name);
        }

        [Fact]
        public async Task Get_By_University_ID_NotFound()
        {
            // TODO: Failed this test. The return type is a University instead of NotFound.
            var result = await _controller.GetUniversity("1793");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_University()
        {
            University test_university = new University()
            {
                Id = "1002",
                Name = "UIC",
                Domain = "illinois.edu"
            };

            var result = await _controller.PostUniversity(test_university);
            var return_university = Assert.IsType<University>(result.Value);

            Assert.Equal(test_university.Id, return_university.Id);
            Assert.Equal(test_university.Name, return_university.Name);
            Assert.Equal(test_university.Domain, return_university.Domain);

        }

        [Fact]
        public async Task Put_University()
        {
            University test_university = new University()
            {
                Id = "1002",
                Name = "UIC",
                Domain = "chicago.illinois.edu"
            };

            var result = await _controller.PutUniversity("1002", test_university);
            Assert.IsType<NoContentResult>(result);

            var modified_result = await _controller.GetUniversity("1002");
            Assert.Equal("chicago.illinois.edu", modified_result.Value.Domain);
        }

        [Fact]
        public async Task Put_University_Bad_Request()
        {
            University test_university = new University()
            {
                Id = "1002",
                Name = "UIC",
                Domain = "chicago.illinois.edu"
            };

            var result = await _controller.PutUniversity("1793", test_university);
            Assert.IsType<BadRequestResult>(result);

        }

        [Fact]
        public async Task Put_University_Not_Found()
        {
            University test_university = new University()
            {
                Id = "1793",
                Name = "UIC",
                Domain = "chicago.illinois.edu"
            };

            var result = await _controller.PutUniversity("1793", test_university);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_University()
        {
            University test_university = new University()
            {
                Id = "1002",
                Name = "UIC",
                Domain = "chicago.illinois.edu"
            };

            var result = await _controller.DeleteUniversity(test_university.Id);
            var return_university = Assert.IsType<University>(result.Value);

            Assert.Equal(test_university.Id, return_university.Id);
            Assert.Equal(test_university.Name, return_university.Name);
            Assert.Equal(test_university.Domain, return_university.Domain);
        }

        [Fact]
        public async Task Delete_University_Not_Found()
        {
            // TODO: Failed. Result should be NotFound instead of null?
            var result = await _controller.DeleteUniversity("1793");
            Assert.IsType<NotFoundResult>(result.Value);
        }

    }
}
