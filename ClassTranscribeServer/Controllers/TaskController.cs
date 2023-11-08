using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;

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
        public async Task<ActionResult> UpdateSceneData(string videoId,  JObject scene)
        {
            string sceneAsString = scene.ToString(0);
            Video video = await _context.Videos.FindAsync(videoId);
            var existingScenes = video.HasSceneObjectData();

            TextData data;
            if (existingScenes)
            {
                data = await _context.TextData.FindAsync(video.SceneObjectDataId);
                data.Text = sceneAsString;
            } else
            {
                data = new TextData() { Text = sceneAsString };
                _context.TextData.Add(data);
                video.SceneObjectDataId = data.Id;
                Trace.Assert(!string.IsNullOrEmpty(data.Id));
            }

            createDescriptionsIfNone(video, data);
            await _context.SaveChangesAsync();
            return Ok();
        }
        private void createDescriptionsIfNone(Video v, TextData scenedata)
        {
            JArray scenes = scenedata.getAsJSON()["Scenes"] as JArray;
            if (scenes == null || v == null || v.Id == null)
            {
                return;
            }

            var exists = v.Transcriptions.Exists(t=>t.TranscriptionType == TranscriptionType.TextDescription);
            if(exists)
            {
                _logger.LogInformation($"{v.Id}: already has descriptions (skipping)");
                return;
            }
            _logger.LogInformation($"{v.Id}: Creating basic descriptions");
            var captions = new List<Caption>();

            int index = 0;
            foreach (JObject scene in scenes)
            {
                var c = new Caption
                {
                    Index = index++,
                    Begin = TimeSpan.Parse(scene["start"].ToString()),
                    End = TimeSpan.Parse(scene["end"].ToString()),
                    CaptionType = CaptionType.AudioDescription,
                    Text = scene["phrases"]?.ToString()
                };
            }
            _logger.LogInformation($"{v.Id}: {index} entries added");
            var transcription = new Transcription()
            {
                Captions = captions,
                TranscriptionType = TranscriptionType.TextDescription,
                VideoId = v.Id,
                Language = Languages.ENGLISH_AMERICAN,
                Label = "Description",
                SourceLabel = "ClassTranscribe",
                SourceInternalRef = "ClassTranscribe/Scene-OCR"
            };

            _context.Add(transcription);
        }

            [HttpGet("GetPhraseHints")]
        public async Task<string> GetPhraseHints(string videoId) {
             Video video = await _context.Videos.FindAsync(videoId);
             if(video.HasPhraseHints()) {
                TextData data = await _context.TextData.FindAsync(video.PhraseHintsDataId);
                return data.Text;
             }
             // old version - 
             return video.PhraseHints ?? "";
        }



        [HttpGet("GetSceneData")]
        public async Task<ActionResult<Object>> GetSceneData(string videoId) {
             Video video = await _context.Videos.FindAsync(videoId);
             if(video.HasSceneObjectData()) {
                TextData data = await _context.TextData.FindAsync(video.SceneObjectDataId);
                return data.getAsJSON();
             }
             // old version - 
             return video.SceneData;
        }

        public class PhraseHintsDTO
        {
            public string PhraseHints { get; set; }
        }

        [HttpPost("UpdatePhraseHints")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdatePhraseHints(string videoId, PhraseHintsDTO phraseHintsDTO)
        {
            Video video = await _context.Videos.FindAsync(videoId);
            string hints = phraseHintsDTO.PhraseHints ?? "";
                       
            if(video.HasPhraseHints()) {
                TextData data = await _context.TextData.FindAsync(video.PhraseHintsDataId);
                data.Text = hints;
            }
            else {
                TextData data = new TextData();
                data.Text = hints;
                _context.TextData.Add(data);
                video.PhraseHintsDataId = data.Id;
                Trace.Assert(!string.IsNullOrEmpty(data.Id));
            }
            await _context.SaveChangesAsync();
            _wakeDownloader.TranscribeVideo(videoId, false /*deleteExisting*/);
            return Ok();
        }

        [HttpPost("UpdateGlossary")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateGlossary(string videoId, JObject glossary)
        {
            string glossaryAsString = glossary.ToString(0);
            Video video = await _context.Videos.FindAsync(videoId);
            if(video.HasGlossaryData())
            {
                TextData data = await _context.TextData.FindAsync(video.GlossaryDataId);
                data.Text = glossaryAsString;
            } else
            {
                TextData data = new TextData();
                data.Text = glossaryAsString;
                _context.TextData.Add(data);
                video.GlossaryDataId = data.Id;
                Trace.Assert(!string.IsNullOrEmpty(data.Id));
            }
           
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetGlossary")]
        public async Task<ActionResult<Object>> GetGlossary(string videoId) {
             Video video = await _context.Videos.FindAsync(videoId);
             if(video.HasGlossaryData()) {
                TextData data = await _context.TextData.FindAsync(video.GlossaryDataId);
                return data.getAsJSON();
             }
             // old version - 
             return video.Glossary;
        }

        [HttpPost("UpdateGlossaryTimestamp")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateGlossaryTimestamp(string videoId, JObject glossaryTimestamp)
        {
            string glossaryTimestampAsString = glossaryTimestamp.ToString(0);
            Video video = await _context.Videos.FindAsync(videoId);
            if(video.HasGlossaryTimestamp())
            {
                TextData data = await _context.TextData.FindAsync(video.GlossaryTimestampId);
                data.Text = glossaryTimestampAsString;
            } else
            {
                TextData data = new TextData();
                data.Text = glossaryTimestampAsString;
                _context.TextData.Add(data);
                video.GlossaryTimestampId = data.Id;
                Trace.Assert(!string.IsNullOrEmpty(data.Id));
            }
           
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetGlossaryTimestamp")]
        public async Task<ActionResult<Object>> GetGlossaryTimestamp(string videoId) {
             Video video = await _context.Videos.FindAsync(videoId);
             if(video.HasGlossaryTimestamp()) {
                TextData data = await _context.TextData.FindAsync(video.GlossaryTimestampId);
                return data.getAsJSON();
             }
             // old version - 
             return NotFound();
        }
    }
}