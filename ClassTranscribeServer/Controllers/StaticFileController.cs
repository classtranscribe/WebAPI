using ClassTranscribeDatabase;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


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
            PhysicalFileProvider provider,
            ILogger<StaticFileController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userUtils = userUtils;
            _provider = provider;
        }

        // Using double asterix to preserve forward slashes: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-5.0#rtr
        // Add "required" attribute to prevent null values: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-5.0#route-constraint-reference
        [HttpGet("{**relpath:required}")]   
        // Will be Task<ActionResult> when we need to do async access checks     
        public ActionResult GetFile(string relpath)
        {
            var fileInfo = _provider.GetFileInfo(relpath);

            if (!fileInfo.Exists)
            {
                return NotFound();
            }

            // Require Authenticated user
            if (!checkFileAccess(relpath, User))
            {
                return Forbid();
            }

            var readStream = fileInfo.CreateReadStream();
            var mimeType = "application/octet-stream"; // arbitrary data
           
            return File(readStream, mimeType, enableRangeProcessing: true);
        }

        private bool checkFileAccess(string path, ClaimsPrincipal User) {
            if (User.Identity.IsAuthenticated && path != null) {
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
#nullable enable
            string? referer = HttpContext.Request.Headers["Referer"];
#nullable disable
            
            if (string.IsNullOrEmpty(referer)) {
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
