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
using System.Security.Claims;
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
        // Will be Task<ActionResult> when we need to do async access checks     
        public ActionResult GetFile(string? relpath)
        {
        
            string urlpath =  HttpContext.Request.Path;
           
            string urlsubpath = urlpath.Split("/data/",2)[1]; // drop /data/

            var fileInfo = _provider.GetFileInfo(urlsubpath);
            if( ! fileInfo.Exists)
            {
                return NotFound();
            }
            // Require Authenticated user
            if( ! checkFileAccess(urlsubpath, User ) ) {
                return Unauthorized();
            }
            var readStream = fileInfo.CreateReadStream();
            var mimeType = "application/octet-stream"; // arbitrary data
           
            return File(readStream, mimeType, enableRangeProcessing:true);
        }

        private bool checkFileAccess(string path, ClaimsPrincipal User) {
            if(User.Identity.IsAuthenticated) {
                // Will be set if a valid Bearer token is presented
                // Future implementation can verify User access for a specific resource
            //var standinMedia = await _context.Medias.FirstAsync();
            //var authorizationResult = await _authorizationService.AuthorizeAsync(User, standinMedia, Globals.POLICY_READ_OFFERING);
            //if(! authorizationResult.Succeeded) {
            //    return new ForbidResult();
            //}urlsubpath.EndsWith(".txt") && 
                return true;
            }
            // Referer checks are deprecated and will be removed in the future
            string? referer = HttpContext.Request.Headers["Referer"];
               
            if(string.IsNullOrEmpty(referer)) {
                return false;
            }
            if (referer.Contains( HttpContext.Request.Host.Host,System.StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
            bool acceptLocalHost = (Globals.appSettings.TEST_SIGN_IN == "true");
            if( acceptLocalHost && referer.Contains( "localhost",System.StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
            return false;
        }
    }
}
