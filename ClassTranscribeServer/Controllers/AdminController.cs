using ClassTranscribeDatabase;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            _wakeDownloader.UpdateOffering(offeringId);
            return Ok();
        }

        /// <summary> 
        /// Enqueue DownloadAllPlaylists task, which updates all playlists for all terms where start date is within 6 months of today.
        /// 
        /// </summary>
        /// <remarks> 
        /// Each playlist update is a separate task. Requesting an update is harmless though
        /// be aware that some external sources (e.g. Youtube) limit API usage.
        /// See QueueAwakerTask.DownloadAllPlaylists, DownloadPlaylistInfoTask for details
        /// This API call is just for the impatient because the PeriodicCheck task also updates 
        /// all playlists and (unlike this API function) also performs a PendingJobs task to kick off transcriptions.
        /// </remarks>
        [HttpPost("UpdateAllPlaylists")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult UpdateAllPlaylists()
        {
            _wakeDownloader.UpdateAllPlaylists();
            return Ok();
        }

        /// <summary> 
        ///  Enqueue DownloadPlaylist task, which updates one playlist.
        /// </summary>
        /// <remarks>
        ///  Requesting an update is harmless though
        ///  be aware that some external sources (e.g. Youtube) limit API usage.
        ///  See QueueAwakerTask.DownloadAllPlaylists, DownloadPlaylistInfoTask for details
        ///  This API call is just for the impatient because the PeriodicCheck task also updates 
        ///  all playlists and (unlike this API function) also performs a PendingJobs task to kick off transcriptions.
        /// </remarks>
        [HttpPost("UpdatePlaylist")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult UpdatePlaylist(string playlistId)
        {
            _wakeDownloader.UpdatePlaylist(playlistId);
            return Ok();
        }
        
        /// <summary>
        /// Requests a re-download of missing media
        /// </summary>
        /// <remarks>
        /// Enqueues a DownloadMedia task. Requests missing media (as opposed to waiting for the periodic check to discover them)
        /// 
        /// Duplicates are discarded. New videos cause captions and video processing tasks to be requested
        /// See DownloadMediaTask.cs for more details.
        /// </remarks>
        [HttpPost("DownloadMedia")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult DownloadMedia(string mediaId)
        {
            _wakeDownloader.DownloadMedia(mediaId);
            return Ok();
        }

        /// <sumarize>
        /// Enqueue a ConvertMedia task. This creates a wav file (no longer used) and request captions
        /// </sumarize>
        /// <remarks>
        /// It is unclear if this request is still useful.
        /// </remarks>
        [HttpPost("ConvertMedia")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult ConvertMedia(string videoId)
        {
            _wakeDownloader.ConvertMedia(videoId);
            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoOrMediaId">A videoId or mediaId</param>
        /// <param name="deleteExisting">If true, existing transriptions are deleted first</param>
        /// <returns></returns>
        [HttpPost("TranscribeVideo")]
        public ActionResult TranscribeVideo(string videoOrMediaId, bool deleteExisting)
        {
            _wakeDownloader.TranscribeVideo(videoOrMediaId, deleteExisting);
            return Ok();
        }

        [HttpPost("ReTranscribePlaylist")]
        public ActionResult ReTranscribePlaylist(string playlistId)
        {
            _wakeDownloader.ReTranscribePlaylist(playlistId);
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
            var path = CommonUtils.GetTmpFile();
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

            var fileToReturn = File(memory, "text/csv", Path.GetFileNameWithoutExtension(path) + ".csv");

            memory.Dispose();

            return fileToReturn;
        }

        /// <summary>
        /// Returns the sha1 commit hash and build number, or 'unspecified' if these are unknown
        /// Example result : {"Commit":"hexadecimalnumber","Build":"123"}
        /// </summary>
        [HttpGet("GetVersion")]
        [AllowAnonymous]
        [Produces("application/json")]
        #pragma warning disable CA1822 // The warning suggests marking this as static but ASP.NET doesn't support static endpoints
        public ActionResult<BuildVersionDTO> GetVersion()
        #pragma warning restore CA1822
        {
            BuildVersionDTO result = new BuildVersionDTO()
            {
                Commit = Globals.appSettings.GITSHA1,
                Build = Globals.appSettings.BUILDNUMBER
            };
            return result;
        }

        public class BuildVersionDTO
        {
            public string Commit { get; set; }
            public string Build { get; set; }
        }        
    }
}