using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using System.Threading.Tasks;
using Xunit;
using System;

namespace UnitTests.ControllerTests
{
    public class MediaControllerTest : BaseControllerTest
    {
        MediaController _controller;

        public MediaControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new MediaController(_authorizationService, null, _context, _userUtils, null);
        }

        [Fact]
        public async Task GetMedia_Returns_Valid_DTO()
        {
            var offering = new Offering { Id = "10" };
            var playlist = new Playlist { OfferingId = offering.Id, Id = "11" };
            var video = new Video { Duration = TimeSpan.FromSeconds(101), Id = "234" };
            var media = new Media { VideoId = video.Id, PlaylistId = playlist.Id, Id = "345" };
            var transcription = new Transcription { Language = "en", VideoId = video.Id, Id = "456" };
            //  Without at least one transcription media.Video.Transcriptions. is null so Select() throws null exception inside _controller.GetMedia() 
            //Is this an artefact of the test memorystore?

            _context.Videos.Add(video);
            _context.Medias.Add(media);
            _context.Offerings.Add(offering);
            _context.Playlists.Add(playlist);
            _context.Transcriptions.Add(transcription);
            await _context.SaveChangesAsync();

            var mediaDTO = (await _controller.GetMedia(media.Id)).Value;
            // Check new DTO Duration field
            Assert.IsType<TimeSpan>(mediaDTO.Duration);
            Assert.True(mediaDTO.Duration.Equals(video.Duration));
            // Can add more assertions here in the future
            Assert.Equal(playlist.Id, mediaDTO.PlaylistId);
            Assert.Single(mediaDTO.Transcriptions);
            Assert.Equal(mediaDTO.Id, media.Id);
        }

    }
}
