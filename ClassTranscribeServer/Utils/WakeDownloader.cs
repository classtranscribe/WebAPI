using ClassTranscribeDatabase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
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

        private static void Wake(JObject message)
        {
            var factory = new ConnectionFactory() { HostName = Globals.appSettings.RabbitMQServer };
            using (var connection = factory.CreateConnection())
            using (var _channel = connection.CreateModel())
            {
                var queueName = "WakeDownloader";
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = CommonUtils.MessageToBytes(message);
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
                Console.WriteLine(" [x] Sent {0}");
            }
        }
    }
}
