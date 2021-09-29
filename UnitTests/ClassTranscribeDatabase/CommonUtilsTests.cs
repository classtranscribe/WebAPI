﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace UnitTests.ClassTranscribeDatabase
{
    public class CommonUtilsTests
    {
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
    }
}