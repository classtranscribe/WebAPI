using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class ImagesControllerTest : BaseControllerTest
    {
        private readonly ImagesController _controller;

        public ImagesControllerTest(GlobalFixture fixture) : base(fixture)
        {
            ILogger<ImagesController> logger = fixture._serviceProvider.GetService<ILogger<ImagesController>>();

            _controller = new ImagesController(_context,logger);
        }

        [Fact]
        public async Task Basic_Post_Get_Delete_Image()
        {
            using var stream = File.OpenRead("Assets/test.png");
            var imageFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

            var co = await Common.GetCourseOfferingForFileRecord(_context);

            var postResult = await _controller.PostImage(imageFile, "Course", co.CourseId);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdImage = createdResult.Value as Image;

            var getResult = await _controller.GetImage(createdImage.Id);
            Assert.Equal(createdImage, getResult.Value);

            Assert.Equal(createdImage.ImageFile.Path, getResult.Value.ImageFile.Path);

            var deleteResult = await _controller.DeleteImage(createdImage.Id);
            Assert.Equal(createdImage, deleteResult.Value);

            getResult = await _controller.GetImage(createdImage.Id);
            Assert.Equal(Status.Deleted, getResult.Value.IsDeletedStatus);
        }

        [Fact]
        public async Task Get_Images_By_Source()
        {
            using var stream = File.OpenRead("Assets/test.png");
            var imageFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

            var co = await Common.GetCourseOfferingForFileRecord(_context);

            var imageInfos = new List<(ResourceType sourceType, string sourceId)>
                {
                    (ResourceType.Offering, co.OfferingId),
                    (ResourceType.Course, co.CourseId),
                    (ResourceType.Offering, co.OfferingId)
                };

            foreach (var imageInfo in imageInfos)
            {
                var postResult = await _controller.PostImage(
                    imageFile,
                    imageInfo.sourceType.ToString(),
                    imageInfo.sourceId
                );

                Assert.IsType<CreatedAtActionResult>(postResult.Result);
            }

            var getResult = await _controller.GetImagesBySource(ResourceType.Offering.ToString(), co.OfferingId);
            List<Image> imagesBySource = getResult.Value.ToList();

            Assert.Equal(2, imagesBySource.Count);
            Assert.Equal(ResourceType.Offering, imagesBySource[0].SourceType);
            Assert.Equal(ResourceType.Offering, imagesBySource[1].SourceType);
            Assert.Equal(co.OfferingId, imagesBySource[0].SourceId);
            Assert.Equal(co.OfferingId, imagesBySource[1].SourceId);
        }

        [Fact]
        public async Task Post_Invalid_Images()
        {
            var postResult = await _controller.PostImage(null, "Media", "id");
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            postResult = await _controller.PostImage(null, null, null);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            using (var stream = File.OpenRead("Assets/test.png"))
            {
                // Set file length to be 0
                var imageFile = new FormFile(stream, 0, 0, null, Path.GetFileName(stream.Name));

                postResult = await _controller.PostImage(imageFile, "Media", "id");
                Assert.IsType<BadRequestObjectResult>(postResult.Result);

                imageFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

                postResult = await _controller.PostImage(imageFile, null, "id");
                Assert.IsType<BadRequestObjectResult>(postResult.Result);

                postResult = await _controller.PostImage(imageFile, "Invalid source", "id");
                Assert.IsType<BadRequestObjectResult>(postResult.Result);

                postResult = await _controller.PostImage(imageFile, "Media", null);
                Assert.IsType<BadRequestObjectResult>(postResult.Result);
            }

            using (var stream = File.OpenRead("Assets/test.txt"))
            {
                // Set file length to be 0
                var textFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));

                postResult = await _controller.PostImage(textFile, "Media", "id");
                Assert.IsType<BadRequestObjectResult>(postResult.Result);
            }
        }

        [Fact]
        public async Task Delete_Image_Not_Found()
        {
            var deleteResult = await _controller.DeleteImage("not_existing");
            Assert.IsType<NotFoundResult>(deleteResult.Result);

            deleteResult = await _controller.DeleteImage(null);
            Assert.IsType<NotFoundResult>(deleteResult.Result);
        }

        [Fact]
        public async Task Get_Image_Not_Found()
        {
            var getResult = await _controller.GetImage("not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetImage(null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_Images_By_Source_Not_Found()
        {
            var getResult = await _controller.GetImagesBySource("Media", "not_existing");
            Assert.IsType<NotFoundResult>(getResult.Result);

            getResult = await _controller.GetImagesBySource("Media", null);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public async Task Get_Images_By_Invalid_Source()
        {
            var getResult = await _controller.GetImagesBySource("invalid_source", "id");
            Assert.IsType<BadRequestObjectResult>(getResult.Result);

            getResult = await _controller.GetImagesBySource(null, null);
            Assert.IsType<BadRequestObjectResult>(getResult.Result);
        }
    }
}