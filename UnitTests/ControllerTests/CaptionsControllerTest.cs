using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static ClassTranscribeServer.Controllers.CaptionsController;

namespace UnitTests.ControllerTests
{
    public class CaptionsControllerTest : BaseControllerTest
    {
        private readonly CaptionsController _controller;

        public CaptionsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new CaptionsController(
                (WakeDownloader) fixture._serviceProvider.GetService(typeof(WakeDownloader)),
                _context,
                new CaptionQueries(_context),
                null
            );
        }

        [Fact]
        public async Task Get_Captions_Success()
        {
            var captions = new List<Caption>()
            {
                new Caption
                {
                    TranscriptionId = "001",
                    Index = 0,
                    Text = "foo bar",
                    CaptionType = CaptionType.TextCaption
                },
                new Caption
                {
                    TranscriptionId = "002",
                    Index = 0,
                    Text = "hello world",
                    CaptionType = CaptionType.TextCaption
                },
                new Caption
                {
                    TranscriptionId = "001",
                    Index = 1,
                    Text = "bar baz",
                    CaptionType = CaptionType.TextCaption
                }
            };

            _context.Captions.AddRange(captions);
            _context.SaveChanges();

            var result = await _controller.GetCaptions(captions[0].TranscriptionId);
            Assert.Equal(2, result.Value.Count());
            Assert.Equal(captions[0], result.Value.ElementAt(0));
            Assert.Equal(captions[2], result.Value.ElementAt(1));

            result = await _controller.GetCaptions(captions[1].TranscriptionId);
            Assert.Single(result.Value);
            Assert.Equal(captions[1], result.Value.ElementAt(0));
        }

