using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Implements message queues using RabbitMQ.
/// </summary>
/// <remarks>
/// See https://www.rabbitmq.com/dotnet-api-guide.html
/// Todo/Toreview: https://www.rabbitmq.com/dotnet-api-guide.html#concurrency-channel-sharing
/// 
/// </remarks>
/// 
namespace ClassTranscribeDatabase.Services
{
    // Used to keep track of all active running tasks
    // And current tasks per message being consumed
    // Some of the apparent twisted design surrounding the use of this class
    // Is to ensure that we can collect keys in the concrete classes
    // Then remove them in a finally{} block even if the client throws an exception
    public class ClientActiveTasks : HashSet<object>
    {
        public ClientActiveTasks() { }
        public ClientActiveTasks(ClientActiveTasks source) : base(source)
        {
        }

    };

    public class RabbitMQConnection : IDisposable
    {
        // Created by the first instance, then re-used
        private static IConnection Connection;
        private static int ConnectionRefCount;

        IModel Channel { get; set; }
        String Expiration; // milliseconds

        private readonly ILogger Logger;
        public RabbitMQConnection(ILogger<RabbitMQConnection> logger)
        {
            Logger = logger;
            CreateSharedConnection();

            // TODO/TOREVIEW: Check number of threads created
            // Potentially Model can be shared too 
            Channel = Connection.CreateModel();

            uint time = Math.Max(1, Convert.ToUInt32(Globals.appSettings.RABBITMQ_TASK_TTL_MINUTES));
            SetMessageExpiration(time);
        }

        private void CreateSharedConnection()
        {

            if (Connection != null)
            {
                ConnectionRefCount++;
                return;
            }
            Logger.LogInformation("Creating RabbitMQ connection");
            var factory = new ConnectionFactory()
            {

                HostName = Globals.appSettings.RABBITMQ_SERVER_NAME.Length > 0 ? Globals.appSettings.RABBITMQ_SERVER_NAME : Globals.appSettings.RabbitMQServer,
                UserName = Globals.appSettings.ADMIN_USER_ID,
                Password = Globals.appSettings.ADMIN_PASSWORD,
                Port = Convert.ToUInt16(Globals.appSettings.RABBITMQ_PORT) // 5672

            };
            // A developer may still want to checkout old code which uses the old env branch
            // so just complain loudly for now
            // In 2021 we can remove support for the old variable
            if (Globals.appSettings.RabbitMQServer.Length > 0)
            {
                Logger.LogError("*** Mixed case 'RabbitMQServer' environment variable is deprecated. Review your .env or vs_appsettings.json environment settings");
                if (Globals.appSettings.RABBITMQ_SERVER_NAME.Length == 0)
                {
                    Logger.LogError("*** Update your environment to use RABBITMQ_SERVER_NAME.");
                }
                else if (Globals.appSettings.RABBITMQ_SERVER_NAME != Globals.appSettings.RabbitMQServer)
                {
                    {
                        Logger.LogError("*** RABBITMQ_SERVER_NAME and RabbitMQServer are both set and different! Using RABBITMQ_SERVER_NAME");
                    }
                }
            }

            Logger.LogInformation($"Connecting to RabbitMQ server {factory.HostName} with user {factory.UserName} on port {factory.Port}...");
            Connection = factory.CreateConnection();
            ConnectionRefCount = 1;
        }


        public void SetMessageExpiration(uint ttlMinutes)
        {

            uint OneMinuteAsMilliseconds = 1000 * 60;
            Expiration = (OneMinuteAsMilliseconds * ttlMinutes).ToString();
            Logger.LogInformation("Using Message TTL {0} minutes", ttlMinutes);
        }

