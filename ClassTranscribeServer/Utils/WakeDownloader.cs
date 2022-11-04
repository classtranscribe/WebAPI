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
            JObject msg = new JObject();
            msg.Add("Type", TaskType.DownloadAllPlaylists.ToString());
            Wake(msg);
        }

        public virtual void UpdatePlaylist(string playlistId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.DownloadPlaylistInfo.ToString());
            msg.Add("PlaylistId", playlistId);
            Wake(msg);
        }

        public virtual void UpdateVTTFile(string transcriptionId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.GenerateVTTFile.ToString());
            msg.Add("TranscriptionId", transcriptionId);
            Wake(msg);
        }

        public void UpdateOffering(string offeringId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.UpdateOffering.ToString());
            msg.Add("offeringId", offeringId);
            Wake(msg);
        }

        public void PeriodicCheck()
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.PeriodicCheck.ToString());
            Wake(msg);
        }

        public void GenerateScenes(string mediaId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.SceneDetection.ToString());
            msg.Add("mediaId", mediaId);
            Wake(msg);
        }

        public void CreateBoxToken(string authCode)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.CreateBoxToken.ToString());
            msg.Add("authCode", authCode);
            Wake(msg);
        }

        public void DownloadMedia(string mediaId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.DownloadMedia.ToString());
            msg.Add("mediaId", mediaId);
            Wake(msg);
        }

        public void ConvertMedia(string videoId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.ConvertMedia.ToString());
            msg.Add("videoId", videoId);
            Wake(msg);
        }

        public void TranscribeVideo(string videoOrMediaId, bool deleteExisting)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.TranscribeVideo.ToString());
            msg.Add("videoOrMediaId", videoOrMediaId);
            msg.Add("DeleteExisting", deleteExisting);
            Wake(msg);
        }
        public virtual void SceneDetection(string videoMediaPlaylistId, bool deleteExisting)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.SceneDetection.ToString());
            msg.Add("videoMediaPlaylistId", videoMediaPlaylistId);
            msg.Add("DeleteExisting", deleteExisting);
            Wake(msg);
        }

        public void UpdateASLVideo(string sourceId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.PythonCrawler.ToString());
            msg.Add("SourceId", sourceId);
            Wake(msg);
        }

        private void Wake(JObject message, TaskParameters taskParameters = null)
        {
            var queueName = TaskType.QueueAwaker.ToString();
            _rabbitMQ.PublishTask(queueName, message, taskParameters);
        }

        public void ReTranscribePlaylist(string playlistId)
        {
            JObject msg = new JObject();
            msg.Add("Type", TaskType.ReTranscribePlaylist.ToString());
            msg.Add("PlaylistId", playlistId);
            Wake(msg);
        }
    }
}