        [Fact]
        public async Task Get_Captions_Empty()
        {
            var result = await _controller.GetCaptions("none");
            Assert.Empty(result.Value);

            result = await _controller.GetCaptions(null);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Get_Caption_Success()
        {
            var transcriptionId = "001";
            var index = 0;

            var captions = new List<Caption>()
            {
                new Caption
                {
                    TranscriptionId = transcriptionId,
                    Index = index,
                    Text = "foo bar",
                    CaptionType = CaptionType.TextCaption,
                    CreatedAt = DateTime.MinValue
                },
                new Caption
                {
                    TranscriptionId = transcriptionId,
                    Index = index,
                    Text = "bar baz",
                    CaptionType = CaptionType.TextCaption,
                    CreatedAt = DateTime.MaxValue
                }
            };

            _context.Captions.AddRange(captions);
            _context.SaveChanges();

            var result = await _controller.GetCaption(transcriptionId, index);
            Assert.Equal(captions[1], result.Value);
        }

        [Fact]
        public async Task Get_Caption_Not_Found()
        {
            var result = await _controller.GetCaption("none", 0);
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.GetCaption(null, -1);
            Assert.IsType<NotFoundResult>(result.Result);

            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            result = await _controller.GetCaption(caption.TranscriptionId, caption.Index + 1);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Post_Caption_Success()
        {
            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            caption.Text = "bar baz";

            var result = await _controller.PostCaption(caption);
            Assert.Equal(caption.TranscriptionId, result.Value.TranscriptionId);
            Assert.Equal(caption.Text, result.Value.Text);
            Assert.NotEqual(caption.CreatedAt, result.Value.CreatedAt);

        }

        [Fact]
        public async Task Post_Caption_Fail()
        {
            var result = await _controller.PostCaption(null);
            Assert.IsType<BadRequestObjectResult>(result.Result);

            var caption = new Caption { };

            result = await _controller.PostCaption(caption);
            Assert.IsType<BadRequestObjectResult>(result.Result);

            caption.Id = "none";

            result = await _controller.PostCaption(caption);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Up_Vote_Success()
        {
            var upvote = 26;

            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption,
                UpVote = upvote
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            var result = await _controller.UpVote(caption.Id);
            Assert.Equal(upvote + 1, result.Value.UpVote);
        }

        [Fact]
        public async Task Up_Vote_Fail()
        {
            var result = await _controller.UpVote("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.UpVote(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Down_Vote_Success()
        {
            var downvote = 26;

            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption,
                DownVote = downvote
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            var result = await _controller.DownVote(caption.Id);
            Assert.Equal(downvote + 1, result.Value.DownVote);
        }

        [Fact]
        public async Task Down_Vote_Fail()
        {
            var result = await _controller.DownVote("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DownVote(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Cancel_Up_Vote_Success()
        {
            var upvote = 26;

            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption,
                UpVote = upvote
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            var result = await _controller.CancelUpVote(caption.Id);
            Assert.Equal(upvote - 1, result.Value.UpVote);
        }

        [Fact]
        public async Task Cancel_Up_Vote_Fail()
        {
            var result = await _controller.CancelUpVote("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.CancelUpVote(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Cancel_Down_Vote_Success()
        {
            var downvote = 26;

            var caption = new Caption
            {
                TranscriptionId = "001",
                Index = 0,
                Text = "foo bar",
                CaptionType = CaptionType.TextCaption,
                DownVote = downvote
            };

            _context.Captions.Add(caption);
            _context.SaveChanges();

            var result = await _controller.CancelDownVote(caption.Id);
            Assert.Equal(downvote - 1, result.Value.DownVote);
        }

        [Fact]
        public async Task Cancel_Down_Vote_Fail()
        {
            var result = await _controller.CancelDownVote("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.CancelDownVote(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Search_In_Offering_Fail()
        {
            var result = await _controller.SearchInOffering("none", "none");
            Assert.Empty(result.Value);

            result = await _controller.SearchInOffering("none", null);
            Assert.Empty(result.Value);

            result = await _controller.SearchInOffering(null, "none");
            Assert.Empty(result.Value);

            result = await _controller.SearchInOffering(null, null);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Search_In_Offering()
        {
            var video = new Video { Id = "789" };
   
            var transcriptions = new List<Transcription>()
            {
                new Transcription
                {
                    Id="1001",
                    Language="en-US",
                    VideoId = video.Id

                },
                new Transcription
                {
                    Id="1002",
                    Language="fr",
                    VideoId = video.Id
                }
            };
            var captions = new List<Caption>()
            {
                new Caption
                {
                    Id = "C1",
                    TranscriptionId = transcriptions[0].Id,
                    Index = 0,
                    Text = "Yes, Fortran!",
                    CaptionType = CaptionType.TextCaption
                },
                new Caption
                {
                    Id = "C2",
                    TranscriptionId = transcriptions[1].Id,
                    Index = 0,
                    Text = "Oui, Fortran!",
                    CaptionType = CaptionType.TextCaption
                }

            };

            var offering = new CourseOffering { Id = "2123000"};
            var playlist = new Playlist { Id = "2456" , OfferingId = offering.Id, Name = "Playlist 1"};
            var media = new Media { Id = "2678", PlaylistId = playlist.Id , VideoId = video.Id, Name = "Media 1"};
            
            _context.CourseOfferings.Add(offering);
            _context.Playlists.Add(playlist);
            _context.Medias.Add(media);
            _context.Videos.Add(video);
            _context.Transcriptions.AddRange(transcriptions);
            _context.Captions.AddRange(captions);
            _context.SaveChanges();


            var noResults = await _controller.SearchInOffering("nosuchcourse", "Fortran");
            Assert.Empty(noResults.Value);

            var onlyEnglishResults = await _controller.SearchInOffering(offering.Id, "fortran");
            Assert.Single(onlyEnglishResults.Value);

            var bothResults = await _controller.SearchInOffering(offering.Id, "fortran","");
            
            List<SearchedCaptionDTO> bothResultList = bothResults.Value.ToList();
            Assert.Equal(captions.Count, bothResultList.Count());
            for (int i = 0; i < captions.Count; i++) {
                Assert.Equal(captions[i].Text, bothResultList[i].Caption.Text);
                Assert.Equal(transcriptions[i].Language, bothResultList[i].Language );
            }

            var oneFrenchResult = await _controller.SearchInOffering(offering.Id, "fortran", "fr");
            
            Assert.Single(oneFrenchResult.Value);
            var c = oneFrenchResult.Value.First(); 
            Assert.Equal( captions[1].Text, c.Caption.Text);
            Assert.Null(c.Caption.Transcription);
            
            Assert.Equal(media.Id, c.MediaId );
            Assert.Equal(playlist.Id, c.PlaylistId);
            Assert.Equal(media.Name, c.MediaName);
            Assert.Equal(playlist.Name, c.PlaylistName);

            var expandedSpanishResults = await _controller.SearchInOffering(offering.Id, "Oui", "es");
            // will drop language filter if no results are found and research against all languages
            Assert.Single(expandedSpanishResults.Value);
        }

        [Fact]
        public void To_PG_Language_Success()
        {
            Assert.Equal("english", CaptionsController.toPGLanguage("en-US"));
            Assert.Equal("romanian", CaptionsController.toPGLanguage("ro"));
            Assert.Equal("simple", CaptionsController.toPGLanguage("non-exist"));
            Assert.Equal("simple", CaptionsController.toPGLanguage(null));
        }
    }
}