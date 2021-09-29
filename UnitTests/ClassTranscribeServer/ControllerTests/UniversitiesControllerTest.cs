using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class UniversitiesControllerTest : BaseControllerTest
    {
        private readonly UniversitiesController _controller;

        public UniversitiesControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new UniversitiesController(_context, null);
        }

        [Fact]
        public async Task Get_Universities_Success()
        {
            var unis = new List<University>()
            {
                new University
                {
                    Name = "UIUC",
                    Domain = "illinois.edu"
                },
                new University
                {
                    Name = "MIT",
                    Domain = "mit.edu"
                },
                // UNK should be filtered out by GetUniversities
                new University
                {
                    Id = Seeder.UNK_UNIVERSITY_ID,
                    Name = "Unknown",
                    Domain = "UNK"
                },
                new University
                {
                    Name = "Harvard",
                    Domain = "harvard.edh"
                },
            };

            _context.Universities.AddRange(unis);
            _context.SaveChanges();

            var result = await _controller.GetUniversities();
            var unisFromGet = result.Value.ToList();
            Assert.Equal(3, unisFromGet.Count());
            Assert.Contains(unis[0], unisFromGet);
            Assert.Contains(unis[1], unisFromGet);
            Assert.Contains(unis[3], unisFromGet);
        }

        [Fact]
        public async Task Get_Universities_Empty()
        {
            var result = await _controller.GetUniversities();
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Get_University_Success()
        {
            var uni = new University
            {
                Name = "UIUC",
                Domain = "illinois.edu"
            };

            _context.Universities.Add(uni);
            _context.SaveChanges();

            var result = await _controller.GetUniversity(uni.Id);
            Assert.Equal(uni, result.Value);
        }

        [Fact]
        public async Task Get_University_Fail()
        {
            var result = await _controller.GetUniversity("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.GetUniversity(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Put_University_Success()
        {
            var uni = new University
            {
                Name = "UIUC",
                Domain = "illinois.edu"
            };

            _context.Universities.Add(uni);
            _context.SaveChanges();

            uni.Name = "Stanford";
            uni.Domain = "stanford.edu";

            var result = await _controller.PutUniversity(uni.Id, uni);
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(uni, _context.Universities.Find(uni.Id));
        }

        [Fact]
        public async Task Put_University_Fail()
        {
            var result = await _controller.PutUniversity("none", new University());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutUniversity("none", null);
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutUniversity(null, new University());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutUniversity(null, null);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_University_Success()
        {
            var uni = new University
            {
                Name = "UIUC",
                Domain = "illinois.edu"
            };

            var result = await _controller.PostUniversity(uni);
            Assert.IsType<CreatedAtActionResult>(result.Result);

            Assert.Single(_context.Universities.ToList());
            Assert.Equal(uni, _context.Universities.ToList()[0]);
        }

        [Fact]
        public async Task Post_University_Fail()
        {
            var result = await _controller.PostUniversity(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Delete_University_Success()
        {
            var uni = new University
            {
                Name = "UIUC",
                Domain = "illinois.edu"
            };

            _context.Universities.Add(uni);
            _context.SaveChanges();

            var result = await _controller.DeleteUniversity(uni.Id);
            Assert.Equal(uni, result.Value);
            Assert.Empty(_context.Universities.ToList());
        }

        [Fact]
        public async Task Delete_University_Fail()
        {
            var result = await _controller.DeleteUniversity("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DeleteUniversity(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}