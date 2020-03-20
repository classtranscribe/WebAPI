using ClassTranscribeDatabase;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly IAuthorizationService _authorizationService;

        public AdminController(IAuthorizationService authorizationService, WakeDownloader wakeDownloader,
            CTDbContext context, ILogger<AdminController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
        }


        [HttpPost("UpdateOffering")]
        public async Task<ActionResult> UpdateOffering(string offeringId)
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offeringId, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }
                else
                {
                    return new ChallengeResult();
                }
            }
            _wakeDownloader.UpdateOffering(offeringId);
            return Ok();
        }

        [HttpPost("UpdateAllPlaylists")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult UpdateAllPlaylists()
        {
            _wakeDownloader.UpdateAllPlaylists();
            return Ok();
        }

        [HttpPost("UpdatePlaylist")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult UpdatePlaylist(string playlistId)
        {
            _wakeDownloader.UpdatePlaylist(playlistId);
            return Ok();
        }

        [HttpPost("DownloadMedia")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult DownloadMedia(string mediaId)
        {
            _wakeDownloader.DownloadMedia(mediaId);
            return Ok();
        }

        [HttpPost("ConvertMedia")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult ConvertMedia(string videoId)
        {
            _wakeDownloader.ConvertMedia(videoId);
            return Ok();
        }

        [HttpPost("TranscribeVideo")]
        public ActionResult TranscribeVideo(string videoId)
        {
            _wakeDownloader.TranscribeVideo(videoId);
            return Ok();
        }

        [HttpPost("PeriodicCheck")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult PeriodicCheck()
        {
            _wakeDownloader.PeriodicCheck();
            return Ok();
        }

        [HttpGet("CreateBoxToken")]
        [AllowAnonymous]
        public ActionResult CreateBoxToken([FromQuery]string code)
        {
            _wakeDownloader.CreateBoxToken(code);
            return Ok("Request made to createBoxToken.");
        }

        [HttpGet("GetLogs")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<IActionResult> GetLogs(DateTime from, DateTime to)
        {
            var logs = await _context.Logs.Where(l => l.CreatedAt >= from && l.CreatedAt <= to).Select(l => new
            {
                l.Id,
                l.CreatedAt,
                l.UserId,
                l.OfferingId,
                l.MediaId,
                l.EventType,
                l.Json
            }).ToListAsync();
            var path = Path.GetTempFileName();
            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture))
                {
                    csv.WriteRecords(logs);
                }
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "text/csv", Path.GetFileNameWithoutExtension(path) + ".csv");
        }
    }
}