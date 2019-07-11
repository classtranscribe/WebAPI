using ClassTranscribeDatabase;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TaskEngine
{
    public class RabbitMQ
    {
        IConnection Connection;
        IModel Channel;
        public RabbitMQ(IOptions<AppSettings> appSettings)
        {
            var factory = new ConnectionFactory() { HostName = appSettings.Value.RabbitMQServer };
            Connection = factory.CreateConnection();
            Channel = Connection.CreateModel();
        }

        ~RabbitMQ()
        {
            Channel.Close();
            Connection.Close();
        }
        public void PublishTask<T>(string queueName, T message)
        {
            Channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var body = MessageToBytes(message);
            var properties = Channel.CreateBasicProperties();
            properties.Persistent = true;

            Channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
        }

        public void ConsumeTask<T>(string queueName, Func<T, Task> OnConsume)
        {

            Channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            Channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += async (model, ea) =>
            {
                var message = BytesToMessage<T>(ea.Body);
                Console.WriteLine(" [x] Received {0}", message);

                await OnConsume(message);

                Console.WriteLine(" [x] Done");

                Channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            Channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
        }

        public static byte[] MessageToBytes<T>(T obj)
        {
            string output = JsonConvert.SerializeObject(obj);
            Console.WriteLine(" [x] Sending {0}", output);
            return Encoding.UTF8.GetBytes(output);
        }

        public static T BytesToMessage<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        public static string QueueNameBuilder(TaskType taskType, string mod)
        {
            return taskType.ToString() + "_" + mod;
        }
    }
}
