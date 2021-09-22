using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; // CancellationToken


namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : BaseController
    {
        private readonly int DB_LONGTIMEOUT_SECONDS = 60*30; /* 30 minutes. Default of 30seconds is insufficient to download all of the logs */
    
        private readonly IAuthorizationService _authorizationService;
        private readonly UserUtils _userUtils;

        public LogsController(IAuthorizationService authorizationService, CTDbContext context, UserUtils userUtils, ILogger<LogsController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _userUtils = userUtils;
        }

        // POST: api/Logs
        [HttpPost]
        public async Task<ActionResult> PostLog(Log log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Get Search Terms for a given offeringId
        /// Only an instructor of a course can view this.
        /// </summary>
        [HttpGet("OfferingSearchHistory")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SearchDTO>>> GetSearchLogs(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            var temp = await _context.Logs.Where(l => l.OfferingId == offeringId && l.EventType == "filtertrans").ToListAsync();
            return temp.GroupBy(l => l.Json["value"].ToString())
            .Select(g => new SearchDTO
            {
                Term = g.Key,
                Count = g.Count()
            }).OrderByDescending(l => l.Count).ToList();
        }

        /// <summary>
        /// Get Search Terms for a given offeringId for a given student
        /// Only the logged in user's search history will be returned
        /// </summary>
        [HttpGet("UserSearchHistory")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SearchDTO>>> UserSearchHistory(string offeringId)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);
            if (offering == null)
            {
                return BadRequest();
            }
            // Get the user
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, offering, Globals.POLICY_READ_OFFERING);
            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }
            // Get the user
            var user = await _userUtils.GetUser(User);
            var temp = await _context.Logs.Where(l => l.OfferingId == offeringId && l.UserId == user.Id && l.EventType == "filtertrans").ToListAsync();
            return temp.GroupBy(l => l.Json["value"].ToString())
            .Select(g => new SearchDTO
            {
                Term = g.Key,
                Count = g.Count()
            }).OrderByDescending(l => l.Count).ToList();
        }

        /// <summary>
        /// Gets all logs for the logged in user.
        /// </summary>
        [HttpGet("UserLogs")]
        [Authorize]
        public async Task<IEnumerable<Log>> GetUserLogs()
        {
            var user = await _userUtils.GetUser(User);
            if (user != null)
            {
                return await _context.Logs.Where(u => u.UserId == user.Id).ToListAsync();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets event count for a particular event type
        /// start and end are optional parameters
        /// Logs returned only for the logged in user
        /// </summary>
        [HttpGet("UserLogs/ByEvent")]
        [Authorize]
        public async Task<IEnumerable<StudentLog>> GetUserLogsByEvent(string eventType, DateTime? start = null, DateTime? end = null)
        {
            var user = await _userUtils.GetUser(User);
            if (user == null)
            {
                return null;
            }

            DateTime startTime = start ?? DateTime.Now.AddMonths(-1);
            DateTime endTime = end ?? DateTime.Now;
            var timeUpdateEvents = await _context.Logs.Where(l => l.CreatedAt >= startTime && l.CreatedAt <= endTime && l.UserId == user.Id && l.EventType == eventType)
                .Select(l => new
                {
                    l.UserId,
                    l.OfferingId,
                    l.MediaId,
                    l.CreatedAt
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

        /// <summary>
        /// Gets event count for a particular event type for a particular offeringId
        /// start and end are optional parameters
        /// Logs returned only if the logged in user is an instructor for the given offeringId
        /// </summary>
        [HttpGet("CourseLogs")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseLog>>> GetCourseLogs(string offeringId, string eventType,
            DateTime? start = null, DateTime? end = null)
        {
            var offering = await _context.Offerings.FindAsync(offeringId);

            if (offering == null)
            {
                return BadRequest();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);

            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }

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
                    User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Id = u.Id
                    }).FirstOrDefault(),
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
                    User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Id = u.Id
                    }).FirstOrDefault(),
                    Medias = g.GroupBy(k => k.MediaId).Select(l => new MediaLog
                    {
                        MediaId = l.Key,
                        Count = l.Count(),
                    }).ToList()
                });
            }

            return Ok(logs);
        }

        /// <summary>
        /// Gets all course logs for an offering
        /// Relevant issue: https://github.com/classtranscribe/WebAPI/issues/50
        /// eventType is an optional parameter (defaults to "timeupdate")
        /// Logs returned only if the logged in user is an instructor for the given offeringId
        /// </summary>
        [HttpGet("AllCourseLogs")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CourseLog>>> GetAllCourseLogs(string offeringId, string eventType = "timeupdate")
        {
            var offering = await _context.Offerings.FindAsync(offeringId);

            if (offering == null)
            {
                return BadRequest();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(this.User, offering, Globals.POLICY_UPDATE_OFFERING);

            if (!authorizationResult.Succeeded)
            {
                if (User.Identity.IsAuthenticated)
                {
                    return new ForbidResult();
                }

                return new ChallengeResult();
            }

            var medias = await _context.Medias
                .Where(m => m.Playlist.OfferingId == offeringId)
                .ToListAsync();

            var timeUpdateEvents = await _context.Logs
                .Where(l => l.OfferingId == offeringId && l.EventType == eventType)
                .ToListAsync();

            return timeUpdateEvents.GroupBy(x => x.UserId).Select(g => new CourseLog
            {
                User = _context.Users.Where(u => u.Id == g.Key).Select(u => new UserDetails
                {
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Id = u.Id
                }).FirstOrDefault(),
                Medias = medias.OrderBy(m => m.Index).Select(m => new MediaLog
                {
                    MediaId = m.Id,
                    MediaName = m.Name,
                    LastHr = g.Where(l => l.MediaId == m.Id && l.CreatedAt >= DateTime.Now.AddHours(-1)).Count(),
                    Last3days = g.Where(l => l.MediaId == m.Id && l.CreatedAt >= DateTime.Now.AddDays(-3)).Count(),
                    LastWeek = g.Where(l => l.MediaId == m.Id && l.CreatedAt >= DateTime.Now.AddDays(-7)).Count(),
                    LastMonth = g.Where(l => l.MediaId == m.Id && l.CreatedAt >= DateTime.Now.AddMonths(-1)).Count(),
                    Total = g.Where(l => l.MediaId == m.Id).Count(),
                    Duration = m.Video?.Duration,
                }).ToList()
            }).ToList();
        }

        /// <summary>
        /// Gets different kinds of events
        /// Only ADMINS and ADVISORS are authorized
        /// </summary>
        [HttpGet("EventTypes")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetEventTypes()
        {
            return await _context.Logs.Select(l => l.EventType).Distinct().ToListAsync();
        }

        /// <summary>
        /// Return the raw log table
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpGet("GetAllCourseLogsByDateRange")]
        [Authorize(Roles = Globals.ROLE_ADMIN)]
        // Example Test: curl --insecure "https://localhost/api/Logs/GetAllCourseLogsByDateRange?from=1/1/2020&to=2/2/2020" -H "Authorization: Bearer ..."
        public async  Task<IActionResult> GetAllCourseLogsByDateRange(DateTime from, DateTime to)
        {
            DateTime startDump = DateTime.Now;
            HttpContext.Response.StatusCode = 200;
            var headers = HttpContext.Response.Headers;
            headers["Content-Disposition"] = $"attachment; filename=logs-{startDump.ToString("yyyyMMddTHHmmss")}.tsv";
            headers["Content-Type"] = "text/tab-separated-values; charset=utf-8";
            headers["Cache-Control"] = "no-cache";
            CancellationToken cancellationToken = HttpContext.RequestAborted;
            await HttpContext.Response.StartAsync(cancellationToken);

            _logger.LogInformation($"GetAllCourseLogsByDateRange({from.ToString("yyyyMMddTHHmmss")},{to.ToString("yyyyMMddTHHmmss")})");
            // I doubt we're close to optimal performance (e.g. it is "awaiting" every line, and we're not re-using byte[] objects)
            // But we're must avoid loading the entire set of events into memory
            // There's lots of bad example code on the interwebs e.g. examples that load the entire result into memory
            // This pipe-based version (BodyWriter) appears to be appropriate
            // On my laptop a complete table scan with no matching results takes 90s
            // a curl request in a local CMD window returns 143K events in 300s; and seems to be (correctly limited) by the client's download speed.
            // Local curl test: For all 1,328,947 events from 2019.  Processing time: 308.5116288 seconds. 4307 emitted events per second.
            // Note there can be a long delay (e.g. 40s) before the first event is written
            // See also-
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/request-response?view=aspnetcore-5.0
            // https://nodogmablog.bryanhogan.net/2019/06/streaming-results-from-entity-framework-core-and-web-api-core/
            // Turn off tracking, Do not materialize
            
            using (var localcontext = CTDbContext.CreateDbContext())
            {
                localcontext.Database.SetCommandTimeout(DB_LONGTIMEOUT_SECONDS); //default 30s is too short to complete
                
                // When developing You can use use this line instead
                //xxx var logs = localcontext.Logs.AsNoTracking().Take(1000).Select(l => 
                var logs = localcontext.Logs.AsNoTracking().Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

                var writer = HttpContext.Response.BodyWriter;
   
                await writer.WriteAsync(
                    System.Text.Encoding.UTF8.GetBytes(
                        "CreatedAt\tUserId\tOfferingId\tMediaId\tEventType\tJson\n"));
 
                int count = 0;
                const int flushEvery = 25000; // Explicitly flushing occasionally may ensure we don't allow buffer to get infinitely large when the client is slow
                DateTime lastLogTime = DateTime.Now;
                foreach (var l in logs) {
                    if(count == 0) {
                        lastLogTime = DateTime.Now;
                        _logger.LogInformation($"Writing first log event after {(lastLogTime - startDump).TotalSeconds} seconds.");
                    }
                    string line = 
                    $"{l.CreatedAt}\t{l.UserId}\t{l.OfferingId}\t{l.MediaId}\t{l.EventType}\t\"{JsonConvert.SerializeObject(l.Json).Replace("\"","\\\"")}\"\n";

                    await writer.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line));

                    if( ((++count) % flushEvery) == 0) {
                        await writer.FlushAsync(cancellationToken);
                        var now = DateTime.Now;
                        _logger.LogInformation($"{count} log events written. Currently {(int)(flushEvery / ((now - lastLogTime).TotalSeconds))} events per second.");  
                        lastLogTime = now;
                    }
                }
                // Is this the best order for FlushAsync & CompleteAsync? It seems to work for us.
                // There's bug discussions about the exact implementation even in 2020
                // https://github.com/dotnet/aspnetcore/issues/12334
                await writer.FlushAsync(cancellationToken);
                _logger.LogInformation($"Flushed all log events. {count} total log event(s) written.");

                _logger.LogInformation("Completing");
                await writer.CompleteAsync(); // No more writes

                
            var duration = (DateTime.Now - startDump).TotalSeconds;
            _logger.LogInformation($"Complete. Total time: {duration} seconds for {count} events. {(int)(count/duration)} events per second. ");

            } // Now we've finished writing we can close the database context
            // Do not attempt to close this before the final flush
            return new EmptyResult(); // already processed
        }

        /// <summary>
        /// Gets unique mailIds
        /// Only ADMINS and ADVISORS are authorized
        /// </summary>
        [HttpGet("UserIds")]
        [Authorize(Roles = Globals.ROLE_ADMIN + "," + Globals.ROLE_ADVISORS)]
        public async Task<IEnumerable<string>> GetUserIds()
        {
            return await _context.Users.Select(u => u.Email).Distinct().ToListAsync();
        }

        public class SearchDTO
        {
            public string Term { get; set; }
            public int Count { get; set; }
        }

        public class UserDetails
        {
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Id { get; set; }
        }

        public class CourseLog
        {
            public UserDetails User { get; set; }
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
            public string MediaName { get; set; }
            public int LastHr { get; set; }
            public int Last3days { get; set; }
            public int LastWeek { get; set; }
            public int LastMonth { get; set; }
            public int Total { get; set; }
            public int Count { get; set; }
            public TimeSpan? Duration { get; set; }
        }
    }
}
