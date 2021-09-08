using ClassTranscribeDatabase.Models;
using Xunit;

namespace UnitTests.OtherTests
{
    public class ModelsTests
    {
        [Fact]
        public void Video_JObjects_Not_Null()
        {
            var video = new Video();
            Assert.NotNull(video.JsonMetadata);
            Assert.NotNull(video.SceneData);
            Assert.NotNull(video.FileMediaInfo);
        }
    }
}
