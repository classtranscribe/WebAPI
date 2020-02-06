using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ClassTranscribeServer.Controllers
{
    public class BaseController : ControllerBase
    {
        protected readonly CTDbContext _context;
        protected readonly ILogger _logger;

        public BaseController(CTDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }
    }
}