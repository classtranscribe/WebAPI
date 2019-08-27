using ClassTranscribeDatabase;
using Newtonsoft.Json;
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
        public static void Wake()
        {
            var factory = new ConnectionFactory() { HostName = Globals.appSettings.RabbitMQServer };
            using (var connection = factory.CreateConnection())
            using (var _channel = connection.CreateModel())
            {
                var queueName = "WakeDownloader";
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: null);
                Console.WriteLine(" [x] Sent {0}");
            }
        }
    }
}
