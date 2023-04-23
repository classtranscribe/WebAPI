using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class BuildElasticIndexTask : RabbitMQTask<string>
    {
        private readonly ElasticClient _client;

        public BuildElasticIndexTask(RabbitMQConnection rabbitMQ,
            ILogger<BuildElasticIndexTask> logger)
            : base(rabbitMQ, TaskType.BuildElasticIndex, logger)
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
            registerTask(cleanup, "BuildElasticIndexTask"); // may throw AlreadyInProgress exception
            GetLogger().LogInformation("BuildElasticIndexTask Starting");


            var skipElasticIndexTask = true;
            if(skipElasticIndexTask) {
                GetLogger().LogInformation("BuildElasticIndexTask Done - No op - EARLY RETURN ");
                await Task.CompletedTask;
                return;
            }

            using (var _context = CTDbContext.CreateDbContext())
            {
                CaptionQueries captionQueries = new CaptionQueries(_context);

                var all_transcriptions = await _context.Transcriptions.Where(t => t.Language == Languages.ENGLISH_AMERICAN).ToListAsync();
                foreach (var transcription in all_transcriptions)
                {
                    var all_captions = transcription.Captions;

                    // each index has the unique name "index_string_unique", the current in use one has the alias "index_string_alias"
                    var index_string_base = transcription.Id + "_" + Languages.ENGLISH_AMERICAN.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                    var index_string_unique = index_string_base + "_" + $"{DateTime.Now:yyyyMMddHHmmss}";
                    var index_string_alias = index_string_base + "_" + "primary";

                    var asyncBulkIndexResponse = await _client.BulkAsync(b => b
                        .Index(index_string_unique)
                        .IndexMany(all_captions)
                    );

                    var alias_exist = await _client.Indices.ExistsAsync(index_string_alias);
                    if (alias_exist.Exists)
                    {
                        var oldIndices = await _client.GetIndicesPointingToAliasAsync(index_string_alias);
                        var oldIndexName = oldIndices.First().ToString();

                        var indexResponse = await _client.Indices.BulkAliasAsync(new BulkAliasRequest
                        {
                            Actions = new List<IAliasAction>
                            {
                                new AliasRemoveAction {Remove = new AliasRemoveOperation {Index = oldIndexName, Alias = index_string_alias}},
                                new AliasAddAction {Add = new AliasAddOperation {Index = index_string_unique, Alias = index_string_alias}}
                            }
                        });
                    } else
                    {
                        var putAliasResponse = await _client.Indices.PutAliasAsync(new PutAliasRequest(index_string_unique, index_string_alias));
                    }
                }
            }

            GetLogger().LogInformation("BuildElasticIndexTask Done");
        }
    }
}
