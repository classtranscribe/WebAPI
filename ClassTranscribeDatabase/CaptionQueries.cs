using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{
    public class CaptionQueries
    {
        private readonly CTDbContext _context;
        public CaptionQueries(CTDbContext context)
        {
            _context = context;
        }

        public async Task<List<Caption>> GetCaptionsAsync(string videoId, string language = "en-US")
        {
            var transcriptionId = _context.Transcriptions.Where(t => t.Language == language && t.VideoId == videoId).First().Id;

            var captions = await _context.Captions.Where(c => c.TranscriptionId == transcriptionId)
                .GroupBy(c => c.Index).Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .OrderBy(c => c.Index).ToListAsync();
            return captions;
        }
    }
}
