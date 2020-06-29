using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{
    /// <summary>
    /// Some commonly used queries to get the captions for a given videoId.
    /// </summary>
    public class CaptionQueries
    {
        private readonly CTDbContext _context;
        public CaptionQueries(CTDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the captions for a given videoId
        /// </summary>
        /// <param name="language">Language of the captions to fetch.</param>
        public async Task<List<Caption>> GetCaptionsAsync(string videoId, string language = "en-US")
        {
            var transcriptionId = _context.Transcriptions.Where(t => t.Language == language && t.VideoId == videoId).First().Id;
            return await GetCaptionsAsync(transcriptionId);
        }

        /// <summary>
        /// Get the captions for a given transcriptionId
        /// </summary>        
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
