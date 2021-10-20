using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class StaticFileControllerTest : BaseControllerTest
    {
        private readonly StaticFileController _controller;

        public StaticFileControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new StaticFileController(
                fixture._authorizationService,
                _context,
                _userUtils,
                fixture._physicalFileProvider,
                null
            )
            {
                ControllerContext = fixture._controllerContext
            };
        }

        [Fact]
        public void Get_File()
        {
            var result = _controller.GetFile(null);
            Assert.IsType<NotFoundResult>(result);

            result = _controller.GetFile("non-existent.txt");
            Assert.IsType<NotFoundResult>(result);

            result = _controller.GetFile("test.txt");
            Assert.IsType<FileStreamResult>(result);

            var fileStream = ((FileStreamResult)result).FileStream;
            using var sr1 = new StreamReader(fileStream, Encoding.UTF8);
            string content1 = sr1.ReadToEnd();

            using var stream = File.OpenRead("Assets/test.txt");
            using var sr2 = new StreamReader(stream, Encoding.UTF8);
            string content2 = sr2.ReadToEnd();

            Assert.Equal(content1, content2);
        }
    }
}