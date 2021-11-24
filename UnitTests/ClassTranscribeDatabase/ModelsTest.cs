using ClassTranscribeDatabase.Models;
using Xunit;

namespace UnitTests.ClassTranscribeDatabase
{
    public class ModelsTest
    {
        [Fact]
        public void Model_JObjects_Not_Null()
        {
            var video = new Video();
            var applicationUser = new ApplicationUser();
            var offering = new Offering();
            var playlist = new Playlist();
            var media = new Media();
            var log = new Log();
            var ePub = new EPub();
            var watchHistory = new WatchHistory();
            var message = new Message();
            var taskItem = new TaskItem();

            Assert.NotNull(video.JsonMetadata);
            Assert.NotNull(video.SceneData);
            Assert.NotNull(video.FileMediaInfo);
            Assert.NotNull(applicationUser.Metadata);
            Assert.NotNull(offering.JsonMetadata);
            Assert.NotNull(playlist.JsonMetadata);
            Assert.NotNull(media.JsonMetadata);
            Assert.NotNull(log.Json);
            Assert.NotNull(ePub.Cover);
            Assert.NotNull(watchHistory.Json);
            Assert.NotNull(message.Payload);
            Assert.NotNull(taskItem.TaskParameters);
            Assert.NotNull(taskItem.ResultData);
            Assert.NotNull(taskItem.RemoteResultData);

            Assert.Empty(video.JsonMetadata);
            Assert.Empty(video.SceneData);
            Assert.Empty(video.FileMediaInfo);
            Assert.Empty(applicationUser.Metadata);
            Assert.Empty(offering.JsonMetadata);
            Assert.Empty(playlist.JsonMetadata);
            Assert.Empty(media.JsonMetadata);
            Assert.Empty(log.Json);
            Assert.Empty(ePub.Cover);
            Assert.Empty(watchHistory.Json);
            Assert.Empty(message.Payload);
            Assert.Empty(taskItem.TaskParameters);
            Assert.Empty(taskItem.ResultData);
            Assert.Empty(taskItem.RemoteResultData);
        }
    }
}
