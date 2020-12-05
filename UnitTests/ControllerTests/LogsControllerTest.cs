using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class LogsControllerTest : BaseControllerTest
    {
        private readonly LogsController _controller;

        public LogsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new LogsController(
                fixture._authorizationService,
                _context,
                _userUtils,
                null
            )
            {
                ControllerContext = fixture._controllerContext
            };
        }

        [Fact]
        public async Task Post_Log()
        {
            var log = new Log
            {
                UserId = "user",
                EventType = "filtertrans"
            };

            var result = await _controller.PostLog(log);
            Assert.IsType<OkResult>(result);

            Assert.Single(_context.Logs.ToList());
            Assert.Equal(log, _context.Logs.FirstOrDefault());
        }

        [Fact]
        public async Task Get_Search_Logs_Success()
        {
            var offeringId = "offering";
            _context.Offerings.Add(new Offering { Id = offeringId });

            var result = await _controller.GetSearchLogs(offeringId);
            Assert.Empty(result.Value);

            var logs = new List<Log>()
            {
                new Log
                {
                    UserId = "user",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = "user",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "bar")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = "user",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                }
            };

            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            result = await _controller.GetSearchLogs(offeringId);
            Assert.Equal(2, result.Value.Count());
            Assert.Equal("foo", result.Value.ElementAt(0).Term);
            Assert.Equal(2, result.Value.ElementAt(0).Count);
            Assert.Equal("bar", result.Value.ElementAt(1).Term);
            Assert.Equal(1, result.Value.ElementAt(1).Count);
        }

        [Fact]
        public async Task Get_Search_Logs_Fail()
        {
            var result = await _controller.GetSearchLogs("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetSearchLogs(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Get_User_Search_History_Success()
        {
            var offeringId = "offering";
            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            var result = await _controller.UserSearchHistory(offeringId);
            Assert.Empty(result.Value);

            var logs = new List<Log>()
            {
                new Log
                {
                    UserId = "none",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = "none",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "bar")),
                    OfferingId = offeringId
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo")),
                    OfferingId = offeringId
                }
            };

            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            result = await _controller.UserSearchHistory(offeringId);
            Assert.Equal(2, result.Value.Count());
            Assert.Equal("foo", result.Value.ElementAt(0).Term);
            Assert.Equal(2, result.Value.ElementAt(0).Count);
            Assert.Equal("bar", result.Value.ElementAt(1).Term);
            Assert.Equal(1, result.Value.ElementAt(1).Count);
        }

        [Fact]
        public async Task Get_User_Search_History_Fail()
        {
            var result = await _controller.UserSearchHistory("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.UserSearchHistory(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Get_User_Logs_Success()
        {
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            var result = await _controller.GetUserLogs();
            Assert.Empty(result);

            var logs = new List<Log>()
            {
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "foo"))
                },
                new Log
                {
                    UserId = "none",
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "bar"))
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    Json = new JObject(new JProperty("value", "baz"))
                }
            };

            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            result = await _controller.GetUserLogs();
            Assert.Equal(2, result.Count());
            Assert.Equal(logs[0], result.ElementAt(0));
            Assert.Equal(logs[2], result.ElementAt(1));
        }

        [Fact]
        public async Task Get_User_Logs_Fail()
        {
            var result = await _controller.GetUserLogs();
            Assert.Null(result);
        }

        [Fact]
        public async Task Get_User_Logs_By_Event_Success()
        {
            var offeringId = "offering";
            var offeringId2 = "offering2";
            _context.Offerings.AddRange(new Offering { Id = offeringId }, new Offering { Id = offeringId2 });
            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            var logs = new List<Log>()
            {
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    OfferingId = offeringId,
                    MediaId = "1",
                    CreatedAt = DateTime.Now
                },
                new Log
                {
                    UserId = "none",
                    EventType = "filtertrans",
                    OfferingId = offeringId,
                    MediaId = "1",
                    CreatedAt = DateTime.Now
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "test",
                    OfferingId = offeringId2,
                    MediaId = "1",
                    CreatedAt = DateTime.Now
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    OfferingId = offeringId2,
                    MediaId = "1",
                    CreatedAt = DateTime.Now
                },
                new Log
                {
                    UserId = TestGlobals.TEST_USER_ID,
                    EventType = "filtertrans",
                    OfferingId = offeringId,
                    MediaId = "1",
                    CreatedAt = DateTime.Now.AddDays(-10)
                }
            };

            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            var result = await _controller.GetUserLogsByEvent(logs[0].EventType);
            Assert.Equal(2, result.Count());

            Assert.Equal(offeringId, result.ElementAt(0).OfferingId);
            Assert.Equal(2, result.ElementAt(0).Medias[0].LastMonth);
            Assert.Equal(1, result.ElementAt(0).Medias[0].LastWeek);

            Assert.Equal(offeringId2, result.ElementAt(1).OfferingId);
            Assert.Equal(1, result.ElementAt(1).Medias[0].LastMonth);

            result = await _controller.GetUserLogsByEvent(logs[0].EventType, DateTime.Now.AddDays(-1), DateTime.MaxValue);
            Assert.Equal(2, result.Count());

            Assert.Equal(offeringId, result.ElementAt(0).OfferingId);
            Assert.Equal(1, result.ElementAt(0).Medias[0].Count);

            Assert.Equal(offeringId2, result.ElementAt(1).OfferingId);
            Assert.Equal(1, result.ElementAt(1).Medias[0].Count);
        }

        [Fact]
        public async Task Get_User_Logs_By_Event_Fail()
        {
            var result = await _controller.GetUserLogsByEvent("none");
            Assert.Null(result);

            result = await _controller.GetUserLogsByEvent(null);
            Assert.Null(result);

            _context.Users.Add(new ApplicationUser { Id = TestGlobals.TEST_USER_ID });

            result = await _controller.GetUserLogsByEvent("none");
            Assert.Empty(result);

            result = await _controller.GetUserLogsByEvent(null);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Get_Course_Logs_Success()
        {
            var offeringId = "offering";
            _context.Offerings.Add(new Offering { Id = offeringId });

            var result = await _controller.GetCourseLogs(offeringId, string.Empty);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Get_Course_Logs_Fail()
        {
            var result = await _controller.GetCourseLogs("none", "none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetCourseLogs(null, "none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetCourseLogs("none", null);
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetCourseLogs(null, null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Get_All_Course_Logs_Success()
        {
            var offering = new Offering
            {
                Id = "offering",
                Playlists = new List<Playlist> {
                    new Playlist
                    {
                        Medias = new List<Media>
                        {
                            new Media
                            {
                                Id = "media1",
                                Name = "media1",
                                Index = 0,
                                Video = new Video
                                {
                                    Duration = TimeSpan.FromSeconds(13)
                                }
                            },
                            new Media
                            {
                                Id = "media2",
                                Name = "media2",
                                Index = 1
                            }
                        }
                    }
                }
            };

            _context.Offerings.Add(offering);
            _context.SaveChanges();

            var result = await _controller.GetAllCourseLogs(offering.Id, string.Empty);
            Assert.Empty(result.Value);

            result = await _controller.GetAllCourseLogs(offering.Id);
            Assert.Empty(result.Value);

            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = TestGlobals.TEST_USER_ID, FirstName = "test" },
                new ApplicationUser { Id = "otherUser", FirstName = "other" },
            };

            var logs = new List<Log>
            {
                new Log
                {
                    UserId = users[0].Id,
                    OfferingId = offering.Id,
                    MediaId = offering.Playlists[0].Medias[0].Id,
                    EventType = "timeupdate"
                },
                new Log
                {
                    UserId = users[0].Id,
                    OfferingId = offering.Id,
                    MediaId = offering.Playlists[0].Medias[0].Id,
                    EventType = "timeupdate"
                },
                new Log
                {
                    UserId = users[0].Id,
                    OfferingId = offering.Id,
                    MediaId = offering.Playlists[0].Medias[1].Id,
                    EventType = "other"
                },
                new Log
                {
                    UserId = users[0].Id,
                    OfferingId = offering.Id,
                    MediaId = offering.Playlists[0].Medias[1].Id,
                    EventType = "timeupdate"
                },
                new Log
                {
                    UserId = users[1].Id,
                    OfferingId = offering.Id,
                    MediaId = offering.Playlists[0].Medias[0].Id,
                    EventType = "timeupdate"
                }
            };

            _context.Users.AddRange(users);
            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            result = await _controller.GetAllCourseLogs(offering.Id);

            Assert.Equal(2, result.Value.Count());

            var testUserLogs = result.Value.First(userLogInfo => userLogInfo.User.Id == users[0].Id);
            var otherUserLogs = result.Value.First(userLogInfo => userLogInfo.User.Id == users[1].Id);

            Assert.Equal(2, testUserLogs.Medias.Count());
            Assert.Equal(users[0].Id, testUserLogs.User.Id);
            Assert.Equal(users[0].FirstName, testUserLogs.User.FirstName);

            Assert.Equal(offering.Playlists[0].Medias[0].Id, testUserLogs.Medias[0].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[0].Name, testUserLogs.Medias[0].MediaName);
            Assert.Equal(2, testUserLogs.Medias[0].Total);
            Assert.Equal(2, testUserLogs.Medias[0].LastHr);
            Assert.Equal(2, testUserLogs.Medias[0].Last3days);
            Assert.Equal(2, testUserLogs.Medias[0].LastWeek);
            Assert.Equal(2, testUserLogs.Medias[0].LastMonth);
            Assert.Equal(offering.Playlists[0].Medias[0].Video.Duration, testUserLogs.Medias[0].Duration);

            Assert.Equal(offering.Playlists[0].Medias[1].Id, testUserLogs.Medias[1].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[1].Name, testUserLogs.Medias[1].MediaName);
            Assert.Equal(1, testUserLogs.Medias[1].Total);
            Assert.Equal(1, testUserLogs.Medias[1].LastHr);
            Assert.Equal(1, testUserLogs.Medias[1].Last3days);
            Assert.Equal(1, testUserLogs.Medias[1].LastWeek);
            Assert.Equal(1, testUserLogs.Medias[1].LastMonth);

            Assert.Equal(2, otherUserLogs.Medias.Count());
            Assert.Equal(users[1].Id, otherUserLogs.User.Id);
            Assert.Equal(users[1].FirstName, otherUserLogs.User.FirstName);

            Assert.Equal(offering.Playlists[0].Medias[0].Id, otherUserLogs.Medias[0].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[0].Name, otherUserLogs.Medias[0].MediaName);
            Assert.Equal(1, otherUserLogs.Medias[0].Total);
            Assert.Equal(1, otherUserLogs.Medias[0].LastHr);
            Assert.Equal(1, otherUserLogs.Medias[0].Last3days);
            Assert.Equal(1, otherUserLogs.Medias[0].LastWeek);
            Assert.Equal(1, otherUserLogs.Medias[0].LastMonth);
            Assert.Equal(offering.Playlists[0].Medias[0].Video.Duration, otherUserLogs.Medias[0].Duration);

            Assert.Equal(offering.Playlists[0].Medias[1].Id, otherUserLogs.Medias[1].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[1].Name, otherUserLogs.Medias[1].MediaName);
            Assert.Equal(0, otherUserLogs.Medias[1].Total);
            Assert.Equal(0, otherUserLogs.Medias[1].LastHr);
            Assert.Equal(0, otherUserLogs.Medias[1].Last3days);
            Assert.Equal(0, otherUserLogs.Medias[1].LastWeek);
            Assert.Equal(0, otherUserLogs.Medias[1].LastMonth);

            result = await _controller.GetAllCourseLogs(offering.Id, "other");

            Assert.Single(result.Value);

            Assert.Equal(2, result.Value.ElementAt(0).Medias.Count());
            Assert.Equal(users[0].Id, result.Value.ElementAt(0).User.Id);
            Assert.Equal(users[0].FirstName, result.Value.ElementAt(0).User.FirstName);

            Assert.Equal(offering.Playlists[0].Medias[0].Id, result.Value.ElementAt(0).Medias[0].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[0].Name, result.Value.ElementAt(0).Medias[0].MediaName);
            Assert.Equal(0, result.Value.ElementAt(0).Medias[0].Total);
            Assert.Equal(0, result.Value.ElementAt(0).Medias[0].LastHr);
            Assert.Equal(0, result.Value.ElementAt(0).Medias[0].Last3days);
            Assert.Equal(0, result.Value.ElementAt(0).Medias[0].LastWeek);
            Assert.Equal(0, result.Value.ElementAt(0).Medias[0].LastMonth);
            Assert.Equal(offering.Playlists[0].Medias[0].Video.Duration, result.Value.ElementAt(0).Medias[0].Duration);

            Assert.Equal(offering.Playlists[0].Medias[1].Id, result.Value.ElementAt(0).Medias[1].MediaId);
            Assert.Equal(offering.Playlists[0].Medias[1].Name, result.Value.ElementAt(0).Medias[1].MediaName);
            Assert.Equal(1, result.Value.ElementAt(0).Medias[1].Total);
            Assert.Equal(1, result.Value.ElementAt(0).Medias[1].LastHr);
            Assert.Equal(1, result.Value.ElementAt(0).Medias[1].Last3days);
            Assert.Equal(1, result.Value.ElementAt(0).Medias[1].LastWeek);
            Assert.Equal(1, result.Value.ElementAt(0).Medias[1].LastMonth);
        }

        [Fact]
        public async Task Get_All_Course_Logs_Fail()
        {
            var result = await _controller.GetAllCourseLogs("none", "none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetAllCourseLogs(null, "none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetAllCourseLogs("none", null);
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetAllCourseLogs(null, null);
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetAllCourseLogs("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetAllCourseLogs(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Get_Event_Types_Success()
        {
            var result = await _controller.GetEventTypes();
            Assert.Empty(result);

            var logs = new List<Log>()
            {
                new Log { EventType = "filtertrans" },
                new Log { EventType = "filtertrans" },
                new Log { EventType = "test" },
                new Log { EventType = "foo" },
                new Log { EventType = "filtertrans" }
            };

            _context.Logs.AddRange(logs);
            _context.SaveChanges();

            result = await _controller.GetEventTypes();
            Assert.Equal(3, result.Count());
            Assert.Contains("filtertrans", result);
            Assert.Contains("test", result);
            Assert.Contains("foo", result);
        }

        [Fact]
        public async Task Get_User_Ids_Success()
        {
            var result = await _controller.GetUserIds();
            Assert.Empty(result);

            var users = new List<ApplicationUser>()
            {
                new ApplicationUser { Email = "filtertrans@test.edu" },
                new ApplicationUser { Email = "filtertrans@test.edu" },
                new ApplicationUser { Email = "test@test.edu" },
                new ApplicationUser { Email = "foo@test.edu" },
                new ApplicationUser { Email = "filtertrans@test.edu" }
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();

            result = await _controller.GetUserIds();
            Assert.Equal(3, result.Count());
            Assert.Contains("filtertrans@test.edu", result);
            Assert.Contains("test@test.edu", result);
            Assert.Contains("foo@test.edu", result);
        }
    }
}