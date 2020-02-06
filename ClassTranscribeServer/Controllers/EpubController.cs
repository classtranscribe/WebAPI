using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using static ClassTranscribeDatabase.CommonUtils;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EpubController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        
        public EpubController(WakeDownloader wakeDownloader, CTDbContext context, ILogger<EpubController> logger) : base(context, logger)
        {
            _wakeDownloader = wakeDownloader;
        }

        
        public class EPubChapter
        {
            public string Image { get; set; }
            public string Text { get; set; }
        }


        [NonAction]
        public List<EPubChapter> GetEPubChapters(JArray scenes, List<Caption> captions)
        {
            var chapters = new List<EPubChapter>();
            var nextStart = new TimeSpan(0);
            foreach (JObject scene in scenes)
            {
                var endTime = TimeSpan.Parse(scene["end"].ToString());
                var subset = captions.Where(c => c.Begin < endTime && c.Begin >= nextStart).ToList();
                StringBuilder sb = new StringBuilder();
                subset.ForEach(c => sb.Append(c.Text + " "));
                string allText = sb.ToString();
                chapters.Add(new EPubChapter
                {
                    Image = scene["img_file"].ToString(),
                    Text = allText
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
        public async Task<ActionResult<List<EPubChapter>>> GetEpubData(string mediaId)
        {
            var media = _context.Medias.Find(mediaId);
            EPub epub = new EPub
            {
                Language = Languages.ENGLISH,
                VideoId = media.VideoId
            };
            Video video = await _context.Videos.FindAsync(epub.VideoId);

            if (video.SceneData == null)
            {
                return NotFound();
            }

            var query = new CaptionQueries(_context);
            var captions = await query.GetCaptionsAsync(epub.VideoId, epub.Language);

            return GetEPubChapters(video.SceneData["Scenes"] as JArray, captions);
        }

        [HttpGet("RequestEpubCreation")]
        public ActionResult RequestEpubCreation(string mediaId)
        {
            _wakeDownloader.GenerateEpub(mediaId);
            return Ok();
        }
    }
}