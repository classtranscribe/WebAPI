using ClassTranscribeDatabase;
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

        public void UpdateAllPlaylists()
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.DownloadAllPlaylists.ToString());
            Wake(msg);
        }

        public void UpdatePlaylist(string playlistId)
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.DownloadPlaylistInfo.ToString());
            msg.Add("PlaylistId", playlistId);
            Wake(msg);
        }

        public void UpdateVTTFile(string transcriptionId)
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.GenerateVTTFile.ToString());
            msg.Add("TranscriptionId", transcriptionId);
            Wake(msg);
        }

        public void PeriodicCheck()
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.PeriodicCheck.ToString());
            Wake(msg);
        }

        public void GenerateEpub(string mediaId)
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.GenerateEPubFile.ToString());
            msg.Add("mediaId", mediaId);
            Wake(msg);
        }

        private void Wake(JObject message)
        {
            var queueName = TaskType.QueueAwaker.ToString();
            _rabbitMQ.PublishTask(queueName, message);
        }
    }
}
