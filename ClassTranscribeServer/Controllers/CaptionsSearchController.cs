using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using Elasticsearch.Net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsSearchController : BaseController
    {
        private readonly IElasticClient _elasticClient;

        public CaptionsSearchController(CTDbContext context,
            IElasticClient client,
            ILogger<CaptionsSearchController> logger) : base(context, logger)
        {
            _elasticClient = client;
        }

        [HttpPost]
        public async Task<ActionResult<Caption>> Search([FromBody] string[] ids, string query, int page = 1, int pageSize = 10)
        {
            // TODO: add authentication
            if (ids == null || ids.Length == 0)
            {

                return NotFound();
            }
            var result = await _elasticClient.SearchAsync<Caption>(s => s
                                   .SearchType(SearchType.DfsQueryThenFetch)
                                   .Index(ids)
                                   .From((page - 1)*pageSize)
                                   .Size(pageSize)
                                   .Query(q => q
                                       .Bool(b => b
                                           .Must(m => m
                                               .Match(m1 => m1
                                                   .Field(f => f.Text)
                                                   .Query(query)
                                                   .Fuzziness(Fuzziness.Auto)
                                               )
                                           )
                                       )
                                   )
                               );

            if (result.Total == 0)
            {
                return NotFound();
            }
            return Ok(new SearchResult<Caption>
            {
                Total = result.Total,
                Page = page,
                Results = result.Documents,
                ElapsedMilliseconds = result.Took
            });
        }
    }

    public class SearchResult<T>
    {
        public long Total { get; set; }

        public int Page { get; set; }

        public IEnumerable<T> Results { get; set; }

        public long ElapsedMilliseconds { get; set; }
    }
}