        /// <summary>
        /// Publishes a Task to the message queue (creating the queue if necessary)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="data"></param>
        /// <param name="taskParameters"></param>
        public void PublishTask<T>(string queueName, T data, TaskParameters taskParameters)
        {
            // Caution. The queue is also declared inside ConsumeTask below
            // I assume this was necessary because as soon as some queues start consuming events
            // they might publish a task to different queue?
            // Better? Should we create the queue (in Program.cs) separately from servicing or publishing the queue
            // Or is always (re-)declaring the queue best practices for RabbitNQ?
            lock ((Channel))
            {
                Channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            var taskObject = new TaskObject<T> { Data = data, TaskParameters = taskParameters };
            var body = CommonUtils.MessageToBytes(taskObject);
            var properties = Channel.CreateBasicProperties();
            properties.Persistent = true;

            // Note delivered but unacked messages do not expire
            properties.Expiration = Expiration; // milliseconds

            // See https://www.rabbitmq.com/dotnet-api-guide.html#concurrency-channel-sharing
            // Use a lock to ensure thread safety
            lock (Channel)
            {
                Channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
            }

        }

        /// <summary>
        /// Registers task and starts consuming messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="OnConsume"></param>
        public void ConsumeTask<T>(string queueName, Func<T, TaskParameters, ClientActiveTasks, Task> OnConsume, Func<ClientActiveTasks, int> PostConsumeCleanup, ushort concurrency)
        {
            // Caution. The queue is also declard inside PublishTask above
            Logger.LogInformation("Prefetch concurrency count {0}", concurrency);
            lock (Channel)
            {
                Channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                // See https://www.rabbitmq.com/consumer-prefetch.html
                // See https://stackoverflow.com/questions/59493540/what-is-prefetchsize-in-rabbitmq

                Channel.BasicQos(prefetchSize: 0, prefetchCount: concurrency, global: false);
                Logger.LogInformation(" [*] Queue created. Purging old messages for {0}", queueName);

                // Channel.QueuePurge(queueName);
            }

            Logger.LogInformation(" [*] Waiting for messages, queueName - {0}", queueName);

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += async (model, ea) =>
            {
                
                    // This object exists so that we can wrap all OnConsumes with a try-finally here
                    // And during the finally block remove the task from the set of active tasks
                    // At this level of the code we don't have the specific task information
                    // Instead the specific task my register a task by calling register
                    ClientActiveTasks clientCleanup = new ClientActiveTasks();

                var bytes = ea.Body.ToArray();
                var taskObject = CommonUtils.BytesToMessage<TaskObject<T>>(bytes);
                Logger.LogInformation(" [x] {0} Received {1}", queueName, taskObject.ToString());
                    // TODO: Update JobStatus table here (started timestamp)
                    try
                {
                    await OnConsume(taskObject.Data, taskObject.TaskParameters, clientCleanup);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error occured in RabbitMQConnection {0} for message {1}", queueName, taskObject.ToString());
                }
                finally
                {
                    PostConsumeCleanup(clientCleanup);

                }
                Logger.LogInformation(" [x] {0} Done {1}", queueName, taskObject.ToString());
                    // TODO Update JobStatus table here (including timestamp +  result + exception if it occurred)
                    lock (Channel)
                {
                    Channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
            lock (Channel)
            {
                Channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);
            }
        }

        public void PurgeQueue(String queueName)
        {
            Logger.LogInformation($"PurgeQueue {queueName}");
            lock (Channel)
            {
                try
                {
                    var count = Channel.MessageCount(queueName);
                    Logger.LogInformation("Purging queue {0}: {1} message(s) will be removed", queueName, count);
                } catch(Exception) {
                    // ignored
                }
                try { 
                    Channel.QueuePurge(queueName);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error purging queue {0}", queueName);
                    throw e;
                }

            }
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
             Logger.LogInformation($"RabbitMQ ****** Dispose ******. _connectionRefCount = {ConnectionRefCount}");
            if (Channel != null && !disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                lock (Channel)
                {
                    Channel.Close(); Channel = null;

                    --ConnectionRefCount;
                    Logger.LogInformation($"RabbitMQ refcount connection{ConnectionRefCount}");
                    if (ConnectionRefCount == 0)
                    {
                        // not sure why we would want to close the connection
                        // rather than just let it live for the 
                        // duration of the app
                        if (Globals.appSettings.RABBITMQ_REFCOUNT_CHANNELS == "Y")
                        {
                            Logger.LogInformation("Closing RabbitMQ connection");
                            Connection.Close(); 
                            Connection = null;
                        }

                        // Or we just keep the connection open even if the channels drop to zero
                        // TODO:Check this in both the TaskEngine and the WebAPI project (uses QueueAwakerTest)
                    }
                }
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
    public class TaskParameters
    {
        public TaskParameters() {}
        public TaskParameters(JObject meta) {this.Metadata = meta;}
        //TODO /TOREVIEW: This is checked in a few places, but is it set anywhere?
        public bool Force { get; set; }
        public JObject Metadata { get; set; }

        /// <summary>
        /// Returns a  readable String for debugging and logging purposes.
        /// </summary>
        /// <returns>A possibly verbose String representation of this object.</returns>
        public override string ToString()
        {
            return $"TaskParameters(Force = {Force}; Metadata = {Metadata})";
        }
    }

    public class TaskObject<T>
    {
        public T Data { get; set; }
        public TaskParameters TaskParameters { get; set; }
        /// <summary>
        /// Returns a  readable String for debugging and logging purposes.
        /// </summary>
        /// <returns>A possibly verbose String representation of this object.</returns>
        public override String ToString()
        {
            return $"TaskObject(Data={Data}; TaskParameters={TaskParameters};";
        }
    }
}
