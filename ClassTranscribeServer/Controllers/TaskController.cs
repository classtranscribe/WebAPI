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
    public class TaskController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;

        public TaskController(WakeDownloader wakeDownloader,
            CTDbContext context,
            ILogger<TaskController> logger) : base(context, logger)
        {
            _wakeDownloader = wakeDownloader;
        }
        
        /// <summary>
        /// Get Video data
        /// </summary>
        /// 
        [HttpGet("Video")]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult<Video>> GetVideo(string videoId)
        {
            
            Video video = await _context.Videos.FindAsync(videoId);
            return video;
        }

        [HttpPost("UpdateSceneData")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateSceneData(string videoId, JObject scene)
        {
            
            Video video = await _context.Videos.FindAsync(videoId);
            video.SceneData = scene;
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        [HttpPost("UpdatePhraseHints")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateSceneData(string videoId, string phraseHints)
        {
           
            Video video = await _context.Videos.FindAsync(videoId);
            video.PhraseHints = phraseHints;
            await _context.SaveChangesAsync();
            _wakeDownloader.TranscribeVideo(videoId, false /*deleteExisting*/);
            return Ok();
        }

        [HttpPost("UpdateGlossary")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateGlossary(string videoId, JObject glossary)
        {
           
            Video video = await _context.Videos.FindAsync(videoId);
            video.Glossary = glossary;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}