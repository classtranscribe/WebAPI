using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System;
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

        [HttpGet]
        public async Task<ActionResult<Caption>> Search(string index, string keyword)
        {
            var result = await _elasticClient.SearchAsync<Caption>(s => s
            .Index(index)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Text)
                            .Query(keyword)
                )
            ));

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}
