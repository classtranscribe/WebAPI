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
        IConnection _connection;
        IModel _channel { get; set; }
        public RabbitMQ()
        {
            var factory = new ConnectionFactory() { HostName = Globals.appSettings.RabbitMQServer };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        ~RabbitMQ()
        {
            _channel.Close();
            _connection.Close();
        }
        public void PublishTask<T>(string queueName, T message)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var body = CommonUtils.MessageToBytes(message);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
        }

        public void ConsumeTask<T>(string queueName, Func<T, Task> OnConsume)
        {

            _channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var message = CommonUtils.BytesToMessage<T>(ea.Body);
                Console.WriteLine(" [x] Received {0}", message);
                try
                {
                    await OnConsume(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine(" [x] Done");

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            _channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
        }

        public void DeleteAllQueues()
        {
            foreach(CommonUtils.TaskType taskType in Enum.GetValues(typeof(CommonUtils.TaskType)))
            {
                string queueName = RabbitMQ.QueueNameBuilder(taskType, "_1");
                _channel.QueueDelete(queueName);
            }
        }

        public static string QueueNameBuilder(CommonUtils.TaskType taskType, string mod)
        {
            return taskType.ToString() + "_" + mod;
        }
    }
}
