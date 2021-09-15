using ClassTranscribeDatabase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        ///  Regenerate one Caption (vtt, srt) file of the given Transcription
        /// </summary>
        [HttpPost("UpdateVTTFile")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public ActionResult UpdateVTTFile(string transcriptionId)
        {
            _logger.LogInformation($"Enqueueing {transcriptionId} caption regeneration");
            _wakeDownloader.UpdateVTTFile(transcriptionId);
            return Ok();
        }

        /// <summary> 
        ///  Regenerate all Caption (vtt, srt) files of the given course offering
        /// </summary>
        [HttpPost("UpdateVTTFilesInCourseOffering")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateVTTFilesInCourseOffering(string offeringId = null)
        {

            var playlistIds = await _context.Playlists.Where(p => p.OfferingId == offeringId).Select(p => p.Id).ToListAsync();
            _logger.LogInformation($"UpdateVTTFilesinPlaylist(${offeringId}): Found {playlistIds.Count} playlists");

            var videoIds = await _context.Medias.Where(m => playlistIds.Contains(m.PlaylistId)).Select(m => m.VideoId).ToListAsync();
            _logger.LogInformation($"UpdateVTTFilesinPlaylist(): Found {videoIds.Count} videos");
            var transcriptionIds = await _context.Transcriptions.Where(t => videoIds.Contains(t.VideoId)).Select(t => t.Id).ToListAsync();
            _logger.LogInformation($"UpdateVTTFilesinPlaylist(): Found {transcriptionIds.Count} vtt transcriptions to regenerate");
            foreach (var t in transcriptionIds)
            {
                _wakeDownloader.UpdateVTTFile(t);
            }
            return Ok($"Requested {transcriptionIds.Count} Transcriptions to be regenerated from {videoIds.Count} videos in {playlistIds.Count} playlists");
        }
        /// <summary> 
        ///  Regenerate all Caption (vtt, srt) files of all transcriptions
        /// </summary>
        [HttpPost("UpdateAllVTTFiles")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateAllVTTFiles()
        {
            var transcriptionIds = await _context.Transcriptions.Select(t => t.Id).ToListAsync();
            _logger.LogInformation($"UpdateAllVTTFiles: Enqueueing {transcriptionIds.Count} vtt transcriptions to regenerate");
            foreach (var t in transcriptionIds)
            {
                _wakeDownloader.UpdateVTTFile(t);
            }
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

        [HttpPost("SceneDetectVideo")]
        public ActionResult SceneDetectVideo(string videoMediaPlaylistId, bool deleteExisting)
        {
            _wakeDownloader.SceneDetection(videoMediaPlaylistId, deleteExisting);
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
        public ActionResult CreateBoxToken([FromQuery] string code)
        {
            _wakeDownloader.CreateBoxToken(code);
            return Ok("Request made to createBoxToken.");
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