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
            return await GetCaptionsAsync(transcriptionId);
        }

        public async Task<List<Caption>> GetCaptionsAsync(string transcriptionId)
        {
            // This has to be split in two because of https://docs.microsoft.com/en-us/ef/core/querying/client-eval
            var allCaptions = await _context.Captions.Where(c => c.TranscriptionId == transcriptionId).ToListAsync();
            var captions = allCaptions.GroupBy(c => c.Index).Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .OrderBy(c => c.Index).ToList();
            return captions;
        }
    }
}
