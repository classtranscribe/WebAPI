using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class EPubsControllerTest : BaseControllerTest
    {
        EPubsController _controller;

        public EPubsControllerTest() : base()
        {
            _controller = new EPubsController(null, _context, null, null);
        }

        [Fact]
        public async Task Basic_Post_Put_Get_EPub()
        {
            var ePub = new EPub
            {
                Title = "example0",
                Language = "en-US",
                Filename = "filename_example",
                Author = "author_example",
                Publisher = "publisher_example",
                SourceId = "mediaId_example",
                SourceType = ResourceType.Media
            };

            var postResult = await _controller.PostEPub(ePub);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            var getResult = await _controller.GetEPub(ePub.Id);

            Assert.NotNull(getResult.Value);
            Assert.Equal(ePub, getResult.Value);

            ePub.Title = "new title";

            var putResult = await _controller.PutEPub(ePub.Id, ePub);
            var noContentResult = putResult as NoContentResult;

            Assert.NotNull(noContentResult);

            getResult = await _controller.GetEPub(ePub.Id);

            Assert.NotNull(getResult.Value);
            Assert.Equal(ePub, getResult.Value);
        }

        [Fact]
        public async Task Basic_Post_And_Delete_EPub()
        {
            var ePub = new EPub
            {
                Title = "example0",
                Language = "en-US",
                Filename = "filename_example",
                Author = "author_example",
                Publisher = "publisher_example",
                SourceId = "mediaId_example",
                SourceType = ResourceType.Media
            };

            var postResult = await _controller.PostEPub(ePub);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            var deleteResult = await _controller.DeleteEPub(ePub.Id);

            Assert.NotNull(deleteResult.Value);
            Assert.Equal(ePub, deleteResult.Value);

            var getResult = await _controller.GetEPub(ePub.Id);

            Assert.Equal(Status.Deleted, getResult.Value.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_EPubs_By_Source()
        {
            var ePubs = new List<EPub> {
                new EPub
                {
                    Title = "example1",
                    Language = "en-US",
                    Filename = "filename_example",
                    Author = "author_example",
                    Publisher = "publisher_example",
                    SourceId = "mediaId_example",
                    SourceType = ResourceType.Media
                },
                new EPub
                {
                    Title = "example2",
                    Language = "en-US",
                    Filename = "filename_example",
                    Author = "author_example",
                    Publisher = "publisher_example",
                    SourceId = "courseId_example",
                    SourceType = ResourceType.Course
                },
                new EPub
                {
                    Title = "example3",
                    Language = "en-US",
                    Filename = "filename_example",
                    Author = "author_example",
                    Publisher = "publisher_example",
                    SourceId = "mediaId_example",
                    SourceType = ResourceType.Media
                }
            };

            foreach (var ePub in ePubs)
            {
                var postResult = await _controller.PostEPub(ePub);
                var createdResult = postResult.Result as CreatedAtActionResult;

                Assert.NotNull(createdResult);
            }

            var getResult = await _controller.GetEPubsBySource(ResourceType.Media.ToString(), ePubs[0].SourceId);
            List<EPub> ePubsBySource = getResult.Value.ToList();

            Assert.Equal(2, ePubsBySource.Count);
            Assert.Equal(ePubs[0], ePubsBySource[0]);
            Assert.Equal(ePubs[2], ePubsBySource[1]);
        }

        [Fact]
        public async Task Post_Invalid_and_Valid_EPubs()
        {
            var ePub = new EPub
            {
                Title = "example0",
                Language = "en-US"
            };

            var postResult = await _controller.PostEPub(ePub);
            var badRequestResult = postResult.Result as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            ePub.Filename = "filename_example";
            ePub.Author = "author_example";

            postResult = await _controller.PostEPub(ePub);
            badRequestResult = postResult.Result as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            ePub.Publisher = "publisher_example";
            ePub.SourceId = "mediaId_example";
            ePub.SourceType = ResourceType.Media;

            postResult = await _controller.PostEPub(ePub);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);
        }

        [Fact]
        public async Task Put_Invalid_EPubs()
        {
            var ePub = new EPub
            {
                Title = "example0",
                Language = "en-US",
                Filename = "filename_example",
                Author = "author_example",
                Publisher = "publisher_example",
                SourceId = "mediaId_example",
                SourceType = ResourceType.Media
            };

            var postResult = await _controller.PostEPub(ePub);
            var createdResult = postResult.Result as CreatedAtActionResult;

            Assert.NotNull(createdResult);

            ePub.Title = string.Empty;
            ePub.Filename = string.Empty;

            var putResult = await _controller.PutEPub(ePub.Id, ePub);
            var badRequestResult = putResult as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            ePub.Title = "example0";
            ePub.Filename = "filename_example";
            ePub.Author = string.Empty;
            ePub.Publisher = string.Empty;

            putResult = await _controller.PutEPub(ePub.Id, ePub);
            badRequestResult = putResult as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            ePub.Author = "author_example";
            ePub.Publisher = "publisher_example";

            putResult = await _controller.PutEPub(ePub.Id, ePub);
            var noContentResult = putResult as NoContentResult;

            Assert.NotNull(noContentResult);
        }
    }
}
