using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWordController : BaseController
    {
        private readonly WakeDownloader _wakeDownloader;
        private readonly IAuthorizationService _authorizationService;

        public AWordController(IAuthorizationService authorizationService, WakeDownloader wakeDownloader,
            CTDbContext context, ILogger<AWordController> logger) : base(context, logger)
        {
            _authorizationService = authorizationService;
            _wakeDownloader = wakeDownloader;
        }


        //[HttpGet("aId}")]
        //public async Task<ActionResult<IEnumerable<Adictionary>>> GetAdictionaries(string aId)
        //{
        //    return await _context.Adictionaries.Where(c => c.id == aId).OrderBy(c => c.id).ToListAsync();
        //}

        //[HttpGet("{ID}")]
        //public Adictionary Get(string ID)
        //{
        //    return _context.Adictionaries.FirstOrDefault(e => e.Id == ID);
        //}



        [HttpGet("{Inword}")]
        public async Task<ActionResult<Adictionary>> GetAdictionary(string Inword)
        {
            //Adictionary adictionary = await _context.Adictionaries.FindAsync(Id);

            //if (adictionary == null)
            //{
            //    return NotFound();
            //}

            //return adictionary;

            var adictionaries = await _context .Adictionaries.Where(c => c.Inword == Inword)
            .ToListAsync();
            if (adictionaries == null || adictionaries.Count == 0)
            {
                return NotFound();
            }
            else
            {
                return adictionaries.First();
            }
        }


        //[HttpGet("{inWord}")]
        //public ActionResult<Adictionary> GetWord(string inWord)
        //{

        //    var adictionary = new Adictionary();
        //    adictionary.inWord = inWord;
        //    adictionary.outWord = "out";
        //    if (adictionary == null)
        //    {
        //        return NotFound();
        //    }

        //    return adictionary;
        //}


        /// <summary> 
        /// Enqueue DownloadAllPlaylists task, which updates all playlists for all terms where start date is within 6 months of today.
        /// 
        /// </summary>
        /// <remarks> 
        /// Each playlist update is a separate task. Requesting an update is harmless though
        /// be aware that some external sources (e.g. Youtube) limit API usage.
        /// See QueueAwakerTask.DownloadAllPlaylists, DownloadPlaylistInfoTask for details
        /// This API call is just for the impatient because the PeriodicCheck task also updates 
        /// all playlists and (unlike this API function) also performs a PendingJobs task to kick off transcriptions.
        /// </remarks>

    }
}