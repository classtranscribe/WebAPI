﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using UnitTests.ClassTranscribeServer.ControllerTests;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeDatabase
{
    public class FileRecordTest : BaseControllerTest
    {
        public FileRecordTest(GlobalFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Set_File_Path_Fail()
        {
            var c = new Course { FilePath = "example" };
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await FileRecord.SetFilePath(_context, c));

            var co = new CourseOffering { FilePath = "example" };
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await FileRecord.SetFilePath(_context, co));

            co = new CourseOffering { };
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await FileRecord.SetFilePath(_context, co));

            var video = new Video { };
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await FileRecord.SetFilePath(_context, video));
        }

        [Fact]
        public async Task Set_File_Path_Success()
        {
            var c = new Course { Id = "001", CreatedAt = DateTime.Parse("01/02/03") };
            var co = new CourseOffering { Id = "002", CourseId = c.Id, OfferingId = "oid3" };
            _context.Courses.Add(c);
            _context.CourseOfferings.Add(co);
            await _context.SaveChangesAsync();

            await FileRecord.SetFilePath(_context, c);
            Assert.True(Common.IsValidFilePath(c));

            await FileRecord.SetFilePath(_context, co);
            Assert.True(Common.IsValidFilePath(co));
        }

        [Fact]
        public async Task Get_New_File_Record_Fail()
        {
            using var stream = File.OpenRead("Assets/test.png");
            var imageFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(stream.Name));
            var fileExt = Path.GetExtension(imageFile.FileName);
            var filePath = CommonUtils.GetTmpFile();
            using var stream2 = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream2);

            var c = new Course { Id = "001" };
            var co = new CourseOffering { Id = "002", CourseId = c.Id, FilePath = "a/../../b", OfferingId="o1" };
            _context.Courses.Add(c);
            _context.CourseOfferings.Add(co);
            await _context.SaveChangesAsync();
            string subdir = CommonUtils.ToCourseOfferingSubDirectory(_context, co);
            // The CourseOffering must have a valid FilePath
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await FileRecord.GetNewFileRecordAsync(filePath, fileExt, subdir)
            );

  

            co.FilePath = null;

            await FileRecord.SetFilePath(_context, c);
            await FileRecord.SetFilePath(_context, co);
            Assert.True(Common.IsValidFilePath(c));
            Assert.True(Common.IsValidFilePath(co));


            co.IsDeletedStatus = Status.Active;

            // File must exist
            var nonExistingFile = Path.Combine(Globals.appSettings.DATA_DIRECTORY, "non-existing");
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await FileRecord.GetNewFileRecordAsync(nonExistingFile, fileExt, "/data/")
            );
        }

        [Fact]
        public async Task Get_New_File_Record_Success()
        {
            var c = new Course { Id = "001", CreatedAt = DateTime.Parse("01/02/03") };
            var co = new CourseOffering { Id = "002", CourseId = c.Id , OfferingId= "Oid2"};
            _context.Courses.Add(c);
            _context.CourseOfferings.Add(co);
            await FileRecord.SetFilePath(_context, c);
            await FileRecord.SetFilePath(_context, co);
            string subdir = CommonUtils.ToCourseOfferingSubDirectory(_context,co);

            var filePath = CommonUtils.GetTmpFile();
            var fileExt = "png";           
            File.Copy("Assets/test.png", filePath);
            var fileRecord = await FileRecord.GetNewFileRecordAsync(filePath, fileExt, subdir);

            Assert.True(fileRecord.IsValidFile());
            Assert.EndsWith(fileExt, fileRecord.Path);
            var expectedPath = Path.Combine(Globals.appSettings.DATA_DIRECTORY, co.FilePath).Replace("\\", "/"); ;
            var actualPath = fileRecord.Path.Replace("\\", "/");
            
            Assert.StartsWith(expectedPath, actualPath);
        }
    }
}
