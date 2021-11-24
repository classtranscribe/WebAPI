using ClassTranscribeDatabase.Models;
using System;
using System.Threading.Tasks;
using UnitTests.ClassTranscribeServer.ControllerTests;
using UnitTests.Utils;
using Xunit;

namespace UnitTests.ClassTranscribeDatabase
{
    public class FileRecordTest: BaseControllerTest
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
            var co = new CourseOffering { CourseId = c.Id };
            _context.Courses.Add(c);
            _context.CourseOfferings.Add(co);
            await _context.SaveChangesAsync();

            await FileRecord.SetFilePath(_context, c);
            Assert.True(Common.IsValidFilePath(c));

            await FileRecord.SetFilePath(_context, co);
            Assert.True(Common.IsValidFilePath(co));
        }
    }
}
