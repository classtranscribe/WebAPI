using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubtitlesParser.Classes.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly CaptionQueries _captionQueries;
        private readonly SubParser parser = new SubParser();

        public CaptionsController(WakeDownloader wakeDownloader,
            CTDbContext context,
            CaptionQueries captionQueries,
            ILogger<CaptionsController> logger) : base(context, logger)
        {
            _captionQueries = captionQueries;
            _wakeDownloader = wakeDownloader;
        }

        // GET: api/Captions/ByTranscription/5
        [HttpGet("ByTranscription/{TranscriptionId}")]
        public async Task<ActionResult<IEnumerable<Caption>>> GetCaptions(string TranscriptionId)
        {
            return await _captionQueries.GetCaptionsAsync(TranscriptionId);
        }

        // GET: api/TranscriptionFile/srt/123
        [HttpGet("TranscriptionFile/{TranscriptionId}/{Format}")]
        public async Task<ActionResult<string>> GetTranscriptionFile(string TranscriptionId,string Format)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var transcription = await _context.Transcriptions.FindAsync(TranscriptionId);
                if (transcription == null)
                {
                    return NotFound();
                }
                var captionQueries = new CaptionQueries(_context);
            
                var captions = await captionQueries.GetCaptionsAsync(TranscriptionId);
                
                switch(Format) {
                    case "vtt":
                        return Caption.GenerateWebVTTString(captions, transcription.Language);
                    case "srt":
                        return Caption.GenerateSrtString(captions);
                    case "txt":
                        return Caption.GenerateParagraphsString(captions);
                    default:
                        return BadRequest("Invalid format");
                }
            }
        }

        // GET: api/Captions
        [HttpGet]
        public async Task<ActionResult<Caption>> GetCaption(string transcriptionId, int index)
        {
            var captions = await _context.Captions.Where(c => c.TranscriptionId == transcriptionId && c.Index == index)
                .OrderByDescending(c => c.CreatedAt).ToListAsync();
            if (captions == null || captions.Count == 0)
            {
                return NotFound();
            }
            else
            {
                return captions.First();
            }
        }

        // POST: api/Captions
        [HttpPost]
        public async Task<ActionResult<Caption>> PostCaption(Caption modifiedCaption)
        {
            if (modifiedCaption == null || modifiedCaption.Id == null)
            {
                return BadRequest("modifiedCaption.Id not present");
            }
            Caption oldCaption = await _context.Captions.FindAsync(modifiedCaption.Id);
            if (oldCaption == null)
            {
                return NotFound();
            }
            Caption newCaption = new Caption
            {
                Begin = oldCaption.Begin,
                End = oldCaption.End,
                Index = oldCaption.Index,
                CaptionType = oldCaption.CaptionType,
                Text = modifiedCaption.Text,
                TranscriptionId = oldCaption.TranscriptionId
            };
            _context.Captions.Add(newCaption);
            await _context.SaveChangesAsync();
            // nope _wakeDownloader.UpdateVTTFile(oldCaption.TranscriptionId);
            return newCaption;
        }

        // POST: api/Captions/UpVote
        [HttpPost("UpVote")]
        public async Task<ActionResult<Caption>> UpVote(string id)
        {
            var caption = await _context.Captions.FindAsync(id);

            if (caption == null)
            {
                return NotFound();
            }

            caption.UpVote++;
            await _context.SaveChangesAsync();
            return caption;
        }

        // POST: api/Captions/DownVote
        [HttpPost("DownVote")]
        public async Task<ActionResult<Caption>> DownVote(string id)
        {
            var caption = await _context.Captions.FindAsync(id);

            if (caption == null)
            {
                return NotFound();
            }

            caption.DownVote++;
            await _context.SaveChangesAsync();
            return caption;
        }

        // POST: api/Captions/CancelUpVote
        [HttpPost("CancelUpVote")]
        public async Task<ActionResult<Caption>> CancelUpVote(string id)
        {
            var caption = await _context.Captions.FindAsync(id);

            if (caption == null)
            {
                return NotFound();
            }

            caption.UpVote--;
            await _context.SaveChangesAsync();
            return caption;
        }

        // POST: api/Captions/CancelDownVote
        [HttpPost("CancelDownVote")]
        public async Task<ActionResult<Caption>> CancelDownVote(string id)
        {
            var caption = await _context.Captions.FindAsync(id);

            if (caption == null)
            {
                return NotFound();
            }

            caption.DownVote--;
            await _context.SaveChangesAsync();
            return caption;
        }
        //  CT production PG 2021/2/5
        // SELECT cfgname FROM pg_ts_config
        //" simple,danish,dutch,english,finnish,french,german,hungarian,italian,norwegian,portuguese,romanian,russian,spanish,swedish,turkish";
        // Language codes from 
        // http://www.lingoes.net/en/translator/langcode.htm

        private static Dictionary<string, string> pgLanguageMap = InitializePgLanguageMap();

        static Dictionary<string, string> InitializePgLanguageMap()
        {
            var languageMap = new Dictionary<string, string>();
            var supportedLanguages = "da:danish,nl:dutch,en:english,fi:finnish,fr:french,de:german,hu:hungarian,it:italian,nb:norwegian," +
                "pt:portuguese,ro:romanian,ru:russian,es:spanish,sv:swedish,tr:turkish";

            foreach (var keyvalue in supportedLanguages.Split(","))
            {
                var pair = keyvalue.Split(":");
                languageMap.Add(pair[0], pair[1]);
            }

            return languageMap;
        }

        /// <summary>
        /// Returns Postgres language word from Language code.
        /// e.g., returns "english" for "en-US"
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Lowercase strings used for ISO Language Codes")]
        public static string toPGLanguage(string code)
        {
            return code != null && code.Length >= 2 ? pgLanguageMap.GetValueOrDefault(code.Substring(0, 2).ToLowerInvariant(), "simple") : "simple";
        }

        // GET: api/Captions/SearchInOffering
        [HttpGet("SearchInOffering")]
        public async Task<ActionResult<IEnumerable<SearchedCaptionDTO>>> SearchInOffering(string offeringId, string query, string filterLanguage = "en-US")
        {
            var allVideos = await _context.Medias.Where(m => m.Playlist.OfferingId == offeringId)
                .Select(m => new { m.VideoId, m.Video, MediaId = m.Id, m.PlaylistId, PlaylistName = m.Playlist.Name, MediaName = m.Name }).ToListAsync();

            var allOfferingCaptions = _context.Medias.Where(m => m.Playlist.OfferingId == offeringId)
                .Select(m => m.Video).SelectMany(v => v.Transcriptions)
                    .Where(t => (string.IsNullOrWhiteSpace(filterLanguage) || t.Language == filterLanguage))
                    .SelectMany(t => t.Captions);

            // ToTsVector is not implemented for the in-memory database used for testing
            // TSVector is a Postgres text search function
            // see https://www.postgresql.org/docs/9.1/textsearch-controls.html
            // Todo: We should set suggested langugae
            // The language setting ignores the most common words (a the etc) and provides rules for reducing words
            // to their simplified form (e.g. plural to singular); so this code is non-optimal for non-English captions

            // It would be useful to do this per transcription language
            // e.g. toPGLanguage( c.Transcription.Language) - but Entity Framework cant inline toPGLanguage and assemble it into SQL
            // So when all languages are included, the current implementation below is biased towards English.
            // A more comprehensive solution might iterate through all languages
            var pgLanguage = toPGLanguage(filterLanguage == null || filterLanguage.Length == 0 ? "en" : filterLanguage);

            var matchingCaptions = _context.Database.IsNpgsql() ?
                    allOfferingCaptions.Where(c => EF.Functions.ToTsVector(pgLanguage, c.Text).Matches(query))
                    : allOfferingCaptions.Where(c => c.Text.Contains(query, System.StringComparison.OrdinalIgnoreCase));

            var result = await matchingCaptions.Take(100).Select(c => new SearchedCaptionDTO
            {
                Caption = c,
                VideoId = c.Transcription.VideoId,
                Language = c.Transcription.Language
            }).ToListAsync();

            // Stitch the two.
            result.ForEach(c =>
            {
                var v = allVideos.Where(v => v.VideoId == c.VideoId).First();
                c.Caption.Transcription = null;
                c.MediaId = v.MediaId;
                c.PlaylistId = v.PlaylistId;
                c.MediaName = v.MediaName;
                c.PlaylistName = v.PlaylistName;
            });

            if(result.Count == 0 && filterLanguage != null && filterLanguage.Length > 0)
            {
                // repeat but with no language restriction
                return await SearchInOffering(offeringId, query,"");
            }

            return result;
        }

        // POST: api/Captions/Upload
        [DisableRequestSizeLimit]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_TEACHING_ASSISTANT + "," + Globals.ROLE_INSTRUCTOR)]
        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<IEnumerable<Caption>>> PostCaptionFile(IFormFile captionFile, [FromForm] string videoId, [FromForm] string language)
        {
            if (videoId == null || language == null || captionFile == null || captionFile.Length <= 0)
            {
                return BadRequest("All of the following parameters are required: 'captionFile', 'videoId', 'language'");
            }

            var allowedLangs = new string[]
            {
                CommonUtils.Languages.ENGLISH_AMERICAN,
                CommonUtils.Languages.SIMPLIFIED_CHINESE,
                CommonUtils.Languages.KOREAN,
                CommonUtils.Languages.SPANISH,
                CommonUtils.Languages.FRENCH
            };

            if (!allowedLangs.Contains(language))
            {
                return BadRequest($"Language not permitted, only the following language codes are accepted: {string.Join(", ", allowedLangs)}");
            }

            var ext = Path.GetExtension(captionFile.FileName).ToLower(System.Globalization.CultureInfo.CurrentCulture);
            var allowedExtensions = new string[] { ".vtt",  ".srt", ".sub", ".ssa", ".ttml" };

            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest($"File format not permitted, only the following formats are accepted: {string.Join(", ", allowedExtensions)}");
            }

            var video = await _context.Videos.FindAsync(videoId);

            if (video == null)
            {
                return NotFound($"Video with ID {videoId} not found");
            }

            using var fileStream = captionFile.OpenReadStream();
            var mostLikelyFormat = parser.GetMostLikelyFormat(captionFile.FileName);
            var items = parser.ParseStream(fileStream, Encoding.UTF8, mostLikelyFormat);

            if (!items.Any())
            {
                return BadRequest("No captions found in the file");
            }

            var captions = items.Select((item, idx) => new Caption
            {
                Begin = new TimeSpan(item.StartTime * TimeSpan.TicksPerMillisecond),
                End = new TimeSpan(item.EndTime * TimeSpan.TicksPerMillisecond),
                Text = string.Join("\n", item.Lines),
                Index = idx + 1,
            }).ToList();

            var transcription = new Transcription
            {
                VideoId = videoId,
                Captions = captions,
                Language = language,
                Label = language,
                SourceInternalRef = "ClassTranscribe/upload"
            };

            await _context.Transcriptions.AddAsync(transcription);
            await _context.Captions.AddRangeAsync(captions);
            await _context.SaveChangesAsync();

            //nope _wakeDownloader.UpdateVTTFile(transcription.Id);

            return captions;
        }

        public class SearchedCaptionDTO
        {
            public Caption Caption { get; set; }
            public string MediaId { get; set; }
            public string PlaylistId { get; set; }
            public string VideoId { get; set; }
            public string MediaName { get; set; }
            public string PlaylistName { get; set; }
            public string Language { get; set; }
        }
    }
}
