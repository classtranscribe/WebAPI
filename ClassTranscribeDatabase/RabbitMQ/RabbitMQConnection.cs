﻿using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{
    public class RabbitMQConnection : IDisposable
    {
        IConnection _connection;
        IModel _channel { get; set; }
        public ushort prefetchCount { get; set; }
        private readonly ILogger _logger;
        public RabbitMQConnection(ILogger<RabbitMQConnection> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory()
            {
                HostName = Globals.appSettings.RabbitMQServer,
                UserName = Globals.appSettings.ADMIN_USER_ID,
                Password = Globals.appSettings.ADMIN_PASSWORD
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            prefetchCount = 10;
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
            _channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);

            _logger.LogInformation(" [*] Waiting for messages, queueName - {0}", queueName);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var message = CommonUtils.BytesToMessage<T>(ea.Body);
                _logger.LogInformation(" [x] Received {0}", message);
                try
                {
                    await OnConsume(message);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occured in RabbitMQConnection for message {0}", message.ToString());
                }

                _logger.LogInformation(" [x] Done {0}", message);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            _channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
        }

        public void DeleteAllQueues()
        {
            foreach (CommonUtils.TaskType taskType in Enum.GetValues(typeof(CommonUtils.TaskType)))
            {
                string queueName = taskType.ToString();
                _channel.QueueDelete(queueName);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _channel.Close();
                _connection.Close();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~RabbitMQConnection()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
