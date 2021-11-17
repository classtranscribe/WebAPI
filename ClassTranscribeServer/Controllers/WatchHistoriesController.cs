﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
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
            var user = await _userUtils.GetUser(User);
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

        // GET: api/WatchHistories/GetAllWatchedMediaForUser
        [HttpGet("GetAllWatchedMediaForUser")]
        public async Task<ActionResult<IEnumerable<MediaDTO>>> GetAllWatchHistoryForUser()
        {
            var user = await _userUtils.GetUser(User);
            if (user != null)
            {
                return _context.WatchHistories
                    .Where(w => w.ApplicationUserId == user.Id && w.Media.Id != null)
                    .AsEnumerable()
                    .GroupBy(w => w.MediaId)
                    .Select(g => g.OrderByDescending(w => w.LastUpdatedAt).FirstOrDefault())
                    .Select(w => new MediaDTO
                    {
                        Id = w.Media.Id,
                        Name = w.Media.Name,
                        PlaylistId = w.Media.PlaylistId,
                        CreatedAt = w.Media.CreatedAt,
                        JsonMetadata = w.Media.JsonMetadata,
                        SourceType = w.Media.SourceType,
                        PublishStatus = w.Media.PublishStatus,
                        Duration = w.Media.Video != null ? w.Media.Video.Duration : null,
                        WatchHistory = w
                    })
                    .OrderByDescending(m => m.WatchHistory.LastUpdatedAt)
                    .ToList();
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
            var user = await _userUtils.GetUser(User);
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
    }
}