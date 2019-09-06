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

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptionsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public CaptionsController(CTDbContext context)
        {
            _context = context;
        }

        // GET: api/Captions/5
        [HttpGet("ByTranscription/{TranscriptionId}")]
        public async Task<ActionResult<IEnumerable<Caption>>> GetCaptions(string TranscriptionId)
        {
            return await _context.Captions.Where(c => c.TranscriptionId == TranscriptionId).GroupBy(c => c.Index).Select(g => g.OrderByDescending(c => c.CreatedAt).First()).OrderBy(c => c.Index).ToListAsync();
        }

        // POST: api/Captions
        [HttpPost]
        [Authorize]
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

        private bool CaptionExists(string id)
        {
            return _context.Captions.Any(e => e.Id == id);
        }
    }
}
