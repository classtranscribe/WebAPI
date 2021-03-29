using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace ClassTranscribeServer.Controllers
{
    [Route("/data/")]
    [ApiController]
    public class StaticFileController : BaseController
    {
        
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;
        private readonly PhysicalFileProvider _provider;
        
        public StaticFileController(IAuthorizationService authorizationService, 
            CTDbContext context,
            UserUtils userUtils,
            ILogger<StaticFileController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userUtils = userUtils;
            _provider = new PhysicalFileProvider(Globals.appSettings.DATA_DIRECTORY);
        }

        //[HttpGet("{relpath}")]
        /* We have files in subdirectories too. Though I experimented with route matching in Startup.cs, the following route definition is required. */
        //[Route("/data/{*id}")]

        [HttpGet("{*relpath}")]        
        public async Task<ActionResult> GetFile(string? relpath)
        {
            string urlpath =  HttpContext.Request.Path;
           
            string urlsubpath = urlpath.Split("/data/",2)[1]; // drop /data/

            var fileInfo = _provider.GetFileInfo(urlsubpath);
            if( ! fileInfo.Exists)
            {
                return NotFound();
            }
            // See Authorization.cs
            string? referer = HttpContext.Request.Headers["Referer"];
           
            if(string.IsNullOrEmpty(referer) || !  referer.Contains( HttpContext.Request.Host.Host,System.StringComparison.InvariantCultureIgnoreCase)) {
                return NotFound();
            }

            // An experiement to simulate MediaController-
            var standinMedia = await _context.Medias.FirstAsync();
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, standinMedia, Globals.POLICY_READ_OFFERING);
            //if(! authorizationResult.Succeeded) {
            //    return new ForbidResult();
            //}urlsubpath.EndsWith(".txt") && 
            if( User.Identity.IsAuthenticated)
            {
                // When tested this was never true.
                //return Ok("User.Identity.IsAuthenticated is true " + urlsubpath); 
            }

            var readStream = fileInfo.CreateReadStream();
            var mimeType = "application/octet-stream"; // arbitrary data
           
            return File(readStream, mimeType, enableRangeProcessing:true);
           
            
            // AppContext.SetSwitch("Switch.Microsoft.AspNetCore.Mvc.EnableRangeProcessing", true);
        }

    }
}
