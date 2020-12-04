using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class WatchHistoriesControllerTest : BaseControllerTest
    {
        private readonly WatchHistoriesController _controller;

        public WatchHistoriesControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new WatchHistoriesController(_context, _userUtils)
            {
                ControllerContext = fixture._controllerContext
            };
        }

        [Fact]
        public async Task Get_Watch_History_Success()
        {
            var mediaId = "media";
            var watchHistory = new WatchHistory
            {
                MediaId = mediaId,
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("hello", "world"))
            };

            _context.Medias.Add(new Media { Id = mediaId });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.WatchHistories.Add(watchHistory);
            _context.SaveChanges();

            var result = await _controller.GetWatchHistory(mediaId);
            Assert.Equal(watchHistory, result.Value);
        }

        [Fact]
        public async Task Get_Watch_History_Fail()
        {
            var result = await _controller.GetWatchHistory("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetWatchHistory(null);
            Assert.IsType<BadRequestResult>(result.Result);

            var mediaId = "media";
            _context.Medias.Add(new Media { Id = mediaId });
            _context.SaveChanges();

            result = await _controller.GetWatchHistory(mediaId);
            Assert.IsType<UnauthorizedResult>(result.Result);

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.SaveChanges();

            result = await _controller.GetWatchHistory(mediaId);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Get_All_Watch_History_For_User_Success()
        {
            var medias = new List<Media>() {
                new Media
                {
                    Id = "media",
                    Name = "exampleMedia",
                    PlaylistId = "none",
                    JsonMetadata = new JObject(new JProperty("foo", "bar")),
                    SourceType = SourceType.Kaltura
                },
                new Media
                {
                    Id = "media2",
                    Name = "exampleMedia2",
                    PlaylistId = "none2",
                    JsonMetadata = new JObject(new JProperty("foo", "baz")),
                    SourceType = SourceType.Box
                }
            };

            var wh1 = new WatchHistory
            {
                MediaId = medias[0].Id,
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("hello", "world")),
            };

            var wh2 = new WatchHistory
            {
                MediaId = medias[1].Id,
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("bar", "baz"))
            };

            var shouldBeIgnored = new List<WatchHistory>()
            {
                new WatchHistory
                {
                    MediaId = medias[0].Id,
                    ApplicationUserId = "other_user"
                },
                new WatchHistory
                {
                    MediaId = "non_existing",
                    ApplicationUserId = TestGlobals.TEST_USER_ID
                },
            };

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.Medias.AddRange(medias);

            _context.WatchHistories.Add(wh1);
            _context.SaveChanges(); // save changes twice so wh2 has LastUpdatedAt later than wh1's LastUpdatedAt

            _context.WatchHistories.Add(wh2);
            _context.WatchHistories.AddRange(shouldBeIgnored);
            _context.SaveChanges();

            var result = await _controller.GetAllWatchHistoryForUser();
            Assert.Equal(2, result.Value.Count());

            Assert.Equal(medias[1].Name, result.Value.ElementAt(0).Name);
            Assert.Equal(medias[1].PlaylistId, result.Value.ElementAt(0).PlaylistId);
            Assert.Equal(medias[1].JsonMetadata, result.Value.ElementAt(0).JsonMetadata);
            Assert.Equal(medias[1].SourceType, result.Value.ElementAt(0).SourceType);
            Assert.Equal(wh2, result.Value.ElementAt(0).WatchHistory);

            Assert.Equal(medias[0].Name, result.Value.ElementAt(1).Name);
            Assert.Equal(medias[0].PlaylistId, result.Value.ElementAt(1).PlaylistId);
            Assert.Equal(medias[0].JsonMetadata, result.Value.ElementAt(1).JsonMetadata);
            Assert.Equal(medias[0].SourceType, result.Value.ElementAt(1).SourceType);
            Assert.Equal(wh1, result.Value.ElementAt(1).WatchHistory);
        }

        [Fact]
        public async Task Get_All_Watch_History_For_User_Filters_Duplicates()
        {
            var media = new Media { Id = "media", Name = "exampleMedia" };

            var wh1 = new WatchHistory
            {
                MediaId = media.Id,
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("hello", "world")),
            };

            var wh2 = new WatchHistory
            {
                MediaId = media.Id,
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("bar", "baz"))
            };

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.Medias.Add(media);

            _context.WatchHistories.Add(wh1);
            _context.SaveChanges(); // save changes twice so wh2 has LastUpdatedAt later than wh1's LastUpdatedAt

            _context.WatchHistories.Add(wh2);
            _context.SaveChanges();

            var result = await _controller.GetAllWatchHistoryForUser();
            Assert.Single(result.Value);

            Assert.Equal(media.Id, result.Value.ElementAt(0).Id);
            Assert.Equal(media.Name, result.Value.ElementAt(0).Name);
            Assert.Equal(wh2.Json, result.Value.ElementAt(0).WatchHistory.Json);
            Assert.Equal(wh2, result.Value.ElementAt(0).WatchHistory);
        }

            [Fact]
        public async Task Get_All_Watch_History_For_User_Fail()
        {
            var result = await _controller.GetAllWatchHistoryForUser();
            Assert.IsType<UnauthorizedResult>(result.Result);

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.SaveChanges();

            result = await _controller.GetAllWatchHistoryForUser();
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Post_Watch_History_Success()
        {
            var mediaId = "media";
            _context.Medias.Add(new Media { Id = mediaId });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });
            _context.SaveChanges();

            var json = new JObject(new JProperty("hello", "world"));
            var result = await _controller.PostWatchHistory(mediaId, json);
            Assert.IsType<NoContentResult>(result);
            Assert.Single(_context.WatchHistories.ToList());

            var newWatchHistory = _context.WatchHistories.First();
            Assert.Equal(TestGlobals.TEST_USER_ID, newWatchHistory.ApplicationUserId);
            Assert.Equal(mediaId, newWatchHistory.MediaId);
            Assert.Equal(json, newWatchHistory.Json);

            var newJson = new JObject(new JProperty("foo", "bar"));
            result = await _controller.PostWatchHistory(mediaId, newJson);
            Assert.IsType<NoContentResult>(result);
            Assert.Single(_context.WatchHistories.ToList());
            Assert.Equal(newJson, _context.WatchHistories.First().Json);
        }

        [Fact]
        public async Task Post_Watch_History_Fail()
        {
            var result = await _controller.PostWatchHistory("none", new JObject());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PostWatchHistory("none", null);
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PostWatchHistory(null, new JObject());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PostWatchHistory(null, null);
            Assert.IsType<BadRequestResult>(result);

            var mediaId = "media";
            _context.Medias.Add(new Media { Id = mediaId });
            _context.SaveChanges();

            result = await _controller.PostWatchHistory(mediaId, new JObject());
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Watch_History_Success()
        {
            var watchHistory = new WatchHistory
            {
                MediaId = "foo",
                ApplicationUserId = TestGlobals.TEST_USER_ID,
                Json = new JObject(new JProperty("hello", "world"))
            };

            _context.WatchHistories.Add(watchHistory);
            _context.SaveChanges();

            var result = await _controller.DeleteWatchHistory(watchHistory.Id);
            Assert.Equal(watchHistory, result.Value);
            Assert.Empty(_context.WatchHistories.ToList());
        }

        [Fact]
        public async Task Delete_Watch_History_Fail()
        {
            var result = await _controller.DeleteWatchHistory("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DeleteWatchHistory(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}