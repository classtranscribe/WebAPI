using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WatchHistoriesController : ControllerBase
    {
        private readonly CTDbContext _context;
        private readonly UserUtils _userUtils;

        public WatchHistoriesController(CTDbContext context, UserUtils userUtils)
        {
            _context = context;
            _userUtils = userUtils;
        }

        // GET: api/WatchHistories/5
        [HttpGet("{mediaId}")]
        public async Task<ActionResult<WatchHistory>> GetWatchHistory(string mediaId)
        {
            var media = await _context.Medias.FindAsync(mediaId);
            if (media == null)
            {
                return BadRequest();
            }
            var user = _userUtils.GetUser(User);
            if (user != null)
            {
                var watchHistory = await _context.WatchHistories
                    .Where(w => w.MediaId == mediaId && w.ApplicationUserId == user.Id)
                    .FirstOrDefaultAsync();

                if (watchHistory == null)
                {
                    return NotFound();
                }
                return watchHistory;

            }
            else
            {
                return Unauthorized();
            }
        }

        // GET: api/WatchHistories/GetAllWatchHistoryForUser
        [HttpGet("GetAllWatchHistoryForUser")]
        public async Task<ActionResult<IEnumerable<WatchHistory>>> GetAllWatchHistoryForUser()
        {
            var user = _userUtils.GetUser(User);
            if (user != null)
            {
                var watchHistories = await _context.WatchHistories
                    .Where(w => w.ApplicationUserId == user.Id)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                return watchHistories;

            }
            else
            {
                return Unauthorized();
            }
        }

        // PUT: api/WatchHistories/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost("{mediaId}")]
        public async Task<IActionResult> PostWatchHistory(string mediaId, JObject json)
        {
            var media = await _context.Medias.FindAsync(mediaId);
            if (media == null || json == null)
            {
                return BadRequest();
            }
            var user = _userUtils.GetUser(User);
            if (user != null)
            {
                var watchHistory = await _context.WatchHistories
                    .Where(w => w.MediaId == mediaId && w.ApplicationUserId == user.Id)
                    .FirstOrDefaultAsync();

                if (watchHistory == null)
                {
                    watchHistory = new WatchHistory
                    {
                        ApplicationUserId = user.Id,
                        MediaId = mediaId,
                        Json = json
                    };
                    await _context.WatchHistories.AddAsync(watchHistory);
                }
                else
                {
                    watchHistory.Json = json;
                    _context.Entry(watchHistory).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();

            }
            else
            {
                return Unauthorized();
            }
            return NoContent();
        }

        [HttpDelete]
        public async Task<ActionResult<WatchHistory>> DeleteWatchHistory(string id)
        {
            var watchHistory = await _context.WatchHistories.FindAsync(id);
            if (watchHistory == null)
            {
                return NotFound();
            }

            _context.WatchHistories.Remove(watchHistory);
            await _context.SaveChangesAsync();

            return watchHistory;
        }

        private bool WatchHistoryExists(string id)
        {
            return _context.WatchHistories.Any(e => e.Id == id);
        }
    }
}
