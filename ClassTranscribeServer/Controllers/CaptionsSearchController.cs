using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System;
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
        public async Task<ActionResult<Caption>> Search([FromBody] string[] ids, string keywords)
        {
            if (ids == null)
            {
                return Ok();
            }
            List<string> indices = new List<string>();
            foreach (string id in ids)
            {
                indices.Add(id + "_en-us_primary");
            }
            var result = await _elasticClient.SearchAsync<Caption>(s => s
                                   .Index(indices.ToArray())
                                   .Size(1000)
                                   .Query(q => q
                                       .Bool(b => b
                                           .Must(m => m
                                               .Match(m1 => m1
                                                   .Field(f => f.Text)
                                                   .Query(keywords)
                                                   .Fuzziness(Fuzziness.Auto)
                                                       .Operator(Nest.Operator.Or)
                                               )
                                           )
                                       )
                                   )
                               );

            if (result.Documents == null)
            {
                return NotFound();
            }
            return Ok(result.Documents);
        }
    }
}
