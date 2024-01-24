using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;
using UnitTests.ClassTranscribeServer.ControllerTests;


namespace UnitTests.ClassTranscribeDatabase
{
    public class CommonUtilsTest : BaseControllerTest
    {
         public CommonUtilsTest(GlobalFixture fixture) : base(fixture) { }

       [Fact]
        public void Get_Media_Name_Echo_360()
        {
            var media = new Media() { SourceType = SourceType.Echo360 };
            Assert.Equal("Untitled January 01, 1970", CommonUtils.GetMediaName(media));

            media.JsonMetadata["createdAt"] = "02/02/1993";
            Assert.Equal("Untitled February 02, 1993", CommonUtils.GetMediaName(media));

            media.JsonMetadata["lessonName"] = "Hello World";
            Assert.Equal("Hello World February 02, 1993", CommonUtils.GetMediaName(media));

            media.JsonMetadata["title"] = "foo bar";
            Assert.Equal("foo bar", CommonUtils.GetMediaName(media));
        }

        [Fact]
        public void Get_Media_Name_Youtube()
        {
            var media = new Media() { SourceType = SourceType.Youtube };
            Assert.Equal("Untitled", CommonUtils.GetMediaName(media));

            media.JsonMetadata["title"] = "";
            Assert.Equal("Untitled", CommonUtils.GetMediaName(media));

            media.JsonMetadata["title"] = "foo bar";
            Assert.Equal("foo bar", CommonUtils.GetMediaName(media));
        }

        [Fact]
        public void Get_Media_Name_Local()
        {
            var media = new Media() { SourceType = SourceType.Local };
            Assert.Equal("Untitled", CommonUtils.GetMediaName(media));

            media.JsonMetadata["video1"] = new JObject();
            Assert.Equal("Untitled", CommonUtils.GetMediaName(media));

            media.JsonMetadata["video1"] = "{FileName: \"foo bar\"}";
            Assert.Equal("foo bar", CommonUtils.GetMediaName(media));

            media.JsonMetadata["video1"] = "{FileName: \"foo.mp4\"}";
            Assert.Equal("foo", CommonUtils.GetMediaName(media));

            media.JsonMetadata["filename"] = "foo bar baz";
            Assert.Equal("foo bar baz", CommonUtils.GetMediaName(media));
        }

        [Fact]
        public void Get_Media_Name_Kaltura()
        {
            var media = new Media() { SourceType = SourceType.Kaltura };
            Assert.Equal("Untitled January 01, 1970", CommonUtils.GetMediaName(media));

            media.JsonMetadata["createdAt"] = "728611200";
            Assert.Equal("Untitled February 02, 1993", CommonUtils.GetMediaName(media));

            media.JsonMetadata["name"] = "foo bar";
            Assert.Equal("foo bar February 02, 1993", CommonUtils.GetMediaName(media));
        }

        [Fact]
        public void Get_Media_Name_Box()
        {
            var media = new Media() { SourceType = SourceType.Box };
            Assert.Equal("Untitled", CommonUtils.GetMediaName(media));

            media.JsonMetadata["name"] = "foo bar";
            Assert.Equal("foo bar", CommonUtils.GetMediaName(media));
        }

        [Fact]
        public void Get_Related_Course_Offering()
        {
            const string expected = "examplePath";

            var co = new CourseOffering { Id = "co1", FilePath = expected };
            var c = new Course { Id = "c1", CourseOfferings = new List<CourseOffering> { co } };
            var o = new Offering { Id = "o1", CourseOfferings = new List<CourseOffering> { co } };
            var p = new Playlist { Id = "p1", Offering = o };
            var m = new Media { Id="m1", Playlist = p };
            var v = new Video { Id = "v1", Medias = new List<Media> { m } };
            var t = new Transcription { Id = "t1", Video = v };

            _context.CourseOfferings.Add(co);
            _context.Offerings.Add(o);
            _context.Courses.Add(c);
            _context.Playlists.Add(p);
            _context.Medias.Add(m);
            _context.Videos.Add(v);
            _context.Transcriptions.Add(t);
            _context.SaveChanges();

            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,c));
            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,o));
            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,p));
            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,m));
            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,v));
            Assert.Equal(expected, CommonUtils.ToCourseOfferingSubDirectory(_context,t));

            
        }
    }
}
