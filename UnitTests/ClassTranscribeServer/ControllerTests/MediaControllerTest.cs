using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeServer.ControllerTests
{
    public class MediaControllerTest : BaseControllerTest
    {
        private readonly MediaController _controller;

        public MediaControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new MediaController(
                fixture._authorizationService,
                (WakeDownloader) fixture._serviceProvider.GetService(typeof(WakeDownloader)),
                _context,
                _userUtils,
                null
            );
        }

        [Fact]
        public async Task Get_Media_Success()
        {
            var offering = new Offering { Id = "10" };
            var playlist = new Playlist { OfferingId = offering.Id, Id = "11" };
            var video = new Video { Duration = TimeSpan.FromSeconds(101), Id = "234" };
            var media = new Media { VideoId = video.Id, PlaylistId = playlist.Id, PublishStatus = PublishStatus.NotPublished };
            var transcription = new Transcription { Language = "en", VideoId = video.Id };

            _context.Videos.Add(video);
            _context.Medias.Add(media);
            _context.Offerings.Add(offering);
            _context.Playlists.Add(playlist);
            _context.Transcriptions.Add(transcription);
            _context.SaveChanges();

            var mediaDTO = (await _controller.GetMedia(media.Id)).Value;

            Assert.Equal(mediaDTO.Id, media.Id);
            Assert.Equal(playlist.Id, mediaDTO.PlaylistId);
            Assert.Equal(video.Id, mediaDTO.Video.Id);
            Assert.Single(mediaDTO.Transcriptions);
            Assert.Equal(transcription.Id, mediaDTO.Transcriptions[0].Id);
            Assert.Equal(transcription.Language, mediaDTO.Transcriptions[0].Language);
            Assert.Equal(PublishStatus.NotPublished, mediaDTO.PublishStatus);

            // Check DTO Duration field
            Assert.IsType<TimeSpan>(mediaDTO.Duration);
            Assert.True(mediaDTO.Duration.Equals(video.Duration));
        }

        [Fact]
        public async Task Get_Media_Fail()
        {
            var result = await _controller.GetMedia("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.GetMedia(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Put_Media_Name_Success()
        {
            var mediaId = "media";
            _context.Medias.Add(new Media { Id = mediaId, Name = "foo" });
            _context.SaveChanges();

            var newName = "bar";
            var result = await _controller.PutMediaName(mediaId, newName);
            Assert.IsType<NoContentResult>(result);

            var updatedMedia = _context.Medias.Find(mediaId);
            Assert.Equal(mediaId, updatedMedia.Id);
            Assert.Equal(newName, updatedMedia.Name);
        }

        [Fact]
        public async Task Put_Media_Name_Fail()
        {
            var result = await _controller.PutMediaName("none", null);
            Assert.IsType<NotFoundResult>(result);

            result = await _controller.PutMediaName(null, null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_Json_Success()
        {
            var mediaId = "media";
            _context.Medias.Add(new Media
            {
                Id = mediaId,
                JsonMetadata = new JObject(new JProperty("hello", "world"))
            });
            _context.SaveChanges();

            var newJson = new JObject(new JProperty("foo", "bar"));
            var result = await _controller.PutJsonMetaData(newJson, mediaId);
            Assert.IsType<NoContentResult>(result);

            var updatedMedia = _context.Medias.Find(mediaId);
            Assert.Equal(mediaId, updatedMedia.Id);
            Assert.Equal(newJson, updatedMedia.JsonMetadata);
        }

        [Fact]
        public async Task Put_Json_Fail()
        {
            var result = await _controller.PutJsonMetaData(null, "none");
            Assert.IsType<NotFoundResult>(result);

            result = await _controller.PutJsonMetaData(null, null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_Media_Success()
        {
            using var stream = File.OpenRead("Assets/test.mp4");
            var videoFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
            {
                Headers = new HeaderDictionary(),
                ContentType = "video/mp4"
            };

            var co = await Common.GetCourseOfferingForFileRecord(_context);
            var playlistId = co.Offering.Playlists[0].Id;

            var postResult = await _controller.PostMedia(videoFile, null, playlistId);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdMedia = createdResult.Value as Media;

            var media = _context.Medias.Find(createdMedia.Id);
            Assert.Equal(playlistId, media.PlaylistId);
            Assert.Equal(SourceType.Local, media.SourceType);
            Assert.Equal(JsonConvert.SerializeObject(videoFile), media.JsonMetadata["video1"]);
            Assert.Equal(PublishStatus.Published, media.PublishStatus);
            Assert.Equal(Visibility.Visible, media.Visibility);
            Assert.Equal("test", media.Name);
            Assert.NotNull(media.VideoId);

            // This was a new video, so it should correspond to the only Video and FileRecord objects
            var videos = _context.Videos.ToList();
            Assert.Single(videos);

            var video = videos.First();
            Assert.Equal(media.VideoId, video.Id);
            Assert.Equal(media.Id, video.Medias.First().Id);

            var fileRecords = _context.FileRecords.ToList();
            Assert.Single(fileRecords);
            Assert.Equal(video.Video1Id, fileRecords.First().Id);
        }

        // When posting a duplicate video that has corresponding FileRecord and Video objects
        // that already exist in the DB, the new file should be ignored
        // and the new media should be linked to the existing video object
        [Fact]
        public async Task Post_Existing_File_With_Existing_Video_Object()
        {
            using var stream = File.OpenRead("Assets/test.mp4");
            var videoFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
            {
                Headers = new HeaderDictionary(),
                ContentType = "video/mp4"
            };

            var co = await Common.GetCourseOfferingForFileRecord(_context);
            var playlistId = co.Offering.Playlists[0].Id;

            var postResult = await _controller.PostMedia(videoFile, null, playlistId);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdMedia = createdResult.Value as Media;
            var media = _context.Medias.Find(createdMedia.Id);

            var postResult2 = await _controller.PostMedia(videoFile, null, playlistId);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult2 = postResult2.Result as CreatedAtActionResult;
            var createdMedia2 = createdResult2.Value as Media;
            var media2 = _context.Medias.Find(createdMedia2.Id);

            // There should be one Video and one FileRecord, which both medias should point to
            var videos = _context.Videos.ToList();
            Assert.Single(videos);
            
            var video = videos.First();
            Assert.Equal(media.VideoId, video.Id);
            Assert.Equal(media2.VideoId, video.Id);
            Assert.Equal(2, video.Medias.Count());
            Assert.Equal(media.Id, video.Medias.First().Id);
            Assert.Equal(media2.Id, video.Medias.Last().Id);

            var fileRecords = _context.FileRecords.ToList();
            Assert.Single(fileRecords);
            Assert.Equal(video.Video1Id, fileRecords.First().Id);
        }

        // When posting a duplicate video that has a corresponding FileRecord
        // and but not a corresponding Video object, the old file should
        // be deleted and the new media should be linked with the new file
        [Fact]
        public async Task Post_Existing_File_Without_Existing_Video_Object()
        {
            using var stream = File.OpenRead("Assets/test.mp4");
            var videoFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
            {
                Headers = new HeaderDictionary(),
                ContentType = "video/mp4"
            };

            var filePath = CommonUtils.GetTmpFile();
            using (var stream2 = new FileStream(filePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream2);
            }

            var co = await Common.GetCourseOfferingForFileRecord(_context);
            var subdir =  CommonUtils.ToCourseOfferingSubDirectory(co);
            var playlistId = co.Offering.Playlists[0].Id;

            // This FileRecord will get deleted in the call to PostMedia below
            var firstFileRecord = await FileRecord.GetNewFileRecordAsync(filePath, Path.GetExtension(filePath), subdir);
            _context.FileRecords.Add(firstFileRecord);
            _context.SaveChanges();

            var postResult = await _controller.PostMedia(videoFile, null, playlistId);
            Assert.IsType<CreatedAtActionResult>(postResult.Result);

            var createdResult = postResult.Result as CreatedAtActionResult;
            var createdMedia = createdResult.Value as Media;
            var media = _context.Medias.Find(createdMedia.Id);

            // There should be one  Video and one FileRecord, and the first FileRecord should have been deleted
            Assert.Equal(Status.Deleted, firstFileRecord.IsDeletedStatus);

            var videos = _context.Videos.ToList();
            Assert.Single(videos);

            var video = videos.First();
            Assert.Equal(media.VideoId, video.Id);
            Assert.Equal(media.Id, video.Medias.First().Id);

            var fileRecords = _context.FileRecords.ToList();
            Assert.Single(fileRecords);
            Assert.Equal(video.Video1Id, fileRecords.First().Id);
        }

        [Fact]
        public async Task Post_Media_Fail()
        {
            using var stream = File.OpenRead("Assets/test.mov");
            var videoFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name))
            {
                Headers = new HeaderDictionary(),
                ContentType = "video/mp4"
            };

            var co = await Common.GetCourseOfferingForFileRecord(_context);
            var playlistId = co.Offering.Playlists[0].Id;

            var postResult = await _controller.PostMedia(videoFile, null, playlistId);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);

            using (var stream2 = File.OpenRead("Assets/test.mp4"))
            {
                var videoFile2 = new FormFile(stream2, 0, stream2.Length, null, Path.GetFileName(stream2.Name))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "video/mp4"
                };

                postResult = await _controller.PostMedia(videoFile2, videoFile, playlistId);
                Assert.IsType<BadRequestObjectResult>(postResult.Result);
            }

            postResult = await _controller.PostMedia(null, null, playlistId);
            Assert.IsType<BadRequestObjectResult>(postResult.Result);
        }

        [Fact]
        public async Task Delete_Media_Success()
        {
            var mediaId = "media";
            _context.Medias.Add(new Media { Id = mediaId });
            _context.SaveChanges();

            var result = await _controller.DeleteMedia(mediaId);
            Assert.Equal(mediaId, result.Value.Id);
            Assert.Empty(_context.Medias.ToList());
        }

        [Fact]
        public async Task Delete_Media_Fail()
        {
            var result = await _controller.DeleteMedia("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DeleteMedia(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Reorder_Success()
        {
            var playlistId = "playlist";
            var offeringId = "offering";

            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.Playlists.Add(new Playlist
            {
                Id = playlistId,
                OfferingId = offeringId
            });

            var mediaIds = new List<string>() { "m1", "m2", "m3" };

            for (var i = 0; i < mediaIds.Count; i++)
            {
                _context.Medias.Add(new Media { Id = mediaIds[i], PlaylistId = playlistId, Index = i });
            }

            _context.SaveChanges();

            var result = await _controller.Reorder(
                playlistId,
                new List<string>() { mediaIds[2], mediaIds[0], mediaIds[1] }
            );

            Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal(0, _context.Medias.Find(mediaIds[2]).Index);
            Assert.Equal(1, _context.Medias.Find(mediaIds[0]).Index);
            Assert.Equal(2, _context.Medias.Find(mediaIds[1]).Index);
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

            var playlistId = "playlist";
            var offeringId = "offering";
            _context.Playlists.Add(new Playlist {
                Id = playlistId,
                OfferingId = offeringId,
                Medias = new List<Media>() { new Media() }
            });
            _context.SaveChanges();

            result = await _controller.Reorder(playlistId, new List<string>() { "foo" });
            Assert.IsType<BadRequestResult>(result);

            _context.Offerings.Add(new Offering { Id = offeringId });
            _context.SaveChanges();

            result = await _controller.Reorder(playlistId, new List<string>() { "foo" });
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
