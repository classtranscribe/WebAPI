using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly CaptionQueries _captionQueries;

        public CaptionsController(WakeDownloader wakeDownloader,
            CTDbContext context,
            CaptionQueries captionQueries,
            ILogger<CaptionsController> logger) : base(context, logger)
        {
            _captionQueries = captionQueries;
            _wakeDownloader = wakeDownloader;
        }

        // GET: api/Captions/5
        [HttpGet("ByTranscription/{TranscriptionId}")]
        public async Task<ActionResult<IEnumerable<Caption>>> GetCaptions(string TranscriptionId)
        {
            return await _captionQueries.GetCaptionsAsync(TranscriptionId);
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
                Text = modifiedCaption.Text,
                TranscriptionId = oldCaption.TranscriptionId
            };
            _context.Captions.Add(newCaption);
            await _context.SaveChangesAsync();
            _wakeDownloader.UpdateVTTFile(oldCaption.TranscriptionId);
            return newCaption;
        }

        // POST: api/Captions
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

        // POST: api/Captions
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

        // POST: api/Captions
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

        // POST: api/Captions
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

        private static Dictionary<string, string> pgLanguageMap;
        static CaptionsController()
        {
            pgLanguageMap = new Dictionary<string, string>();
            var supportedLanguages = "da:danish,nl:dutch,en:english,fi:finnish,fr:french,de:german,hu:hungarian,it:italian,nb:norwegian," +
                "pt:portuguese,ro:romanian,ru:russian,es:spanish,sv:swedish,tr:turkish";
            foreach (var keyvalue in supportedLanguages.Split(","))
            {
                var pair = keyvalue.Split(":");
                pgLanguageMap.Add(pair[0], pair[1]);
            }
        }

        /// <summary>
        /// Returns Postgres language word from Language code.
        /// e.g., returns "english" for "en-US"
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string toPGLanguage(string code)
        {
            return code.Length >= 2 ? pgLanguageMap.GetValueOrDefault(code.Substring(0, 2).ToLowerInvariant(), "simple") : "simple";
        }

        // POST: api/Captions
        [HttpGet("SearchInOffering")]
        public async Task<ActionResult<IEnumerable<SearchedCaptionDTO>>> SearchInOffering(string offeringId, string query, string filterLanguage = "")
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
            var matchingCaptions = _context.Database.IsNpgsql() ?
                    allOfferingCaptions.Where(c => EF.Functions.ToTsVector(toPGLanguage(c.Transcription.Language), c.Text).Matches(query))
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

            return result;
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

        private bool CaptionExists(string id)
        {
            return _context.Captions.Any(e => e.Id == id);
        }
    }
}
