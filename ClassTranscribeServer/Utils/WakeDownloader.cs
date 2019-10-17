using ClassTranscribeDatabase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeServer
{
    public static class WakeDownloader
    {
        public static void UpdateAllPlaylists()
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.DownloadAllPlaylists.ToString());
            Wake(msg);
        }

        public static void UpdatePlaylist(string playlistId)
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.DownloadPlaylistInfo.ToString());
            msg.Add("PlaylistId", playlistId);
            Wake(msg);
        }

        public static void UpdateVTTFile(string transcriptionId)
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.GenerateVTTFile.ToString());
            msg.Add("TranscriptionId", transcriptionId);
            Wake(msg);
        }

        public static void PeriodicCheck()
        {
            JObject msg = new JObject();
            msg.Add("Type", CommonUtils.TaskType.PeriodicCheck.ToString());
            Wake(msg);
        }

        private static void Wake(JObject message)
        {
            using (var rabbitmq = new RabbitMQConnection())
            {
                var queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.QueueAwaker, "_1");
                rabbitmq.PublishTask<JObject>(queueName, message);
            }
        }
    }
}
