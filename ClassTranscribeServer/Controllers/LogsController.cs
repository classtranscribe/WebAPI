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

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly CTDbContext _context;

        public LogsController(CTDbContext context)
        {
            _context = context;
        }

        // POST: api/Logs
        [HttpPost]
        public async Task<ActionResult> PostLog(Log log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpGet("StudentLogs")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<StudentLog>> GetStudentLogs(string mailId, string eventType,
            DateTime? start = null, DateTime? end = null)
        {
            var userId = await _context.Users.Where(u => u.Email == mailId).Select(x => x.Id).FirstOrDefaultAsync();
            DateTime startTime = start ?? DateTime.Now.AddMonths(-1);
            DateTime endTime = end ?? DateTime.Now;
            var timeUpdateEvents = await _context.Logs.Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime && l.UserId == userId && l.EventType == eventType)
                .Select(l => new
                {
                    UserId = l.UserId,
                    OfferingId = l.OfferingId,
                    MediaId = l.MediaId,
                    CreatedAt = l.CreatedAt
                }).ToListAsync();

            IEnumerable<StudentLog> logs;
            if (start == null)
            {
                logs = timeUpdateEvents.GroupBy(x => x.OfferingId).Select(g => new StudentLog
                {
                    OfferingId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        LastHr = l.Where(m => m.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                        Last3days = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                        LastWeek = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                        LastMonth = l.Count(),
                    }).ToList()
                });
            }
            else
            {
                logs = timeUpdateEvents.GroupBy(x => x.OfferingId).Select(g => new StudentLog
                {
                    OfferingId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        Count = l.Count(),
                    }).ToList()
                });
            }

            return logs;
        }

        [HttpGet("CourseLogs")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<CourseLog>> GetCourseLogs(string offeringId, string eventType,
            DateTime? start = null, DateTime? end = null)
        {
            DateTime startTime = start ?? DateTime.Now.AddMonths(-1);
            DateTime endTime = end ?? DateTime.Now;
            var timeUpdateEvents = await _context.Logs
                .Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime && l.OfferingId == offeringId && l.EventType == eventType)
                .Select(l => new
                {
                    UserId = l.UserId,
                    OfferingId = l.OfferingId,
                    MediaId = l.MediaId,
                    CreatedAt = l.CreatedAt
                }).ToListAsync();

            IEnumerable<CourseLog> logs;
            if (start == null)
            {
                logs = timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
                {
                    UserId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        LastHr = l.Where(m => m.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                        Last3days = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                        LastWeek = l.Where(m => m.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                        LastMonth = l.Count(),
                    }).ToList()
                });
            }
            else
            {
                logs = timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
                {
                    UserId = g.Key,
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        Count = l.Count(),
                    }).ToList()
                });
            }                

            return logs;
        }

        [HttpGet("EventTypes")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetEventTypes()
        {
            return await _context.Logs.Select(l => l.EventType).Distinct().ToListAsync();
        }

        [HttpGet("UserIds")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetUserIds()
        {
            return await _context.Users.Select(u => u.Email).Distinct().ToListAsync();
        }

        public class CourseLog
        {
            public string UserId { get; set; }
            public List<MediaLog> Medias { get; set; }
        }


        public class StudentLog
        {
            public string OfferingId { get; set; }
            public List<MediaLog> Medias { get; set; }
        }

        public class MediaLog
        {
            public string MediaId { get; set; }
            public int LastHr { get; set; }
            public int Last3days { get; set; }
            public int LastWeek { get; set; }
            public int LastMonth { get; set; }
            public int Count { get; set; }
        }
    }
}
