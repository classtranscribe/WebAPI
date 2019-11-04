using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassTranscribeDatabase;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = Globals.ROLE_ADMIN)]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly CTDbContext _context;

        public AdminController(CTDbContext context)
        {
            _context = context;
        }

        [HttpPost("UpdateAllPlaylists")]
        public ActionResult UpdateAllPlaylists()
        {
            WakeDownloader.UpdateAllPlaylists();
            return Ok();
        }

        [HttpPost("UpdatePlaylist")]
        public ActionResult UpdatePlaylist(string playlistId)
        {
            WakeDownloader.UpdatePlaylist(playlistId);
            return Ok();
        }

        [HttpPost("PeriodicCheck")]
        public ActionResult PeriodicCheck()
        {
            WakeDownloader.PeriodicCheck();
            return Ok();
        }

        [HttpGet("GetLogs")]
        public async Task<IActionResult> GetLogs(DateTime from, DateTime to)
        {
            var logs = await _context.Logs.Where(l => l.CreatedAt >= from && l.CreatedAt <= to).Select(l => new {
                Id = l.Id,
                CreatedAt = l.CreatedAt,
                UserId = l.UserId,
                OfferingId = l.OfferingId,
                MediaId = l.MediaId,
                EventType = l.EventType,
                Json = l.Json
            }).ToListAsync();
            var path = Path.GetTempFileName();
            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(logs);
                }
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "text/csv", Path.GetFileNameWithoutExtension(path) + ".csv");
        }
    }
}