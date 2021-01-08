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
        private readonly int _time_to_live;

        public CleanUpElasticIndexTask(RabbitMQConnection rabbitMQ,
            ILogger<CleanUpElasticIndexTask> logger)
            : base(rabbitMQ, TaskType.CleanUpElasticIndex, logger)
        {
            var configuration = CTDbContext.GetConfigurations();

            // initialize elastic client
            var node = new Uri(configuration.GetValue<string>("ES_CONNECTION_ADDR"));
            _time_to_live = Int32.Parse(configuration.GetValue<string>("ES_INDEX_TIME_TO_LIVE"));
            using (var settings = new ConnectionSettings(node))
            {
                //settings.DefaultIndex("classTranscribe");
                _client = new ElasticClient(settings);
            }
        }
        protected async override Task OnConsume(string example, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            registerTask(cleanup, "CleanUpElasticIndexTask"); // may throw AlreadyInProgress exception
            GetLogger().LogInformation("CleanUpElasticIndexTask Starting");

            var result = await _client.Indices.GetAsync(new GetIndexRequest(Indices.All));
            var indices = result.Indices;
            foreach (var index in indices)
            {
                try
                {
                    string index_name = index.Key.ToString();

                    // elastic search internal management index starts with '.'
                    if (index_name[0] == '.')
                    {
                        continue;
                    }

                    GetLogger().LogInformation("Running Index: " + index_name);
                    string[] parts = index_name.Split("_");

                    if (parts.Length != 3)
                    {
                        GetLogger().LogError("Parsing Error: invalid Index Name");
                        continue;
                    }

                    var index_string_id = parts[0];
                    var index_string_lang = parts[1];
                    var index_string_time = parts[2];

                    // Indices that were created in the last 2 days would not be removed.
                    DateTime createdAt = DateTime.ParseExact(index_string_time, "yyyyMMddHHmmss", null);
                    GetLogger().LogInformation("Created at: " + createdAt.ToString());
                    DateTime range = DateTime.Now.AddMinutes(-_time_to_live);
                    if (DateTime.Compare(createdAt, range) >= 0)
                    {
                        GetLogger().LogInformation("Skipped: Index does not exceed time_to_live");
                        continue;
                    }

                    // If alias exist, this index is currently in use and should not be removed.
                    var index_string_alias = index_string_id + "_" + index_string_lang + "_" + "primary";
                    var cur_index = await _client.GetIndicesPointingToAliasAsync(index_string_alias);
                    var cur_index_list = cur_index.ToList();
                    if (cur_index_list.Any() && cur_index_list[0].ToString() == index_name)
                    {
                        GetLogger().LogInformation("Skipped: Index is in use");
                        continue;
                    }

                    // remove index
                    GetLogger().LogInformation("Removing Index: " + index_name);
                    var resp = await _client.Indices.DeleteAsync(index_name);
                    GetLogger().LogInformation(resp.ToString());
                }
                catch (Exception e)
                {
                    GetLogger().LogError("Error: " + e.ToString());
                    continue;
                }
            }
            GetLogger().LogInformation("CleanUpElasticIndexTask Done");
        }
    }
}
