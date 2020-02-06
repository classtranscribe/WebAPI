using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;

        public CaptionsController(WakeDownloader wakeDownloader, CTDbContext context, ILogger<CaptionsController> logger) : base(context, logger)
        {
            _wakeDownloader = wakeDownloader;
        }

        // GET: api/Captions/5
        [HttpGet("ByTranscription/{TranscriptionId}")]
        public async Task<ActionResult<IEnumerable<Caption>>> GetCaptions(string TranscriptionId)
        {
            return await new CaptionQueries(_context).GetCaptionsAsync(TranscriptionId);
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

        // POST: api/Captions
        [HttpGet("SearchInOffering")]
        public async Task<ActionResult<IEnumerable<SearchedCaptionDTO>>> SearchInOffering(string offeringId, string query)
        {
            

            var allVideos = await _context.Medias.Where(m => m.Playlist.OfferingId == offeringId)
                .Select(m => new { VideoId = m.VideoId, Video = m.Video, MediaId = m.Id, PlaylistId = m.PlaylistId }).ToListAsync();

            var captions = await _context.Medias.Where(m => m.Playlist.OfferingId == offeringId)
                .Select(m => m.Video).SelectMany(v => v.Transcriptions)
                    .SelectMany(t => t.Captions)
                    .Where(c => EF.Functions.ToTsVector("english", c.Text).Matches(query))
                    .Take(100).Select(c => new SearchedCaptionDTO
                    {
                        Caption = c,
                        VideoId = c.Transcription.VideoId
                    }).ToListAsync();

            // Stitch the two.

            captions.ForEach(c =>
            {
                c.MediaId = allVideos.Where(v => v.VideoId == c.VideoId).Select(v => v.MediaId).First();
                c.PlaylistId = allVideos.Where(v => v.VideoId == c.VideoId).Select(v => v.PlaylistId).First();
            });

            return captions;
        }

        public class SearchedCaptionDTO
        {
            public Caption Caption { get; set; }
            public string MediaId { get; set; }
            public string PlaylistId { get; set; }
            public string VideoId { get; set; }
        }

        private bool CaptionExists(string id)
        {
            return _context.Captions.Any(e => e.Id == id);
        }
    }
}
