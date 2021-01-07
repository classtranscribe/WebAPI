using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using CTCommons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Nest;
using Elasticsearch.Net;
using System;
using System.Collections.Generic;

namespace TaskEngine.Tasks
{
    class CleanUpElasticIndexTask : RabbitMQTask<string>
    {
        private readonly ElasticClient _client;

        public CleanUpElasticIndexTask(RabbitMQConnection rabbitMQ,
            ILogger<CleanUpElasticIndexTask> logger)
            : base(rabbitMQ, TaskType.CleanUpElasticIndex, logger)
        {
            var configuration = CTDbContext.GetConfigurations();

            // initialize elastic client
            var node = new Uri(configuration.GetValue<string>("ES_CONNECTION_ADDR"));
            using (var settings = new ConnectionSettings(node))
            {
                //settings.DefaultIndex("classTranscribe");
                _client = new ElasticClient(settings);
            }
        }
        protected async override Task OnConsume(string example, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, "CleanUpElasticIndexTask"); // may throw AlreadyInProgress exception
            _logger.LogInformation("CleanUpElasticIndexTask Starting");

            var result = await _client.Indices.GetAsync(new GetIndexRequest(Indices.All));
            var indices = result.Indices;
            foreach (var index in indices)
            {
                string index_name = index.Key.ToString();

                // elastic search internal management index starts with '.'
                if (index_name[0] == '.')
                {
                    continue;
                }

                _logger.LogInformation(index.Key.ToString());
                string[] parts = index_name.Split("_");
                var index_string_id = parts[0];
                var index_string_lang = parts[1];
                var index_string_time = parts[2];

                // Indices that were created in the last 2 days would not be removed.
                DateTime createdAt = DateTime.ParseExact(index_string_time, "yyyyMMddHHmmss", null);
                _logger.LogInformation("Created at: " + createdAt.ToString());
                DateTime range = DateTime.Now.AddDays(-2);
                if (DateTime.Compare(createdAt, range) >= 0)
                {
                    _logger.LogInformation("Skipped: Index is created in the last two days");
                    continue;
                }

                // If alias exist, this index is currently in use and should not be removed.
                var index_string_alias = index_string_id + "_" + index_string_lang + "_" + "primary";
                var cur_index = await _client.GetIndicesPointingToAliasAsync(index_string_alias);
                var cur_index_list = cur_index.ToList();
                if (cur_index_list.Any() && cur_index_list[0].ToString() == index_name)
                {
                    _logger.LogInformation("Skipped: Index is in use");
                    continue;
                }

                // remove index
                var resp = _client.Indices.DeleteAsync(index_name);
                _logger.LogInformation(resp.Result.ToString());
                _logger.LogInformation("Removed");
            }
            _logger.LogInformation("CleanUpElasticIndexTask Done");
        }
    }
}
