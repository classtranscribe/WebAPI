using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
            JArray scenes = scenedata.GetAsJSON()["Scenes"] as JArray;
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
                StringBuilder sb = new StringBuilder();
                var again = false;
                foreach (string phrase in scene["phrases"])
                {
                    sb.Append(again ? '\n' : '\"' );
                    sb.Append(phrase);
                    again = true;
                }
                sb.Append('\"');
                
                var c = new Caption
                {
                    Index = index++,
                    Begin = TimeSpan.Parse(scene["start"].ToString()),
                    End = TimeSpan.Parse(scene["end"].ToString()),
                    CaptionType = CaptionType.AudioDescription,
                    Text = sb.ToString()
                };
                
                captions.Add(c);
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
                return data.GetAsJSON();
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
            if (videoId == null || phraseHintsDTO == null) return BadRequest("Missing parameters");

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
            if (videoId == null || glossary == null) return BadRequest("Missing parameters");

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
                return data.GetAsJSON();
             }
             // old version - 
             return video.Glossary;
        }

        [HttpPost("UpdateGlossaryTimestamp")]
        [DisableRequestSizeLimit]
        //Future: [Authorize(Roles = Globals.ROLE_MEDIA_WORKER + "," + Globals.ROLE_ADMIN)]
        public async Task<ActionResult> UpdateGlossaryTimestamp(string videoId, JObject glossaryTimestamp)
        {
            if (videoId == null || glossaryTimestamp == null) return BadRequest("Missing parameters");
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
                return data.GetAsJSON();
             }
             // old version - 
             return NotFound();
        }

        private static (JObject glossaryDoc, JObject timestampDoc) BuildGlossaryDocs(JArray terms, double videoDuration)
        {
            var glossaryArray = new JArray();
            var timestampDict = new JObject();
            for (int i = 0; i < terms.Count; i++)
            {
                var t = terms[i];
                string tm = t["term"]?.ToString() ?? "";
                string df = t["definition"]?.ToString() ?? "";
                string sr = t["source"]?.ToString() ?? "transcript";
                double st = t["timestamp_seconds"]?.Value<double>() ?? 0;
                double nx = (i + 1 < terms.Count) ? (terms[i + 1]["timestamp_seconds"]?.Value<double>() ?? st + 90) : st + 90;
                double en = Math.Min(nx, videoDuration);
                if (string.IsNullOrWhiteSpace(tm)) continue;
                glossaryArray.Add(new JArray(tm, df, "", sr, "", "", ""));
                timestampDict[tm] = new JArray(
                    TimeSpan.FromSeconds(st).ToString(@"hh\:mm\:ss"),
                    TimeSpan.FromSeconds(en).ToString(@"hh\:mm\:ss")
                );
            }
            return (new JObject { ["Glossary"] = glossaryArray }, new JObject { ["glossaryTimestamp"] = timestampDict });
        }

        /// <summary>
        /// Extracts glossary terms from a video's captions and OCR using an LLM.
        /// Stores results in GlossaryDataId + GlossaryTimestampId for the Watch page popup.
        /// Pass force=true to regenerate even if a glossary already exists.
        /// </summary>
        [HttpPost("ExtractGlossary")]
        public async Task<ActionResult> ExtractGlossary(string videoId, bool force = false)
        {
            if (string.IsNullOrEmpty(videoId)) return BadRequest("videoId is required");

            var apiKey = Globals.appSettings.OPENAI_API_KEY;
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(503, "OPENAI_API_KEY is not configured on the server");

            Video video = await _context.Videos.FindAsync(videoId);
            if (video == null) return NotFound($"Video {videoId} not found");
            if (video.HasGlossaryData() && !force)
                return Ok(new { message = "Glossary already exists. Pass force=true to regenerate.", videoId });

            var captions = await _context.Captions
                .Where(c => c.Transcription.VideoId == videoId && c.Transcription.TranscriptionType == TranscriptionType.Caption)
                .OrderBy(c => c.Begin).ToListAsync();
            if (captions.Count == 0) return BadRequest($"No captions for video {videoId}. Complete transcription first.");

            var captionSb = new StringBuilder();
            foreach (var cap in captions) captionSb.AppendLine($"[{cap.Begin.TotalSeconds:F1}s] {cap.Text}");
            var captionText = captionSb.ToString();
            if (captionText.Length > 14000) captionText = captionText.Substring(0, 14000);

            var ocrText = "";
            if (video.HasSceneObjectData())
            {
                try
                {
                    var sd = await _context.TextData.FindAsync(video.SceneObjectDataId);
                    if (sd?.Text != null)
                    {
                        var ocrSb = new StringBuilder();
                        foreach (var scene in JToken.Parse(sd.Text))
                        {
                            var raw = scene["raw_text"]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(raw)) ocrSb.AppendLine(raw);
                        }
                        ocrText = ocrSb.ToString();
                        if (ocrText.Length > 4000) ocrText = ocrText.Substring(0, 4000);
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, $"{videoId}: OCR parse failed"); }
            }

            const string systemPrompt =
                "You are an expert teaching assistant for university courses. " +
                "Identify the most important domain-specific concepts from the provided lecture material. " +
                "\nRules:" +
                "\n- Only include terms central to understanding the subject (algorithms, mathematical concepts, scientific principles, technical methods, key theories)." +
                "\n- Do NOT include common words, filler phrases, instructor names, or trivial terms." +
                "\n- For each term write a concise definition (1-3 sentences) grounded in how it is used in THIS lecture." +
                "\n- Set timestamp_seconds to the float seconds where the term first meaningfully appears. Use 0 if only in OCR." +
                "\n- Set source to \"transcript\", \"ocr\", or \"both\"." +
                "\n- Return ONLY a valid JSON array. No markdown fences, no commentary." +
                "\n- Each element must have exactly: term, definition, timestamp_seconds, source.";

            var userContent = new StringBuilder();
            userContent.AppendLine("=== LECTURE TRANSCRIPT (timestamps in seconds) ===");
            userContent.AppendLine(captionText);
            if (!string.IsNullOrWhiteSpace(ocrText)) { userContent.AppendLine("\n=== SLIDE / IMAGE TEXT (OCR) ==="); userContent.AppendLine(ocrText); }

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var llmBody = new { model = Globals.appSettings.OPENAI_MODEL, messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userContent.ToString() } }, temperature = 0.1 };
            var httpContent = new StringContent(JsonConvert.SerializeObject(llmBody), Encoding.UTF8, "application/json");
            var response = await http.PostAsync(Globals.appSettings.OPENAI_API_ENDPOINT, httpContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) { _logger.LogError($"ExtractGlossary({videoId}): {(int)response.StatusCode} {responseBody}"); return StatusCode(502, $"LLM API error: {response.StatusCode}"); }

            var rawContent = JObject.Parse(responseBody)["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim() ?? "[]";
            JArray terms;
            try { terms = JArray.Parse(rawContent); }
            catch (JsonException) { return StatusCode(502, "LLM returned non-JSON response"); }

            var (glossaryDoc, timestampDoc) = BuildGlossaryDocs(terms, video.Duration?.TotalSeconds ?? 99999);

            async Task Upsert(string existingId, string text, Action<string> setId)
            {
                if (!string.IsNullOrEmpty(existingId)) { var td = await _context.TextData.FindAsync(existingId); if (td != null) { td.Text = text; return; } }
                var newTd = new TextData { Text = text };
                _context.TextData.Add(newTd);
                await _context.SaveChangesAsync();
                setId(newTd.Id);
            }

            await Upsert(video.GlossaryDataId,      glossaryDoc.ToString(Formatting.None),  id => video.GlossaryDataId      = id);
            await Upsert(video.GlossaryTimestampId, timestampDoc.ToString(Formatting.None), id => video.GlossaryTimestampId = id);
            _context.Update(video);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"ExtractGlossary({videoId}): stored {terms.Count} terms");
            return Ok(new { videoId, termCount = terms.Count });
        }
    }
}
