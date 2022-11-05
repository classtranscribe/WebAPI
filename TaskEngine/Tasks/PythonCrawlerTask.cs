﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;


namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class PythonCrawlerTask : RabbitMQTask<string>
    {
        public PythonCrawlerTask(RabbitMQConnection rabbitMQ, ILogger<PythonCrawlerTask> logger)
            : base(rabbitMQ, TaskType.PythonCrawler, logger)
        {

        }
        /// <summary>Extracts ASL videos from online sources

        protected async override Task OnConsume(string sourceId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
        }
    }
}
