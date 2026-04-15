using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static ClassTranscribeDatabase.CommonUtils;

#pragma warning disable CA2007

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")]
    class ExtractGlossaryTask : RabbitMQTask<string>
    {
        private static readonly HttpClient _http = new HttpClient();
        private const int MAX_CAPTION_CHARS = 14000;
        private const int MAX_OCR_CHARS = 4000;

        private const string SYSTEM_PROMPT =
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

        public ExtractGlossaryTask(RabbitMQConnection rabbitMQ, ILogger<ExtractGlossaryTask> logger)
            : base(rabbitMQ, TaskType.ExtractGlossaryTerms, logger) { }

        protected async override Task OnConsume(string videoId, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, videoId);
            GetLogger().LogInformation($"ExtractGlossaryTask({videoId}): starting");

            var apiKey = Globals.appSettings.OPENAI_API_KEY;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                GetLogger().LogWarning($"{videoId}: OPENAI_API_KEY not set — skipping");
                return;
            }

            using var _context = CTDbContext.CreateDbContext();
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null) { GetLogger().LogWarning($"{videoId}: video not found"); return; }
            if (video.HasGlossaryData()) { GetLogger().LogInformation($"{videoId}: glossary exists — skipping"); return; }

            var captions = await _context.Captions
                .Where(c => c.Transcription.VideoId == videoId && c.Transcription.TranscriptionType == TranscriptionType.Caption)
                .OrderBy(c => c.Begin).ToListAsync();

            if (captions.Count == 0) { GetLogger().LogInformation($"{videoId}: no captions — skipping"); return; }

            var captionSb = new StringBuilder();
            foreach (var cap in captions) captionSb.AppendLine($"[{cap.Begin.TotalSeconds:F1}s] {cap.Text}");
            var captionText = captionSb.ToString();
            if (captionText.Length > MAX_CAPTION_CHARS) captionText = captionText.Substring(0, MAX_CAPTION_CHARS);

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
                        if (ocrText.Length > MAX_OCR_CHARS) ocrText = ocrText.Substring(0, MAX_OCR_CHARS);
                    }
                }
                catch (Exception ex) { GetLogger().LogWarning(ex, $"{videoId}: OCR parse failed"); }
            }

            var userContent = new StringBuilder();
            userContent.AppendLine("=== LECTURE TRANSCRIPT (timestamps in seconds) ===");
            userContent.AppendLine(captionText);
            if (!string.IsNullOrWhiteSpace(ocrText)) { userContent.AppendLine("\n=== SLIDE / IMAGE TEXT (OCR) ==="); userContent.AppendLine(ocrText); }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var requestBody = new { model = Globals.appSettings.OPENAI_MODEL, messages = new[] { new { role = "system", content = SYSTEM_PROMPT }, new { role = "user", content = userContent.ToString() } }, temperature = 0.1 };
            var httpContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Globals.appSettings.OPENAI_API_ENDPOINT, httpContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) { GetLogger().LogError($"{videoId}: LLM {(int)response.StatusCode}: {responseBody}"); throw new Exception($"LLM API {response.StatusCode}"); }

            var rawContent = JObject.Parse(responseBody)["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim() ?? "[]";
            JArray terms;
            try { terms = JArray.Parse(rawContent); }
            catch (JsonException ex) { GetLogger().LogError(ex, $"{videoId}: invalid JSON from LLM"); throw; }

            var (glossaryDoc, timestampDoc) = BuildGlossaryDocs(terms, video.Duration?.TotalSeconds ?? 99999);

            var gTd = new TextData { Text = glossaryDoc.ToString(Formatting.None) };
            _context.TextData.Add(gTd);
            await _context.SaveChangesAsync();
            video.GlossaryDataId = gTd.Id;

            var tTd = new TextData { Text = timestampDoc.ToString(Formatting.None) };
            _context.TextData.Add(tTd);
            await _context.SaveChangesAsync();
            video.GlossaryTimestampId = tTd.Id;

            _context.Update(video);
            await _context.SaveChangesAsync();
            GetLogger().LogInformation($"{videoId}: stored {terms.Count} terms");
        }

        internal static (JObject glossaryDoc, JObject timestampDoc) BuildGlossaryDocs(JArray terms, double videoDuration)
        {
            var glossaryArray = new JArray();
            var timestampDict = new JObject();
            for (int i = 0; i < terms.Count; i++)
            {
                var t     = terms[i];
                string tm = t["term"]?.ToString() ?? "";
                string df = t["definition"]?.ToString() ?? "";
                string sr = t["source"]?.ToString() ?? "transcript";
                double st = t["timestamp_seconds"]?.Value<double>() ?? 0;
                double nx = (i + 1 < terms.Count) ? (terms[i + 1]["timestamp_seconds"]?.Value<double>() ?? st + 90) : st + 90;
                double en = Math.Min(nx, videoDuration);
                if (string.IsNullOrWhiteSpace(tm)) continue;
                glossaryArray.Add(new JArray(tm, df, "", sr, "", "", ""));
                timestampDict[tm] = new JArray(TimeSpan.FromSeconds(st).ToString(@"hh\:mm\:ss"), TimeSpan.FromSeconds(en).ToString(@"hh\:mm\:ss"));
            }
            return (new JObject { ["Glossary"] = glossaryArray }, new JObject { ["glossaryTimestamp"] = timestampDict });
        }
    }
}
