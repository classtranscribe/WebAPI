using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EPubsController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly CaptionQueries _captionQueries;

        public EPubsController(WakeDownloader wakeDownloader,
            CTDbContext context,
            CaptionQueries captionQueries,
            ILogger<EPubsController> logger) : base(context, logger)
        {
            _captionQueries = captionQueries;
            _wakeDownloader = wakeDownloader;
        }

        public class EPubSceneData
        {
            public string Image { get; set; }
            public string Text { get; set; }
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
        }

        [NonAction]
        public static List<EPubSceneData> GetSceneData(JArray scenes, List<Caption> captions)
        {
            var chapters = new List<EPubSceneData>();
            var nextStart = new TimeSpan(0);

            if (scenes == null)
            {
                return chapters;
            }

            foreach (JObject scene in scenes)
            {
                var endTime = TimeSpan.Parse(scene["end"].ToString());
                var subset = captions.Where(c => c.Begin < endTime && c.Begin >= nextStart).ToList();

                StringBuilder sb = new StringBuilder();
                subset.ForEach(c => sb.Append(c.Text + " "));

                chapters.Add(new EPubSceneData
                {
                    Image = scene["img_file"].ToString(),
                    Start = TimeSpan.Parse(scene["start"].ToString()),
                    End = TimeSpan.Parse(scene["end"].ToString()),
                    Text = sb.ToString()
                });

                nextStart = endTime;
            }

            return chapters;
        }

        /// <summary>
        /// Gets captions and images for a given video
        /// </summary>
        /// 
        [HttpGet("GetEpubData")]
        [Authorize]
        public async Task<ActionResult<List<EPubSceneData>>> GetEpubData(string mediaId, string language)
        {
            var media = _context.Medias.Find(mediaId);
            Video video = await _context.Videos.FindAsync(media.VideoId);

            if (video.SceneData == null)
            {
                return NotFound();
            }

            EPub epub = new EPub
            {
                Language = language,
                SourceType = ResourceType.Media,
                SourceId = mediaId
            };

            var captions = await _captionQueries.GetCaptionsAsync(media.VideoId, epub.Language);

            return GetSceneData(video.SceneData["Scenes"] as JArray, captions);
        }

        [HttpGet("RequestEpubCreation")]
        [Authorize]
        public ActionResult RequestEpubCreation(string mediaId)
        {
            _wakeDownloader.GenerateScenes(mediaId);
            return Ok();
        }

        // GET: api/EPubs/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<EPub>> GetEPub(string id)
        {
            var ePub = await _context.EPubs.FindAsync(id);

            if (ePub == null)
            {
                return NotFound();
            }

            return ePub;
        }

        // GET: api/EPubs/BySource/{sourceType}/{sourceId}
        [HttpGet("BySource/{sourceType}/{sourceId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<EPub>>> GetEPubsBySource(string sourceType, string sourceId)
        {
            try
            {
                ResourceType type = (ResourceType)Enum.Parse(typeof(ResourceType), sourceType);

                var ePubs = await _context.EPubs.Where(i => i.SourceType == type && i.SourceId == sourceId).ToListAsync();

                if (!ePubs.Any())
                {
                    return NotFound();
                }

                ePubs.ForEach(ePub =>
                {
                    ePub.Chapters = null;
                });

                return ePubs;
            }
            catch (ArgumentException)
            {
                return BadRequest($"{sourceType} is not a valid resource type");
            }
        }

        // PUT: api/EPubs/5
        [HttpPut("{id}")]
        [DisableRequestSizeLimit]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<IActionResult> PutEPub(string id, EPub ePub)
        {
            if (ePub == null || id != ePub.Id)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(ePub.Title) ||
                string.IsNullOrEmpty(ePub.Filename) ||
                string.IsNullOrEmpty(ePub.Language) ||
                string.IsNullOrEmpty(ePub.Author) ||
                string.IsNullOrEmpty(ePub.Publisher) ||
                string.IsNullOrEmpty(ePub.SourceId))
            {
                return BadRequest("The following fields may not be empty: title, filename, language, author, publisher, sourceId");
            }

            _context.Entry(ePub).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.EPubs.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/EPubs
        [HttpPost]
        [DisableRequestSizeLimit]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult<EPub>> PostEPub(EPub ePub)
        {
            if (ePub == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(ePub.Title) ||
                string.IsNullOrEmpty(ePub.Filename) ||
                string.IsNullOrEmpty(ePub.Language) ||
                string.IsNullOrEmpty(ePub.Author) ||
                string.IsNullOrEmpty(ePub.Publisher) ||
                string.IsNullOrEmpty(ePub.SourceId))
            {
                return BadRequest("The following fields may not be empty: title, filename, language, author, publisher, sourceId");
            }

             _context.EPubs.Add(ePub);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEPub", new { id = ePub.Id }, ePub);
        }

        // DELETE: api/EPubs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        public async Task<ActionResult<EPub>> DeleteEPub(string id)
        {
            var ePub = await _context.EPubs.FindAsync(id);

            if (ePub == null)
            {
                return NotFound();
            }

            _context.EPubs.Remove(ePub);
            await _context.SaveChangesAsync();

            return ePub;
        }
    }
}