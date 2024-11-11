using ClassTranscribeDatabase.Services;
using Newtonsoft.Json.Linq;
using static ClassTranscribeDatabase.CommonUtils;

namespace ClassTranscribeServer
{
    public class WakeDownloader
    {
        private readonly RabbitMQConnection _rabbitMQ;
        public WakeDownloader(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
        }
        //Todo: Fix field capitalization in here and QueueAwakerTask.cs
        public void UpdateAllPlaylists()
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.DownloadAllPlaylists.ToString() }
            };
            Wake(msg);
        }

        public virtual void UpdatePlaylist(string playlistId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.DownloadPlaylistInfo.ToString() },
                { "PlaylistId", playlistId }
            };
            Wake(msg);
        }

        // public virtual void UpdateVTTFile(string transcriptionId)
        // {
        //     JObject msg = new JObject
        //     {
        //         { "Type", TaskType.GenerateVTTFile.ToString() },
        //         { "TranscriptionId", transcriptionId }
        //     };
        //     Wake(msg);
        // }

        public void UpdateOffering(string offeringId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.UpdateOffering.ToString() },
                { "offeringId", offeringId }
            };
            Wake(msg);
        }

        public void PeriodicCheck()
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.PeriodicCheck.ToString() }
            };
            Wake(msg);
        }

        public void GenerateScenes(string mediaId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.SceneDetection.ToString() },
                { "mediaId", mediaId }
            };
            Wake(msg);
        }

        public void CreateBoxToken(string authCode)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.CreateBoxToken.ToString() },
                { "authCode", authCode }
            };
            Wake(msg);
        }

        public void DownloadMedia(string mediaId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.DownloadMedia.ToString() },
                { "mediaId", mediaId }
            };
            Wake(msg);
        }

        public void ConvertMedia(string videoId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.ConvertMedia.ToString() },
                { "videoId", videoId }
            };
            Wake(msg);
        }

        public void TranscribeVideo(string videoOrMediaId, bool deleteExisting)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.LocalTranscribeVideo.ToString() },
                { "videoOrMediaId", videoOrMediaId },
                { "DeleteExisting", deleteExisting }
            };
            Wake(msg);
        }
        public virtual void SceneDetection(string videoMediaPlaylistId, bool deleteExisting)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.SceneDetection.ToString() },
                { "videoMediaPlaylistId", videoMediaPlaylistId },
                { "DeleteExisting", deleteExisting }
            };
            Wake(msg);
        }

        public virtual void DescribeVideo(string videoMediaPlaylistId, bool deleteExisting) {
            JObject msg = new JObject
            {
                { "Type", TaskType.DescribeVideo.ToString() },
                { "videoMediaPlaylistId", videoMediaPlaylistId },
                { "DeleteExisting", deleteExisting }
            };
            Wake(msg);
        }

        public void UpdateASLVideo(string sourceId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.PythonCrawler.ToString() },
                { "SourceId", sourceId }
            };
            Wake(msg);
        }

        private void Wake(JObject message, TaskParameters taskParameters = null)
        {
            var queueName = TaskType.QueueAwaker.ToString();
            _rabbitMQ.PublishTask(queueName, message, taskParameters);
        }

        public void ReTranscribePlaylist(string playlistId)
        {
            JObject msg = new JObject
            {
                { "Type", TaskType.ReTranscribePlaylist.ToString() },
                { "PlaylistId", playlistId }
            };
            Wake(msg);
        }
    }
}
