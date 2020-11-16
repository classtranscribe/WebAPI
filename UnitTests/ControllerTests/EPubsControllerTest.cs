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
        private readonly EPubsController _controller;

        public EPubsControllerTest(GlobalFixture fixture) : base(fixture)
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var getResult = await _controller.GetEPub(ePub.Id);
            Assert.Equal(ePub, getResult.Value);

            ePub.Title = "new title";

            var putResult = await _controller.PutEPub(ePub.Id, ePub);
            Assert.IsType<NoContentResult>(putResult);

            getResult = await _controller.GetEPub(ePub.Id);
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var deleteResult = await _controller.DeleteEPub(ePub.Id);
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
                Assert.IsType<CreatedAtActionResult>(postResult.Result);
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
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            ePub.Filename = "filename_example";
            ePub.Author = "author_example";

            postResult = await _controller.PostEPub(ePub);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            ePub.Publisher = "publisher_example";
            ePub.SourceId = "mediaId_example";
            ePub.SourceType = ResourceType.Media;

            postResult = await _controller.PostEPub(ePub);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);
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
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            ePub.Title = string.Empty;
            ePub.Filename = string.Empty;

            var putResult = await _controller.PutEPub(ePub.Id, ePub);
            Assert.IsType<BadRequestObjectResult>(putResult);

            ePub.Title = "example0";
            ePub.Filename = "filename_example";
            ePub.Author = string.Empty;
            ePub.Publisher = string.Empty;

            putResult = await _controller.PutEPub(ePub.Id, ePub);
            Assert.IsType<BadRequestObjectResult>(putResult);

            ePub.Author = "author_example";
            ePub.Publisher = "publisher_example";

            putResult = await _controller.PutEPub(ePub.Id, ePub);
            Assert.IsType<NoContentResult>(putResult);
        }

        [Fact]
        public async Task Put_EPub_Not_Found()
        {
            var ePub = new EPub
            {
                Id = "not_existing",
                Title = "example0",
                Language = "en-US",
                Filename = "filename_example",
                Author = "author_example",
                Publisher = "publisher_example",
                SourceId = "mediaId_example",
                SourceType = ResourceType.Media
            };

            var putResult = await _controller.PutEPub(ePub.Id, ePub);
            Assert.IsType<NotFoundResult>(putResult);
        }

        [Fact]
        public async Task Put_EPub_Bad_Request()
        {
            var ePub = new EPub
            {
                Id = "not_existing",
                Title = "example0",
                Language = "en-US",
                Filename = "filename_example",
                Author = "author_example",
                Publisher = "publisher_example",
                SourceId = "mediaId_example",
                SourceType = ResourceType.Media
            };

            var putResult = await _controller.PutEPub("not_matching_id", ePub);
            Assert.IsType<BadRequestResult>(putResult);

            putResult = await _controller.PutEPub(null, null);
            Assert.IsType<BadRequestResult>(putResult);
        }

        [Fact]
        public async Task Delete_EPub_Not_Found()
        {
            var deleteResult = await _controller.DeleteEPub("not_existing");
            Assert.IsType<NotFoundResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteEPub(null);
            Assert.IsType<NotFoundResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_EPub_Not_Found()
        {
            var getResult = await _controller.GetEPub("not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetEPub(null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_EPubs_By_Source_Not_Found()
        {
            var getResult = await _controller.GetEPubsBySource("Media", "not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetEPubsBySource("Media", null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_EPubs_By_Invalid_Source()
        {
            var getResult = await _controller.GetEPubsBySource("invalid_source", "id");
            Assert.IsType<BadRequestObjectResult>(getResult.Result);

            getResult = await _controller.GetEPubsBySource(null, null);
            Assert.IsType<BadRequestObjectResult>(getResult.Result);
        }

        [Fact]
        public async Task Post_Null_EPub()
        {
            var postResult = await _controller.PostEPub(null);
            Assert.IsType<BadRequestResult>(postResult.Result);
        }
    }
}
