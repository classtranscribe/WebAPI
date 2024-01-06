using ClassTranscribeDatabase.Models;
using ClassTranscribeServer;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class PlaylistsControllerTest : BaseControllerTest
    {
        private readonly PlaylistsController _controller;

        public PlaylistsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new PlaylistsController(
                fixture._authorizationService,
                (WakeDownloader)fixture._serviceProvider.GetService(typeof(WakeDownloader)),
                _context,
                _userUtils,
                null
            );
        }

        [Fact]
        public async Task Get_Playlists_Success()
        {
            var offeringId = "offering";
            var playlists = new List<Playlist>()
            {
                new Playlist
                {
                    OfferingId = offeringId,
                    SourceType = SourceType.Local,
                    Name = "foo",
                    Index = 0,
                    PublishStatus = PublishStatus.Published
                },
                new Playlist
                {
                    OfferingId = "none",
                    SourceType = SourceType.Local,
                    Name = "bar",
                    PublishStatus = PublishStatus.Published
                },
                new Playlist
                {
                    OfferingId = offeringId,
                    SourceType = SourceType.Echo360,
                    Name = "baz",
                    Index = 1,
                    PublishStatus = PublishStatus.NotPublished
                }
            };

            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.Playlists.AddRange(playlists);
            _context.SaveChanges();

            var result = await _controller.GetPlaylists(offeringId);
            Assert.Equal(2, result.Value.Count());

            Assert.Equal(playlists[0].Id, result.Value.ElementAt(0).Id);
            Assert.Equal(playlists[0].SourceType, result.Value.ElementAt(0).SourceType);
            Assert.Equal(playlists[0].Name, result.Value.ElementAt(0).Name);
            Assert.Equal(playlists[0].Index, result.Value.ElementAt(0).Index);
            Assert.Equal(playlists[0].PublishStatus, result.Value.ElementAt(0).PublishStatus);

            Assert.Equal(playlists[2].Id, result.Value.ElementAt(1).Id);
            Assert.Equal(playlists[2].SourceType, result.Value.ElementAt(1).SourceType);
            Assert.Equal(playlists[2].Name, result.Value.ElementAt(1).Name);
            Assert.Equal(playlists[2].Index, result.Value.ElementAt(1).Index);
            Assert.Equal(playlists[2].PublishStatus, result.Value.ElementAt(1).PublishStatus);
        }

        [Fact]
        public async Task Get_Playlists_Fail()
        {
            var result = await _controller.GetPlaylists("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetPlaylists(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Get_Playlists2_Success()
        {
            var offeringId = "offering";
            var playlists = new List<Playlist>()
            {
                new Playlist
                {
                    OfferingId = offeringId,
                    SourceType = SourceType.Local,
                    Name = "foo",
                    Index = 0,
                    PublishStatus = PublishStatus.Published,
                    Options = "{\"a\":\"b\"}",
                    Medias = new List<Media>()
                    {
                        new Media()
                        {
                            Id = "media_foo",
                            SourceType = SourceType.Local,
                            PublishStatus = PublishStatus.NotPublished,
                            Options = "{\"c\":\"d\"}",
                            Video = new Video()
                            {
                                Transcriptions = new List<Transcription>(),
                                Duration = TimeSpan.FromSeconds(13)
                            }
                        }
                    }
                },
                new Playlist
                {
                    OfferingId = "none",
                    SourceType = SourceType.Local,
                    Name = "bar",
                    PublishStatus = PublishStatus.NotPublished,
                    Options="{}"
                },
                new Playlist
                {
                    OfferingId = offeringId,
                    SourceType = SourceType.Echo360,
                    Name = "baz",
                    Index = 1,
                    PublishStatus = PublishStatus.NotPublished,
                    Medias = new List<Media>()
                    {
                        new Media()
                        {
                            Id = "media_baz",
                            SourceType = SourceType.Echo360,
                            PublishStatus = PublishStatus.NotPublished,
                            Video = new Video()
                            {
                                Transcriptions = new List<Transcription>(),
                                Duration = TimeSpan.FromSeconds(1313)
                            }
                        }
                    }
                }
            };

            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.Playlists.AddRange(playlists);
            _context.SaveChanges();

            var result = await _controller.GetPlaylists2(offeringId);
            Assert.Equal(2, result.Value.Count());

            Assert.Equal(playlists[0].Id, result.Value.ElementAt(0).Id);
            Assert.Equal(playlists[0].SourceType, result.Value.ElementAt(0).SourceType);
            Assert.Equal(playlists[0].Name, result.Value.ElementAt(0).Name);
            Assert.Equal(playlists[0].Options, result.Value.ElementAt(0).Options.ToString(Newtonsoft.Json.Formatting.None));
            Assert.Equal(playlists[0].Index, result.Value.ElementAt(0).Index);
            Assert.Equal(playlists[0].PublishStatus, result.Value.ElementAt(0).PublishStatus);
            Assert.Equal(playlists[0].Medias[0].Id, result.Value.ElementAt(0).Medias[0].Id);
            Assert.Equal(playlists[0].Medias[0].SourceType, result.Value.ElementAt(0).Medias[0].SourceType);
            Assert.Equal(playlists[0].Medias[0].Video.Duration, result.Value.ElementAt(0).Medias[0].Duration);
            Assert.Equal(playlists[0].Medias[0].PublishStatus, result.Value.ElementAt(0).Medias[0].PublishStatus);

            Assert.Equal(playlists[2].Id, result.Value.ElementAt(1).Id);
            Assert.Equal(playlists[2].SourceType, result.Value.ElementAt(1).SourceType);
            Assert.Equal(playlists[2].Name, result.Value.ElementAt(1).Name);
            Assert.Equal(playlists[2].Index, result.Value.ElementAt(1).Index);
            Assert.Equal(playlists[2].PublishStatus, result.Value.ElementAt(1).PublishStatus);
            Assert.Equal(playlists[2].Medias[0].Id, result.Value.ElementAt(1).Medias[0].Id);
            Assert.Equal(playlists[2].Medias[0].SourceType, result.Value.ElementAt(1).Medias[0].SourceType);
            Assert.Equal(playlists[2].Medias[0].Video.Duration, result.Value.ElementAt(1).Medias[0].Duration);
            Assert.Equal(playlists[2].Medias[0].PublishStatus, result.Value.ElementAt(1).Medias[0].PublishStatus);
        }

        [Fact]
        public async Task Get_Playlists2_Fail()
        {
            var result = await _controller.GetPlaylists2("none");
            Assert.IsType<BadRequestResult>(result.Result);

            result = await _controller.GetPlaylists2(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Search_For_Media_Fail()
        {
            var result = await _controller.SearchForMedia("none", "none");
            Assert.Empty(result.Value);

            result = await _controller.SearchForMedia("none", null);
            Assert.Empty(result.Value);

            result = await _controller.SearchForMedia(null, "none");
            Assert.Empty(result.Value);

            result = await _controller.SearchForMedia(null, null);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Get_Playlist_Success()
        {
            var playlist = new Playlist
            {
                Id = "playlist",
                SourceType = SourceType.Box,
                Name = "foo",
                PublishStatus = PublishStatus.NotPublished,
                Medias = new List<Media>()
                {
                    new Media {
                        Id = "media_foo",
                        Name="fooy",
                        UniqueMediaIdentifier="123",
                        SourceType = SourceType.Local,
                        PublishStatus = PublishStatus.NotPublished,
                        Options = "{}",
                        Video = new Video {
                            Duration = TimeSpan.FromSeconds(13),
                            Transcriptions = new List<Transcription>(),
                        },
                    }
                },
                Options = "{}"
            };
            var watch = new WatchHistory
            {
                Id = "1",
                ApplicationUserId = "123",
                MediaId = "media_foo",
                Json = new Newtonsoft.Json.Linq.JObject()   
            };
            _context.WatchHistories.Add(watch);
            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            var result = await _controller.GetPlaylist(playlist.Id);
            Assert.Equal(playlist.Id, result.Value.Id);
            Assert.Equal(playlist.SourceType, result.Value.SourceType);
            Assert.Equal(playlist.Name, result.Value.Name);
            Assert.Equal(playlist.PublishStatus, result.Value.PublishStatus);
            Assert.Equal(playlist.Medias[0].Id, result.Value.Medias[0].Id);
            Assert.Equal(playlist.Medias[0].SourceType, result.Value.Medias[0].SourceType);
            Assert.Equal(playlist.Medias[0].Video.Duration, result.Value.Medias[0].Duration);
            Assert.Equal(playlist.Medias[0].PublishStatus, result.Value.Medias[0].PublishStatus);
        }

        [Fact]
        public async Task Get_Playlist_Fail()
        {
            var result = await _controller.GetPlaylist("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.GetPlaylist(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Put_Playlist_Success()
        {
            var offeringId = "offering";
            var playlist = new Playlist
            {
                OfferingId = offeringId,
                SourceType = SourceType.Local,
                Name = "foo",
                Index = 0,
                Options = "{}"
            };

            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.Playlists.Add(playlist);
            _context.SaveChanges();
            var update = new PlaylistUpdateDTO 
            {
                Id = playlist.Id,
                OfferingId = playlist.OfferingId,
                Name = "bart",
                Options = JObject.Parse("{\"a\":\"b\"}"),
                PublishStatus = PublishStatus.NotPublished
            };
            var result = await _controller.PutPlaylist(playlist.Id, update);
            Assert.IsType<NoContentResult>(result);

            var updatedPlaylist = _context.Playlists.Find(playlist.Id);
            Assert.Equal(update.Name, updatedPlaylist.Name);
            Assert.Equal(update.PublishStatus, updatedPlaylist.PublishStatus);
            Assert.Equal(update.Options, updatedPlaylist.GetOptionsAsJson());
            Assert.Equal(playlist.SourceType, updatedPlaylist.SourceType);
        }

        [Fact]
        public async Task Put_Playlist_Fail()
        {
            var result = await _controller.PutPlaylist("none", new PlaylistUpdateDTO());
            Assert.IsType<BadRequestObjectResult>(result);

            result = await _controller.PutPlaylist(null, new PlaylistUpdateDTO());
            Assert.IsType<BadRequestObjectResult>(result);

            result = await _controller.PutPlaylist("none", null);
            Assert.IsType<BadRequestObjectResult>(result);

            result = await _controller.PutPlaylist(null, null);
            Assert.IsType<BadRequestObjectResult>(result);

            var playlistId = "playlist";
            _context.Playlists.Add(new Playlist { Id = playlistId, OfferingId = "none" });
            _context.SaveChanges();

            result = await _controller.PutPlaylist(playlistId, new PlaylistUpdateDTO { Id = playlistId, OfferingId = "hiya" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Post_Playlist_Success()
        {
            var playlist = new NewPlaylistDTO
            {
                OfferingId = "offering",
                SourceType = SourceType.Local,
                Name = "foo",
                JsonMetadata = JObject.Parse("{'JM':'XY'}"),
                Options =JObject.Parse("{'OptionA':'A'}")
            };

            _context.Offerings.Add(new Offering { Id = playlist.OfferingId });

            var result = await _controller.PostPlaylist(playlist);
            // Assert.IsType<ActionResult>(result);

            var newPlaylist = _context.Playlists.Find(result.Value.Id);
            Assert.Equal(playlist.Name, newPlaylist.Name);
            Assert.Equal(PublishStatus.Published, playlist.PublishStatus);
            Assert.Equal(Visibility.Visible, newPlaylist.Visibility);
            Assert.Equal(SourceType.Local, newPlaylist.SourceType);
            Assert.Equal(playlist.OfferingId, newPlaylist.OfferingId);
            Assert.Contains("OptionA", newPlaylist.Options);
            Assert.Contains("XY", newPlaylist.JsonMetadata.ToString());

        }

        [Fact]
        public async Task Post_Playlist_Fail()
        {
            var result = await _controller.PostPlaylist(null);
            Assert.IsType<BadRequestObjectResult>(result.Result);

            result = await _controller.PostPlaylist(new NewPlaylistDTO());
            Assert.IsType<BadRequestObjectResult>(result.Result);

            result = await _controller.PostPlaylist(new NewPlaylistDTO() { OfferingId = "none" });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Delete_Playlist_Success()
        {
            var playlistId = "playlist";
            _context.Playlists.Add(new Playlist { Id = playlistId });
            _context.SaveChanges();

            var result = await _controller.DeletePlaylist(playlistId);
            Assert.Equal(playlistId, result.Value.Id);
            Assert.Empty(_context.Playlists.ToList());
        }

        [Fact]
        public async Task Delete_Playlist_Fail()
        {
            var result = await _controller.DeletePlaylist("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DeletePlaylist(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Reorder_Success()
        {
            var offeringId = "offering";
            _context.Offerings.Add(new Offering { Id = offeringId });

            var playlistIds = new List<string>() { "p1", "p2", "p3" };

            for (var i = 0; i < playlistIds.Count; i++)
            {
                _context.Playlists.Add(new Playlist { Id = playlistIds[i], OfferingId = offeringId, Index = i });
            }

            _context.SaveChanges();

            var result = await _controller.Reorder(
                offeringId,
                new List<string>() { playlistIds[2], playlistIds[0], playlistIds[1] }
            );

            Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal(0, _context.Playlists.Find(playlistIds[2]).Index);
            Assert.Equal(1, _context.Playlists.Find(playlistIds[0]).Index);
            Assert.Equal(2, _context.Playlists.Find(playlistIds[1]).Index);
        }

        [Fact]
        public async Task Reorder_Fail()
        {
            var result = await _controller.Reorder(null, null);
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.Reorder("none", null);
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.Reorder("none", new List<string>());
            Assert.IsType<BadRequestResult>(result);

            var offeringId = "offering";
            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.SaveChanges();

            result = await _controller.Reorder(offeringId, new List<string>() { "foo" });
            Assert.IsType<BadRequestResult>(result);

            _context.Playlists.Add(new Playlist { OfferingId = offeringId });
            _context.SaveChanges();

            result = await _controller.Reorder(offeringId, new List<string>() { "foo" });
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}