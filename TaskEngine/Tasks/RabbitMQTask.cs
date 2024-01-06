using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

// #pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread


namespace TaskEngine
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    public abstract class RabbitMQTask<T>
    {
        private RabbitMQConnection RabbitMQ { get; set; }
        private readonly string QueueName;
        private readonly ILogger Logger;

        // All access to _inProgress and _unregisterLater should be locked using _inProgress
        // We keep track of all active tasks for this process
        // Note  during message consumption there is a another ClientActiveTasks object which tracks
        // the task currently running for that message
        private static readonly Dictionary<string, ClientActiveTasks> _inProgress = new Dictionary<string, ClientActiveTasks>();
       

        public RabbitMQTask() { }

        public RabbitMQTask(RabbitMQConnection rabbitMQ, TaskType taskType, ILogger logger)
        {
            RabbitMQ = rabbitMQ;
            QueueName = taskType.ToString();
            Logger = logger;
            lock (_inProgress)
            {
                if (!_inProgress.ContainsKey(QueueName))
                {
                    _inProgress.Add(QueueName, new ClientActiveTasks());
                }
            }
        }
        public void PurgeQueue()
        {
            RabbitMQ.PurgeQueue(QueueName);
        }

        public void Publish(T data, TaskParameters taskParameters = null)
        {
            try
            {
                if (taskParameters == null)
                {
                    taskParameters = new TaskParameters();
                }
                Logger.LogInformation($"Publish Task ({QueueName}). data=({data})");
                RabbitMQ.PublishTask(QueueName, data, taskParameters);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error Publishing Task to {QueueName} !");
            }
        }

        protected abstract Task OnConsume(T data, TaskParameters taskParameters, ClientActiveTasks cleanup);
        protected int PostConsumeCleanup(ClientActiveTasks cleanup)
        {
            if (cleanup == null)
            {
                return 0;
            }

            lock (_inProgress)
            {
                foreach(object id in cleanup)
                
                {
                    bool removed = _inProgress[QueueName].Remove(id);
                    if(!removed)
                    {
                        Logger.LogError($"_inProgress Q {QueueName} failed to remove '{id}'");
                    }

                }
            }
            cleanup.Clear();
            return 0; //Func<> must declare a return type; cannot be void
        }

        public void Consume(ushort concurrency)
        {
            if (concurrency == 0) {
                return; // note this means the queue will not be purged 
            }
            try
            {
                RabbitMQ.ConsumeTask<T>(QueueName, OnConsume, PostConsumeCleanup, concurrency);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(e, "RabbitMQTask Consume()->ConsumeTask Error Occured on Queue {0}", QueueName);
            }
        }

        /// <summary>
        /// Throws InProgressException if this task is already running
        /// </summary>
        /// <param name="keyId"></param>
        public void RegisterTask(HashSet<object> cleanup, Object keyId)
        {
            if (cleanup == null || keyId == null)
            {
                return;
            }
            bool alreadyRunning;

            lock (_inProgress)
            {
                if (cleanup.Contains(keyId))
                {
                    // This is a programming error the same message may not register the same key twice
                    throw new Exception($"Cleanup set may not already contain key ({keyId})");
                }
                // Now check that globally there is no other task working on the same id
                // This may happen rarely. The purpose of registerTask is to immediately stop (by throwing an exception) if we discover we are late to the party.
                alreadyRunning = !_inProgress[QueueName].Add(keyId);
            }
            if (alreadyRunning)
            {
                Logger.LogError("{0} for {1} Task already running, so skipping this request and throwing exception", QueueName, keyId);
                throw new InProgressException($"{ QueueName} for {keyId} Task already running, so skipping this request");
            }

            cleanup.Add(keyId);

        }
        /// <summary>
        /// Returns a new shallow copy of the current task set
        /// </summary>
        /// <returns></returns>
        public ClientActiveTasks GetCurrentTasks()
        {
            lock (_inProgress)
            {
                return new ClientActiveTasks(_inProgress[QueueName]);
            }
        }

        protected ILogger GetLogger()
        {
            return Logger;
        }
    }
    [Serializable]
    public class InProgressException : Exception
    {
        public InProgressException(string message) : base(message) { }

        public InProgressException()
        {
            throw new NotImplementedException();
        }

        public InProgressException(string message, Exception innerException) : base(message, innerException)
        {
            throw new NotImplementedException();
        }

        protected InProgressException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }


}
